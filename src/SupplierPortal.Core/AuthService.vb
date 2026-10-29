Imports SupplierPortal.Core.Data
Imports SupplierPortal.Core.Domain
Imports SupplierPortal.Core.Security

Public Enum LoginStatus
    Ok
    InvalidCredentials
    LockedOut
    MustChangePassword
End Enum

Public Class LoginResult
    Public Property Status As LoginStatus
    Public Property Supplier As Supplier
    Public Property RetryAfter As TimeSpan?
    Public Property Upgraded As Boolean
End Class

''' <summary>Supplier sign-in: NIT + password, lockout, transparent hash upgrade, forced change.</summary>
Public NotInheritable Class AuthService
    Private Shared ReadOnly DummyHash As String = New PasswordHasher(10000).Hash("not-a-real-password")

    Private ReadOnly _repo As ISupplierRepository
    Private ReadOnly _hasher As PasswordHasher
    Private ReadOnly _throttle As LoginThrottle

    Public Sub New(repo As ISupplierRepository, hasher As PasswordHasher, throttle As LoginThrottle)
        _repo = repo
        _hasher = hasher
        _throttle = throttle
    End Sub

    Public Function Login(nitInput As String, password As String) As LoginResult
        Dim nitBase = Nit.NormalizeBase(nitInput)
        Dim key = If(nitBase, If(nitInput, "").Trim())

        Dim locked = _throttle.LockedFor(key)
        If locked.HasValue Then Return New LoginResult With {.Status = LoginStatus.LockedOut, .RetryAfter = locked}

        Dim supplier = If(nitBase Is Nothing, Nothing, _repo.Find(nitBase))
        ' Always run one hash verification so response time does not reveal registered NITs.
        Dim check = _hasher.Verify(If(supplier?.PasswordHash, DummyHash), If(password, ""))
        If supplier Is Nothing OrElse Not supplier.IsActive OrElse check = PasswordCheck.Failed Then
            _throttle.RegisterFailure(key)
            Return New LoginResult With {.Status = LoginStatus.InvalidCredentials}
        End If

        _throttle.RegisterSuccess(key)
        Dim upgraded = False
        If check = PasswordCheck.SuccessRehashNeeded Then
            _repo.UpdatePassword(supplier.NitBase, _hasher.Hash(password), supplier.MustChangePassword)
            upgraded = True
        End If
        Return New LoginResult With {
            .Status = If(supplier.MustChangePassword, LoginStatus.MustChangePassword, LoginStatus.Ok),
            .Supplier = supplier, .Upgraded = upgraded}
    End Function

    Public Function ChangePassword(nitBase As String, current As String, newPassword As String) As List(Of String)
        Dim errors = PasswordPolicy.Validate(newPassword, nitBase)
        Dim supplier = _repo.Find(nitBase)
        If supplier Is Nothing OrElse _hasher.Verify(supplier.PasswordHash, current) = PasswordCheck.Failed Then
            errors.Insert(0, "La contraseña actual no es correcta.")
        ElseIf current = newPassword Then
            errors.Add("La nueva contraseña debe ser diferente a la actual.")
        End If
        If errors.Count = 0 Then _repo.UpdatePassword(nitBase, _hasher.Hash(newPassword), False)
        Return errors
    End Function
End Class

Public NotInheritable Class PasswordPolicy
    Private Shared ReadOnly Common As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {
        "password", "contraseña", "123456789", "1234567890", "proveedor", "proveedor123", "qwerty123"}

    Private Sub New()
    End Sub

    Public Shared Function Validate(password As String, nitBase As String) As List(Of String)
        Dim errors As New List(Of String)()
        If String.IsNullOrEmpty(password) OrElse password.Length < 10 Then errors.Add("Use al menos 10 caracteres.")
        If password IsNot Nothing AndAlso password.Length > 128 Then errors.Add("Use máximo 128 caracteres.")
        If password IsNot Nothing AndAlso Common.Contains(password) Then errors.Add("Esa contraseña es demasiado común.")
        If password IsNot Nothing AndAlso Not String.IsNullOrEmpty(nitBase) AndAlso password.Contains(nitBase) Then
            errors.Add("La contraseña no puede contener el NIT.")
        End If
        Return errors
    End Function
End Class
