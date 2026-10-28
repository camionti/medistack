<%@ Page Title="Crear cuenta" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Registro.aspx.cs" Inherits="MediStack.Web.Account.Registro" MaintainScrollPositionOnPostback="true" %>

<asp:Content ID="RegistroHead" ContentPlaceHolderID="HeadContent" runat="server">
    <link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700;800&display=swap" rel="stylesheet" />
    <link href="<%: ResolveUrl("~/Content/login.css") %>?v=<%: System.IO.File.GetLastWriteTimeUtc(Server.MapPath("~/Content/login.css")).Ticks %>" rel="stylesheet" />
</asp:Content>

<asp:Content ID="RegistroContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="login-page" aria-labelledby="registro-title">
        <div class="login-card login-card-ancho">
            <div class="login-logo">
                <img src="<%: ResolveUrl("~/Content/MediStack-logo-violeta.png") %>" alt="MediStack, gestión clínica" />
            </div>
            <div class="login-heading">
                <h1 id="registro-title">Crear cuenta de paciente</h1>
                <p>Completa tus datos para solicitar turnos y ver tu historial</p>
            </div>

            <asp:Label ID="ErrorMessage" runat="server" CssClass="alert alert-error" role="alert" Visible="false" />
            <asp:Label ID="ExitoMessage" runat="server" CssClass="alert alert-success" role="status" Visible="false" />

            <asp:Panel ID="FormularioPanel" runat="server" CssClass="login-form">
                <div class="form-fila">
                    <div class="form-group">
                        <asp:Label runat="server" AssociatedControlID="Nombre" Text="Nombre" />
                        <asp:TextBox ID="Nombre" runat="server" CssClass="form-control" MaxLength="100" autocomplete="given-name" required="required" />
                    </div>
                    <div class="form-group">
                        <asp:Label runat="server" AssociatedControlID="Apellido" Text="Apellido" />
                        <asp:TextBox ID="Apellido" runat="server" CssClass="form-control" MaxLength="100" autocomplete="family-name" required="required" />
                    </div>
                </div>
                <div class="form-fila">
                    <div class="form-group">
                        <asp:Label runat="server" AssociatedControlID="Dni" Text="DNI" />
                        <asp:TextBox ID="Dni" runat="server" CssClass="form-control" MaxLength="10" inputmode="numeric" required="required" pattern="[0-9]{7,8}|[0-9]{1,2}\.[0-9]{3}\.[0-9]{3}" title="Ingresa solo numeros: 7 u 8 digitos (por ejemplo 41736377)." />
                    </div>
                    <div class="form-group">
                        <asp:Label runat="server" AssociatedControlID="FechaNacimiento" Text="Fecha de nacimiento" />
                        <asp:TextBox ID="FechaNacimiento" runat="server" CssClass="form-control" TextMode="Date" required="required" />
                    </div>
                </div>
                <div class="form-fila">
                    <div class="form-group">
                        <asp:Label runat="server" AssociatedControlID="Email" Text="Correo electrónico" />
                        <asp:TextBox ID="Email" runat="server" CssClass="form-control" TextMode="Email" MaxLength="256" autocomplete="email" required="required" />
                    </div>
                    <div class="form-group">
                        <asp:Label runat="server" AssociatedControlID="Telefono" Text="Teléfono / WhatsApp" />
                        <asp:TextBox ID="Telefono" runat="server" CssClass="form-control" TextMode="Phone" MaxLength="30" autocomplete="tel" />
                    </div>
                </div>
                <div class="form-fila">
                    <div class="form-group">
                        <asp:Label runat="server" AssociatedControlID="ObraSocial" Text="Obra social o prepaga" />
                        <asp:DropDownList ID="ObraSocial" runat="server" CssClass="form-control" />
                    </div>
                    <div class="form-group">
                        <asp:Label runat="server" AssociatedControlID="NumeroAfiliado" Text="N.º de afiliado (opcional)" />
                        <asp:TextBox ID="NumeroAfiliado" runat="server" CssClass="form-control" MaxLength="50" />
                    </div>
                </div>
                <div class="form-group">
                    <asp:Label runat="server" AssociatedControlID="Usuario" Text="Nombre de usuario" />
                    <asp:TextBox ID="Usuario" runat="server" CssClass="form-control" MaxLength="50" autocomplete="username" required="required" />
                </div>
                <div class="form-fila">
                    <div class="form-group">
                        <asp:Label runat="server" AssociatedControlID="Password" Text="Contraseña" />
                        <div class="password-input-wrapper">
                            <asp:TextBox ID="Password" runat="server" CssClass="form-control" TextMode="Password" MaxLength="128" placeholder="Mínimo 10 caracteres" autocomplete="new-password" required="required" />
                            <button type="button" class="password-toggle" aria-label="Mostrar contraseña" aria-pressed="false" title="Mostrar contraseña">
                                <svg class="password-toggle-icon password-toggle-icon-show" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                                    <path d="M2.5 12s3.5-6 9.5-6 9.5 6 9.5 6-3.5 6-9.5 6-9.5-6Z" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
                                    <circle cx="12" cy="12" r="2.5" fill="none" stroke="currentColor" stroke-width="1.8" />
                                </svg>
                                <svg class="password-toggle-icon password-toggle-icon-hide" viewBox="0 0 24 24" aria-hidden="true" focusable="false" hidden>
                                    <path d="M3 3l18 18M10.6 6.3A10.5 10.5 0 0 1 12 6c6 0 9.5 6 9.5 6a16.7 16.7 0 0 1-3.2 3.7M6.2 6.2C3.7 8 2.5 12 2.5 12S6 18 12 18c1.2 0 2.3-.2 3.3-.6" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
                                </svg>
                            </button>
                        </div>
                    </div>
                    <div class="form-group">
                        <asp:Label runat="server" AssociatedControlID="ConfirmarPassword" Text="Confirmar contraseña" />
                        <div class="password-input-wrapper">
                            <asp:TextBox ID="ConfirmarPassword" runat="server" CssClass="form-control" TextMode="Password" MaxLength="128" autocomplete="new-password" required="required" />
                            <button type="button" class="password-toggle" aria-label="Mostrar contraseña" aria-pressed="false" title="Mostrar contraseña">
                                <svg class="password-toggle-icon password-toggle-icon-show" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                                    <path d="M2.5 12s3.5-6 9.5-6 9.5 6 9.5 6-3.5 6-9.5 6-9.5-6Z" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
                                    <circle cx="12" cy="12" r="2.5" fill="none" stroke="currentColor" stroke-width="1.8" />
                                </svg>
                                <svg class="password-toggle-icon password-toggle-icon-hide" viewBox="0 0 24 24" aria-hidden="true" focusable="false" hidden>
                                    <path d="M3 3l18 18M10.6 6.3A10.5 10.5 0 0 1 12 6c6 0 9.5 6 9.5 6a16.7 16.7 0 0 1-3.2 3.7M6.2 6.2C3.7 8 2.5 12 2.5 12S6 18 12 18c1.2 0 2.3-.2 3.3-.6" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
                                </svg>
                            </button>
                        </div>
                    </div>
                </div>
                <asp:CheckBox ID="Terminos" runat="server" CssClass="form-check" Text="Acepto los términos del servicio y la política de privacidad médica." />
                <asp:Button ID="Registrarme" runat="server" Text="Registrarme como paciente" CssClass="button button-primary button-wide" OnClick="Registrarme_Click" />
            </asp:Panel>

            <p class="form-note">¿Ya tienes una cuenta? <a href="<%: ResolveUrl("~/Account/Login.aspx") %>">Inicia sesión</a></p>
        </div>
    </section>
    <script>
        (function () {
            var toggles = document.querySelectorAll('.password-toggle');
            toggles.forEach(function (btn) {
                btn.addEventListener('click', function () {
                    var wrapper = btn.closest('.password-input-wrapper');
                    if (!wrapper) return;
                    var input = wrapper.querySelector('input');
                    if (!input) return;

                    var isVisible = input.type === 'text';
                    input.type = isVisible ? 'password' : 'text';
                    btn.setAttribute('aria-pressed', String(!isVisible));
                    btn.setAttribute('aria-label', isVisible ? 'Mostrar contraseña' : 'Ocultar contraseña');
                    btn.setAttribute('title', isVisible ? 'Mostrar contraseña' : 'Ocultar contraseña');

                    var iconShow = btn.querySelector('.password-toggle-icon-show');
                    var iconHide = btn.querySelector('.password-toggle-icon-hide');
                    if (iconShow) iconShow.hidden = !isVisible;
                    if (iconHide) iconHide.hidden = isVisible;
                });
            });

            function mostrarErrorCliente(mensaje) {
                var alertBox = document.getElementById('<%= ErrorMessage.ClientID %>') || document.getElementById('ClientAlert');
                if (!alertBox) {
                    alertBox = document.createElement('div');
                    alertBox.id = 'ClientAlert';
                    alertBox.className = 'alert alert-error';
                    alertBox.setAttribute('role', 'alert');
                    var heading = document.querySelector('.login-heading');
                    if (heading && heading.parentNode) {
                        heading.parentNode.insertBefore(alertBox, heading.nextSibling);
                    }
                }
                alertBox.textContent = mensaje;
                alertBox.style.display = 'block';
                alertBox.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }

            var form = document.forms[0];
            var btnRegistrar = document.getElementById('<%= Registrarme.ClientID %>');
            var txtPassword = document.getElementById('<%= Password.ClientID %>');
            var txtConfirmar = document.getElementById('<%= ConfirmarPassword.ClientID %>');
            var txtUsuario = document.getElementById('<%= Usuario.ClientID %>');
            var chkTerminos = document.getElementById('<%= Terminos.ClientID %>');

            if (form && btnRegistrar) {
                form.addEventListener('submit', function (e) {
                    if (txtPassword && txtConfirmar) {
                        if (txtPassword.value.length < 10) {
                            e.preventDefault();
                            mostrarErrorCliente('La contraseña debe tener al menos 10 caracteres.');
                            txtPassword.focus();
                            return;
                        }
                        if (txtPassword.value !== txtConfirmar.value) {
                            e.preventDefault();
                            mostrarErrorCliente('Las contraseñas no coinciden.');
                            txtConfirmar.focus();
                            return;
                        }
                        if (txtUsuario && txtPassword.value.trim().toLowerCase() === txtUsuario.value.trim().toLowerCase()) {
                            e.preventDefault();
                            mostrarErrorCliente('La contraseña no puede ser igual a tu nombre de usuario.');
                            txtPassword.focus();
                            return;
                        }
                    }
                    if (chkTerminos && !chkTerminos.checked) {
                        e.preventDefault();
                        mostrarErrorCliente('Debes aceptar los términos para continuar.');
                        chkTerminos.focus();
                        return;
                    }
                });
            }

            var errorAlert = document.getElementById('<%= ErrorMessage.ClientID %>');
            if (errorAlert && errorAlert.offsetParent !== null) {
                errorAlert.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
        }());
    </script>
</asp:Content>
