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
                    <div class="password-input-wrapper">
                        <asp:TextBox ID="Password" runat="server" CssClass="form-control" TextMode="Password" MaxLength="128" autocomplete="current-password" required="required" />
                        <button type="button" class="password-toggle" aria-label="Mostrar contraseña" aria-pressed="false" title="Mostrar contraseña">
                            <svg class="password-toggle-icon password-toggle-icon-show" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                                <path d="M2.5 12s3.5-6 9.5-6 9.5 6 9.5 6-3.5 6-9.5 6-9.5-6-9.5-6Z" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
                                <circle cx="12" cy="12" r="2.5" fill="none" stroke="currentColor" stroke-width="1.8" />
                            </svg>
                            <svg class="password-toggle-icon password-toggle-icon-hide" viewBox="0 0 24 24" aria-hidden="true" focusable="false" hidden>
                                <path d="M3 3l18 18M10.6 6.3A10.5 10.5 0 0 1 12 6c6 0 9.5 6 9.5 6a16.7 16.7 0 0 1-3.2 3.7M6.2 6.2C3.7 8 2.5 12 2.5 12S6 18 12 18c1.2 0 2.3-.2 3.3-.6" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
                            </svg>
                        </button>
                    </div>
                </div>
                <asp:Button ID="Ingresar" runat="server" Text="Iniciar sesión" CssClass="button button-primary button-wide" OnClick="Ingresar_Click" />
                <p class="enlace-recuperar"><a href="<%: ResolveUrl("~/Account/RecuperarCuenta.aspx") %>">¿Olvidaste tu usuario o contraseña?</a></p>
               <p class="form-note">¿No tienes cuenta? <a href="<%: ResolveUrl("~/Account/Registro.aspx") %>">Regístrate aquí</a></p> 
            </div>
        </div>
    </section>
    <script>
        (function () {
            var passwordInput = document.getElementById('<%= Password.ClientID %>');
            var toggleButton = document.querySelector('.password-toggle');

            if (!passwordInput || !toggleButton) {
                return;
            }

            toggleButton.addEventListener('click', function () {
                var isVisible = passwordInput.type === 'text';
                passwordInput.type = isVisible ? 'password' : 'text';
                toggleButton.setAttribute('aria-pressed', String(!isVisible));
                toggleButton.setAttribute('aria-label', isVisible ? 'Mostrar contraseña' : 'Ocultar contraseña');
                toggleButton.setAttribute('title', isVisible ? 'Mostrar contraseña' : 'Ocultar contraseña');
                toggleButton.querySelector('.password-toggle-icon-show').hidden = !isVisible;
                toggleButton.querySelector('.password-toggle-icon-hide').hidden = isVisible;
            });
        }());
    </script>
</asp:Content>
