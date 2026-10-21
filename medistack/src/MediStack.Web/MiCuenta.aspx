<%@ Page Title="Mi cuenta" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="MiCuenta.aspx.cs" Inherits="MediStack.Web.MiCuenta" %>
<asp:Content ID="MiCuentaContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="page-heading">
        <p class="eyebrow">Tu perfil</p>
        <h1>Mi cuenta</h1>
        <p>Consultá tus datos y cambiá tu contraseña.</p>
    </section>
    <asp:Label ID="Mensaje" runat="server" Visible="false" role="status" />
    <section class="panel management-panel" aria-labelledby="datos-cuenta-title">
        <h2 id="datos-cuenta-title" class="section-title">Tus datos</h2>
        <p><strong>Nombre:</strong> <%: Convert.ToString(Session["NombreCompleto"]) %></p>
        <p><strong>Usuario:</strong> <%: Convert.ToString(Session["NombreUsuario"]) %></p>
        <p><strong>Rol:</strong> <%: Convert.ToString(Session["RolNombre"]) %></p>
    </section>
    <section class="panel management-panel" aria-labelledby="cambiar-clave-title">
        <h2 id="cambiar-clave-title" class="section-title">Cambiar contraseña</h2>
        <div class="form-grid form-grid-short">
            <div class="form-group">
                <label for="<%= ClaveActual.ClientID %>">Contraseña actual</label>
                <asp:TextBox ID="ClaveActual" runat="server" CssClass="form-control" TextMode="Password"
                    MaxLength="128" autocomplete="current-password" required="required" />
            </div>
            <div class="form-group">
                <label for="<%= ClaveNueva.ClientID %>">Contraseña nueva</label>
                <asp:TextBox ID="ClaveNueva" runat="server" CssClass="form-control" TextMode="Password"
                    MaxLength="128" placeholder="Mínimo 10 caracteres" autocomplete="new-password" required="required" />
            </div>
            <div class="form-group">
                <label for="<%= ClaveRepetir.ClientID %>">Repetir contraseña nueva</label>
                <asp:TextBox ID="ClaveRepetir" runat="server" CssClass="form-control" TextMode="Password"
                    MaxLength="128" autocomplete="new-password" required="required" />
            </div>
        </div>
        <div class="form-actions">
            <asp:Button ID="Cambiar" runat="server" Text="Cambiar contraseña" CssClass="button button-primary"
                OnClick="Cambiar_Click" />
        </div>
        <p class="form-note">Usá al menos 10 caracteres. Te vamos a enviar un aviso por correo cuando se cambie.</p>
    </section>
</asp:Content>