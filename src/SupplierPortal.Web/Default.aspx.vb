Imports System.Globalization
Imports System.Web.UI.WebControls

Public Class _Default
    Inherits SecurePage

    ''' <summary>Colombian peso formatting for the markup ($ 1.234.567).</summary>
    Protected ReadOnly Property Pesos As CultureInfo = CultureInfo.GetCultureInfo("es-CO")

    Private Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
        If IsPostBack Then Return
        Dim summary = AppServices.Current.Repository.YearlySummary(NitBase)
        SummaryRepeater.DataSource = summary
        SummaryRepeater.DataBind()

        For Each s In summary
            YearList.Items.Add(New ListItem(s.Year.ToString(CultureInfo.InvariantCulture)))
        Next
        If YearList.Items.Count = 0 Then YearList.Items.Add(New ListItem(Date.Today.Year.ToString(CultureInfo.InvariantCulture)))
        BindPayments()
    End Sub

    Private Sub YearList_SelectedIndexChanged(sender As Object, e As EventArgs) Handles YearList.SelectedIndexChanged
        LinkPanel.Visible = False
        BindPayments()
    End Sub

    Private Sub CertificateButton_Click(sender As Object, e As EventArgs) Handles CertificateButton.Click
        Dim year = SelectedYear()
        Dim token = AppServices.Current.Links.Sign("withholding:" & year, NitBase, TimeSpan.FromDays(7))
        Dim url = ResolveUrl($"~/Certificate.ashx?nit={Server.UrlEncode(NitBase)}&year={year}&exp={token.Expires}&sig={token.Signature}")
        CertificateLink.NavigateUrl = url
        CertificateLink.Text = $"Certificado de retención {year}"
        LinkPanel.Visible = True
        BindPayments()
    End Sub

    Private Function SelectedYear() As Integer
        Dim year As Integer
        Return If(Integer.TryParse(YearList.SelectedValue, year), year, Date.Today.Year)
    End Function

    Private Sub BindPayments()
        PaymentsGrid.DataSource = AppServices.Current.Repository.Payments(NitBase, SelectedYear())
        PaymentsGrid.DataBind()
    End Sub
End Class
