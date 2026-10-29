<%@ Page Title="Ingresar" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="false" CodeBehind="Login.aspx.vb" Inherits="SupplierPortal.Web.Login" %>
<asp:Content ContentPlaceHolderID="Body" runat="server">
    <section class="card narrow">
        <h1>Ingreso de proveedores</h1>
        <p class="muted">Consulte sus pagos y descargue certificados de retención.</p>

        <asp:Panel ID="LoginPanel" runat="server" DefaultButton="SignInButton">
            <label for="<%= NitBox.ClientID %>">NIT</label>
            <asp:TextBox ID="NitBox" runat="server" MaxLength="20" autocomplete="username" placeholder="900.123.456-7" />
            <asp:RequiredFieldValidator ID="NitRequired" runat="server" ControlToValidate="NitBox" Display="Dynamic" CssClass="error" ErrorMessage="Ingrese su NIT." />

            <label for="<%= PasswordBox.ClientID %>">Contraseña</label>
            <asp:TextBox ID="PasswordBox" runat="server" TextMode="Password" MaxLength="128" autocomplete="current-password" />
            <asp:RequiredFieldValidator ID="PasswordRequired" runat="server" ControlToValidate="PasswordBox" Display="Dynamic" CssClass="error" ErrorMessage="Ingrese su contraseña." />

            <asp:Label ID="Message" runat="server" CssClass="error" EnableViewState="false" />
            <asp:Button ID="SignInButton" runat="server" Text="Ingresar" CssClass="button" />
        </asp:Panel>

        <asp:PlaceHolder ID="DemoHint" runat="server" Visible="false">
            <p class="hint">Demo: NIT <b>900.123.456</b> / <b>andes-demo-2026</b> · migrado del portal anterior: <b>830987654</b> / <b>puente-old-pass</b></p>
        </asp:PlaceHolder>
    </section>
</asp:Content>
