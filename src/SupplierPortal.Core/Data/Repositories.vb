Imports System.Data
Imports SupplierPortal.Core.Domain

Namespace Data

    Public Interface ISupplierRepository
        Function Find(nitBase As String) As Supplier
        Sub UpdatePassword(nitBase As String, newHash As String, mustChange As Boolean)
        Function Payments(nitBase As String, year As Integer) As List(Of Payment)
        Function YearlySummary(nitBase As String) As List(Of PaymentSummary)
    End Interface

    ''' <summary>SQL Server implementation (tables and procedures in /sql).</summary>
    Public NotInheritable Class SqlSupplierRepository
        Implements ISupplierRepository

        Private ReadOnly _db As Db

        Public Sub New(db As Db)
            _db = db
        End Sub

        Public Function Find(nitBase As String) As Supplier Implements ISupplierRepository.Find
            Return _db.Query(
                "SELECT NitBase, Name, Email, PasswordHash, MustChangePassword, IsActive FROM dbo.Suppliers WHERE NitBase = @nit",
                Function(r) New Supplier With {
                    .NitBase = r.GetString(0), .Name = r.GetString(1),
                    .Email = If(r.IsDBNull(2), Nothing, r.GetString(2)),
                    .PasswordHash = r.GetString(3), .MustChangePassword = r.GetBoolean(4), .IsActive = r.GetBoolean(5)},
                DbParam.Text("@nit", nitBase, 15)).FirstOrDefault()
        End Function

        Public Sub UpdatePassword(nitBase As String, newHash As String, mustChange As Boolean) Implements ISupplierRepository.UpdatePassword
            _db.Execute(
                "UPDATE dbo.Suppliers SET PasswordHash = @hash, MustChangePassword = @must, PasswordChangedAt = SYSUTCDATETIME() WHERE NitBase = @nit",
                DbParam.Text("@hash", newHash, 200), DbParam.Bool("@must", mustChange), DbParam.Text("@nit", nitBase, 15))
        End Sub

        Public Function Payments(nitBase As String, year As Integer) As List(Of Payment) Implements ISupplierRepository.Payments
            Return _db.Query(
                "EXEC dbo.usp_SupplierPayments @nit = @nit, @year = @year",
                Function(r) New Payment With {
                    .Id = r.GetInt32(0), .NitBase = r.GetString(1), .PaidOn = r.GetDateTime(2),
                    .InvoiceNumber = r.GetString(3), .GrossAmount = r.GetDecimal(4), .Withholding = r.GetDecimal(5)},
                DbParam.Text("@nit", nitBase, 15), DbParam.Int("@year", year))
        End Function

        Public Function YearlySummary(nitBase As String) As List(Of PaymentSummary) Implements ISupplierRepository.YearlySummary
            Return _db.Query(
                "SELECT YEAR(PaidOn), COUNT(*), SUM(GrossAmount), SUM(Withholding) FROM dbo.Payments WHERE NitBase = @nit GROUP BY YEAR(PaidOn) ORDER BY 1 DESC",
                Function(r) New PaymentSummary With {.Year = r.GetInt32(0), .Payments = r.GetInt32(1), .Gross = r.GetDecimal(2), .Withholding = r.GetDecimal(3)},
                DbParam.Text("@nit", nitBase, 15))
        End Function
    End Class

    ''' <summary>In-memory implementation for tests and for running the site without SQL Server (demo mode).</summary>
    Public NotInheritable Class InMemorySupplierRepository
        Implements ISupplierRepository

        Private ReadOnly _suppliers As New Dictionary(Of String, Supplier)()
        Private ReadOnly _payments As New List(Of Payment)()
        Private ReadOnly _gate As New Object()

        Public Sub Add(supplier As Supplier, ParamArray payments As Payment())
            SyncLock _gate
                _suppliers(supplier.NitBase) = supplier
                _payments.AddRange(payments)
            End SyncLock
        End Sub

        Public Function Find(nitBase As String) As Supplier Implements ISupplierRepository.Find
            SyncLock _gate
                Dim s As Supplier = Nothing
                Return If(_suppliers.TryGetValue(If(nitBase, ""), s), s, Nothing)
            End SyncLock
        End Function

        Public Sub UpdatePassword(nitBase As String, newHash As String, mustChange As Boolean) Implements ISupplierRepository.UpdatePassword
            SyncLock _gate
                Dim s = _suppliers(nitBase)
                s.PasswordHash = newHash
                s.MustChangePassword = mustChange
            End SyncLock
        End Sub

        Public Function Payments(nitBase As String, year As Integer) As List(Of Payment) Implements ISupplierRepository.Payments
            SyncLock _gate
                Return _payments.Where(Function(p) p.NitBase = nitBase AndAlso p.PaidOn.Year = year).OrderByDescending(Function(p) p.PaidOn).ToList()
            End SyncLock
        End Function

        Public Function YearlySummary(nitBase As String) As List(Of PaymentSummary) Implements ISupplierRepository.YearlySummary
            SyncLock _gate
                Return _payments.Where(Function(p) p.NitBase = nitBase).
                    GroupBy(Function(p) p.PaidOn.Year).
                    Select(Function(g) New PaymentSummary With {
                        .Year = g.Key, .Payments = g.Count(), .Gross = g.Sum(Function(p) p.GrossAmount), .Withholding = g.Sum(Function(p) p.Withholding)}).
                    OrderByDescending(Function(s) s.Year).ToList()
            End SyncLock
        End Function
    End Class

End Namespace
