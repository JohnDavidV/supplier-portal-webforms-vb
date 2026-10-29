Imports System.Web
Imports System.Web.Security
Imports System.Web.UI

''' <summary>
''' Base class for every page that requires a signed-in supplier.
''' - Anti-CSRF: ViewStateUserKey ties the ViewState (and every postback) to the user, so a
'''   forged postback from another site fails validation.
''' - No caching of pages with private data (back button after logout, shared computers).
''' - Suppliers with a temporary/migrated password are forced to change it first.
''' </summary>
Public MustInherit Class SecurePage
    Inherits Page

    Protected ReadOnly Property NitBase As String
        Get
            Return Context.User.Identity.Name
        End Get
    End Property

    Protected Overrides Sub OnInit(e As EventArgs)
        If Not Context.User.Identity.IsAuthenticated Then
            FormsAuthentication.RedirectToLoginPage()
            Context.ApplicationInstance.CompleteRequest()
            Return
        End If
        ViewStateUserKey = Context.User.Identity.Name & "|" & Session.SessionID

        If TypeOf Session("MustChangePassword") Is Boolean AndAlso CBool(Session("MustChangePassword")) _
           AndAlso Not Request.Path.EndsWith("/Account/ChangePassword.aspx", StringComparison.OrdinalIgnoreCase) Then
            Response.Redirect("~/Account/ChangePassword.aspx", False)
            Context.ApplicationInstance.CompleteRequest()
            Return
        End If

        Response.Cache.SetCacheability(HttpCacheability.NoCache)
        Response.Cache.SetNoStore()
        MyBase.OnInit(e)
    End Sub
End Class
