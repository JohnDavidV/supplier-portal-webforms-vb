Imports System.Web.Security
Imports System.Web.UI

Public Class SiteMaster
    Inherits MasterPage

    Private Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
        DemoBanner.Visible = AppServices.Current.DemoMode
        If Context.User.Identity.IsAuthenticated Then
            UserBox.Visible = True
            SupplierName.Text = Server.HtmlEncode(TryCast(Session("SupplierName"), String))
        End If
    End Sub

    Private Sub LogoutButton_Click(sender As Object, e As EventArgs) Handles LogoutButton.Click
        FormsAuthentication.SignOut()
        Session.Clear()
        Session.Abandon()
        Response.Redirect("~/Login.aspx", False)
        Context.ApplicationInstance.CompleteRequest()
    End Sub
End Class
