<%@ Page Title="Turnos" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Turnos.aspx.cs" Inherits="MediStack.Web.TurnosPagina" %>
<asp:Content ID="TurnosContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="page-heading">
        <p class="eyebrow">Atención clínica</p>
        <h1>Turnos</h1>
        <p>Disponibilidad calculada según la agenda, la duración de la especialidad y los turnos vigentes.</p>
    </section>
    <asp:Label ID="Mensaje" runat="server" Visible="false" role="status" />

    <asp:Panel ID="SolicitudPanel" runat="server" Visible="false">
        <section class="panel management-panel" aria-labelledby="solicitar-turno-title">
            <div class="section-heading-row">
                <div>
                    <h2 id="solicitar-turno-title" class="section-title"><asp:Literal ID="TituloSolicitud" runat="server">Solicitar turno</asp:Literal></h2>
                    <p class="form-note">Los turnos se registran como solicitados. El personal administrativo puede confirmarlos.</p>
                </div>
                <asp:Button ID="CancelarReprogramacion" runat="server" Text="Salir de la reprogramación"
                    CssClass="button button-light" CausesValidation="false" Visible="false"
                    OnClick="CancelarReprogramacion_Click" formnovalidate="formnovalidate" />
            </div>
            <div class="form-grid">
                <asp:Panel ID="PacienteSelectorPanel" runat="server" CssClass="form-group" Visible="false">
                    <asp:Label ID="PacienteLabel" runat="server" AssociatedControlID="Paciente" Text="Paciente" />
                    <asp:DropDownList ID="Paciente" runat="server" CssClass="form-control" />
                </asp:Panel>
                <div class="form-group">
                    <asp:Label ID="EspecialidadLabel" runat="server" AssociatedControlID="Especialidad" Text="1. Especialidad" />
                    <asp:DropDownList ID="Especialidad" runat="server" CssClass="form-control"
                        AutoPostBack="true" OnSelectedIndexChanged="Especialidad_SelectedIndexChanged" />
                </div>
                <div class="form-group">
                    <asp:Label ID="ProfesionalLabel" runat="server" AssociatedControlID="Profesional" Text="2. Profesional" />
                    <asp:DropDownList ID="Profesional" runat="server" CssClass="form-control"
                        AutoPostBack="true" OnSelectedIndexChanged="Profesional_SelectedIndexChanged" />
                </div>
                <div class="form-group">
                    <asp:Label ID="FechaDisponibilidadLabel" runat="server" AssociatedControlID="FechaDisponibilidad" Text="3. Fecha del turno" />
                    <asp:TextBox ID="FechaDisponibilidad" runat="server" CssClass="form-control" TextMode="Date" />
                </div>
                <div class="form-group form-group-full">
                    <asp:Label ID="MotivoLabel" runat="server" AssociatedControlID="Motivo" Text="Motivo de consulta (opcional)" />
                    <asp:TextBox ID="Motivo" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="2" MaxLength="500" />
                </div>
            </div>
            <p class="form-note"><asp:Literal ID="DuracionEspecialidad" runat="server" /></p>
            <div class="form-actions">
                <asp:Button ID="ConsultarDisponibilidad" runat="server" Text="Ver horarios disponibles"
                    CssClass="button button-secondary" CausesValidation="false" OnClick="ConsultarDisponibilidad_Click" formnovalidate="formnovalidate" />
            </div>
            <div class="table-wrap">
                <asp:GridView ID="DisponibilidadGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                    GridLines="None" DataKeyNames="ProfesionalId,EspecialidadId,FechaHora,Disponible,DuracionMinutos"
                    EmptyDataText="Seleccioná especialidad, profesional y fecha para ver los horarios."
                    OnRowCommand="DisponibilidadGrid_RowCommand">
                    <Columns>
                        <asp:BoundField DataField="FechaHora" HeaderText="Desde" DataFormatString="{0:HH:mm}" />
                        <asp:BoundField DataField="Fin" HeaderText="Hasta" DataFormatString="{0:HH:mm}" />
                        <asp:BoundField DataField="Especialidad" HeaderText="Especialidad" />
                        <asp:BoundField DataField="DuracionMinutos" HeaderText="Duración (min)" />
                        <asp:TemplateField HeaderText="Estado">
                            <ItemTemplate>
                                <span class='<%# Convert.ToBoolean(Eval("Disponible")) ? "status-tag status-available" : "status-tag status-occupied" %>'>
                                    <%#: EstadoHorario(Eval("Disponible"), Eval("Estado")) %>
                                </span>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Paciente">
                            <ItemTemplate><%#: PacienteOcupante(Eval("Paciente")) %></ItemTemplate>
                            <ItemStyle />
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Acción">
                            <ItemTemplate>
                                <asp:LinkButton ID="Solicitar" runat="server"
                                    Text='<%# EsReprogramacion ? "Elegir horario" : "Solicitar" %>'
                                    CommandName="ElegirHorario" CommandArgument="<%# Container.DataItemIndex %>"
                                    CausesValidation="false" Visible='<%# Convert.ToBoolean(Eval("Disponible")) %>' />
                                <span class="form-note" runat="server" visible='<%# !Convert.ToBoolean(Eval("Disponible")) %>'>No disponible</span>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>
        </section>
    </asp:Panel>

    <section class="panel management-panel" aria-labelledby="mis-turnos-title">
        <h2 id="mis-turnos-title" class="section-title"><asp:Literal ID="TituloListado" runat="server">Turnos</asp:Literal></h2>
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
        <p class="form-note">Elige una fecha en "Desde" para ver solo ese día, o completa "Desde" y "Hasta" para un rango. Sin fechas se muestran todos los turnos. <strong><asp:Literal ID="ResumenFiltro" runat="server" /></strong></p>
        <div class="table-wrap">
            <asp:GridView ID="TurnosGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" DataKeyNames="TurnoId,PacienteId,ProfesionalId,EspecialidadId,FechaHora,Estado"
                EmptyDataText="No hay turnos para el período seleccionado." OnRowCommand="TurnosGrid_RowCommand">
                <Columns>
                    <asp:BoundField DataField="FechaHora" HeaderText="Fecha y hora" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                    <asp:TemplateField HeaderText="Paciente">
                        <ItemTemplate>
                            <asp:HyperLink ID="FichaPacienteLink" runat="server" Text='<%#: Eval("Paciente") %>'
                                NavigateUrl='<%# ResolveUrl("~/FichaPaciente.aspx?PacienteId=" + Eval("PacienteId")) %>'
                                Visible='<%# !EsPaciente %>' />
                            <asp:Literal ID="MiNombre" runat="server" Text='<%#: Eval("Paciente") %>'
                                Visible='<%# EsPaciente %>' />
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:BoundField DataField="Profesional" HeaderText="Profesional" />
                    <asp:BoundField DataField="Especialidad" HeaderText="Especialidad" />
                    <asp:BoundField DataField="DuracionEstandarMinutos" HeaderText="Duración (min)" />
                    <asp:BoundField DataField="Motivo" HeaderText="Motivo" />
                    <asp:TemplateField HeaderText="Estado">
                        <ItemTemplate><span class="status-tag"><%#: Eval("Estado") %></span></ItemTemplate>
                    </asp:TemplateField>
                    <asp:BoundField DataField="EstadoSena" HeaderText="Seña" />
                    <asp:TemplateField HeaderText="Acciones">
                        <ItemTemplate>
                            <div class="row-actions">
                                <asp:LinkButton ID="Confirmar" runat="server" Text="Confirmar"
                                    CommandName="Confirmar" CommandArgument="<%# Container.DataItemIndex %>"
                                    CausesValidation="false" Visible='<%# EsAdministrativo && Convert.ToString(Eval("Estado")) == "Solicitado" %>' />
                                <asp:LinkButton ID="MarcarAtendido" runat="server" Text="Atendido"
                                    CommandName="Atendido" CommandArgument="<%# Container.DataItemIndex %>"
                                    CausesValidation="false" Visible='<%# PuedeCerrarTurno(Eval("Estado"), Eval("FechaHora")) %>' />
                                <asp:LinkButton ID="MarcarAusente" runat="server" Text="Ausente"
                                    CommandName="Ausente" CommandArgument="<%# Container.DataItemIndex %>"
                                    CausesValidation="false" Visible='<%# PuedeCerrarTurno(Eval("Estado"), Eval("FechaHora")) %>' />
                                <asp:LinkButton ID="Reprogramar" runat="server" Text="Reprogramar"
                                    CommandName="Reprogramar" CommandArgument="<%# Container.DataItemIndex %>"
                                    CausesValidation="false" Visible='<%# PuedeCancelarOReprogramar(Eval("PacienteId"), Eval("Estado"), Eval("FechaHora")) %>' />
                                <asp:LinkButton ID="CancelarTurno" runat="server" Text="Cancelar"
                                    CommandName="CancelarTurno" CommandArgument="<%# Container.DataItemIndex %>"
                                    CausesValidation="false" Visible='<%# PuedeCancelarOReprogramar(Eval("PacienteId"), Eval("Estado"), Eval("FechaHora")) %>'
                                    OnClientClick="return confirm('¿Cancelar este turno? La operación no procesa reembolsos de señas.');" />
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
        <p class="form-note">Cancelar un turno libera su horario. El estado de una seña pagada no implica un reembolso; los cobros y reembolsos se gestionarán en la etapa de Caja.</p>
    </section>
</asp:Content>
