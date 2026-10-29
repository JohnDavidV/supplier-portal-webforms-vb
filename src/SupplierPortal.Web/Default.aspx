<%@ Page Title="Mis pagos" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="false" CodeBehind="Default.aspx.vb" Inherits="SupplierPortal.Web._Default" %>
<asp:Content ContentPlaceHolderID="Body" runat="server">
    <h1>Mis pagos</h1>

    <asp:Repeater ID="SummaryRepeater" runat="server" ItemType="SupplierPortal.Core.Domain.PaymentSummary">
        <HeaderTemplate><div class="tiles"></HeaderTemplate>
        <ItemTemplate>
            <div class="tile">
                <div class="tile-label">Año <%#: Item.Year %></div>
                <div class="tile-value"><%#: Item.Gross.ToString("C0", Pesos) %></div>
                <div class="muted"><%#: Item.Payments %> pagos · retención <%#: Item.Withholding.ToString("C0", Pesos) %></div>
            </div>
        </ItemTemplate>
        <FooterTemplate></div></FooterTemplate>
    </asp:Repeater>

    <section class="card">
        <div class="row">
            <label for="<%= YearList.ClientID %>">Año</label>
            <asp:DropDownList ID="YearList" runat="server" AutoPostBack="true" />
            <span class="spacer"></span>
            <asp:Button ID="CertificateButton" runat="server" Text="Generar enlace del certificado de retención" CssClass="button secondary" />
        </div>

        <asp:Panel ID="LinkPanel" runat="server" Visible="false" CssClass="notice">
            Enlace válido por 7 días (puede enviarlo a su contador):
            <asp:HyperLink ID="CertificateLink" runat="server" Target="_blank" />
        </asp:Panel>

        <asp:GridView ID="PaymentsGrid" runat="server" AutoGenerateColumns="false" CssClass="grid" GridLines="None"
            ItemType="SupplierPortal.Core.Domain.Payment" EmptyDataText="No hay pagos registrados para este año.">
            <Columns>
                <asp:TemplateField HeaderText="Fecha"><ItemTemplate><%#: Item.PaidOn.ToString("dd/MM/yyyy") %></ItemTemplate></asp:TemplateField>
                <asp:TemplateField HeaderText="Factura"><ItemTemplate><%#: Item.InvoiceNumber %></ItemTemplate></asp:TemplateField>
                <asp:TemplateField HeaderText="Valor bruto" ItemStyle-CssClass="num" HeaderStyle-CssClass="num"><ItemTemplate><%#: Item.GrossAmount.ToString("C0", Pesos) %></ItemTemplate></asp:TemplateField>
                <asp:TemplateField HeaderText="Retención" ItemStyle-CssClass="num" HeaderStyle-CssClass="num"><ItemTemplate><%#: Item.Withholding.ToString("C0", Pesos) %></ItemTemplate></asp:TemplateField>
                <asp:TemplateField HeaderText="Neto pagado" ItemStyle-CssClass="num" HeaderStyle-CssClass="num"><ItemTemplate><%#: Item.NetAmount.ToString("C0", Pesos) %></ItemTemplate></asp:TemplateField>
            </Columns>
        </asp:GridView>
    </section>
</asp:Content>
