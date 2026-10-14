<%@ Page Title="Pagar seña" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="PagarSena.aspx.cs" Inherits="MediStack.Web.PagarSenaPagina" %>
<asp:Content ID="PagarSenaContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="page-heading">
        <p class="eyebrow">Turnos</p>
        <h1>Pagar seña</h1>
        <p>Pagá la seña para confirmar tu turno. El pago se hace de forma segura en Mercado Pago.</p>
    </section>
    <asp:Label ID="Mensaje" runat="server" Visible="false" role="status" />

    <asp:Panel ID="DetallePanel" runat="server" Visible="false">
        <section class="panel management-panel" aria-labelledby="detalle-sena-title">
            <h2 id="detalle-sena-title" class="section-title">Detalle del turno</h2>
            <dl class="record-grid">
                <div><dt>Especialidad</dt><dd><asp:Literal ID="EspecialidadTexto" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Profesional</dt><dd><asp:Literal ID="ProfesionalTexto" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Fecha y hora</dt><dd><asp:Literal ID="FechaHoraTexto" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Estado del turno</dt><dd><asp:Literal ID="EstadoTexto" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Seña a pagar</dt><dd><strong><asp:Literal ID="MontoTexto" runat="server" Mode="Encode" /></strong></dd></div>
                <div><dt>Pagá antes de</dt><dd><asp:Literal ID="VencimientoTexto" runat="server" Mode="Encode" /></dd></div>
            </dl>
            <p class="form-note"><strong><asp:Literal ID="TiempoRestante" runat="server" Mode="Encode" /></strong></p>
            <div class="form-actions">
                <asp:Button ID="PagarMercadoPago" runat="server" Text="Pagar con tarjeta o Mercado Pago"
                    CssClass="button button-primary" CausesValidation="false" OnClick="PagarMercadoPago_Click"
                    OnClientClick="this.disabled=true; this.value='Redirigiendo a Mercado Pago...';" UseSubmitBehavior="false" />
                <asp:HyperLink ID="VolverTurnos" runat="server" NavigateUrl="~/Turnos.aspx"
                    CssClass="button button-light" Text="Volver a mis turnos" />
            </div>
            <p class="form-note">El pago lo procesa Mercado Pago: MediStack no ve ni guarda los datos de tu tarjeta.
                Si no pagás antes del vencimiento, el horario se libera.</p>
        </section>
    </asp:Panel>

    <asp:Panel ID="SinDetallePanel" runat="server" Visible="false">
        <asp:HyperLink ID="VolverSinDetalle" runat="server" NavigateUrl="~/Turnos.aspx"
            CssClass="button button-light" Text="Volver a mis turnos" />
    </asp:Panel>
</asp:Content>
