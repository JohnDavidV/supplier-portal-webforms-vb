Imports System.Web.Security
Imports System.Web.UI
Imports SupplierPortal.Core

Public Class Login
    Inherits Page

    Private Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
        DemoHint.Visible = AppServices.Current.DemoMode
        If Not IsPostBack AndAlso Context.User.Identity.IsAuthenticated Then
            Response.Redirect("~/Default.aspx", False)
            Context.ApplicationInstance.CompleteRequest()
        End If
    End Sub

    Private Sub SignInButton_Click(sender As Object, e As EventArgs) Handles SignInButton.Click
        If Not IsValid Then Return

        Dim result = AppServices.Current.Auth.Login(NitBox.Text, PasswordBox.Text)
        Select Case result.Status
            Case LoginStatus.LockedOut
                Message.Text = $"Demasiados intentos fallidos. Intente de nuevo en {Math.Ceiling(result.RetryAfter.Value.TotalMinutes)} minuto(s)."
                Return
            Case LoginStatus.InvalidCredentials
                ' Same message for unknown NIT and wrong password (no account enumeration).
                Message.Text = "NIT o contraseña incorrectos."
                Return
        End Select

        ' Drop anything stored before authentication.
        Session.Clear()
        Session("SupplierName") = result.Supplier.Name
        Session("MustChangePassword") = (result.Status = LoginStatus.MustChangePassword)
        FormsAuthentication.SetAuthCookie(result.Supplier.NitBase, createPersistentCookie:=False)

        Dim target = If(result.Status = LoginStatus.MustChangePassword, "~/Account/ChangePassword.aspx", "~/Default.aspx")
        Response.Redirect(target, False)
        Context.ApplicationInstance.CompleteRequest()
    End Sub
End Class
