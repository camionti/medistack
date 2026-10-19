<%@ Page Title="Reembolsos" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Reembolsos.aspx.cs" Inherits="MediStack.Web.ReembolsosPagina" %>
<asp:Content ID="ReembolsosContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="page-heading" aria-labelledby="reembolsos-title">
        <p class="eyebrow">Administración financiera</p>
        <h1 id="reembolsos-title">Reembolsos de señas</h1>
        <p>Señas de turnos cancelados que hay que devolver. La devolución se hace por el mismo medio con el que se pagó y se registra acá con su comprobante.</p>
    </section>

    <asp:Label ID="Mensaje" runat="server" Visible="false" role="status" />

    <section class="panel management-panel" aria-labelledby="pendientes-title">
        <h2 id="pendientes-title" class="section-title">Pendientes</h2>
        <asp:Panel ID="AvisoCajaPanel" runat="server" CssClass="alert alert-error" Visible="false">
            Para devolver una seña en efectivo la caja del día tiene que estar abierta.
            <asp:HyperLink ID="IrACajaLink" runat="server" NavigateUrl="~/Caja.aspx" Text="Ir a Caja" />
        </asp:Panel>
        <div class="table-wrap">
            <asp:GridView ID="PendientesGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" DataKeyNames="ReembolsoId" OnRowCommand="PendientesGrid_RowCommand"
                EmptyDataText="No hay reembolsos pendientes.">
                <Columns>
                    <asp:BoundField DataField="TurnoId" HeaderText="Turno" DataFormatString="#{0}" />
                    <asp:BoundField DataField="Paciente" HeaderText="Paciente" />
                    <asp:BoundField DataField="FechaTurno" HeaderText="Fecha del turno" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                    <asp:BoundField DataField="Profesional" HeaderText="Profesional" />
                    <asp:BoundField DataField="Especialidad" HeaderText="Especialidad" />
                    <asp:BoundField DataField="Monto" HeaderText="Monto" DataFormatString="{0:C2}" />
                    <asp:TemplateField HeaderText="Pagó con">
                        <ItemTemplate><%# TextoMedio(Eval("MedioPago")) %></ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Devolver a">
                        <ItemTemplate><%# HttpUtility.HtmlEncode(DestinoDevolucion(Eval("MedioPago"), Eval("CuentaDevolucion"), Eval("MercadoPagoPagoId"))) %></ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Motivo">
                        <ItemTemplate><%# TextoMotivo(Eval("Motivo")) %></ItemTemplate>
                    </asp:TemplateField>
                    <asp:BoundField DataField="FechaHoraSolicitud" HeaderText="Cancelado el" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                    <asp:TemplateField HeaderText="Acción">
                        <ItemTemplate>
                            <div class="table-actions">
                                <asp:LinkButton ID="DevolverEfectivo" runat="server" Text="Devolver en efectivo"
                                    CommandName="Efectivo" CommandArgument="<%# Container.DataItemIndex %>"
                                    CausesValidation="false" Visible='<%# Eval("MedioPago").ToString() == "Efectivo" %>'
                                    OnClientClick="return confirm('¿Confirmás que le entregaste la seña en efectivo al paciente? Se registra como egreso de la caja.');" />
                                <asp:Panel ID="ManualPanel" runat="server" Visible='<%# Eval("MedioPago").ToString() != "Efectivo" %>'>
                                    <asp:TextBox ID="Comprobante" runat="server" CssClass="form-control" MaxLength="100"
                                        placeholder="N° de comprobante de la devolución" />
                                    <asp:LinkButton ID="RegistrarManual" runat="server" Text="Registrar devolución"
                                        CommandName="Manual" CommandArgument="<%# Container.DataItemIndex %>"
                                        CausesValidation="false" />
                                </asp:Panel>
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
        <p class="form-note">Mercado Pago: desde la cuenta de la clínica, buscá la operación y tocá "Devolver dinero"; cargá acá el ID que muestra Mercado Pago.
            Tarjeta o transferencia: transferí a la cuenta indicada y cargá el número de comprobante.
            Efectivo: se entrega en el mostrador y se descuenta de la caja como egreso.</p>
    </section>

    <section class="panel management-panel" aria-labelledby="historial-title">
        <h2 id="historial-title" class="section-title">Historial</h2>
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
            <asp:GridView ID="HistorialGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" EmptyDataText="No hay reembolsos en el período seleccionado.">
                <Columns>
                    <asp:BoundField DataField="TurnoId" HeaderText="Turno" DataFormatString="#{0}" />
                    <asp:BoundField DataField="Paciente" HeaderText="Paciente" />
                    <asp:BoundField DataField="FechaTurno" HeaderText="Fecha del turno" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                    <asp:BoundField DataField="Profesional" HeaderText="Profesional" />
                    <asp:BoundField DataField="Especialidad" HeaderText="Especialidad" />
                    <asp:BoundField DataField="Monto" HeaderText="Monto" DataFormatString="{0:C2}" />
                    <asp:TemplateField HeaderText="Pagó con">
                        <ItemTemplate><%# TextoMedio(Eval("MedioPago")) %></ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Motivo">
                        <ItemTemplate><%# TextoMotivo(Eval("Motivo")) %></ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Estado">
                        <ItemTemplate>
                            <span class='<%# Eval("Estado").ToString() == "Completado" ? "status-tag status-available" : "status-tag status-occupied" %>'>
                                <%# Eval("Estado") %></span>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:BoundField DataField="FechaHoraSolicitud" HeaderText="Cancelado el" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                    <asp:BoundField DataField="FechaHoraCompletado" HeaderText="Devuelto el" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                    <asp:BoundField DataField="CompletadoPor" HeaderText="Registrado por" />
                    <asp:BoundField DataField="Constancia" HeaderText="Comprobante" />
                </Columns>
            </asp:GridView>
        </div>
    </section>
</asp:Content>
