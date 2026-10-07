<%@ Page Title="Cobros" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Cobros.aspx.cs" Inherits="MediStack.Web.CobrosPagina" %>
<asp:Content ID="CobrosContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="page-heading" aria-labelledby="cobros-title">
        <p class="eyebrow">Administración financiera</p>
        <h1 id="cobros-title">Cobros</h1>
        <p><asp:Literal ID="AlcanceCobros" runat="server" /></p>
    </section>

    <asp:Label ID="Mensaje" runat="server" Visible="false" role="status" />

    <asp:Panel ID="RegistroPanel" runat="server" CssClass="panel management-panel" Visible="false">
        <h2 class="section-title">Registrar cobro</h2>
        <p class="form-note">Solo se habilitan señas pendientes y consultas de turnos atendidos. El sistema obtiene los importes del turno, la cobertura vigente y la seña ya pagada.</p>
        <div class="form-grid">
            <div class="form-group">
                <asp:Label ID="TurnoLabel" runat="server" AssociatedControlID="Turno" Text="Turno" />
                <asp:DropDownList ID="Turno" runat="server" CssClass="form-control" AutoPostBack="true"
                    OnSelectedIndexChanged="Turno_SelectedIndexChanged" />
            </div>
            <div class="form-group">
                <asp:Label ID="TipoCobroLabel" runat="server" AssociatedControlID="TipoCobro" Text="Concepto" />
                <asp:DropDownList ID="TipoCobro" runat="server" CssClass="form-control" AutoPostBack="true"
                    OnSelectedIndexChanged="TipoCobro_SelectedIndexChanged" />
            </div>
            <div class="form-group">
                <span class="form-label">Importe a cobrar</span>
                <asp:Literal ID="ImporteCobro" runat="server" />
            </div>
            <div class="form-group">
                <asp:Label ID="MedioPagoLabel" runat="server" AssociatedControlID="MedioPago" Text="Medio de pago" />
                <asp:DropDownList ID="MedioPago" runat="server" CssClass="form-control">
                    <asp:ListItem Text="Efectivo" Value="Efectivo" />
                    <asp:ListItem Text="Tarjeta" Value="Tarjeta" />
                    <asp:ListItem Text="Transferencia" Value="Transferencia" />
                </asp:DropDownList>
            </div>
            <div class="form-group">
                <asp:Label ID="MontoRecibidoLabel" runat="server" AssociatedControlID="MontoRecibido" Text="Importe recibido" />
                <asp:TextBox ID="MontoRecibido" runat="server" CssClass="form-control" TextMode="Number"
                    min="0" max="9999999999.99" step="0.01" inputmode="decimal" />
            </div>
            <div class="form-group form-actions">
                <asp:Button ID="Registrar" runat="server" Text="Registrar cobro"
                    CssClass="button button-primary" OnClick="Registrar_Click" />
            </div>
        </div>
    </asp:Panel>

    <section class="panel management-panel" aria-labelledby="cobros-listado-title">
        <h2 id="cobros-listado-title" class="section-title">Cobros registrados</h2>
        <div class="search-row agenda-filter">
            <div class="form-group">
                <asp:Label ID="DesdeLabel" runat="server" AssociatedControlID="Desde" Text="Desde" />
                <asp:TextBox ID="Desde" runat="server" CssClass="form-control" TextMode="Date" />
            </div>
            <div class="form-group">
                <asp:Label ID="HastaLabel" runat="server" AssociatedControlID="Hasta" Text="Hasta" />
                <asp:TextBox ID="Hasta" runat="server" CssClass="form-control" TextMode="Date" />
            </div>
            <asp:Button ID="Filtrar" runat="server" Text="Filtrar" CssClass="button button-secondary"
                CausesValidation="false" OnClick="Filtrar_Click" formnovalidate="formnovalidate" />
            <asp:Button ID="LimpiarFiltro" runat="server" Text="Limpiar filtro" CssClass="button button-light"
                CausesValidation="false" OnClick="LimpiarFiltro_Click" formnovalidate="formnovalidate" />
        </div>
        <p class="form-note">Elige una fecha en "Desde" para ver solo ese día, o completa "Desde" y "Hasta" para un rango. Sin fechas se muestran todos los cobros. <strong><asp:Literal ID="ResumenFiltro" runat="server" /></strong></p>
        <div class="table-wrap">
            <asp:GridView ID="CobrosGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" EmptyDataText="No hay cobros para el período seleccionado.">
                <Columns>
                    <asp:BoundField DataField="FechaHoraCobro" HeaderText="Fecha de cobro" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                    <asp:BoundField DataField="TurnoId" HeaderText="Turno" />
                    <asp:BoundField DataField="Paciente" HeaderText="Paciente" />
                    <asp:BoundField DataField="Profesional" HeaderText="Profesional" />
                    <asp:BoundField DataField="Especialidad" HeaderText="Especialidad" />
                    <asp:BoundField DataField="TipoCobro" HeaderText="Concepto" />
                    <asp:BoundField DataField="MontoCobrado" HeaderText="Importe" DataFormatString="{0:C2}" />
                    <asp:BoundField DataField="MedioPago" HeaderText="Medio" />
                    <asp:BoundField DataField="EstadoCobro" HeaderText="Estado" />
                </Columns>
            </asp:GridView>
        </div>
        <p class="form-note">El esquema actual no incluye observaciones por cobro. La seña se muestra con el estado almacenado en el turno; las consultas registradas se identifican como pagadas.</p>
    </section>
</asp:Content>
