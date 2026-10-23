<%@ Page Title="Recepción del día" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RevisionDia.aspx.cs" Inherits="MediStack.Web.RevisionDiaPagina" %>
<asp:Content ID="RevisionContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="page-heading" aria-labelledby="recepcion-title">
        <p class="eyebrow">Operación Diaria</p>
        <h1 id="recepcion-title">Recepción y Sala de Espera del Día</h1>
        <p>Mostrador operativo de recepción: localizá pacientes por DNI o apellido, registrá su llegada a sala de espera (check-in) con un solo clic y verificá el estado de señas y copagos.</p>
    </section>

    <asp:Label ID="Mensaje" runat="server" Visible="false" role="status" />

    <section class="recepcion-stats" aria-label="Métricas de recepción de hoy">
        <div class="recepcion-stat-card stat-espera">
            <span class="recepcion-stat-num"><asp:Literal ID="LitEnEspera" runat="server">0</asp:Literal></span>
            <span class="recepcion-stat-label">En Sala de Espera</span>
        </div>
        <div class="recepcion-stat-card stat-total">
            <span class="recepcion-stat-num"><asp:Literal ID="LitTotalHoy" runat="server">0</asp:Literal></span>
            <span class="recepcion-stat-label">Turnos del Día</span>
        </div>
        <div class="recepcion-stat-card stat-atendidos">
            <span class="recepcion-stat-num"><asp:Literal ID="LitAtendidosHoy" runat="server">0</asp:Literal></span>
            <span class="recepcion-stat-label">Atendidos Hoy</span>
        </div>
    </section>

    <section class="panel management-panel" aria-labelledby="mostrador-title">
        <div class="section-heading-row">
            <div>
                <h2 id="mostrador-title" class="section-title">Mostrador de Recepción</h2>
                <p class="form-note">
                    <asp:Literal ID="LitResumenTurnos" runat="server" />
                </p>
            </div>
        </div>

        <div class="recepcion-toolbar">
            <div class="form-group" style="flex: 2 1 220px; margin-bottom: 0;">
                <asp:Label ID="FiltroBusquedaLabel" runat="server" AssociatedControlID="FiltroBusqueda" Text="Buscar por DNI o Paciente" />
                <asp:TextBox ID="FiltroBusqueda" runat="server" CssClass="form-control" placeholder="Ej: 38456123 o Gómez" MaxLength="100" />
            </div>

            <div class="form-group" style="flex: 1 1 160px; margin-bottom: 0;">
                <asp:Label ID="FiltroEstadoLabel" runat="server" AssociatedControlID="FiltroEstado" Text="Estado" />
                <asp:DropDownList ID="FiltroEstado" runat="server" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="Filtro_SelectedIndexChanged">
                    <asp:ListItem Text="Todos los estados" Value="Todos" />
                    <asp:ListItem Text="En sala de espera" Value="EnEspera" />
                    <asp:ListItem Text="Confirmados (por llegar)" Value="Confirmado" />
                    <asp:ListItem Text="Solicitados" Value="Solicitado" />
                    <asp:ListItem Text="Atendidos" Value="Atendido" />
                    <asp:ListItem Text="Ausentes" Value="Ausente" />
                </asp:DropDownList>
            </div>

            <div class="form-group" style="flex: 1 1 180px; margin-bottom: 0;">
                <asp:Label ID="FiltroProfesionalLabel" runat="server" AssociatedControlID="FiltroProfesional" Text="Profesional" />
                <asp:DropDownList ID="FiltroProfesional" runat="server" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="Filtro_SelectedIndexChanged" />
            </div>

            <div class="form-group" style="flex: 1 1 180px; margin-bottom: 0;">
                <asp:Label ID="FiltroEspecialidadLabel" runat="server" AssociatedControlID="FiltroEspecialidad" Text="Especialidad" />
                <asp:DropDownList ID="FiltroEspecialidad" runat="server" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="Filtro_SelectedIndexChanged" />
            </div>

            <div style="display: flex; gap: 8px;">
                <asp:Button ID="BtnBuscar" runat="server" Text="Buscar" CssClass="button button-primary"
                    OnClick="BtnBuscar_Click" CausesValidation="false" />
                <asp:Button ID="BtnLimpiar" runat="server" Text="Limpiar" CssClass="button button-light"
                    OnClick="BtnLimpiar_Click" CausesValidation="false" />
            </div>
        </div>

        <div class="table-wrap">
            <asp:GridView ID="RecepcionGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" DataKeyNames="TurnoId,Estado" OnRowCommand="RecepcionGrid_RowCommand"
                EmptyDataText="No hay turnos para hoy que coincidan con el criterio de búsqueda.">
                <Columns>
                    <asp:TemplateField HeaderText="Horario">
                        <ItemTemplate>
                            <span class="hora-destacada"><%#: Convert.ToDateTime(Eval("FechaHora")).ToString("HH:mm") %> hs</span>
                            <%# Eval("FechaHoraLlegada") != DBNull.Value ? "<span class='llegada-time'>Llegó " + Convert.ToDateTime(Eval("FechaHoraLlegada")).ToString("HH:mm") + " hs</span>" : "" %>
                        </ItemTemplate>
                    </asp:TemplateField>

                    <asp:TemplateField HeaderText="Paciente">
                        <ItemTemplate>
                            <strong><%#: Eval("Paciente") %></strong>
                            <div style="font-size: 0.82rem; color: #64748b;">
                                DNI: <span><%#: Eval("PacienteDni") ?? "—" %></span>
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>

                    <asp:TemplateField HeaderText="Atención">
                        <ItemTemplate>
                            <div><strong><%#: Eval("Profesional") %></strong></div>
                            <div style="font-size: 0.85rem; color: #475569;"><%#: Eval("Especialidad") %> (<%#: Eval("DuracionEstandarMinutos") %> min)</div>
                            <%# !string.IsNullOrWhiteSpace(Convert.ToString(Eval("Motivo"))) ? "<div style='font-size: 0.8rem; color: #64748b; font-style: italic;' title='" + HttpUtility.HtmlEncode(Convert.ToString(Eval("Motivo"))) + "'>" + HttpUtility.HtmlEncode(MotivoCorto(Eval("Motivo"))) + "</div>" : "" %>
                        </ItemTemplate>
                    </asp:TemplateField>

                    <asp:TemplateField HeaderText="Estado">
                        <ItemTemplate>
                            <span class="status-tag status-turno" data-estado='<%#: Eval("Estado") %>'><%#: TextoEstado(Eval("Estado")) %></span>
                        </ItemTemplate>
                    </asp:TemplateField>

                    <asp:TemplateField HeaderText="Seña">
                        <ItemTemplate>
                            <%# BadgeSena(Eval("EstadoSena"), Eval("MontoSena"), Eval("TurnoId")) %>
                        </ItemTemplate>
                    </asp:TemplateField>

                    <asp:TemplateField HeaderText="Acciones">
                        <ItemTemplate>
                            <div style="display: flex; gap: 6px; flex-wrap: wrap; align-items: center;">
                                <%-- Check-in de llegada (1 clic) --%>
                                <asp:Button runat="server" Text="Llegó (Check-in)" CssClass="button button-primary button-sm"
                                    CommandName="CheckIn" CommandArgument='<%# Eval("TurnoId") %>'
                                    Visible='<%# Convert.ToString(Eval("Estado")) == "Confirmado" %>' />

                                <%-- Deshacer llegada en caso de error --%>
                                <asp:Button runat="server" Text="Deshacer llegada" CssClass="button button-light button-sm"
                                    CommandName="DeshacerCheckIn" CommandArgument='<%# Eval("TurnoId") %>'
                                    Visible='<%# Convert.ToString(Eval("Estado")) == "EnEspera" %>' />

                                <%-- Atención médica o cierre --%>
                                <asp:Button runat="server" Text="Atendido" CssClass="button button-primary button-sm"
                                    CommandName="Atendido" CommandArgument='<%# Eval("TurnoId") %>'
                                    Visible='<%# Convert.ToString(Eval("Estado")) == "EnEspera" || (Convert.ToString(Eval("Estado")) == "Confirmado" && Convert.ToDateTime(Eval("FechaHora")) <= DateTime.Now) %>' />

                                <asp:Button runat="server" Text="Ausente" CssClass="button button-secondary button-sm"
                                    CommandName="Ausente" CommandArgument='<%# Eval("TurnoId") %>'
                                    Visible='<%# Convert.ToString(Eval("Estado")) == "EnEspera" || (Convert.ToString(Eval("Estado")) == "Confirmado" && Convert.ToDateTime(Eval("FechaHora")) <= DateTime.Now) %>' />

                                <%-- Turno Solicitado pendiente de confirmación --%>
                                <asp:Button runat="server" Text="Confirmar" CssClass="button button-secondary button-sm"
                                    CommandName="Confirmado" CommandArgument='<%# Eval("TurnoId") %>'
                                    Visible='<%# Convert.ToString(Eval("Estado")) == "Solicitado" %>' />

                                <asp:Button runat="server" Text="Cancelar" CssClass="button button-light button-sm"
                                    CommandName="Cancelado" CommandArgument='<%# Eval("TurnoId") %>'
                                    Visible='<%# Convert.ToString(Eval("Estado")) == "Solicitado" || Convert.ToString(Eval("Estado")) == "Confirmado" %>' />

                                <%-- Ficha clínica --%>
                                <asp:HyperLink runat="server" Text="Ficha" CssClass="button button-light button-sm"
                                    NavigateUrl='<%# ResolveUrl("~/FichaPaciente.aspx?PacienteId=" + Eval("PacienteId")) %>' />
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </section>

    <%-- Sección de resolución de turnos pasados sin resolver --%>
    <asp:Panel ID="PendientesPanel" runat="server" CssClass="panel management-panel" style="margin-top: 2rem;">
        <h2 id="pendientes-title" class="section-title">Turnos de horas pasadas sin resolver</h2>
        <p class="form-note">Turnos cuya hora de atención ya pasó y no fueron marcados como atendidos ni ausentes.</p>
        <div class="table-wrap">
            <asp:GridView ID="PendientesGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" DataKeyNames="TurnoId" OnRowCommand="PendientesGrid_RowCommand"
                EmptyDataText="No hay turnos de horas pasadas sin resolver. ¡Todo al día!">
                <Columns>
                    <asp:BoundField DataField="FechaHora" HeaderText="Fecha y hora" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                    <asp:BoundField DataField="Paciente" HeaderText="Paciente" />
                    <asp:BoundField DataField="Profesional" HeaderText="Profesional" />
                    <asp:BoundField DataField="Especialidad" HeaderText="Especialidad" />
                    <asp:TemplateField HeaderText="Estado">
                        <ItemTemplate>
                            <span class="status-tag status-turno" data-estado='<%#: Eval("Estado") %>'><%#: TextoEstado(Eval("Estado")) %></span>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Acciones">
                        <ItemTemplate>
                            <asp:Button runat="server" Text="Atendido" CssClass="button button-primary button-sm"
                                CommandName="Atendido" CommandArgument='<%# Eval("TurnoId") %>'
                                Visible='<%# Convert.ToString(Eval("Estado")) == "Confirmado" || Convert.ToString(Eval("Estado")) == "EnEspera" %>' />
                            <asp:Button runat="server" Text="Ausente" CssClass="button button-secondary button-sm"
                                CommandName="Ausente" CommandArgument='<%# Eval("TurnoId") %>'
                                Visible='<%# Convert.ToString(Eval("Estado")) == "Confirmado" || Convert.ToString(Eval("Estado")) == "EnEspera" %>' />
                            <asp:Button runat="server" Text="Confirmar" CssClass="button button-primary button-sm"
                                CommandName="Confirmado" CommandArgument='<%# Eval("TurnoId") %>'
                                Visible='<%# Convert.ToString(Eval("Estado")) == "Solicitado" %>' />
                            <asp:Button runat="server" Text="Cancelar" CssClass="button button-secondary button-sm"
                                CommandName="Cancelado" CommandArgument='<%# Eval("TurnoId") %>'
                                Visible='<%# Convert.ToString(Eval("Estado")) == "Solicitado" %>' />
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </asp:Panel>
</asp:Content>
