<%@ Page Title="Cambiar contraseña" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="false" CodeBehind="ChangePassword.aspx.vb" Inherits="SupplierPortal.Web.ChangePassword" %>
<asp:Content ContentPlaceHolderID="Body" runat="server">
    <section class="card narrow">
        <h1>Cambiar contraseña</h1>
        <asp:PlaceHolder ID="ForcedNotice" runat="server" Visible="false">
            <p class="notice">Por seguridad debe definir una contraseña nueva antes de continuar.</p>
        </asp:PlaceHolder>
        <asp:Panel ID="FormPanel" runat="server" DefaultButton="SaveButton">
            <label for="<%= CurrentBox.ClientID %>">Contraseña actual</label>
            <asp:TextBox ID="CurrentBox" runat="server" TextMode="Password" MaxLength="128" autocomplete="current-password" />
            <label for="<%= NewBox.ClientID %>">Nueva contraseña (mínimo 10 caracteres)</label>
            <asp:TextBox ID="NewBox" runat="server" TextMode="Password" MaxLength="128" autocomplete="new-password" />
            <label for="<%= ConfirmBox.ClientID %>">Confirmar nueva contraseña</label>
            <asp:TextBox ID="ConfirmBox" runat="server" TextMode="Password" MaxLength="128" autocomplete="new-password" />
            <asp:CompareValidator ID="ConfirmMatches" runat="server" ControlToValidate="ConfirmBox" ControlToCompare="NewBox" Display="Dynamic" CssClass="error" ErrorMessage="Las contraseñas no coinciden." />
            <asp:BulletedList ID="Errors" runat="server" CssClass="error" EnableViewState="false" />
            <asp:Button ID="SaveButton" runat="server" Text="Guardar" CssClass="button" />
        </asp:Panel>
        <asp:Label ID="Done" runat="server" CssClass="ok" Visible="false" EnableViewState="false">Contraseña actualizada.</asp:Label>
    </section>
</asp:Content>
