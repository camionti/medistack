<%@ Page Title="Agendas" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Agenda.aspx.cs" Inherits="MediStack.Web.AgendaPagina" %>
<asp:Content ID="AgendaContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="page-heading">
        <p class="eyebrow">Organización clínica</p>
        <h1>Agendas</h1>
        <p>Elige un profesional y una fecha para ver sus horarios de atención, cuáles están libres u ocupados y los turnos asignados.</p>
    </section>
    <asp:Label ID="Mensaje" runat="server" Visible="false" role="status" />

    <section class="panel management-panel" aria-labelledby="agenda-dia-title">
        <h2 id="agenda-dia-title" class="section-title">Agenda diaria: horarios del día</h2>
        <div class="search-row agenda-filter">
            <div class="form-group agenda-professional-filter">
                <asp:Label ID="ProfesionalLabel" runat="server" AssociatedControlID="Profesional" Text="Profesional" />
                <asp:DropDownList ID="Profesional" runat="server" CssClass="form-control"
                    AutoPostBack="true" OnSelectedIndexChanged="Profesional_SelectedIndexChanged" />
            </div>
            <div class="form-group">
                <asp:Label ID="FechaLabel" runat="server" AssociatedControlID="Fecha" Text="Fecha" />
                <asp:TextBox ID="Fecha" runat="server" CssClass="form-control" TextMode="Date"
                    AutoPostBack="true" OnTextChanged="Actualizar_Click" />
            </div>
            <asp:Button ID="Actualizar" runat="server" Text="Consultar agenda" CssClass="button button-secondary"
                CausesValidation="false" OnClick="Actualizar_Click" formnovalidate="formnovalidate" />
        </div>
        <p class="form-note"><asp:Literal ID="ResumenAgenda" runat="server" /></p>
        <div class="table-wrap">
            <asp:GridView ID="DisponibilidadGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" EmptyDataText="El profesional no tiene horarios de atención en la fecha seleccionada.">
                <Columns>
                    <asp:BoundField DataField="FechaHora" HeaderText="Desde" DataFormatString="{0:HH:mm}" />
                    <asp:BoundField DataField="Fin" HeaderText="Hasta" DataFormatString="{0:HH:mm}" />
                    <asp:BoundField DataField="Especialidad" HeaderText="Especialidad" />
                    <asp:TemplateField HeaderText="Estado">
                        <ItemTemplate>
                            <span class='<%# Convert.ToBoolean(Eval("Disponible")) ? "status-tag status-available" : "status-tag status-occupied" %>'>
                                <%#: Convert.ToBoolean(Eval("Disponible")) ? "Disponible" : "Ocupado · " + Eval("Estado") %>
                            </span>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:BoundField DataField="Paciente" HeaderText="Paciente" />
                    <asp:BoundField DataField="Motivo" HeaderText="Detalle" />
                    <asp:TemplateField HeaderText="Acceso">
                        <ItemTemplate>
                            <asp:HyperLink ID="FichaPacienteLink" runat="server" Text="Ficha"
                                NavigateUrl='<%# ResolveUrl("~/FichaPaciente.aspx?PacienteId=" + Eval("PacienteId")) %>'
                                Visible='<%# Eval("PacienteId") != null && Eval("PacienteId") != DBNull.Value %>' />
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </section>

    <section class="panel management-panel" aria-labelledby="turnos-dia-title">
        <h2 id="turnos-dia-title" class="section-title">Turnos asignados en la fecha</h2>
        <div class="table-wrap">
            <asp:GridView ID="TurnosGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" EmptyDataText="No hay turnos asignados para este profesional en la fecha seleccionada.">
                <Columns>
                    <asp:BoundField DataField="FechaHora" HeaderText="Hora" DataFormatString="{0:HH:mm}" />
                    <asp:BoundField DataField="Paciente" HeaderText="Paciente" />
                    <asp:BoundField DataField="Especialidad" HeaderText="Especialidad" />
                    <asp:BoundField DataField="Motivo" HeaderText="Motivo" />
                    <asp:BoundField DataField="Estado" HeaderText="Estado" />
                    <asp:BoundField DataField="EstadoSena" HeaderText="Seña" />
                    <asp:TemplateField HeaderText="Ficha">
                        <ItemTemplate>
                            <asp:HyperLink ID="FichaPacienteTurnoLink" runat="server" Text="Ver ficha"
                                NavigateUrl='<%# ResolveUrl("~/FichaPaciente.aspx?PacienteId=" + Eval("PacienteId")) %>' />
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </section>

    <asp:Panel ID="GestionHorariosPanel" runat="server" Visible="false">
        <section class="panel management-panel" aria-labelledby="horarios-titulo">
            <div class="section-heading-row">
                <h2 id="horarios-titulo" class="section-title">Franjas semanales</h2>
                <asp:Button ID="NuevoHorario" runat="server" Text="Nueva franja" CssClass="button button-light"
                    CausesValidation="false" OnClick="NuevoHorario_Click" formnovalidate="formnovalidate" />
            </div>
            <div class="table-wrap">
                <asp:GridView ID="HorariosGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                    GridLines="None" DataKeyNames="HorarioAtencionId,ProfesionalId,EspecialidadId,DiaSemana,HoraInicio,HoraFin,Activo"
                    EmptyDataText="Este profesional todavía no tiene franjas semanales."
                    OnRowCommand="HorariosGrid_RowCommand">
                    <Columns>
                        <asp:BoundField DataField="Especialidad" HeaderText="Especialidad" />
                        <asp:TemplateField HeaderText="Día">
                            <ItemTemplate><%#: DiaSemanaTexto(Eval("DiaSemana")) %></ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Desde">
                            <ItemTemplate><%# ((TimeSpan)Eval("HoraInicio")).ToString(@"hh\:mm") %></ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Hasta">
                            <ItemTemplate><%# ((TimeSpan)Eval("HoraFin")).ToString(@"hh\:mm") %></ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Estado">
                            <ItemTemplate><%# Convert.ToBoolean(Eval("Activo")) ? "Activa" : "Inactiva" %></ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Acciones">
                            <ItemTemplate>
                                <div class="row-actions">
                                    <asp:LinkButton ID="EditarHorario" runat="server" Text="Editar" CommandName="EditarHorario"
                                        CommandArgument="<%# Container.DataItemIndex %>" CausesValidation="false" />
                                    <asp:LinkButton ID="CambiarEstadoHorario" runat="server"
                                        Text='<%# Convert.ToBoolean(Eval("Activo")) ? "Desactivar" : "Reactivar" %>'
                                        CommandName="CambiarEstadoHorario" CommandArgument="<%# Container.DataItemIndex %>"
                                        CausesValidation="false"
                                        OnClientClick="return confirm('¿Cambiar el estado de esta franja semanal?');" />
                                </div>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>
        </section>

        <section class="panel management-panel" aria-labelledby="formulario-horario-title">
            <h2 id="formulario-horario-title" class="section-title"><asp:Literal ID="TituloHorario" runat="server" /></h2>
            <div class="form-grid form-grid-short">
                <div class="form-group">
                    <asp:Label ID="EspecialidadHorarioLabel" runat="server" AssociatedControlID="EspecialidadHorario" Text="Especialidad" />
                    <asp:DropDownList ID="EspecialidadHorario" runat="server" CssClass="form-control" />
                </div>
                <div class="form-group">
                    <asp:Label ID="DiaSemanaLabel" runat="server" AssociatedControlID="DiaSemana" Text="Día disponible" />
                    <asp:DropDownList ID="DiaSemana" runat="server" CssClass="form-control">
                        <asp:ListItem Text="Lunes" Value="1" />
                        <asp:ListItem Text="Martes" Value="2" />
                        <asp:ListItem Text="Miércoles" Value="3" />
                        <asp:ListItem Text="Jueves" Value="4" />
                        <asp:ListItem Text="Viernes" Value="5" />
                        <asp:ListItem Text="Sábado" Value="6" />
                        <asp:ListItem Text="Domingo" Value="7" />
                    </asp:DropDownList>
                </div>
                <div class="form-group">
                    <asp:Label ID="HoraInicioLabel" runat="server" AssociatedControlID="HoraInicio" Text="Hora de inicio" />
                    <asp:TextBox ID="HoraInicio" runat="server" CssClass="form-control" TextMode="Time" required="required" />
                </div>
                <div class="form-group">
                    <asp:Label ID="HoraFinLabel" runat="server" AssociatedControlID="HoraFin" Text="Hora de fin" />
                    <asp:TextBox ID="HoraFin" runat="server" CssClass="form-control" TextMode="Time" required="required" />
                </div>
            </div>
            <p class="form-note">Los espacios se calculan con la duración estándar de la especialidad. No se permiten franjas superpuestas ni cambios que dejen turnos confirmados fuera del horario.</p>
            <div class="form-actions">
                <asp:Button ID="GuardarHorario" runat="server" Text="Guardar franja" CssClass="button button-primary"
                    OnClick="GuardarHorario_Click" />
                <asp:Button ID="CancelarHorario" runat="server" Text="Cancelar" CssClass="button button-light"
                    CausesValidation="false" OnClick="NuevoHorario_Click" formnovalidate="formnovalidate" />
            </div>
        </section>
    </asp:Panel>
</asp:Content>
