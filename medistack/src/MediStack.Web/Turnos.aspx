<%@ Page Title="Turnos" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Turnos.aspx.cs" Inherits="MediStack.Web.TurnosPagina" %>
<asp:Content ID="TurnosContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="page-heading">
        <p class="eyebrow">Atención clínica</p>
        <h1>Turnos</h1>
        <p>Disponibilidad calculada según la agenda, la duración de la especialidad y los turnos vigentes.</p>
    </section>
    <asp:Label ID="Mensaje" runat="server" Visible="false" role="status" />

    <asp:Panel ID="ConfirmarCancelacionUrlPanel" runat="server" Visible="false" CssClass="panel" style="margin-bottom: 24px; border-left: 4px solid #dc2626; background: #fff;">
        <h2 class="section-title" style="color: #dc2626; margin-top: 0;">Confirmación de cancelación de turno</h2>
        <p><asp:Literal ID="ConfirmarCancelacionDetalle" runat="server" /></p>
        <p class="form-note"><asp:Literal ID="ConfirmarCancelacionNota" runat="server" Mode="Encode" /></p>
        <asp:Panel ID="MotivoCancelacionPanel" runat="server" CssClass="form-group" Visible="false">
            <asp:Label ID="MotivoCancelacionLabel" runat="server" AssociatedControlID="MotivoCancelacionLista" Text="¿Quién cancela el turno?" />
            <asp:DropDownList ID="MotivoCancelacionLista" runat="server" CssClass="form-control">
                <asp:ListItem Text="A pedido del paciente" Value="Paciente" />
                <asp:ListItem Text="La clínica o el profesional" Value="Clinica" />
            </asp:DropDownList>
        </asp:Panel>
        <div style="margin-top: 16px; display: flex; gap: 12px; align-items: center;">
            <asp:Button ID="BotonConfirmarCancelacionUrl" runat="server" Text="Sí, cancelar este turno"
                CssClass="button button-danger" OnClick="BotonConfirmarCancelacionUrl_Click" CausesValidation="false" />
            <asp:Button ID="BotonDescartarCancelacionUrl" runat="server" Text="Mantener turno"
                CssClass="button button-light" OnClick="BotonDescartarCancelacionUrl_Click" CausesValidation="false" />
        </div>
    </asp:Panel>

    <asp:HiddenField ID="VistaActiva" runat="server" Value="listado" />

    <asp:PlaceHolder ID="PhTurnosTabs" runat="server" Visible="false">
        <nav class="turnos-tabs-nav" role="tablist" aria-label="Secciones de turnos">
            <button type="button" class='<%= (VistaActiva != null && VistaActiva.Value == "solicitar") ? "turnos-tab-btn" : "turnos-tab-btn is-active" %>' id="tabBtnListado" role="tab" aria-selected='<%= (VistaActiva != null && VistaActiva.Value == "solicitar") ? "false" : "true" %>' aria-controls="<%= ListadoPanel.ClientID %>">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="width:16px;height:16px;" aria-hidden="true">
                    <rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
                    <line x1="16" y1="2" x2="16" y2="6"></line>
                    <line x1="8" y1="2" x2="8" y2="6"></line>
                    <line x1="3" y1="10" x2="21" y2="10"></line>
                </svg>
                <span id="tabTextoListado"><asp:Literal ID="TabTituloListado" runat="server">Mis turnos</asp:Literal></span>
            </button>
            <button type="button" class='<%= (VistaActiva != null && VistaActiva.Value == "solicitar") ? "turnos-tab-btn is-active" : "turnos-tab-btn" %>' id="tabBtnSolicitud" role="tab" aria-selected='<%= (VistaActiva != null && VistaActiva.Value == "solicitar") ? "true" : "false" %>' aria-controls="<%= SolicitudPanel.ClientID %>">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="width:16px;height:16px;" aria-hidden="true">
                    <circle cx="12" cy="12" r="10"></circle>
                    <line x1="12" y1="8" x2="12" y2="16"></line>
                    <line x1="8" y1="12" x2="16" y2="12"></line>
                </svg>
                <span id="tabTextoSolicitud"><asp:Literal ID="TabTituloSolicitud" runat="server">Solicitar turno</asp:Literal></span>
            </button>
        </nav>
    </asp:PlaceHolder>

    <asp:Panel ID="SolicitudPanel" runat="server" Visible="false" CssClass="turnos-tab-panel">
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
            <div class="solicitud-wizard" data-solicitud-steps="true">
                <div class="solicitud-progress" aria-label="Etapas de solicitud">
                    <span class="solicitud-progress-step solicitud-progress-active" data-stage="1">1. Especialidad</span>
                    <span class="solicitud-progress-step" data-stage="2">2. Fecha</span>
                    <span class="solicitud-progress-step" data-stage="3">3. Horario</span>
                    <span class="solicitud-progress-step" data-stage="4">4. Confirmación</span>
                </div>
                <asp:Panel ID="PacienteSelectorPanel" runat="server" CssClass="form-group solicitud-step solicitud-step-active" Visible="false">
                    <asp:Label ID="PacienteLabel" runat="server" AssociatedControlID="Paciente" Text="Paciente" />
                    <asp:DropDownList ID="Paciente" runat="server" CssClass="form-control"
                        AutoPostBack="true" OnSelectedIndexChanged="Paciente_SelectedIndexChanged" />
                    <span class="form-note solicitud-step-hint">Elegí el paciente para comenzar la solicitud.</span>
                </asp:Panel>
                <div class="form-group solicitud-step solicitud-step-active" data-step="1">
                    <asp:Label ID="EspecialidadLabel" runat="server" AssociatedControlID="Especialidad" Text="Especialidad" />
                    <asp:DropDownList ID="Especialidad" runat="server" CssClass="form-control"
                        AutoPostBack="true" OnSelectedIndexChanged="Especialidad_SelectedIndexChanged" />
                    <span class="form-note solicitud-step-hint">Elegí el motivo principal de tu consulta.</span>
                    <div class="form-actions">
                        <asp:Button ID="AvanzarEspecialidad" runat="server" Text="Avanzar"
                            CssClass="button button-primary" CausesValidation="false"
                            OnClick="AvanzarEspecialidad_Click" formnovalidate="formnovalidate" />
                    </div>
                </div>
                <div class="form-group solicitud-step solicitud-hidden-control" data-step="2">
                    <asp:Label ID="ProfesionalLabel" runat="server" AssociatedControlID="Profesional" Text="Profesional" />
                    <asp:DropDownList ID="Profesional" runat="server" CssClass="form-control"
                        AutoPostBack="true" OnSelectedIndexChanged="Profesional_SelectedIndexChanged" />
                    <span class="form-note solicitud-step-hint">Se habilita al elegir una especialidad.</span>
                </div>
                <div class="form-group solicitud-step solicitud-hidden-control" data-step="3">
                    <asp:Label ID="FechaDisponibilidadLabel" runat="server" AssociatedControlID="FechaDisponibilidad" Text="Fecha del turno" />
                    <asp:TextBox ID="FechaDisponibilidad" runat="server" CssClass="form-control" TextMode="Date" />
                    <span class="form-note solicitud-step-hint">Se habilita al elegir un profesional.</span>
                </div>
            </div>
            <p class="form-note"><asp:Literal ID="DuracionEspecialidad" runat="server" /></p>
            <asp:Button ID="ConsultarDisponibilidad" runat="server" Text="Ver horarios disponibles"
                CssClass="button button-secondary" CausesValidation="false" OnClick="ConsultarDisponibilidad_Click"
                formnovalidate="formnovalidate" style="display:none;" />
            <asp:Panel ID="ExploradorDisponibilidadPanel" runat="server" CssClass="availability-explorer solicitud-wizard-stage" Visible="false">
                <h3 class="section-subtitle">Profesionales con disponibilidad</h3>
                <p class="form-note">Elegí un profesional para ver sus próximas fechas libres. Los resultados se actualizan con la disponibilidad real de los próximos 60 días.</p>
                <div class="table-wrap">
                    <asp:GridView ID="ProfesionalesDisponibilidadGrid" runat="server" AutoGenerateColumns="false"
                        CssClass="data-table" GridLines="None"
                        DataKeyNames="ProfesionalId,TieneDisponibilidad"
                        EmptyDataText="Seleccioná una especialidad para buscar profesionales."
                        OnRowCommand="ProfesionalesDisponibilidadGrid_RowCommand">
                        <Columns>
                            <asp:BoundField DataField="Nombre" HeaderText="Profesional" />
                            <asp:TemplateField HeaderText="Próxima fecha">
                                <ItemTemplate>
                                    <%# FechaResumen(Eval("ProximaFecha")) %>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="CantidadFechas" HeaderText="Fechas disponibles" />
                            <asp:TemplateField HeaderText="Cobertura / seña">
                                <ItemTemplate><%# CoberturaResumen(Eval("TieneCobertura"), Eval("MontoSena")) %></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Estado">
                                <ItemTemplate>
                                    <span class='<%# Convert.ToBoolean(Eval("TieneDisponibilidad")) ? "status-tag status-available" : "status-tag status-occupied" %>'>
                                        <%#: Convert.ToBoolean(Eval("TieneDisponibilidad")) ? "Hay disponibilidad" : Eval("MotivoSinDisponibilidad") %>
                                    </span>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Acción">
                                <ItemTemplate>
                                    <asp:LinkButton ID="ElegirProfesional" runat="server" Text="Ver fechas"
                                        CommandName="ElegirProfesional" CommandArgument="<%# Container.DataItemIndex %>"
                                        CausesValidation="false" Enabled='<%# Convert.ToBoolean(Eval("TieneDisponibilidad")) %>' />
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </div>
            </asp:Panel>
            <asp:Panel ID="FechasDisponiblesPanel" runat="server" CssClass="availability-explorer solicitud-wizard-stage" Visible="false">
                <div class="section-heading-row">
                    <h3 class="section-subtitle">Fechas disponibles</h3>
                    <asp:Button ID="VolverProfesionales" runat="server" Text="Volver a profesionales"
                        CssClass="button button-light" CausesValidation="false"
                        OnClick="VolverProfesionales_Click" formnovalidate="formnovalidate" />
                </div>
                <p class="form-note"><asp:Literal ID="ProfesionalSeleccionadoResumen" runat="server" /></p>
                <div class="table-wrap">
                    <asp:GridView ID="FechasDisponiblesGrid" runat="server" AutoGenerateColumns="false"
                        CssClass="data-table" GridLines="None" DataKeyNames="Fecha,CantidadHorarios"
                        EmptyDataText="Este profesional no tiene fechas libres en la ventana consultada."
                        OnRowCommand="FechasDisponiblesGrid_RowCommand">
                        <Columns>
                            <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dddd dd/MM/yyyy}" />
                            <asp:BoundField DataField="CantidadHorarios" HeaderText="Horarios libres" />
                            <asp:TemplateField HeaderText="Acción">
                                <ItemTemplate>
                                    <asp:LinkButton ID="ElegirFecha" runat="server" Text="Ver horarios"
                                        CommandName="ElegirFecha" CommandArgument="<%# Container.DataItemIndex %>"
                                        CausesValidation="false" />
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </div>
            </asp:Panel>
            <asp:Panel ID="HorariosPanel" runat="server" CssClass="availability-explorer solicitud-wizard-stage" Visible="false">
                <div class="section-heading-row">
                    <h3 class="section-subtitle">Horarios disponibles</h3>
                    <asp:Button ID="VolverFechas" runat="server" Text="Volver a fechas"
                        CssClass="button button-light" CausesValidation="false"
                        OnClick="VolverFechas_Click" formnovalidate="formnovalidate" />
                </div>
                <p class="form-note">Elegí un horario para continuar con la confirmación.</p>
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
            </asp:Panel>
            <asp:Panel ID="ConfirmacionSolicitudPanel" runat="server" CssClass="confirmation-panel" Visible="false">
                <div class="section-heading-row">
                    <h3 class="section-subtitle">Confirmá el turno</h3>
                    <asp:Button ID="VolverHorarios" runat="server" Text="Volver a horarios"
                        CssClass="button button-light" CausesValidation="false"
                        OnClick="VolverHorarios_Click" formnovalidate="formnovalidate" />
                </div>
                <p><asp:Literal ID="ResumenSolicitud" runat="server" /></p>
                <asp:Panel ID="MotivoPanel" runat="server" CssClass="form-group" Visible="false">
                    <asp:Label ID="MotivoLabel" runat="server" AssociatedControlID="Motivo" Text="Motivo de consulta (opcional)" />
                    <asp:TextBox ID="Motivo" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="2" MaxLength="500" />
                </asp:Panel>
                <asp:Panel ID="SenaPresencialPanel" runat="server" CssClass="form-group" Visible="false">
                    <p class="form-note"><strong><asp:Literal ID="SenaPresencialTexto" runat="server" Mode="Encode" /></strong></p>
                    <asp:Label ID="MedioPagoSenaPresencialLabel" runat="server" AssociatedControlID="MedioPagoSenaPresencial" Text="¿Cómo paga la seña?" />
                    <asp:DropDownList ID="MedioPagoSenaPresencial" runat="server" CssClass="form-control">
                        <asp:ListItem Text="Efectivo" Value="Efectivo" />
                        <asp:ListItem Text="Tarjeta de débito o crédito" Value="Tarjeta" />
                        <asp:ListItem Text="Transferencia / Mercado Pago" Value="Transferencia" />
                    </asp:DropDownList>
                    <asp:Label ID="CuentaDevolucionSenaLabel" runat="server" AssociatedControlID="CuentaDevolucionSena"
                        Text="CBU/CVU o alias del paciente (solo tarjeta o transferencia, para devolver la seña si cancela)" />
                    <asp:TextBox ID="CuentaDevolucionSena" runat="server" CssClass="form-control" MaxLength="30"
                        placeholder="22 números o alias" />
                    <asp:Panel ID="CajaNoAbiertaPanel" runat="server" CssClass="alert alert-error" Visible="false">
                        <asp:Literal ID="CajaNoAbiertaTexto" runat="server" Mode="Encode" />
                        <asp:HyperLink ID="AbrirCajaLink" runat="server" NavigateUrl="~/Caja.aspx?abrir=1&amp;volver=turnos"
                            Text="Abrir caja" />
                    </asp:Panel>
                </asp:Panel>
                <div class="form-actions">
                    <asp:Button ID="ConfirmarSolicitud" runat="server" Text="Finalizar turno"
                        CssClass="button button-primary" CausesValidation="false"
                        OnClick="ConfirmarSolicitud_Click" formnovalidate="formnovalidate" />
                    <asp:Button ID="CancelarSolicitud" runat="server" Text="Elegir otro horario"
                        CssClass="button button-light" CausesValidation="false"
                        OnClick="CancelarSolicitud_Click" formnovalidate="formnovalidate" />
                </div>
            </asp:Panel>
        </section>
    </asp:Panel>

    <script type="text/javascript">
        (function () {
            var steps = document.querySelector('[data-solicitud-steps="true"]');
            if (!steps) {
                return;
            }

            var specialty = document.getElementById('<%= Especialidad.ClientID %>');
            var patient = document.getElementById('<%= Paciente.ClientID %>');
            var professional = document.getElementById('<%= Profesional.ClientID %>');
            var date = document.getElementById('<%= FechaDisponibilidad.ClientID %>');
            var reason = document.getElementById('<%= Motivo.ClientID %>');
            var availability = document.getElementById('<%= ConsultarDisponibilidad.ClientID %>');
            if (!specialty || !professional || !date || !reason || !availability) {
                return;
            }

            function updateSteps() {
                var hasPatient = !patient || patient.value !== '';
                var hasSpecialty = specialty.value !== '';
                var hasProfessional = professional.value !== '';
                var hasDate = date.value !== '';
                var patientStep = patient ? patient.closest('.solicitud-step') : null;
                var specialtyStep = specialty.closest('.solicitud-step');
                var professionalStep = professional.closest('.solicitud-step');
                var dateStep = date.closest('.solicitud-step');
                var stages = steps.querySelectorAll('.solicitud-progress-step');
                var explorer = document.getElementById('<%= ExploradorDisponibilidadPanel.ClientID %>');
                var dates = document.getElementById('<%= FechasDisponiblesPanel.ClientID %>');
                var schedules = document.getElementById('<%= HorariosPanel.ClientID %>');
                var confirmation = document.getElementById('<%= ConfirmacionSolicitudPanel.ClientID %>');

                specialty.disabled = !hasPatient;
                professional.disabled = !hasPatient || !hasSpecialty;
                date.disabled = !hasPatient || !hasProfessional;
                reason.disabled = !hasDate;
                availability.disabled = !hasDate || !hasProfessional;

                if (patientStep) {
                    patientStep.classList.add('solicitud-step-active');
                }
                if (specialtyStep) {
                    specialtyStep.classList.toggle('solicitud-step-active', hasPatient);
                }
                if (professionalStep) {
                    professionalStep.classList.toggle('solicitud-step-active', hasPatient && hasSpecialty);
                }
                if (dateStep) {
                    dateStep.classList.toggle('solicitud-step-active', hasPatient && hasProfessional);
                }

                var activeStage = confirmation && confirmation.offsetParent !== null
                    ? '4'
                    : schedules && schedules.offsetParent !== null
                        ? '3'
                        : dates && dates.offsetParent !== null
                            ? '2'
                            : explorer && explorer.offsetParent !== null
                                ? '2' : '1';
                Array.prototype.forEach.call(stages, function (stage) {
                    stage.classList.toggle('solicitud-progress-active', stage.getAttribute('data-stage') === activeStage);
                });
            }

            if (patient) {
                patient.addEventListener('change', updateSteps);
            }
            specialty.addEventListener('change', updateSteps);
            professional.addEventListener('change', updateSteps);
            date.addEventListener('input', updateSteps);
            window.actualizarWizardTurnos = updateSteps;
            updateSteps();
        }());
    </script>

   <asp:Panel ID="ListadoPanel" runat="server" CssClass="turnos-tab-panel">
        <section class="panel management-panel" aria-labelledby="mis-turnos-title">
        <div class="section-heading-row">
            <h2 id="mis-turnos-title" class="section-title" style="margin-bottom: 0;"><asp:Literal ID="TituloListado" runat="server">Turnos</asp:Literal></h2>
            <div class="acciones-exportar">
                <asp:LinkButton ID="btnExportarCsv" runat="server" CssClass="btn-exportar-sutil"
                    OnClick="btnExportarCsv_Click" CausesValidation="false" ToolTip="Exportar vista actual a CSV">
                    <svg viewBox="0 0 24 24" fill="none" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
                        <polyline points="7 10 12 15 17 10"></polyline>
                        <line x1="12" y1="15" x2="12" y2="3"></line>
                    </svg>
                    CSV
                </asp:LinkButton>
                <asp:LinkButton ID="btnExportarPdf" runat="server" CssClass="btn-exportar-sutil"
                    OnClick="btnExportarPdf_Click" CausesValidation="false" ToolTip="Descargar reporte en PDF">
                    <svg viewBox="0 0 24 24" fill="none" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
                        <polyline points="14 2 14 8 20 8"></polyline>
                        <line x1="12" y1="18" x2="12" y2="12"></line>
                        <polyline points="9 15 12 18 15 15"></polyline>
                    </svg>
                    PDF
                </asp:LinkButton>
            </div>
        </div>
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
                    <asp:TemplateField HeaderText="Seña">
                        <ItemTemplate><%#: TextoSena(Eval("EstadoSena"), Eval("MontoSena"), Eval("VencimientoSena")) %></ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Acciones">
                        <ItemTemplate>
                            <div class="row-actions">
                                <asp:HyperLink ID="PagarSena" runat="server" Text="Pagar seña"
                                    NavigateUrl='<%# ResolveUrl("~/PagarSena.aspx?turnoId=" + Eval("TurnoId")) %>'
                                    Visible='<%# PuedePagarSena(Eval("PacienteId"), Eval("Estado"), Eval("EstadoSena"), Eval("VencimientoSena")) %>' />
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
                                    OnClientClick='<%# EsAdministrativo ? "" : "return confirm(\u0027¿Cancelar este turno? Si pagaste una seña, se te devuelve.\u0027);" %>' />
                                <asp:Label ID="FueraDePlazo" runat="server" CssClass="form-note"
                                    Text="Faltan menos de 24 hs: para cancelarlo o reprogramarlo comunicate con la clínica."
                                    Visible='<%# FueraDePlazoParaPaciente(Eval("PacienteId"), Eval("Estado"), Eval("FechaHora")) %>' />
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
        <p class="form-note">Los pacientes pueden cancelar o reprogramar sus turnos hasta 24 horas antes. Cancelar un turno libera su horario. El estado de una seña pagada no implica un reembolso; los cobros y reembolsos se gestionarán en la etapa de Caja.</p>
    </section>
    </asp:Panel>

    <script type="text/javascript">
        (function () {
            function getPanelSolicitud() {
                return document.getElementById('<%= SolicitudPanel.ClientID %>');
            }
            function getPanelListado() {
                return document.getElementById('<%= ListadoPanel.ClientID %>');
            }
            function getVistaActivaInput() {
                return document.getElementById('<%= VistaActiva.ClientID %>');
            }

            function setVista(vista, actualizarHash) {
                var panelSolicitud = getPanelSolicitud();
                var panelListado = getPanelListado();
                if (!panelSolicitud || !panelListado) return;

                var esSolicitar = vista === 'solicitar';

                panelSolicitud.classList.toggle('is-hidden', !esSolicitar);
                panelListado.classList.toggle('is-hidden', esSolicitar);

                var btnSolicitud = document.getElementById('tabBtnSolicitud');
                var btnListado = document.getElementById('tabBtnListado');

                if (btnSolicitud) {
                    btnSolicitud.classList.toggle('is-active', esSolicitar);
                    btnSolicitud.setAttribute('aria-selected', esSolicitar ? 'true' : 'false');
                }
                if (btnListado) {
                    btnListado.classList.toggle('is-active', !esSolicitar);
                    btnListado.setAttribute('aria-selected', !esSolicitar ? 'true' : 'false');
                }

                var vistaActivaInput = getVistaActivaInput();
                if (vistaActivaInput) {
                    vistaActivaInput.value = esSolicitar ? 'solicitar' : 'listado';
                }

                if (esSolicitar && typeof window.actualizarWizardTurnos === 'function') {
                    window.actualizarWizardTurnos();
                }

                if (actualizarHash) {
                    var targetHash = esSolicitar ? '#solicitar-turno-title' : '#mis-turnos-title';
                    try {
                        history.replaceState(null, '', targetHash);
                    } catch (e) {
                        window.location.hash = targetHash;
                    }
                    if (typeof window.highlightSidebarActiveLink === 'function') {
                        window.highlightSidebarActiveLink();
                    }
                }
            }

            function resolverVistaDesdeHash() {
                var hash = (window.location.hash || '').toLowerCase();
                if (hash === '#solicitar-turno-title' || hash === '#solicitar-turno') {
                    setVista('solicitar', false);
                } else if (hash === '#mis-turnos-title' || hash === '#mis-turnos') {
                    setVista('listado', false);
                } else {
                    var input = getVistaActivaInput();
                    var vistaVal = input ? input.value : 'listado';
                    setVista(vistaVal || 'listado', false);
                }
            }

            function initTurnosTabs() {
                var btnListado = document.getElementById('tabBtnListado');
                var btnSolicitud = document.getElementById('tabBtnSolicitud');

                if (btnListado) {
                    btnListado.addEventListener('click', function () {
                        setVista('listado', true);
                    });
                }

                if (btnSolicitud) {
                    btnSolicitud.addEventListener('click', function () {
                        setVista('solicitar', true);
                    });
                }

                window.addEventListener('hashchange', resolverVistaDesdeHash);
                window.addEventListener('popstate', resolverVistaDesdeHash);

                resolverVistaDesdeHash();
            }

            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', initTurnosTabs);
            } else {
                initTurnosTabs();
            }
        }());
    </script>
</asp:Content>
