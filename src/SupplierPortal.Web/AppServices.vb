Imports System.Configuration
Imports System.Data.SqlClient
Imports SupplierPortal.Core
Imports SupplierPortal.Core.Data
Imports SupplierPortal.Core.Domain
Imports SupplierPortal.Core.Security

''' <summary>
''' Composition root. Secrets and the connection string come from Config\appSettings.config,
''' which is NOT in source control (Web.config only references it). Without a connection
''' string the site starts in demo mode with fictional data.
''' </summary>
Public NotInheritable Class AppServices
    Private Shared _instance As AppServices

    Public ReadOnly Property Repository As ISupplierRepository
    Public ReadOnly Property Auth As AuthService
    Public ReadOnly Property Links As SignedLink
    Public ReadOnly Property DemoMode As Boolean

    Private Sub New()
        Dim settings = ConfigurationManager.AppSettings
        Dim connectionString = settings("Portal.ConnectionString")
        DemoMode = String.IsNullOrWhiteSpace(connectionString)

        If DemoMode Then
            Repository = DemoData.Create()
        Else
            Repository = New SqlSupplierRepository(New Db(Function() New SqlConnection(connectionString)))
        End If

        Dim iterations As Integer
        If Not Integer.TryParse(settings("Security.Pbkdf2Iterations"), iterations) Then iterations = 310000
        Dim hasher As New PasswordHasher(iterations)
        Auth = New AuthService(Repository, hasher, New LoginThrottle())

        Dim keySetting = settings("Security.LinkSigningKey")
        If String.IsNullOrWhiteSpace(keySetting) Then
            If Not DemoMode Then Throw New ConfigurationErrorsException("Security.LinkSigningKey is required (base64, 32+ bytes).")
            Links = New SignedLink(Crypto.RandomBytes(32))   ' demo: links stop working after a restart, which is fine
        Else
            Links = New SignedLink(Convert.FromBase64String(keySetting))
        End If
    End Sub

    Public Shared ReadOnly Property Current As AppServices
        Get
            Return _instance
        End Get
    End Property

    Public Shared Sub Initialize()
        _instance = New AppServices()
    End Sub
End Class

Friend NotInheritable Class DemoData
    Private Sub New()
    End Sub

    Public Shared Function Create() As InMemorySupplierRepository
        Dim repo As New InMemorySupplierRepository()
        Dim hasher As New PasswordHasher(10000)
        Dim rnd As New Random(7)

        repo.Add(New Supplier With {.NitBase = "900123456", .Name = "Suministros Los Andes S.A.S.", .Email = "facturas@andes.example",
                                    .PasswordHash = hasher.Hash("andes-demo-2026")}, MakePayments(rnd, "900123456"))
        ' Migrated from the old portal: unsalted SHA-256, upgraded automatically on first login.
        repo.Add(New Supplier With {.NitBase = "830987654", .Name = "Ferretería El Puente Ltda.",
                                    .PasswordHash = PasswordHasher.LegacyPrefix & "$" & Crypto.Sha256Hex("puente-old-pass")}, MakePayments(rnd, "830987654"))
        Return repo
    End Function

    Private Shared Function MakePayments(rnd As Random, nit As String) As Payment()
        Dim list As New List(Of Payment)()
        Dim id = 1
        For Each yr In {2025, 2026}
            For mon = 1 To If(yr = 2026, 9, 12)
                Dim gross = CDec(rnd.Next(800, 9000)) * 1000D
                list.Add(New Payment With {.Id = id, .NitBase = nit, .PaidOn = New Date(yr, mon, rnd.Next(5, 28)),
                                           .InvoiceNumber = $"FE-{1000 + id}", .GrossAmount = gross,
                                           .Withholding = Math.Round(gross * 0.025D, 0)})
                id += 1
            Next
        Next
        Return list.ToArray()
    End Function
End Class
