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
        <p class="form-note">Los egresos en efectivo se registran en la sección siguiente.</p>
    </section>

    <section class="panel management-panel" aria-labelledby="egresos-title">
        <h2 id="egresos-title" class="section-title">Egresos de caja</h2>
        <p class="form-note">Gastos menores pagados en efectivo desde el cajón. Se descuentan del efectivo esperado al cerrar la caja.</p>
        <asp:Panel ID="EgresoPanel" runat="server" Visible="false">
            <div class="search-row">
                <div class="form-group">
                    <asp:Label ID="EgresoConceptoLabel" runat="server" AssociatedControlID="EgresoConcepto" Text="Concepto" />
                    <asp:TextBox ID="EgresoConcepto" runat="server" CssClass="form-control" MaxLength="200" />
                </div>
                <div class="form-group">
                    <asp:Label ID="EgresoMontoLabel" runat="server" AssociatedControlID="EgresoMonto" Text="Monto" />
                    <asp:TextBox ID="EgresoMonto" runat="server" CssClass="form-control"
                        TextMode="Number" min="0.01" max="9999999999.99" step="0.01" inputmode="decimal" />
                </div>
                <div class="form-group">
                    <asp:Label ID="EgresoCategoriaLabel" runat="server" AssociatedControlID="EgresoCategoria" Text="Categoría" />
                    <asp:DropDownList ID="EgresoCategoria" runat="server" CssClass="form-control">
                        <asp:ListItem Value="Insumos" Text="Insumos" />
                        <asp:ListItem Value="Viaticos" Text="Viáticos" />
                        <asp:ListItem Value="Mantenimiento" Text="Mantenimiento" />
                        <asp:ListItem Value="Otros" Text="Otros" />
                    </asp:DropDownList>
                </div>
                <div class="form-group">
                    <asp:Label ID="EgresoComprobanteLabel" runat="server" AssociatedControlID="EgresoComprobante" Text="Comprobante (opcional)" />
                    <asp:TextBox ID="EgresoComprobante" runat="server" CssClass="form-control" MaxLength="100" />
                </div>
                <asp:Button ID="RegistrarEgresoBoton" runat="server" Text="Registrar egreso"
                    CssClass="button button-secondary" OnClick="RegistrarEgreso_Click" />
            </div>
        </asp:Panel>
        <div class="table-wrap">
            <asp:GridView ID="EgresosGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" EmptyDataText="No hay egresos para el período seleccionado.">
                <Columns>
                    <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy}" />
                    <asp:BoundField DataField="Concepto" HeaderText="Concepto" />
                    <asp:BoundField DataField="Categoria" HeaderText="Categoría" />
                    <asp:BoundField DataField="Monto" HeaderText="Monto" DataFormatString="{0:C2}" />
                    <asp:BoundField DataField="Comprobante" HeaderText="Comprobante" />
                    <asp:BoundField DataField="Administrativo" HeaderText="Registrado por" />
                </Columns>
            </asp:GridView>
        </div>
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
                    <asp:BoundField DataField="TotalEgresos" HeaderText="Egresos" DataFormatString="{0:C2}" />
                    <asp:BoundField DataField="EfectivoContado" HeaderText="Efectivo contado" DataFormatString="{0:C2}" />
                    <asp:BoundField DataField="Diferencia" HeaderText="Diferencia" DataFormatString="{0:C2}" />
                </Columns>
            </asp:GridView>
        </div>
        <p class="form-note">La base implementa un cierre diario único. La caja se considera abierta hasta que se registra ese cierre. La diferencia se calcula como efectivo contado − (efectivo cobrado − egresos).</p>
    </section>
</asp:Content>
