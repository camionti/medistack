<%@ Page Title="Caja" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Caja.aspx.cs" Inherits="MediStack.Web.CajaPagina" %>
<asp:Content ID="CajaContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="page-heading" aria-labelledby="caja-title">
        <p class="eyebrow">Administración financiera</p>
        <h1 id="caja-title">Caja</h1>
        <p>Los cobros registrados son los ingresos disponibles en el esquema actual.</p>
    </section>

    <asp:Label ID="Mensaje" runat="server" Visible="false" role="status" />

    <section class="panel management-panel" aria-labelledby="estado-caja-title">
        <h2 id="estado-caja-title" class="section-title">Estado de hoy · <asp:Literal ID="FechaHoy" runat="server" /></h2>
        <p><asp:Literal ID="EstadoHoy" runat="server" /></p>
        <asp:Panel ID="CerrarPanel" runat="server" Visible="false">
            <div class="search-row">
                <div class="form-group">
                    <asp:Label ID="EfectivoContadoLabel" runat="server" AssociatedControlID="EfectivoContado"
                        Text="Efectivo contado" />
                    <asp:TextBox ID="EfectivoContado" runat="server" CssClass="form-control"
                        TextMode="Number" min="0" max="9999999999.99" step="0.01" inputmode="decimal" />
                </div>
                <asp:Button ID="Cerrar" runat="server" Text="Cerrar caja de hoy"
                    CssClass="button button-primary" OnClick="Cerrar_Click" />
            </div>
        </asp:Panel>
    </section>

    <section class="panel management-panel" aria-labelledby="resumen-caja-title">
        <h2 id="resumen-caja-title" class="section-title">Resumen del período</h2>
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
        </div>
        <div class="table-wrap">
            <asp:GridView ID="TotalesGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None">
                <Columns>
                    <asp:BoundField DataField="CantidadCobros" HeaderText="Cantidad de cobros" />
                    <asp:BoundField DataField="TotalEfectivo" HeaderText="Efectivo" DataFormatString="{0:C2}" />
                    <asp:BoundField DataField="TotalTarjeta" HeaderText="Tarjeta" DataFormatString="{0:C2}" />
                    <asp:BoundField DataField="TotalTransferencia" HeaderText="Transferencia" DataFormatString="{0:C2}" />
                    <asp:BoundField DataField="TotalGeneral" HeaderText="Total del período" DataFormatString="{0:C2}" />
                </Columns>
            </asp:GridView>
        </div>
    </section>

    <section class="panel management-panel" aria-labelledby="movimientos-title">
        <h2 id="movimientos-title" class="section-title">Ingresos y movimientos asociados</h2>
        <div class="table-wrap">
            <asp:GridView ID="MovimientosGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" EmptyDataText="No hay movimientos para el período seleccionado.">
                <Columns>
                    <asp:BoundField DataField="FechaHoraCobro" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                    <asp:BoundField DataField="CobroId" HeaderText="Cobro" />
                    <asp:BoundField DataField="TurnoId" HeaderText="Turno" />
                    <asp:BoundField DataField="Paciente" HeaderText="Paciente" />
                    <asp:BoundField DataField="Profesional" HeaderText="Profesional" />
                    <asp:BoundField DataField="Especialidad" HeaderText="Especialidad" />
                    <asp:BoundField DataField="TipoCobro" HeaderText="Concepto" />
                    <asp:BoundField DataField="MontoCobrado" HeaderText="Ingreso" DataFormatString="{0:C2}" />
                    <asp:BoundField DataField="MedioPago" HeaderText="Medio" />
                    <asp:BoundField DataField="Cobrador" HeaderText="Registrado por" />
                </Columns>
            </asp:GridView>
        </div>
        <p class="form-note">No se muestran egresos porque el esquema no tiene una entidad para registrarlos.</p>
    </section>

    <section class="panel management-panel" aria-labelledby="cierres-title">
        <h2 id="cierres-title" class="section-title">Cierres registrados</h2>
        <div class="table-wrap">
            <asp:GridView ID="CierresGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" EmptyDataText="No hay cierres en el período seleccionado.">
                <Columns>
                    <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy}" />
                    <asp:BoundField DataField="FechaHoraCierre" HeaderText="Hora de cierre" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                    <asp:BoundField DataField="Administrativo" HeaderText="Administrativo" />
                    <asp:BoundField DataField="CantidadCobros" HeaderText="Cobros" />
                    <asp:BoundField DataField="TotalEfectivo" HeaderText="Efectivo" DataFormatString="{0:C2}" />
                    <asp:BoundField DataField="TotalTarjeta" HeaderText="Tarjeta" DataFormatString="{0:C2}" />
                    <asp:BoundField DataField="TotalTransferencia" HeaderText="Transferencia" DataFormatString="{0:C2}" />
                    <asp:BoundField DataField="TotalGeneral" HeaderText="Total" DataFormatString="{0:C2}" />
                    <asp:BoundField DataField="EfectivoContado" HeaderText="Efectivo contado" DataFormatString="{0:C2}" />
                    <asp:BoundField DataField="Diferencia" HeaderText="Diferencia" DataFormatString="{0:C2}" />
                </Columns>
            </asp:GridView>
        </div>
        <p class="form-note">La base implementa un cierre diario único. La caja se considera abierta hasta que se registra ese cierre; no existen operaciones independientes de apertura ni movimientos de egreso.</p>
    </section>
</asp:Content>
