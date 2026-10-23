<%@ Page Title="Mi Consultorio" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Consultorio.aspx.cs" Inherits="MediStack.Web.ConsultorioPagina" %>
<asp:Content ID="ConsultorioContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="page-heading" aria-labelledby="consultorio-title">
        <p class="eyebrow">Atención Médica</p>
        <h1 id="consultorio-title">Mi Consultorio</h1>
        <p>Espacio de trabajo operativo médico: gestioná la fila de atención del día, llamá a pacientes en sala de espera, consultá antecedentes clínicos y registrá la evolución y diagnóstico obligatorios para cerrar la jornada.</p>
    </section>

    <asp:Label ID="Mensaje" runat="server" Visible="false" role="status" />

    <!-- Métricas del Consultorio -->
    <section class="recepcion-stats" aria-label="Métricas de atención de la jornada">
        <div class="recepcion-stat-card stat-espera">
            <span class="recepcion-stat-num"><asp:Literal ID="LitEnEspera" runat="server">0</asp:Literal></span>
            <span class="recepcion-stat-label">En Sala de Espera</span>
        </div>
        <div class="recepcion-stat-card stat-total">
            <span class="recepcion-stat-num"><asp:Literal ID="LitCitadosHoy" runat="server">0</asp:Literal></span>
            <span class="recepcion-stat-label">Citados de la Fecha</span>
        </div>
        <div class="recepcion-stat-card stat-atendidos">
            <span class="recepcion-stat-num"><asp:Literal ID="LitAtendidosHoy" runat="server">0</asp:Literal></span>
            <span class="recepcion-stat-label">Atendidos Hoy</span>
        </div>
        <div class="recepcion-stat-card stat-pendiente-evolucion">
            <span class="recepcion-stat-num"><asp:Literal ID="LitEvolucionesPendientes" runat="server">0</asp:Literal></span>
            <span class="recepcion-stat-label">Evolución Pendiente</span>
        </div>
    </section>

    <!-- Panel de Consultorio y Barra de Trabajo -->
    <section class="panel management-panel" aria-labelledby="consultorio-agenda-title">
        <div class="section-heading-row">
            <div>
                <h2 id="consultorio-agenda-title" class="section-title">
                    <asp:Literal ID="LitTituloConsultorio" runat="server">Pacientes Citados para Hoy</asp:Literal>
                </h2>
                <p class="form-note">
                    <asp:Literal ID="LitResumenConsultorio" runat="server" />
                </p>
            </div>
            <div>
                <asp:HyperLink ID="LinkAgendaCompleta" runat="server" CssClass="button button-light"
                    NavigateUrl="~/Agenda.aspx" Text="Ver agenda semanal" />
            </div>
        </div>

        <div class="recepcion-toolbar">
            <asp:Panel ID="PanelSelectorProfesional" runat="server" CssClass="form-group" style="flex: 1 1 200px; margin-bottom: 0;" Visible="false">
                <asp:Label ID="FiltroProfesionalLabel" runat="server" AssociatedControlID="FiltroProfesional" Text="Profesional a Consultar" />
                <asp:DropDownList ID="FiltroProfesional" runat="server" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="FiltroProfesional_SelectedIndexChanged" />
            </asp:Panel>

            <div class="form-group" style="flex: 1 1 140px; margin-bottom: 0;">
                <asp:Label ID="FiltroFechaLabel" runat="server" AssociatedControlID="FiltroFecha" Text="Fecha" />
                <asp:TextBox ID="FiltroFecha" runat="server" TextMode="Date" CssClass="form-control" AutoPostBack="true" OnTextChanged="FiltroFecha_TextChanged" />
            </div>

            <div class="form-group" style="flex: 2 1 200px; margin-bottom: 0;">
                <asp:Label ID="FiltroBusquedaLabel" runat="server" AssociatedControlID="FiltroBusqueda" Text="Buscar Paciente o DNI" />
                <asp:TextBox ID="FiltroBusqueda" runat="server" CssClass="form-control" placeholder="Ej: 38456123 o Pérez" MaxLength="100" />
            </div>

            <div class="form-group" style="flex: 1 1 170px; margin-bottom: 0;">
                <asp:Label ID="FiltroEstadoLabel" runat="server" AssociatedControlID="FiltroEstado" Text="Estado" />
                <asp:DropDownList ID="FiltroEstado" runat="server" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="Filtro_SelectedIndexChanged">
                    <asp:ListItem Text="Todos los estados" Value="Todos" />
                    <asp:ListItem Text="En sala de espera" Value="EnEspera" />
                    <asp:ListItem Text="Citados (sin ingreso)" Value="Confirmado" />
                    <asp:ListItem Text="Atendidos (completo)" Value="AtendidoConEvolucion" />
                    <asp:ListItem Text="Atendidos (sin evolución)" Value="AtendidoSinEvolucion" />
                    <asp:ListItem Text="Ausentes" Value="Ausente" />
                </asp:DropDownList>
            </div>

            <div style="display: flex; gap: 8px;">
                <asp:Button ID="BtnBuscar" runat="server" Text="Buscar" CssClass="button button-primary"
                    OnClick="BtnBuscar_Click" CausesValidation="false" />
                <asp:Button ID="BtnLimpiar" runat="server" Text="Limpiar" CssClass="button button-light"
                    OnClick="BtnLimpiar_Click" CausesValidation="false" />
            </div>
        </div>

        <!-- Tabla Operativa de Turnos del Consultorio -->
        <div class="table-wrap">
            <asp:GridView ID="ConsultorioGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" DataKeyNames="TurnoId,PacienteId,Estado" OnRowCommand="ConsultorioGrid_RowCommand"
                EmptyDataText="No hay turnos para este profesional en la fecha seleccionada.">
                <Columns>
                    <asp:TemplateField HeaderText="Horario">
                        <ItemTemplate>
                            <span class="hora-destacada"><%#: Convert.ToDateTime(Eval("FechaHora")).ToString("HH:mm") %> hs</span>
                            <%# Eval("FechaHoraLlegada") != null ? "<span class='llegada-time'>Llegó " + Convert.ToDateTime(Eval("FechaHoraLlegada")).ToString("HH:mm") + " hs</span>" : "" %>
                        </ItemTemplate>
                    </asp:TemplateField>

                    <asp:TemplateField HeaderText="Paciente">
                        <ItemTemplate>
                            <strong><%#: Eval("NombrePaciente") %></strong>
                            <div style="font-size: 0.82rem; color: #64748b;">
                                DNI: <span><%#: Eval("NumeroDocumento") ?? "—" %></span> · 
                                OS: <span><%#: Eval("ObraSocial") ?? "Particular" %></span>
                                <%# !string.IsNullOrWhiteSpace(Convert.ToString(Eval("NumeroAfiliado"))) ? " (" + HttpUtility.HtmlEncode(Convert.ToString(Eval("NumeroAfiliado"))) + ")" : "" %>
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>

                    <asp:TemplateField HeaderText="Especialidad / Motivo">
                        <ItemTemplate>
                            <div><strong><%#: Eval("Especialidad") %></strong></div>
                            <%# !string.IsNullOrWhiteSpace(Convert.ToString(Eval("MotivoTurno"))) ? "<div style='font-size: 0.8rem; color: #64748b; font-style: italic;'>" + HttpUtility.HtmlEncode(Convert.ToString(Eval("MotivoTurno"))) + "</div>" : "<span style='color: #94a3b8;'>—</span>" %>
                        </ItemTemplate>
                    </asp:TemplateField>

                    <asp:TemplateField HeaderText="Estado">
                        <ItemTemplate>
                            <span class="status-tag status-turno" data-estado='<%#: Eval("Estado") %>'><%#: Eval("Estado") %></span>
                            <%# Convert.ToBoolean(Eval("RequiereEvolucion")) ? "<div style='margin-top: 4px;'><span class='badge-pendiente-evolucion'>Evolución pendiente</span></div>" : "" %>
                            <%# Convert.ToBoolean(Eval("TieneEvolucion")) ? "<div style='margin-top: 4px;'><span class='badge-con-evolucion'>Evolucionado</span></div>" : "" %>
                        </ItemTemplate>
                    </asp:TemplateField>

                    <asp:TemplateField HeaderText="Evolución / Diagnóstico">
                        <ItemTemplate>
                            <%# !string.IsNullOrWhiteSpace(Convert.ToString(Eval("Diagnostico"))) 
                                ? "<div style='font-size: 0.85rem; max-width: 250px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;' title='" + HttpUtility.HtmlEncode(Convert.ToString(Eval("Diagnostico"))) + "'>" + HttpUtility.HtmlEncode(Convert.ToString(Eval("Diagnostico"))) + "</div>" 
                                : "<span style='color: #94a3b8; font-style: italic;'>Sin registro clínico</span>" %>
                        </ItemTemplate>
                    </asp:TemplateField>

                    <asp:TemplateField HeaderText="Acciones">
                        <ItemTemplate>
                            <div class="row-actions" style="display: flex; gap: 6px; align-items: center;">
                                <asp:Button runat="server" Text="Llamar / Atender" CssClass="button button-primary button-sm"
                                    CommandName="Atender" CommandArgument='<%# Eval("TurnoId") %>' CausesValidation="false"
                                    Visible='<%# Convert.ToString(Eval("Estado")) == "EnEspera" || Convert.ToString(Eval("Estado")) == "Confirmado" %>' />

                                <asp:Button runat="server" Text="Cargar Evolución" CssClass="button button-warning button-sm"
                                    CommandName="Atender" CommandArgument='<%# Eval("TurnoId") %>' CausesValidation="false"
                                    Visible='<%# Convert.ToBoolean(Eval("RequiereEvolucion")) %>' />

                                <asp:Button runat="server" Text="Ver / Editar" CssClass="button button-light button-sm"
                                    CommandName="Atender" CommandArgument='<%# Eval("TurnoId") %>' CausesValidation="false"
                                    Visible='<%# Convert.ToString(Eval("Estado")) == "Atendido" && Convert.ToBoolean(Eval("TieneEvolucion")) %>' />

                                <asp:Button runat="server" Text="Ficha Rápida" CssClass="button button-light button-sm"
                                    CommandName="VerFicha" CommandArgument='<%# Eval("PacienteId") %>' CausesValidation="false" />
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </section>

    <!-- Modal de Atención Médica, Evolución y Resumen Clínico -->
    <asp:Panel ID="ModalAtencionPanel" runat="server" Visible="false" CssClass="consultorio-modal-backdrop">
        <div class="consultorio-modal-card">
            <div class="consultorio-modal-header">
                <div>
                    <h2 class="consultorio-modal-title">
                        <asp:Literal ID="ModalTitulo" runat="server">Atención en Consultorio</asp:Literal>
                    </h2>
                    <p class="consultorio-modal-sub">
                        Paciente: <strong><asp:Literal ID="ModalPacienteNombre" runat="server" /></strong> · 
                        DNI: <span><asp:Literal ID="ModalPacienteDni" runat="server" /></span> · 
                        Horario: <span><asp:Literal ID="ModalHorarioTurno" runat="server" /></span>
                    </p>
                </div>
                <asp:LinkButton ID="BtnCerrarModalCruz" runat="server" CssClass="consultorio-modal-close" OnClick="BtnCerrarModal_Click" CausesValidation="false" Text="&times;" />
            </div>

            <asp:Label ID="ModalMensajeError" runat="server" Visible="false" CssClass="alert alert-error" style="margin: 12px 20px 0 20px;" />

            <div class="consultorio-modal-body">
                <!-- Columna Izquierda: Ficha y Antecedentes Rápidos -->
                <div class="consultorio-modal-col-resumen">
                    <h3 class="modal-section-title">Antecedentes Clínicos</h3>
                    
                    <div class="paciente-resumen-box">
                        <div><strong>Edad:</strong> <asp:Literal ID="ModalPacienteEdad" runat="server" /> años</div>
                        <div><strong>Teléfono:</strong> <asp:Literal ID="ModalPacienteTelefono" runat="server" /></div>
                        <div><strong>Obra Social:</strong> <asp:Literal ID="ModalPacienteObraSocial" runat="server" /></div>
                        <div><strong>Nº Afiliado:</strong> <asp:Literal ID="ModalPacienteAfiliado" runat="server" /></div>
                        <div><strong>Emergencias:</strong> <asp:Literal ID="ModalPacienteEmergencia" runat="server" /></div>
                    </div>

                    <h4 style="font-size: 0.88rem; font-weight: 600; margin: 12px 0 6px 0; color: #1e293b;">Últimas Consultas Registradas:</h4>
                    <div class="antecedentes-list-wrap">
                        <asp:Repeater ID="ModalAntecedentesRepeater" runat="server">
                            <ItemTemplate>
                                <div class="antecedente-item">
                                    <div class="antecedente-head">
                                        <span class="antecedente-fecha"><%#: Convert.ToDateTime(Eval("Fecha")).ToString("dd/MM/yyyy HH:mm") %></span>
                                        <span class="antecedente-prof"><%#: Eval("Profesional") %> (<%#: Eval("Especialidad") %>)</span>
                                    </div>
                                    <div class="antecedente-diag">
                                        <strong>Diagnóstico:</strong> <%#: Eval("Diagnostico") ?? "—" %>
                                    </div>
                                    <%# !string.IsNullOrWhiteSpace(Convert.ToString(Eval("Observaciones"))) ? "<div class='antecedente-obs'>" + HttpUtility.HtmlEncode(Convert.ToString(Eval("Observaciones"))) + "</div>" : "" %>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                        <asp:Panel ID="PanelSinAntecedentes" runat="server" Visible="false">
                            <p style="font-size: 0.82rem; color: #94a3b8; font-style: italic; margin: 8px 0;">No se registran antecedentes previos en el sistema.</p>
                        </asp:Panel>
                    </div>

                    <div style="margin-top: 14px; text-align: center;">
                        <asp:HyperLink ID="ModalLinkFichaCompleta" runat="server" Target="_blank"
                            CssClass="button button-light button-sm" Text="Abrir Historia Clínica Integral ↗" />
                    </div>
                </div>

                <!-- Columna Derecha: Formulario de Atención y Evolución -->
                <div class="consultorio-modal-col-form">
                    <h3 class="modal-section-title">Registro de la Consulta Actual</h3>

                    <asp:HiddenField ID="ModalTurnoId" runat="server" />
                    <asp:HiddenField ID="ModalPacienteId" runat="server" />

                    <div class="form-group">
                        <asp:Label ID="ModalMotivoLabel" runat="server" AssociatedControlID="ModalMotivoConsulta" Text="Motivo de Consulta" />
                        <asp:TextBox ID="ModalMotivoConsulta" runat="server" CssClass="form-control" MaxLength="500" placeholder="Motivo o causa manifestada por el paciente" />
                    </div>

                    <div class="form-group">
                        <asp:Label ID="ModalDiagnosticoLabel" runat="server" AssociatedControlID="ModalDiagnostico" Text="Diagnóstico Clínico (Obligatorio)" />
                        <asp:TextBox ID="ModalDiagnostico" runat="server" TextMode="MultiLine" Rows="3" CssClass="form-control" placeholder="Describí el diagnóstico o presunción clínica de la atención..." />
                    </div>

                    <div class="form-group">
                        <asp:Label ID="ModalObservacionesLabel" runat="server" AssociatedControlID="ModalObservaciones" Text="Observaciones, Tratamiento e Indicaciones" />
                        <asp:TextBox ID="ModalObservaciones" runat="server" TextMode="MultiLine" Rows="5" CssClass="form-control" placeholder="Plan terapéutico, prescripciones, estudios solicitados o notas internas..." />
                    </div>

                    <div class="consultorio-modal-actions">
                        <asp:Button ID="BtnGuardarAtencion" runat="server" Text="Finalizar Atención y Guardar Evolución"
                            CssClass="button button-primary" OnClick="BtnGuardarAtencion_Click" CausesValidation="false" />
                        <asp:Button ID="BtnCerrarModal" runat="server" Text="Cerrar sin guardar"
                            CssClass="button button-light" OnClick="BtnCerrarModal_Click" CausesValidation="false" />
                    </div>
                </div>
            </div>
        </div>
    </asp:Panel>
</asp:Content>
