<%@ Page Title="Iniciar sesión" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="MediStack.Web.Account.Login" %>
<asp:Content ID="LoginHead" ContentPlaceHolderID="HeadContent" runat="server">
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
    <link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700;800&display=swap" rel="stylesheet" />
    <link href="<%: ResolveUrl("~/Content/login.css") %>?v=<%: System.IO.File.GetLastWriteTimeUtc(Server.MapPath("~/Content/login.css")).Ticks %>" rel="stylesheet" />
</asp:Content>
<asp:Content ID="LoginContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="login-page" aria-labelledby="login-title">
        <div class="login-card">
            <div class="login-logo">
                <img src="<%: ResolveUrl("~/Content/MediStack-logo-violeta.png") %>" alt="MediStack, gestión clínica" />
            </div>
            <div class="login-heading">
                <h1 id="login-title">Iniciar sesión</h1>
                <p>Ingresa tus credenciales para acceder al sistema</p>
            </div>
            <div class="login-form">
                <asp:Label ID="ErrorMessage" runat="server" CssClass="alert alert-error" role="alert" Visible="false" />
                <div class="form-group">
                    <asp:Label ID="NombreUsuarioLabel" runat="server" AssociatedControlID="NombreUsuario" Text="Usuario" />
                    <asp:TextBox ID="NombreUsuario" runat="server" CssClass="form-control" MaxLength="50" autocomplete="username" required="required" />
                </div>
                <div class="form-group">
                    <asp:Label ID="PasswordLabel" runat="server" AssociatedControlID="Password" Text="Contraseña" />
                    <asp:TextBox ID="Password" runat="server" CssClass="form-control" TextMode="Password" MaxLength="128" autocomplete="current-password" required="required" />
                </div>
                <asp:Button ID="Ingresar" runat="server" Text="Iniciar sesión" CssClass="button button-primary button-wide" OnClick="Ingresar_Click" />
               <p class="form-note">¿No tienes cuenta? <a href="<%: ResolveUrl("~/Account/Registro.aspx") %>">Regístrate aquí</a></p> 
            </div>
        </div>
    </section>
</asp:Content>
