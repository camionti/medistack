<%@ Page Title="Nueva contraseña" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="NuevaPassword.aspx.cs" Inherits="MediStack.Web.Account.NuevaPassword" %>

<asp:Content ID="NuevaHead" ContentPlaceHolderID="HeadContent" runat="server">
    <meta name="referrer" content="no-referrer" />
    <link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700;800&display=swap" rel="stylesheet" />
    <link href="<%: ResolveUrl("~/Content/login.css") %>?v=<%: System.IO.File.GetLastWriteTimeUtc(Server.MapPath("~/Content/login.css")).Ticks %>" rel="stylesheet" />
</asp:Content>

<asp:Content ID="NuevaContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="login-page" aria-labelledby="nueva-title">
        <div class="login-card">
            <div class="login-logo">
                <img src="<%: ResolveUrl("~/Content/MediStack-logo-violeta.png") %>" alt="MediStack, gestión clínica" />
            </div>
            <div class="login-heading">
                <h1 id="nueva-title">Nueva contraseña</h1>
                <p>Elegí una contraseña nueva para tu cuenta</p>
            </div>

            <asp:Label ID="ErrorMessage" runat="server" CssClass="alert alert-error" role="alert" Visible="false" />

            <asp:Panel ID="ExitoPanel" runat="server" Visible="false">
                <div class="alert alert-success" role="status">¡Listo! Tu contraseña se actualizó correctamente.</div>
                <a class="button button-primary button-wide" href="<%: ResolveUrl("~/Account/Login.aspx") %>">Ir a iniciar sesión</a>
            </asp:Panel>

            <asp:Panel ID="FormularioPanel" runat="server" CssClass="login-form">
                <p class="cuenta-recuperada">Cuenta: <strong><asp:Literal ID="CorreoCuenta" runat="server" /></strong></p>
                <div class="form-group">
                    <asp:Label runat="server" AssociatedControlID="NuevaClave" Text="Nueva contraseña" />
                    <asp:TextBox ID="NuevaClave" runat="server" CssClass="form-control" TextMode="Password" MaxLength="128" placeholder="Mínimo 10 caracteres" autocomplete="new-password" required="required" />
                </div>
                <div class="form-group">
                    <asp:Label runat="server" AssociatedControlID="RepetirClave" Text="Repetir contraseña" />
                    <asp:TextBox ID="RepetirClave" runat="server" CssClass="form-control" TextMode="Password" MaxLength="128" autocomplete="new-password" required="required" />
                </div>
                <asp:Button ID="Guardar" runat="server" Text="Guardar contraseña" CssClass="button button-primary button-wide" OnClick="Guardar_Click" />
            </asp:Panel>

            <p class="form-note"><a href="<%: ResolveUrl("~/Account/Login.aspx") %>">Volver a iniciar sesión</a></p>
        </div>
    </section>
</asp:Content>