<%@ Page Title="Resultado del pago" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="PagoResultado.aspx.cs" Inherits="MediStack.Web.PagoResultadoPagina" %>
<asp:Content ID="PagoResultadoContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="page-heading">
        <p class="eyebrow">Turnos</p>
        <h1><asp:Literal ID="Titulo" runat="server" Mode="Encode" Text="Resultado del pago" /></h1>
    </section>
    <asp:Label ID="Mensaje" runat="server" Visible="false" role="status" />

    <section class="panel management-panel">
        <p><asp:Literal ID="Detalle" runat="server" Mode="Encode" /></p>
        <div class="form-actions">
            <asp:HyperLink ID="ReintentarPago" runat="server" CssClass="button button-primary"
                Text="Intentar pagar de nuevo" Visible="false" />
            <asp:HyperLink ID="VerTurnos" runat="server" NavigateUrl="~/Turnos.aspx"
                CssClass="button button-light" Text="Ver mis turnos" />
        </div>
    </section>
</asp:Content>
