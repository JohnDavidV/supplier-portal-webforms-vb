Imports SupplierPortal.Core
Imports SupplierPortal.Core.Data
Imports SupplierPortal.Core.Domain
Imports SupplierPortal.Core.Security
Imports Xunit

Namespace Global.SupplierPortal.Tests

    Public Class PasswordHasherTests
        Private ReadOnly _hasher As New PasswordHasher(10000)

        <Fact>
        Public Sub Hash_IsSaltedAndVerifiable()
            Dim a = _hasher.Hash("una frase larga")
            Dim b = _hasher.Hash("una frase larga")
            Assert.StartsWith("pbkdf2$10000$", a)
            Assert.NotEqual(a, b)
            Assert.Equal(PasswordCheck.Success, _hasher.Verify(a, "una frase larga"))
            Assert.Equal(PasswordCheck.Failed, _hasher.Verify(a, "otra"))
        End Sub

        <Fact>
        Public Sub LegacySha256_IsAcceptedAndFlaggedForUpgrade()
            Dim stored = "legacy$" & Crypto.Sha256Hex("clave-vieja")
            Assert.Equal(PasswordCheck.SuccessRehashNeeded, _hasher.Verify(stored, "clave-vieja"))
            Assert.Equal(PasswordCheck.Failed, _hasher.Verify(stored, "Clave-vieja"))
        End Sub

        <Fact>
        Public Sub HigherWorkFactor_TriggersUpgrade()
            Dim weak = New PasswordHasher(10000).Hash("frase-secreta")
            Assert.Equal(PasswordCheck.SuccessRehashNeeded, New PasswordHasher(20000).Verify(weak, "frase-secreta"))
        End Sub

        <Theory>
        <InlineData("")>
        <InlineData("plaintext-password")>
        <InlineData("pbkdf2$abc$x$y")>
        <InlineData("pbkdf2$10000$not-base64!$x")>
        <InlineData("legacy$a$b")>
        Public Sub UnknownOrMalformed_FailsClosed(stored As String)
            Assert.Equal(PasswordCheck.Failed, _hasher.Verify(stored, "plaintext-password"))
        End Sub
    End Class

    Public Class NitTests
        <Theory>
        <InlineData("800197268", 4)>
        <InlineData("899999034", 1)>
        <InlineData("860034313", 7)>
        Public Sub CheckDigit_MatchesDian(base As String, dv As Integer)
            Assert.Equal(dv, Nit.CheckDigit(base))
        End Sub

        <Theory>
        <InlineData("800.197.268-4", "800197268")>
        <InlineData(" 800197268 ", "800197268")>
        <InlineData("800 197 268 - 4", "800197268")>
        <InlineData("800.197.268-5", Nothing)>
        <InlineData("12345", Nothing)>
        <InlineData("abc", Nothing)>
        <InlineData("", Nothing)>
        Public Sub NormalizeBase_AcceptsWhatPeopleType(input As String, expected As String)
            Assert.Equal(expected, Nit.NormalizeBase(input))
        End Sub

        <Fact>
        Public Sub Format_UsesColombianGrouping()
            Assert.Equal("800.197.268-4", Nit.Format("800197268"))
        End Sub
    End Class

    Public Class SignedLinkTests
        Private Shared ReadOnly Key As Byte() = Enumerable.Range(0, 32).Select(Function(i) CByte(i)).ToArray()

        <Fact>
        Public Sub ValidLink_Verifies_AndTamperingFails()
            Dim links As New SignedLink(Key)
            Dim t = links.Sign("withholding:2026", "900123456", TimeSpan.FromDays(7))
            Assert.True(links.IsValid("withholding:2026", "900123456", t.Expires, t.Signature))
            Assert.False(links.IsValid("withholding:2025", "900123456", t.Expires, t.Signature))
            Assert.False(links.IsValid("withholding:2026", "830987654", t.Expires, t.Signature))
            Assert.False(links.IsValid("withholding:2026", "900123456", t.Expires + 3600, t.Signature))
            Assert.False(links.IsValid("withholding:2026", "900123456", t.Expires, "%%%"))
            Assert.False(links.IsValid("withholding:2026", "900123456", t.Expires, Nothing))
        End Sub

        <Fact>
        Public Sub Link_Expires()
            Dim now = New DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            Dim links As New SignedLink(Key, Function() now)
            Dim t = links.Sign("withholding:2026", "900123456", TimeSpan.FromMinutes(5))
            now = now.AddMinutes(6)
            Assert.False(links.IsValid("withholding:2026", "900123456", t.Expires, t.Signature))
        End Sub

        <Fact>
        Public Sub ShortKey_IsRejected()
            Assert.Throws(Of ArgumentException)(
                Sub()
                    Dim unused As New SignedLink(New Byte(15) {})
                End Sub)
        End Sub
    End Class

    Public Class AuthServiceTests
        Private ReadOnly _hasher As New PasswordHasher(10000)

        Private Function Build(ParamArray suppliers As Supplier()) As (Auth As AuthService, Repo As InMemorySupplierRepository)
            Dim repo As New InMemorySupplierRepository()
            For Each s In suppliers
                repo.Add(s)
            Next
            Return (New AuthService(repo, _hasher, New LoginThrottle(3)), repo)
        End Function

        <Fact>
        Public Sub Login_AcceptsAnyNitFormat()
            Dim env = Build(New Supplier With {.NitBase = "900123456", .Name = "Andes", .PasswordHash = _hasher.Hash("frase-segura-1")})
            Dim dv = Nit.CheckDigit("900123456")
            Assert.Equal(LoginStatus.Ok, env.Auth.Login($"900.123.456-{dv}", "frase-segura-1").Status)
            Assert.Equal(LoginStatus.Ok, env.Auth.Login("900123456", "frase-segura-1").Status)
        End Sub

        <Fact>
        Public Sub LegacyPassword_IsUpgradedTransparently()
            Dim env = Build(New Supplier With {.NitBase = "830987654", .Name = "Puente", .PasswordHash = "legacy$" & Crypto.Sha256Hex("vieja-clave")})
            Dim r = env.Auth.Login("830987654", "vieja-clave")
            Assert.Equal(LoginStatus.Ok, r.Status)
            Assert.True(r.Upgraded)
            Assert.StartsWith("pbkdf2$", env.Repo.Find("830987654").PasswordHash)
        End Sub

        <Fact>
        Public Sub WrongPasswordUnknownNitAndInactive_LookTheSame()
            Dim env = Build(
                New Supplier With {.NitBase = "900123456", .Name = "Andes", .PasswordHash = _hasher.Hash("frase-segura-1")},
                New Supplier With {.NitBase = "811222333", .Name = "Inactivo", .PasswordHash = _hasher.Hash("frase-segura-2"), .IsActive = False})
            Assert.Equal(LoginStatus.InvalidCredentials, env.Auth.Login("900123456", "mala").Status)
            Assert.Equal(LoginStatus.InvalidCredentials, env.Auth.Login("123456789", "mala").Status)
            Assert.Equal(LoginStatus.InvalidCredentials, env.Auth.Login("811222333", "frase-segura-2").Status)
        End Sub

        <Fact>
        Public Sub RepeatedFailures_LockTheAccount()
            Dim env = Build(New Supplier With {.NitBase = "900123456", .Name = "Andes", .PasswordHash = _hasher.Hash("frase-segura-1")})
            For i = 1 To 3
                env.Auth.Login("900123456", "mala")
            Next
            Assert.Equal(LoginStatus.LockedOut, env.Auth.Login("900.123.456", "frase-segura-1").Status)
        End Sub

        <Fact>
        Public Sub ForcedChange_AndPolicy()
            Dim env = Build(New Supplier With {.NitBase = "900123456", .Name = "Andes", .PasswordHash = _hasher.Hash("temporal-123"), .MustChangePassword = True})
            Assert.Equal(LoginStatus.MustChangePassword, env.Auth.Login("900123456", "temporal-123").Status)
            Assert.Contains("Use al menos 10 caracteres.", env.Auth.ChangePassword("900123456", "temporal-123", "corta"))
            Assert.Contains("La contraseña no puede contener el NIT.", env.Auth.ChangePassword("900123456", "temporal-123", "clave900123456"))
            Assert.Contains("La contraseña actual no es correcta.", env.Auth.ChangePassword("900123456", "otra", "una-frase-buena"))
            Assert.Empty(env.Auth.ChangePassword("900123456", "temporal-123", "una-frase-buena"))
            Assert.Equal(LoginStatus.Ok, env.Auth.Login("900123456", "una-frase-buena").Status)
        End Sub
    End Class

    Public Class RepositoryTests
        <Fact>
        Public Sub YearlySummary_AggregatesPayments()
            Dim repo As New InMemorySupplierRepository()
            repo.Add(New Supplier With {.NitBase = "900123456", .Name = "Andes", .PasswordHash = "x"},
                     New Payment With {.Id = 1, .NitBase = "900123456", .PaidOn = #2026-01-10#, .InvoiceNumber = "FE-1", .GrossAmount = 1000D, .Withholding = 25D},
                     New Payment With {.Id = 2, .NitBase = "900123456", .PaidOn = #2026-02-10#, .InvoiceNumber = "FE-2", .GrossAmount = 2000D, .Withholding = 50D},
                     New Payment With {.Id = 3, .NitBase = "900123456", .PaidOn = #2025-12-10#, .InvoiceNumber = "FE-0", .GrossAmount = 500D, .Withholding = 0D})
            Dim summary = repo.YearlySummary("900123456")
            Assert.Equal(2026, summary(0).Year)
            Assert.Equal(3000D, summary(0).Gross)
            Assert.Equal(75D, summary(0).Withholding)
            Assert.Equal(2925D, repo.Payments("900123456", 2026).Sum(Function(p) p.NetAmount))
        End Sub
    End Class

End Namespace
