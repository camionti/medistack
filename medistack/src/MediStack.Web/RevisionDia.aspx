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

                    <asp:TemplateField HeaderText="Pagos / Caja">
                        <ItemTemplate>
                            <div style="display: flex; flex-direction: column; gap: 4px; align-items: flex-start;">
                                <%# RenderBadgePagos(Eval("Estado"), Eval("EstadoSena"), Eval("MontoSena"), Eval("ConsultaCobrada")) %>
                                <div style="display: flex; gap: 4px; flex-wrap: wrap; margin-top: 2px;">
                                    <asp:Button runat="server" Text="Cobrar seña" CssClass="button button-secondary button-sm"
                                        CommandName="AbrirCobroSena" CommandArgument='<%# Eval("TurnoId") %>'
                                        Visible='<%# Convert.ToString(Eval("EstadoSena")) == "Pendiente" && Convert.ToDecimal(Eval("MontoSena")) > 0 && Convert.ToString(Eval("Estado")) != "Cancelado" %>' />

                                    <asp:Button runat="server" Text="Cobrar consulta" CssClass="button button-primary button-sm"
                                        CommandName="AbrirCobroConsulta" CommandArgument='<%# Eval("TurnoId") %>'
                                        Visible='<%# (Convert.ToString(Eval("Estado")) == "EnEspera" || Convert.ToString(Eval("Estado")) == "Atendido") && !Convert.ToBoolean(Eval("ConsultaCobrada")) %>' />

                                    <asp:Button runat="server" Text="Recibo PDF" CssClass="button button-light button-sm"
                                        CommandName="DescargarComprobante" CommandArgument='<%# Eval("UltimoCobroId") %>'
                                        Visible='<%# Eval("UltimoCobroId") != DBNull.Value %>' CausesValidation="false" />
                                </div>
                            </div>
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
    <%-- Modal de Cobro Rápido en Mostrador --%>
    <asp:Panel ID="ModalCobro" runat="server" CssClass="cobro-modal-backdrop" Visible="false">
        <div class="cobro-modal-card">
            <div class="cobro-modal-header">
                <div>
                    <h3 class="cobro-modal-title">Cobro en Mostrador</h3>
                    <p class="cobro-modal-sub"><asp:Literal ID="LitCobroSubtitulo" runat="server" /></p>
                </div>
                <asp:Button ID="BtnCerrarModalCobro" runat="server" Text="X" CssClass="cobro-modal-close"
                    OnClick="BtnCerrarModalCobro_Click" CausesValidation="false" />
            </div>

            <asp:Label ID="MensajeModalCobro" runat="server" Visible="false" role="status" />

            <div class="cobro-modal-body">
                <asp:HiddenField ID="HdnCobroTurnoId" runat="server" />
                <asp:HiddenField ID="HdnMontoCobrado" runat="server" />

                <div class="form-grid">
                    <div class="form-group">
                        <asp:Label ID="LblCobroTipo" runat="server" AssociatedControlID="DdlCobroTipo" Text="Concepto a cobrar" />
                        <asp:DropDownList ID="DdlCobroTipo" runat="server" CssClass="form-control" AutoPostBack="true"
                            OnSelectedIndexChanged="DdlCobroTipo_SelectedIndexChanged" />
                    </div>

                    <div class="form-group">
                        <asp:Label ID="LblCobroMedioPago" runat="server" AssociatedControlID="DdlCobroMedioPago" Text="Medio de pago" />
                        <asp:DropDownList ID="DdlCobroMedioPago" runat="server" CssClass="form-control" onchange="actualizarMedioPago()">
                            <asp:ListItem Text="Efectivo" Value="Efectivo" />
                            <asp:ListItem Text="Tarjeta" Value="Tarjeta" />
                            <asp:ListItem Text="Transferencia" Value="Transferencia" />
                        </asp:DropDownList>
                    </div>
                </div>

                <div class="cobro-desglose-panel">
                    <h4 class="modal-section-title" style="margin-bottom: 0.5rem;">Liquidación Arancelaria</h4>
                    <div class="cobro-desglose-grid">
                        <asp:Panel ID="PnlDesgloseConsulta" runat="server">
                            <div class="desglose-row">
                                <span>Valor Consulta Base:</span>
                                <strong><asp:Literal ID="LitCobroValorBase" runat="server">$ 0,00</asp:Literal></strong>
                            </div>
                            <div class="desglose-row">
                                <span>Cobertura Obra Social:</span>
                                <strong style="color: #059669;">- <asp:Literal ID="LitCobroMontoOS" runat="server">$ 0,00</asp:Literal></strong>
                            </div>
                            <div class="desglose-row">
                                <span>Copago Paciente:</span>
                                <strong><asp:Literal ID="LitCobroCopago" runat="server">$ 0,00</asp:Literal></strong>
                            </div>
                            <asp:Panel ID="PnlCobroSenaDescontada" runat="server" CssClass="desglose-row" Visible="false">
                                <span>SeÃ±a Previa Abonada:</span>
                                <strong style="color: #0284c7;">- <asp:Literal ID="LitCobroSenaDescontada" runat="server">$ 0,00</asp:Literal></strong>
                            </asp:Panel>
                        </asp:Panel>

                        <div class="desglose-row desglose-total">
                            <span>TOTAL A COBRAR:</span>
                            <span class="total-monto"><asp:Literal ID="LitCobroTotalCobrar" runat="server">$ 0,00</asp:Literal></span>
                        </div>
                    </div>
                </div>

                <div id="pnlCuentaDevolucion" class="form-group" style="display: none;">
                    <asp:Label ID="LblCuentaDevolucion" runat="server" AssociatedControlID="TxtCuentaDevolucion"
                        Text="CBU/CVU o alias de la cuenta del paciente (para devoluciÃ³n si cancela)" />
                    <asp:TextBox ID="TxtCuentaDevolucion" runat="server" CssClass="form-control" MaxLength="30"
                        placeholder="Ej: 0000003100012345678901 o alias.mp" />
                </div>

                <div class="form-grid">
                    <div class="form-group">
                        <asp:Label ID="LblCobroMontoRecibido" runat="server" AssociatedControlID="TxtCobroMontoRecibido" Text="Importe recibido ($)" />
                        <asp:TextBox ID="TxtCobroMontoRecibido" runat="server" CssClass="form-control" TextMode="Number"
                            min="0" max="9999999999.99" step="0.01" inputmode="decimal" oninput="calcularVuelto()" />
                    </div>
                    <div class="form-group" style="display: flex; flex-direction: column; justify-content: flex-end;">
                        <div id="vueltoContainer" class="vuelto-badge-box">
                            <span class="vuelto-label">Vuelto a entregar:</span>
                            <span id="lblVueltoValor" class="vuelto-valor">$ 0,00</span>
                        </div>
                    </div>
                </div>

                <asp:Panel ID="PnlCobroExito" runat="server" Visible="false" CssClass="cobro-exito-box">
                    <div>
                        <strong style="color: #065f46; display: block;">Â¡Cobro registrado exitosamente!</strong>
                        <span style="font-size: 0.85rem; color: #047857;">La atenciÃ³n mÃ©dica quedÃ³ habilitada.</span>
                    </div>
                    <asp:Button ID="BtnDescargarPdfModal" runat="server" Text="Descargar Comprobante PDF"
                        CssClass="button button-primary button-sm" OnClick="BtnDescargarPdfModal_Click" CausesValidation="false" />
                </asp:Panel>
            </div>

            <div class="cobro-modal-footer">
                <asp:Button ID="BtnConfirmarCobro" runat="server" Text="Registrar Cobro"
                    CssClass="button button-primary" OnClick="BtnConfirmarCobro_Click" />
                <asp:Button ID="BtnCancelarCobro" runat="server" Text="Cerrar"
                    CssClass="button button-light" OnClick="BtnCerrarModalCobro_Click" CausesValidation="false" />
            </div>
        </div>
    </asp:Panel>

    <script type="text/javascript">
        function actualizarMedioPago() {
            var ddlMedio = document.getElementById('<%= DdlCobroMedioPago.ClientID %>');
            var ddlTipo = document.getElementById('<%= DdlCobroTipo.ClientID %>');
            var pnlCuenta = document.getElementById('pnlCuentaDevolucion');
            var txtRecibido = document.getElementById('<%= TxtCobroMontoRecibido.ClientID %>');
            var hdnTotal = document.getElementById('<%= HdnMontoCobrado.ClientID %>');
            
            if (!ddlMedio || !txtRecibido || !hdnTotal) return;
            
            var medio = ddlMedio.value;
            var tipo = ddlTipo ? ddlTipo.value : '';
            var total = parseFloat(hdnTotal.value) || 0;
            
            if (pnlCuenta) {
                pnlCuenta.style.display = (tipo === 'Sena' && (medio === 'Tarjeta' || medio === 'Transferencia')) ? 'block' : 'none';
            }
            
            if (medio !== 'Efectivo') {
                txtRecibido.value = total.toFixed(2);
                txtRecibido.readOnly = true;
            } else {
                txtRecibido.readOnly = false;
            }
            calcularVuelto();
        }

        function calcularVuelto() {
            var txtRecibido = document.getElementById('<%= TxtCobroMontoRecibido.ClientID %>');
            var hdnTotal = document.getElementById('<%= HdnMontoCobrado.ClientID %>');
            var lblVuelto = document.getElementById('lblVueltoValor');
            var container = document.getElementById('vueltoContainer');
            
            if (!txtRecibido || !hdnTotal || !lblVuelto) return;
            
            var total = parseFloat(hdnTotal.value) || 0;
            var recibido = parseFloat(txtRecibido.value) || 0;
            var vuelto = recibido - total;
            
            if (recibido >= total) {
                lblVuelto.innerText = '$ ' + vuelto.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
                if (container) {
                    container.className = 'vuelto-badge-box vuelto-ok';
                }
            } else {
                lblVuelto.innerText = 'Faltan $ ' + (total - recibido).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
                if (container) {
                    container.className = 'vuelto-badge-box vuelto-insuficiente';
                }
            }
        }
    </script>
</asp:Content>
