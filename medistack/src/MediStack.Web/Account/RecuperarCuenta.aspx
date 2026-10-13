<%@ Page Title="Recuperar cuenta" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RecuperarCuenta.aspx.cs" Inherits="MediStack.Web.Account.RecuperarCuenta" %>

<asp:Content ID="RecuperarHead" ContentPlaceHolderID="HeadContent" runat="server">
    <link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700;800&display=swap" rel="stylesheet" />
    <link href="<%: ResolveUrl("~/Content/login.css") %>?v=<%: System.IO.File.GetLastWriteTimeUtc(Server.MapPath("~/Content/login.css")).Ticks %>" rel="stylesheet" />
</asp:Content>

<asp:Content ID="RecuperarContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="login-page" aria-labelledby="recuperar-title">
        <div class="login-card">
            <div class="login-logo">
                <img src="<%: ResolveUrl("~/Content/MediStack-logo-violeta.png") %>" alt="MediStack, gestión clínica" />
            </div>
            <div class="login-heading">
                <h1 id="recuperar-title">Recuperar cuenta</h1>
                <p>Elegí qué necesitás y te enviamos las instrucciones por correo</p>
            </div>

            <asp:Label ID="ErrorMessage" runat="server" CssClass="alert alert-error" role="alert" Visible="false" />
            <asp:Label ID="ExitoMessage" runat="server" CssClass="alert alert-success" role="status" Visible="false" />

            <asp:Panel ID="FormularioPanel" runat="server" CssClass="login-form">
                <div class="form-group">
                    <span class="etiqueta-grupo" id="opcion-etiqueta">¿Qué necesitás recuperar?</span>
                    <asp:RadioButtonList ID="Opcion" runat="server" CssClass="opciones-recuperacion" RepeatLayout="UnorderedList" aria-labelledby="opcion-etiqueta">
                        <asp:ListItem Value="usuario" Text="Olvidé mi usuario" />
                        <asp:ListItem Value="password" Text="Olvidé mi contraseña" Selected="True" />
                    </asp:RadioButtonList>
                </div>
                <div class="form-group">
                    <asp:Label runat="server" AssociatedControlID="Email" Text="Correo electrónico" />
                    <asp:TextBox ID="Email" runat="server" CssClass="form-control" TextMode="Email" MaxLength="256" autocomplete="email" required="required" placeholder="El correo con el que te registraste" />
                </div>
                <asp:Button ID="Enviar" runat="server" Text="Enviar instrucciones" CssClass="button button-primary button-wide" OnClick="Enviar_Click" />
            </asp:Panel>

            <p class="form-note"><a href="<%: ResolveUrl("~/Account/Login.aspx") %>">Volver a iniciar sesión</a></p>
        </div>
    </section>
</asp:Content>