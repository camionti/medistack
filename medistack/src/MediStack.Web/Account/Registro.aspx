<%@ Page Title="Crear cuenta" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Registro.aspx.cs" Inherits="MediStack.Web.Account.Registro" %>

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
                        <asp:TextBox ID="Dni" runat="server" CssClass="form-control" MaxLength="20" inputmode="numeric" required="required" />
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
                        <asp:TextBox ID="Password" runat="server" CssClass="form-control" TextMode="Password" MaxLength="128" placeholder="Mínimo 10 caracteres" autocomplete="new-password" required="required" />
                    </div>
                    <div class="form-group">
                        <asp:Label runat="server" AssociatedControlID="ConfirmarPassword" Text="Confirmar contraseña" />
                        <asp:TextBox ID="ConfirmarPassword" runat="server" CssClass="form-control" TextMode="Password" MaxLength="128" autocomplete="new-password" required="required" />
                    </div>
                </div>
                <asp:CheckBox ID="Terminos" runat="server" CssClass="form-check" Text="Acepto los términos del servicio y la política de privacidad médica." />
                <asp:Button ID="Registrarme" runat="server" Text="Registrarme como paciente" CssClass="button button-primary button-wide" OnClick="Registrarme_Click" />
            </asp:Panel>

            <p class="form-note">¿Ya tienes una cuenta? <a href="<%: ResolveUrl("~/Account/Login.aspx") %>">Inicia sesión</a></p>
        </div>
    </section>
</asp:Content>