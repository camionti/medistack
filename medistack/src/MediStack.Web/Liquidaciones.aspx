<%@ Page Title="Liquidación de honorarios" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Liquidaciones.aspx.cs" Inherits="MediStack.Web.LiquidacionesPagina" %>
<asp:Content ID="LiquidacionesContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="page-heading" aria-labelledby="liquidaciones-title">
        <p class="eyebrow">Caja y facturación</p>
        <h1 id="liquidaciones-title">Liquidación de honorarios</h1>
        <p>Calcula lo que corresponde a cada profesional según el esquema de su especialidad en cada consulta cobrada del período.</p>
    </section>

    <asp:Label ID="Mensaje" runat="server" Visible="false" role="status" />

    <section class="panel management-panel" aria-labelledby="calcular-title">
        <h2 id="calcular-title" class="section-title">Calcular liquidación</h2>
        <div class="search-row agenda-filter">
            <div class="form-group">
                <asp:Label ID="ProfesionalLabel" runat="server" AssociatedControlID="Profesional" Text="Profesional" />
                <asp:DropDownList ID="Profesional" runat="server" CssClass="form-control" />
            </div>
            <div class="form-group">
                <asp:Label ID="DesdeLabel" runat="server" AssociatedControlID="Desde" Text="Desde" />
                <asp:TextBox ID="Desde" runat="server" CssClass="form-control" TextMode="Date" />
            </div>
            <div class="form-group">
                <asp:Label ID="HastaLabel" runat="server" AssociatedControlID="Hasta" Text="Hasta" />
                <asp:TextBox ID="Hasta" runat="server" CssClass="form-control" TextMode="Date" />
            </div>
            <asp:Button ID="Calcular" runat="server" Text="Calcular" CssClass="button button-secondary"
                OnClick="Calcular_Click" />
        </div>

        <asp:Panel ID="DetallePanel" runat="server" Visible="false">
            <p><strong>Total a liquidar: <asp:Literal ID="TotalLiteral" runat="server" /></strong>
                · <asp:Literal ID="CantidadLiteral" runat="server" /></p>
            <div class="table-wrap">
                <asp:GridView ID="DetalleGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                    GridLines="None" EmptyDataText="El profesional no tiene cobros de consulta en el período.">
                    <Columns>
                        <asp:BoundField DataField="FechaHoraCobro" HeaderText="Fecha del cobro" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                        <asp:BoundField DataField="Paciente" HeaderText="Paciente" />
                        <asp:BoundField DataField="Especialidad" HeaderText="Especialidad" />
                        <asp:BoundField DataField="Modalidad" HeaderText="Modalidad" />
                        <asp:BoundField DataField="BaseCalculo" HeaderText="Base de cálculo" DataFormatString="{0:C2}" />
                        <asp:BoundField DataField="EsquemaTipo" HeaderText="Esquema" />
                        <asp:BoundField DataField="EsquemaValor" HeaderText="Valor" DataFormatString="{0:0.##}" />
                        <asp:BoundField DataField="Honorario" HeaderText="Honorario" DataFormatString="{0:C2}" />
                    </Columns>
                </asp:GridView>
            </div>
            <div class="form-actions">
                <asp:Button ID="Liquidar" runat="server" Text="Registrar liquidación"
                    CssClass="button button-primary" OnClick="Liquidar_Click" />
            </div>
            <p class="form-note">Al registrar, el total queda congelado: cobros o esquemas posteriores no lo modifican. No se pueden superponer períodos de un mismo profesional.</p>
        </asp:Panel>
    </section>

    <section class="panel management-panel" aria-labelledby="historial-title">
        <h2 id="historial-title" class="section-title">Liquidaciones registradas</h2>
        <div class="table-wrap">
            <asp:GridView ID="HistorialGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" EmptyDataText="Todavía no hay liquidaciones registradas.">
                <Columns>
                    <asp:BoundField DataField="FechaLiquidacion" HeaderText="Registrada" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                    <asp:BoundField DataField="Profesional" HeaderText="Profesional" />
                    <asp:BoundField DataField="PeriodoDesde" HeaderText="Desde" DataFormatString="{0:dd/MM/yyyy}" />
                    <asp:BoundField DataField="PeriodoHasta" HeaderText="Hasta" DataFormatString="{0:dd/MM/yyyy}" />
                    <asp:BoundField DataField="TotalLiquidado" HeaderText="Total liquidado" DataFormatString="{0:C2}" />
                </Columns>
            </asp:GridView>
        </div>
    </section>
</asp:Content>
