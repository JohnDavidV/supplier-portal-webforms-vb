Public Class ChangePassword
    Inherits SecurePage

    Private Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
        ForcedNotice.Visible = TypeOf Session("MustChangePassword") Is Boolean AndAlso CBool(Session("MustChangePassword"))
    End Sub

    Private Sub SaveButton_Click(sender As Object, e As EventArgs) Handles SaveButton.Click
        If Not IsValid Then Return
        Dim problems = AppServices.Current.Auth.ChangePassword(NitBase, CurrentBox.Text, NewBox.Text)
        If problems.Count > 0 Then
            Errors.DataSource = problems
            Errors.DataBind()
            Return
        End If

        Dim wasForced = ForcedNotice.Visible
        Session("MustChangePassword") = False
        If wasForced Then
            Response.Redirect("~/Default.aspx", False)
            Context.ApplicationInstance.CompleteRequest()
        Else
            FormPanel.Visible = False
            Done.Visible = True
        End If
    End Sub
End Class
