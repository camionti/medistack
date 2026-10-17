using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;
using MediStack.Dominio;
using MediStack.Negocio;

namespace MediStack.Web
{
    public partial class TurnosPagina : PaginaProtegida
    {
        private readonly TurnosNegocio _negocio = new TurnosNegocio();

        protected bool EsPaciente
        {
            get { return TieneRol("PACIENTE"); }
        }

        protected bool EsAdministrativo
        {
            get { return TieneRol("ADMINISTRATIVO"); }
        }

        protected bool EsReprogramacion
        {
            get { return ViewState["TurnoAReprogramar"] != null; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!EsPaciente && !EsAdministrativo && !TieneRol("PROFESIONAL"))
            {
                Response.Redirect(ResolveUrl("~/Dashboard.aspx"), true);
            }

            SolicitudPanel.Visible = EsPaciente || EsAdministrativo;
            PacienteSelectorPanel.Visible = EsAdministrativo;
            if (TieneRol("PROFESIONAL"))
            {
                TituloListado.Text = "Turnos de mis pacientes";
            }
            else if (EsPaciente)
            {
                TituloListado.Text = "Mis turnos";
            }
            else
            {
                TituloListado.Text = "Turnos de la clínica";
            }

            if (!IsPostBack)
            {
                string vista = (Request.QueryString["vista"] ?? string.Empty).ToLowerInvariant();
                string accion = (Request.QueryString["accion"] ?? string.Empty).ToLowerInvariant();
                int turnoId;
                bool tieneTurnoId = int.TryParse(Request.QueryString["turnoId"], out turnoId) && turnoId > 0;

                if (accion == "cancelar" || tieneTurnoId)
                {
                    if (VistaActiva != null) VistaActiva.Value = "listado";
                }
                else if (vista == "solicitar")
                {
                    if (VistaActiva != null) VistaActiva.Value = "solicitar";
                }
                else
                {
                    if (VistaActiva != null) VistaActiva.Value = "listado";
                }

                EjecutarConManejoDeErrores("No se pudieron cargar los turnos", () =>
                {
                    CargarOpcionesIniciales();
                    if (EsReprogramacion)
                    {
                        FechaDisponibilidad.Text = DateTime.Today.AddDays(1)
                            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }
                    CargarTurnos();
                    VerificarAccionUrl();
                });
            }

            ConfigurarPasosSolicitud();
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            ActualizarVisibilidadVistas();
        }

        private void ActualizarVisibilidadVistas()
        {
            if (TieneRol("PROFESIONAL"))
            {
                if (PhTurnosTabs != null) PhTurnosTabs.Visible = false;
                if (SolicitudPanel != null) SolicitudPanel.Visible = false;
                if (ListadoPanel != null) ListadoPanel.CssClass = "turnos-tab-panel";
                return;
            }

            bool tabsHabilitadas = EsPaciente || EsAdministrativo;
            if (PhTurnosTabs != null) PhTurnosTabs.Visible = tabsHabilitadas;

            if (tabsHabilitadas)
            {
                if (TabTituloListado != null)
                {
                    TabTituloListado.Text = EsPaciente ? "Mis turnos" : "Gestión de turnos";
                }
                if (TabTituloSolicitud != null)
                {
                    TabTituloSolicitud.Text = EsPaciente ? "Solicitar turno" : "Agendar turno";
                }

                bool mostrarSolicitud = string.Equals(VistaActiva != null ? VistaActiva.Value : string.Empty, "solicitar", StringComparison.OrdinalIgnoreCase);
                if (SolicitudPanel != null)
                {
                    SolicitudPanel.CssClass = mostrarSolicitud ? "turnos-tab-panel" : "turnos-tab-panel is-hidden";
                }
                if (ListadoPanel != null)
                {
                    ListadoPanel.CssClass = mostrarSolicitud ? "turnos-tab-panel is-hidden" : "turnos-tab-panel";
                }
            }
        }

        protected void Especialidad_SelectedIndexChanged(object sender, EventArgs e)
        {
            EjecutarConManejoDeErrores("No se pudieron cargar los profesionales", () =>
            {
                LimpiarMensaje();
                CargarProfesionales();
                MostrarDuracionSeleccionada();
                LimpiarDisponibilidad();
                FechasDisponiblesPanel.Visible = false;
                CargarResumenProfesionales();
                if (!EsReprogramacion)
                {
                    ExploradorDisponibilidadPanel.Visible = false;
                }
                if (EsReprogramacion && Profesional.Items.Count == 2)
                {
                    Profesional.SelectedIndex = 1;
                    ProponerProximaFecha();
                    CargarDisponibilidad();
                }
                else if (Profesional.Items.Count == 1 && Especialidad.SelectedValue.Length > 0)
                {
                    MostrarError("No hay profesionales activos que atiendan esta especialidad.");
                }
                else if (!EsReprogramacion && Especialidad.SelectedValue.Length > 0)
                {
                    int espId;
                    if (int.TryParse(Especialidad.SelectedValue, out espId) && espId > 0)
                    {
                        try
                        {
                            Guid pacId = ObtenerPacienteSeleccionado();
                            if (_negocio.TieneTurnoVigenteEspecialidad(pacId, espId, null))
                            {
                                string aviso = EsPaciente
                                    ? "Atención: Ya tenés un turno vigente para esta especialidad. Recordá que podés reprogramarlo o cancelarlo desde la sección 'Mis turnos'."
                                    : "Atención: El paciente ya cuenta con un turno vigente para esta especialidad.";
                                MostrarError(aviso);
                            }
                        }
                        catch
                        {
                            // Si todavía no se seleccionó paciente (ej. administrativo), no validar aún
                        }
                    }
                }
            });
        }

        protected void AvanzarEspecialidad_Click(object sender, EventArgs e)
        {
            LimpiarMensaje();
            if (string.IsNullOrWhiteSpace(Especialidad.SelectedValue))
            {
                MostrarError("Seleccioná una especialidad para avanzar.");
                return;
            }

            EjecutarConManejoDeErrores("No se pudo cargar la disponibilidad", () =>
            {
                CargarResumenProfesionales();
                ExploradorDisponibilidadPanel.Visible = true;
                FechasDisponiblesPanel.Visible = false;
                HorariosPanel.Visible = false;
                ConfirmacionSolicitudPanel.Visible = false;
            });
        }

        protected void Paciente_SelectedIndexChanged(object sender, EventArgs e)
        {
            EjecutarConManejoDeErrores("No se pudo cambiar el paciente", () =>
            {
                LimpiarMensaje();
                Especialidad.ClearSelection();
                CargarProfesionales();
                FechaDisponibilidad.Text = string.Empty;
                Motivo.Text = string.Empty;
                LimpiarDisponibilidad();
                FechasDisponiblesPanel.Visible = false;
                CargarResumenProfesionales();
                ExploradorDisponibilidadPanel.Visible = false;
                ConfigurarPasosSolicitud();
            });
        }

        protected void Profesional_SelectedIndexChanged(object sender, EventArgs e)
        {
            EjecutarConManejoDeErrores("No se pudo consultar la disponibilidad", () =>
            {
                LimpiarMensaje();
                LimpiarDisponibilidad();
                if (Profesional.SelectedValue.Length > 0)
                {
                    if (EsReprogramacion)
                    {
                        ProponerProximaFecha();
                        CargarDisponibilidad();
                    }
                }
            });
        }

        protected void ProfesionalesDisponibilidadGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "ElegirProfesional")
            {
                return;
            }

            int indice;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out indice)
                || indice < 0 || indice >= ProfesionalesDisponibilidadGrid.DataKeys.Count)
            {
                return;
            }

            try
            {
                DataKey datos = ProfesionalesDisponibilidadGrid.DataKeys[indice];
                if (!Convert.ToBoolean(datos.Values["TieneDisponibilidad"]))
                {
                    return;
                }

                Guid profesionalId = Guid.Parse(Convert.ToString(datos.Values["ProfesionalId"]));
                ListItem profesional = Profesional.Items.FindByValue(profesionalId.ToString());
                if (profesional == null)
                {
                    Profesional.Items.Add(new ListItem(
                        Convert.ToString(ProfesionalesDisponibilidadGrid.Rows[indice].Cells[0].Text),
                        profesionalId.ToString()));
                }

                Profesional.ClearSelection();
                Profesional.SelectedValue = profesionalId.ToString();
                CargarFechasProfesional(profesionalId);
                ExploradorDisponibilidadPanel.Visible = false;
                FechasDisponiblesPanel.Visible = true;
                HorariosPanel.Visible = false;
                DisponibilidadGrid.DataSource = null;
                DisponibilidadGrid.DataBind();
                ConfirmacionSolicitudPanel.Visible = false;
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(ex.Message);
            }
            catch (SqlException ex)
            {
                MostrarError("No se pudieron cargar las fechas (error SQL " + ex.Number + ").");
            }
        }

        protected void FechasDisponiblesGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "ElegirFecha")
            {
                return;
            }

            int indice;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out indice)
                || indice < 0 || indice >= FechasDisponiblesGrid.DataKeys.Count)
            {
                return;
            }

            DataKey datos = FechasDisponiblesGrid.DataKeys[indice];
            DateTime fecha = Convert.ToDateTime(datos.Values["Fecha"]);
            FechaDisponibilidad.Text = fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            CargarDisponibilidad();
            FechasDisponiblesPanel.Visible = false;
            HorariosPanel.Visible = true;
            ConfirmacionSolicitudPanel.Visible = false;
        }

        protected void VolverProfesionales_Click(object sender, EventArgs e)
        {
            FechasDisponiblesPanel.Visible = false;
            ExploradorDisponibilidadPanel.Visible = true;
            HorariosPanel.Visible = false;
            LimpiarDisponibilidad();
            LimpiarSeleccionHorario();
        }

        protected void VolverFechas_Click(object sender, EventArgs e)
        {
            HorariosPanel.Visible = false;
            FechasDisponiblesPanel.Visible = true;
            ConfirmacionSolicitudPanel.Visible = false;
            LimpiarSeleccionHorario();
        }

        protected void VolverHorarios_Click(object sender, EventArgs e)
        {
            ConfirmacionSolicitudPanel.Visible = false;
            HorariosPanel.Visible = true;
            MotivoPanel.Visible = false;
        }

        protected void ConsultarDisponibilidad_Click(object sender, EventArgs e)
        {
            LimpiarMensaje();
            EjecutarConManejoDeErrores("No se pudo consultar la disponibilidad", CargarDisponibilidad);
        }

        protected void Filtrar_Click(object sender, EventArgs e)
        {
            if (VistaActiva != null) VistaActiva.Value = "listado";
            EjecutarConManejoDeErrores("No se pudieron filtrar los turnos", CargarTurnos);
        }

        protected void LimpiarFiltro_Click(object sender, EventArgs e)
        {
            if (VistaActiva != null) VistaActiva.Value = "listado";
            Desde.Text = string.Empty;
            Hasta.Text = string.Empty;
            EjecutarConManejoDeErrores("No se pudieron cargar los turnos", CargarTurnos);
        }

        protected void btnExportarCsv_Click(object sender, EventArgs e)
        {
            EjecutarConManejoDeErrores("No se pudo exportar a CSV", () =>
            {
                string subtitulo;
                DataTable tabla = ObtenerTablaTurnosParaExportar(out subtitulo);
                if (tabla == null) return;
                ExportadorUtil.ExportarCSV(tabla, "Turnos_MediStack_" + DateTime.Now.ToString("yyyyMMdd_HHmm"));
            });
        }

        protected void btnExportarPdf_Click(object sender, EventArgs e)
        {
            EjecutarConManejoDeErrores("No se pudo exportar a PDF", () =>
            {
                string subtitulo;
                DataTable tabla = ObtenerTablaTurnosParaExportar(out subtitulo);
                if (tabla == null) return;
                ExportadorUtil.ExportarPDF(tabla, "Turnos_MediStack_" + DateTime.Now.ToString("yyyyMMdd_HHmm"), TituloListado.Text, subtitulo);
            });
        }

        private DataTable ObtenerTablaTurnosParaExportar(out string subtitulo)
        {
            subtitulo = string.Empty;
            DateTime? desde;
            DateTime? hasta;
            string errorFiltro;
            if (!TryLeerFiltroFechas(Desde, Hasta, out desde, out hasta, out errorFiltro))
            {
                MostrarError(errorFiltro);
                return null;
            }

            Guid? pacienteId = EsPaciente ? (Guid?)ObtenerUsuarioActual() : null;
            Guid? profesionalId = TieneRol("PROFESIONAL") ? (Guid?)ObtenerUsuarioActual() : null;
            DataTable datosCrudos = _negocio.ObtenerTurnos(pacienteId, profesionalId, desde, hasta);
            subtitulo = DescribirFiltro(desde, hasta, datosCrudos.Rows.Count, "turno");

            var columnas = new Dictionary<string, string>
            {
                { "FechaHora", "Fecha y hora" },
                { "Paciente", "Paciente" },
                { "Profesional", "Profesional" },
                { "Especialidad", "Especialidad" },
                { "DuracionEstandarMinutos", "Duración (min)" },
                { "Motivo", "Motivo" },
                { "Estado", "Estado" },
                { "EstadoSena", "Estado seña" },
                { "MontoSena", "Monto seña" }
            };

            return ExportadorUtil.PrepararTablaParaReporte(datosCrudos, columnas);
        }
        protected void DisponibilidadGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "ElegirHorario")
            {
                return;
            }

            int indice;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out indice)
                || indice < 0 || indice >= DisponibilidadGrid.DataKeys.Count)
            {
                return;
            }

            try
            {
                DataKey horario = DisponibilidadGrid.DataKeys[indice];
                if (!Convert.ToBoolean(horario.Values["Disponible"]))
                {
                    CargarDisponibilidad();
                    MostrarError("Ese horario ya no está disponible. Consulta nuevamente la agenda.");
                    return;
                }

                Guid profesionalId = Guid.Parse(Convert.ToString(horario.Values["ProfesionalId"]));
                int especialidadId = Convert.ToInt32(horario.Values["EspecialidadId"]);
                DateTime fechaHora = Convert.ToDateTime(horario.Values["FechaHora"]);
                Guid pacienteId = ObtenerPacienteSeleccionado();

                if (!EsPropioPaciente(pacienteId) && !EsAdministrativo)
                {
                    MostrarError("No tienes permiso para solicitar un turno para otro paciente.");
                    return;
                }

                if (!EsReprogramacion && _negocio.TieneTurnoVigenteEspecialidad(pacienteId, especialidadId, null))
                {
                    CargarDisponibilidad();
                    string mensajeTurnoVigente = EsPaciente
                        ? "Ya tenés un turno vigente para esta especialidad. Recordá que solo podés tener un turno por especialidad a la vez; podés reprogramar o cancelar tu turno actual desde la sección 'Mis turnos'."
                        : "El paciente ya cuenta con un turno vigente para esta especialidad.";
                    MostrarError(mensajeTurnoVigente);
                    return;
                }

                ViewState["SolicitudProfesionalId"] = profesionalId.ToString();
                ViewState["SolicitudEspecialidadId"] = especialidadId;
                ViewState["SolicitudFechaHora"] = fechaHora;
                string profesional = Profesional.SelectedItem == null
                    ? "el profesional seleccionado" : Profesional.SelectedItem.Text;
                string especialidad = Especialidad.SelectedItem == null
                    ? "la especialidad seleccionada" : Especialidad.SelectedItem.Text;
                ResumenSolicitud.Text = HttpUtility.HtmlEncode(
                    "Vas a solicitar " + especialidad + " con " + profesional
                    + " el " + fechaHora.ToString("dddd dd/MM/yyyy 'a las' HH:mm", CultureInfo.GetCultureInfo("es-AR"))
                    + " hs. Duración: " + Convert.ToInt32(horario.Values["DuracionMinutos"]) + " minutos.");
                ConfirmacionSolicitudPanel.Visible = true;
                HorariosPanel.Visible = false;
                MotivoPanel.Visible = true;
                MostrarExito("Revisá los datos y confirmá el horario.");
            }
            catch (InvalidOperationException ex)
            {
                CargarDisponibilidad();
                MostrarError(ex.Message);
            }

            catch (SqlException ex)
            {
                CargarDisponibilidad();
                MostrarError("No se pudo guardar el turno (error SQL " + ex.Number + "). Actualiza la disponibilidad e intenta nuevamente.");
            }
        }

        protected void ConfirmarSolicitud_Click(object sender, EventArgs e)
        {
            try
            {
                Guid profesionalId;
                int especialidadId;
                DateTime fechaHora;
                if (!Guid.TryParse(Convert.ToString(ViewState["SolicitudProfesionalId"]), out profesionalId)
                    || !int.TryParse(Convert.ToString(ViewState["SolicitudEspecialidadId"]), out especialidadId)
                    || !DateTime.TryParse(Convert.ToString(ViewState["SolicitudFechaHora"]), out fechaHora))
                {
                    throw new InvalidOperationException("La selección de horario expiró. Volvé a consultar la disponibilidad.");
                }

                Guid pacienteId = ObtenerPacienteSeleccionado();
                SolicitudTurno solicitud = new SolicitudTurno
                {
                    PacienteId = pacienteId,
                    ProfesionalId = profesionalId,
                    EspecialidadId = especialidadId,
                    FechaHora = fechaHora,
                    Motivo = Motivo.Text,
                    EsReservaOnline = EsPaciente
                };

                if (EsReprogramacion)
                {
                    int turnoOriginalId = (int)ViewState["TurnoAReprogramar"];
                    TurnoDetalle original = _negocio.ObtenerTurno(turnoOriginalId);
                    if (original == null)
                    {
                        throw new InvalidOperationException("El turno a reprogramar ya no existe. Actualiza la pagina.");
                    }

                    solicitud.PacienteId = original.PacienteId;
                    _negocio.ReprogramarTurno(
                        turnoOriginalId, EsPaciente ? (Guid?)ObtenerUsuarioActual() : null, solicitud);
                    FinalizarReprogramacion();
                    MostrarExito("El turno se reprogramo correctamente. El turno anterior quedo cancelado.");
                }
                else
                {
                    ReservaTurno reserva = _negocio.SolicitarTurno(solicitud);
                    if (reserva.RequierePagoSena && EsPaciente)
                    {
                        Response.Redirect(ResolveUrl("~/PagarSena.aspx?turnoId=" + reserva.TurnoId), false);
                        Context.ApplicationInstance.CompleteRequest();
                        return;
                    }

                    if (reserva.RequierePagoSena)
                    {
                        MostrarExito("Reservamos tu turno por " + ReglasTurno.MinutosParaPagarSena
                            + " minutos. Para confirmarlo, pagá la seña de "
                            + reserva.MontoSena.ToString("C", CultureInfo.GetCultureInfo("es-AR"))
                            + "; si no, el horario se libera.");
                    }
                    else
                    {
                        MostrarExito("La solicitud de turno se registro correctamente.");
                        _negocio.EnviarConfirmacionTurnoAsync(reserva.TurnoId, ConfiguracionSitio.ObtenerUrlBase());
                    }
                }

                LimpiarSeleccionHorario();
                Motivo.Text = string.Empty;
                CargarTurnos();
                CargarResumenProfesionales();
                if (VistaActiva != null) VistaActiva.Value = "listado";
            }
            catch (InvalidOperationException ex)
            {
                if (VistaActiva != null) VistaActiva.Value = "solicitar";
                ConfirmacionSolicitudPanel.Visible = false;
                CargarDisponibilidad();
                MostrarError(ex.Message);
            }
            catch (SqlException ex)
            {
                if (VistaActiva != null) VistaActiva.Value = "solicitar";
                ConfirmacionSolicitudPanel.Visible = false;
                CargarDisponibilidad();
                MostrarError("No se pudo guardar el turno (error SQL " + ex.Number + "). Actualiza la disponibilidad e intenta nuevamente.");
            }
        }

        protected void CancelarSolicitud_Click(object sender, EventArgs e)
        {
            LimpiarSeleccionHorario();
            LimpiarMensaje();
        }

        protected void TurnosGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int indice;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out indice)
                || indice < 0 || indice >= TurnosGrid.DataKeys.Count)
            {
                return;
            }

            try
            {
                DataKey datos = TurnosGrid.DataKeys[indice];
                int turnoId = Convert.ToInt32(datos.Values["TurnoId"]);
                Guid pacienteId = Guid.Parse(Convert.ToString(datos.Values["PacienteId"]));

                if (e.CommandName == "CancelarTurno")
                {
                    _negocio.CancelarTurno(turnoId, EsPaciente ? (Guid?)ObtenerUsuarioActual() : null);
                    MostrarExito("El turno fue cancelado. El horario vuelve a estar disponible.");
                }
                else if (e.CommandName == "Reprogramar")
                {
                    PrepararReprogramacion(turnoId, pacienteId);
                    return;
                }
                else if (e.CommandName == "Confirmar")
                {
                    if (!EsAdministrativo)
                    {
                        MostrarError("Solo el personal administrativo puede confirmar turnos.");
                        return;
                    }

                    _negocio.CambiarEstado(turnoId, "Confirmado", true, null);
                    MostrarExito("El turno fue confirmado.");
                    _negocio.EnviarConfirmacionTurnoAsync(turnoId, ConfiguracionSitio.ObtenerUrlBase());
                }
                else if (e.CommandName == "Atendido" || e.CommandName == "Ausente")
                {
                    Guid? profesionalAutorizado = TieneRol("PROFESIONAL")
                        ? (Guid?)ObtenerUsuarioActual() : null;
                    _negocio.CambiarEstado(turnoId, e.CommandName, EsAdministrativo, profesionalAutorizado);
                    MostrarExito(e.CommandName == "Atendido"
                        ? "El turno fue marcado como atendido."
                        : "El turno fue marcado como ausente.");
                }
                else
                {
                    return;
                }

                CargarTurnos();
                if (SolicitudPanel.Visible)
                {
                    CargarDisponibilidad();
                }
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(ex.Message);
            }
            catch (SqlException ex)
            {
                MostrarError("No se pudo actualizar el turno (error SQL " + ex.Number + ").");
            }
        }

        protected void CancelarReprogramacion_Click(object sender, EventArgs e)
        {
            FinalizarReprogramacion();
            LimpiarMensaje();
            Motivo.Text = string.Empty;
            LimpiarDisponibilidad();
            if (VistaActiva != null) VistaActiva.Value = "listado";
        }

        private void VerificarAccionUrl()
        {
            string accion = (Request.QueryString["accion"] ?? string.Empty).ToLowerInvariant();
            int turnoId;
            if (!int.TryParse(Request.QueryString["turnoId"], out turnoId) || turnoId <= 0)
            {
                return;
            }

            if (accion == "cancelar")
            {
                TurnoDetalle turno = _negocio.ObtenerTurno(turnoId);
                if (turno == null)
                {
                    MostrarError("El turno indicado no existe o fue retirado.");
                    return;
                }

                if (EsPaciente && turno.PacienteId != ObtenerUsuarioActual())
                {
                    MostrarError("Solo podés gestionar turnos que pertenezcan a tu propia cuenta.");
                    return;
                }

                if (turno.Estado == "Cancelado")
                {
                    MostrarError("Este turno ya se encuentra cancelado.");
                    return;
                }

                if (turno.Estado == "Atendido" || turno.Estado == "Ausente")
                {
                    MostrarError("No es posible cancelar un turno que ya figura como " + turno.Estado.ToLower() + ".");
                    return;
                }

                ViewState["TurnoACancelarUrl"] = turnoId;
                ConfirmarCancelacionDetalle.Text = string.Format(
                    "Estás por cancelar el turno #{0} programado para el <b>{1:dd/MM/yyyy 'a las' HH:mm} hs</b>.",
                    turnoId, turno.FechaHora);
                ConfirmarCancelacionUrlPanel.Visible = true;
            }
        }

        protected void BotonConfirmarCancelacionUrl_Click(object sender, EventArgs e)
        {
            int turnoId = ViewState["TurnoACancelarUrl"] != null ? (int)ViewState["TurnoACancelarUrl"] : 0;
            if (turnoId <= 0)
            {
                ConfirmarCancelacionUrlPanel.Visible = false;
                return;
            }

            try
            {
                _negocio.CancelarTurno(turnoId, EsPaciente ? (Guid?)ObtenerUsuarioActual() : null);
                ConfirmarCancelacionUrlPanel.Visible = false;
                ViewState.Remove("TurnoACancelarUrl");
                MostrarExito("El turno #" + turnoId + " fue cancelado correctamente. El horario vuelve a estar disponible.");
                CargarTurnos();
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(ex.Message);
            }
            catch (SqlException ex)
            {
                MostrarError("No se pudo cancelar el turno (error SQL " + ex.Number + ").");
            }
        }

        protected void BotonDescartarCancelacionUrl_Click(object sender, EventArgs e)
        {
            ConfirmarCancelacionUrlPanel.Visible = false;
            ViewState.Remove("TurnoACancelarUrl");
        }

        protected string EstadoHorario(object disponible, object estado)
        {
            if (Convert.ToBoolean(disponible))
            {
                return "Disponible";
            }

            return EsPaciente ? "Ocupado" : Convert.ToString(estado);
        }

        protected string PacienteOcupante(object paciente)
        {
            if (EsPaciente)
            {
                return string.Empty;
            }

            string nombre = Convert.ToString(paciente);
            return string.IsNullOrWhiteSpace(nombre) ? string.Empty : nombre;
        }

        protected bool PuedeCancelarOReprogramar(object paciente, object estado, object fechaHora)
        {
            DateTime fecha = Convert.ToDateTime(fechaHora);
            bool vigente = Convert.ToString(estado) == "Solicitado"
                || Convert.ToString(estado) == "Confirmado";
            if (!vigente || fecha <= DateTime.Now)
            {
                return false;
            }

            return EsAdministrativo
                || (EsPaciente && Guid.TryParse(Convert.ToString(paciente), out Guid pacienteId)
                    && pacienteId == ObtenerUsuarioActual()
                    && ReglasTurno.PacienteDentroDelPlazo(fecha));
        }

        // Turno propio y vigente del paciente al que le faltan menos de 24 hs:
        // no puede cancelarlo ni reprogramarlo, y se le muestra el motivo en lugar de los botones.
        protected bool FueraDePlazoParaPaciente(object paciente, object estado, object fechaHora)
        {
            DateTime fecha = Convert.ToDateTime(fechaHora);
            bool vigente = Convert.ToString(estado) == "Solicitado"
                || Convert.ToString(estado) == "Confirmado";
            return EsPaciente && vigente && fecha > DateTime.Now
                && !ReglasTurno.PacienteDentroDelPlazo(fecha)
                && Guid.TryParse(Convert.ToString(paciente), out Guid pacienteId)
                && pacienteId == ObtenerUsuarioActual();
        }

        // Turno propio con seña pendiente y todavia dentro del plazo: se muestra "Pagar seña".
        protected bool PuedePagarSena(object paciente, object estado, object estadoSena, object vencimientoSena)
        {
            string estadoTurno = Convert.ToString(estado);
            return EsPaciente
                && Convert.ToString(estadoSena) == "Pendiente"
                && (estadoTurno == "Solicitado" || estadoTurno == "Confirmado")
                && vencimientoSena != null && vencimientoSena != DBNull.Value
                && Convert.ToDateTime(vencimientoSena) > DateTime.Now
                && Guid.TryParse(Convert.ToString(paciente), out Guid pacienteId)
                && pacienteId == ObtenerUsuarioActual();
        }

        // Texto de la columna Seña: el estado y, si esta pendiente con plazo, el monto y hasta cuando.
        protected string TextoSena(object estadoSena, object montoSena, object vencimientoSena)
        {
            string estado = Convert.ToString(estadoSena);
            if (estado == "Pendiente" && vencimientoSena != null && vencimientoSena != DBNull.Value)
            {
                CultureInfo es = CultureInfo.GetCultureInfo("es-AR");
                return "Pendiente · " + Convert.ToDecimal(montoSena).ToString("C", es)
                    + " · vence " + Convert.ToDateTime(vencimientoSena).ToString("HH:mm", es) + " hs";
            }

            return estado == "NoRequerida" ? "No requerida" : estado;
        }

        protected bool PuedeCerrarTurno(object estado, object fechaHora)
        {
            return Convert.ToString(estado) == "Confirmado"
                && Convert.ToDateTime(fechaHora) <= DateTime.Now
                && (EsAdministrativo || TieneRol("PROFESIONAL"));
        }

        private void CargarOpcionesIniciales()
        {
            if (EsAdministrativo)
            {
                Paciente.DataSource = _negocio.ObtenerPacientesActivos();
                Paciente.DataTextField = "Nombre";
                Paciente.DataValueField = "PacienteId";
                Paciente.DataBind();
                Paciente.Items.Insert(0, new ListItem("Selecciona un paciente", string.Empty));
            }

            CargarEspecialidades();
            CargarProfesionales();
        }

        private void ConfigurarPasosSolicitud()
        {
            if (EsReprogramacion)
            {
                Especialidad.Enabled = true;
                Profesional.Enabled = true;
                FechaDisponibilidad.Enabled = true;
                Motivo.Enabled = true;
                ConsultarDisponibilidad.Enabled = true;
                return;
            }

            bool tienePaciente = !EsAdministrativo || Paciente.SelectedValue.Length > 0;
            bool tieneEspecialidad = tienePaciente && Especialidad.SelectedValue.Length > 0;
            bool tieneProfesional = tieneEspecialidad && Profesional.SelectedValue.Length > 0;
            bool tieneFecha = !string.IsNullOrWhiteSpace(FechaDisponibilidad.Text);

            Paciente.Enabled = EsAdministrativo;
            Especialidad.Enabled = tienePaciente;
            Profesional.Enabled = tieneEspecialidad;
            FechaDisponibilidad.Enabled = tieneProfesional;
            Motivo.Enabled = tieneFecha;
            ConsultarDisponibilidad.Enabled = tieneProfesional && tieneFecha;
        }

        // Todas las especialidades activas del sistema (no depende del profesional).
        private void CargarEspecialidades()
        {
            Especialidad.DataSource = _negocio.ObtenerEspecialidadesActivas();
            Especialidad.DataTextField = "Nombre";
            Especialidad.DataValueField = "EspecialidadId";
            Especialidad.DataBind();
            Especialidad.Items.Insert(0, new ListItem("Selecciona una especialidad", string.Empty));
            DuracionEspecialidad.Text = string.Empty;
        }

        // Profesionales activos que atienden la especialidad elegida.
        private void CargarProfesionales()
        {
            int especialidadId;
            Profesional.Items.Clear();
            if (int.TryParse(Especialidad.SelectedValue, out especialidadId))
            {
                if (EsPaciente)
                {
                    // El paciente ve si su obra social cubre a cada profesional (cubiertos primero).
                    CultureInfo es = CultureInfo.GetCultureInfo("es-AR");
                    foreach (ProfesionalParaPaciente opcion in
                        _negocio.ObtenerProfesionalesParaPaciente(especialidadId, ObtenerUsuarioActual()))
                    {
                        string detalle = opcion.TieneCobertura
                            ? "Cubierto por tu obra social"
                            : "Sin cobertura - seña " + opcion.MontoSena.ToString("C", es);
                        Profesional.Items.Add(new ListItem(
                            opcion.Nombre + " · " + detalle, opcion.ProfesionalId.ToString()));
                    }
                }
                else
                {
                    Profesional.DataSource = _negocio.ObtenerProfesionalesPorEspecialidad(especialidadId);
                    Profesional.DataTextField = "Nombre";
                    Profesional.DataValueField = "ProfesionalId";
                    Profesional.DataBind();
                }
            }

            Profesional.Items.Insert(0, new ListItem(
                Especialidad.SelectedValue.Length == 0
                    ? "Primero selecciona una especialidad" : "Selecciona un profesional",
                string.Empty));
        }

        private void CargarResumenProfesionales()
        {
            ProfesionalesDisponibilidadGrid.DataSource = null;
            ProfesionalesDisponibilidadGrid.DataBind();
            FechasDisponiblesPanel.Visible = false;

            int especialidadId;
            if (!int.TryParse(Especialidad.SelectedValue, out especialidadId))
            {
                return;
            }

            Guid pacienteId;
            try
            {
                pacienteId = ObtenerPacienteSeleccionado();
            }
            catch (InvalidOperationException)
            {
                MostrarError("Seleccioná un paciente para consultar la disponibilidad.");
                return;
            }

            DateTime desde = DateTime.Today;
            DateTime hasta = desde.AddDays(59);
            IList<ResumenDisponibilidadProfesional> resumen = _negocio.ObtenerResumenDisponibilidad(
                especialidadId, pacienteId, desde, hasta, TurnoExcluido(), EsPaciente);
            ProfesionalesDisponibilidadGrid.DataSource = resumen;
            ProfesionalesDisponibilidadGrid.DataBind();
            ExploradorDisponibilidadPanel.Visible = true;
        }

        private void CargarFechasProfesional(Guid profesionalId)
        {
            int especialidadId;
            if (!int.TryParse(Especialidad.SelectedValue, out especialidadId))
            {
                return;
            }

            IList<FechaDisponible> fechas = _negocio.ObtenerFechasDisponibles(
                profesionalId, especialidadId, DateTime.Today, DateTime.Today.AddDays(59), TurnoExcluido());
            FechasDisponiblesGrid.DataSource = fechas;
            FechasDisponiblesGrid.DataBind();
            ProfesionalSeleccionadoResumen.Text = HttpUtility.HtmlEncode(
                "Mostrando fechas con al menos un horario libre para "
                + (Profesional.SelectedItem == null ? "el profesional seleccionado" : Profesional.SelectedItem.Text) + ".");
        }

        protected string FechaResumen(object valor)
        {
            if (valor == null || valor == DBNull.Value)
            {
                return "Sin fechas libres";
            }

            return Convert.ToDateTime(valor).ToString("dddd dd/MM/yyyy", CultureInfo.GetCultureInfo("es-AR"));
        }

        protected string CoberturaResumen(object cobertura, object montoSena)
        {
            if (!EsPaciente)
            {
                return "Seña no requerida en carga administrativa";
            }

            if (Convert.ToBoolean(cobertura))
            {
                return "Cubierto por tu obra social";
            }

            return "Sin cobertura · seña "
                + Convert.ToDecimal(montoSena).ToString("C", CultureInfo.GetCultureInfo("es-AR"));
        }

        private void MostrarDuracionSeleccionada()
        {
            int especialidadId;
            if (!int.TryParse(Especialidad.SelectedValue, out especialidadId))
            {
                DuracionEspecialidad.Text = "Elige una especialidad para ver la duración de la consulta.";
                return;
            }

            foreach (DataRow fila in _negocio.ObtenerEspecialidadesActivas().Rows)
            {
                if (Convert.ToInt32(fila["EspecialidadId"]) == especialidadId)
                {
                    DuracionEspecialidad.Text = HttpUtility.HtmlEncode(
                        "Duración estándar: " + fila["DuracionEstandarMinutos"] + " minutos.");
                    return;
                }
            }

            DuracionEspecialidad.Text = string.Empty;
        }

        // Ubica la fecha en el primer día con horarios libres para el profesional y la especialidad.
        private void ProponerProximaFecha()
        {
            Guid profesionalId;
            int especialidadId;
            if (!Guid.TryParse(Profesional.SelectedValue, out profesionalId)
                || !int.TryParse(Especialidad.SelectedValue, out especialidadId))
            {
                return;
            }

            DateTime? proxima = _negocio.BuscarProximaFechaDisponible(
                profesionalId, especialidadId, DateTime.Today, TurnoExcluido());
            if (proxima.HasValue)
            {
                FechaDisponibilidad.Text = proxima.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
        }

        private int? TurnoExcluido()
        {
            return ViewState["TurnoAReprogramar"] == null
                ? (int?)null : Convert.ToInt32(ViewState["TurnoAReprogramar"]);
        }

        private string ExplicarSinHorarios(Guid profesionalId, int especialidadId, DateTime fecha)
        {
            CultureInfo es = CultureInfo.GetCultureInfo("es-AR");
            System.Collections.Generic.IList<int> dias = _negocio.ObtenerDiasAtencion(profesionalId, especialidadId);
            if (dias.Count == 0)
            {
                return "El profesional no tiene una agenda semanal activa para esta especialidad.";
            }

            string[] nombres = { string.Empty, "lunes", "martes", "miércoles", "jueves", "viernes", "sábado", "domingo" };
            string atiende = string.Join(", ", System.Linq.Enumerable.Select(dias, d => nombres[d]));
            int diaSeleccionado = ((int)fecha.DayOfWeek + 6) % 7 + 1;
            string texto = dias.Contains(diaSeleccionado)
                ? "No quedan horarios libres el " + fecha.ToString("dddd dd/MM/yyyy", es) + "."
                : "El profesional no atiende los días " + nombres[diaSeleccionado] + ". Atiende: " + atiende + ".";

            DateTime? proxima = _negocio.BuscarProximaFechaDisponible(
                profesionalId, especialidadId, fecha.AddDays(1), TurnoExcluido());
            return proxima.HasValue
                ? texto + " Próxima fecha con horarios libres: " + proxima.Value.ToString("dddd dd/MM/yyyy", es) + "."
                : texto + " No hay horarios libres en los próximos 60 días.";
        }

        private void CargarDisponibilidad()
        {
            Guid profesionalId;
            int especialidadId;
            DateTime fecha;
            if (!Guid.TryParse(Profesional.SelectedValue, out profesionalId)
                || !int.TryParse(Especialidad.SelectedValue, out especialidadId)
                || !DateTime.TryParseExact(FechaDisponibilidad.Text, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
            {
                MostrarError("Selecciona una especialidad, un profesional y una fecha válidos.");
                LimpiarDisponibilidad();
                return;
            }

            if (fecha.Date < DateTime.Today)
            {
                MostrarError("Selecciona hoy o una fecha futura para consultar disponibilidad.");
                LimpiarDisponibilidad();
                return;
            }

            int? turnoExcluido = ViewState["TurnoAReprogramar"] == null
                ? (int?)null
                : Convert.ToInt32(ViewState["TurnoAReprogramar"]);
            DisponibilidadGrid.DataSource = _negocio.ObtenerDisponibilidad(
                profesionalId, especialidadId, fecha, turnoExcluido);
            DisponibilidadGrid.DataBind();
            HorariosPanel.Visible = true;
            MostrarDuracionSeleccionada();
            if (DisponibilidadGrid.Rows.Count == 0)
            {
                MostrarError(ExplicarSinHorarios(profesionalId, especialidadId, fecha));
            }
        }

        private void CargarTurnos()
        {
            DateTime? desde;
            DateTime? hasta;
            string errorFiltro;
            if (!TryLeerFiltroFechas(Desde, Hasta, out desde, out hasta, out errorFiltro))
            {
                MostrarError(errorFiltro);
                return;
            }

            Guid? pacienteId = EsPaciente ? (Guid?)ObtenerUsuarioActual() : null;
            Guid? profesionalId = TieneRol("PROFESIONAL") ? (Guid?)ObtenerUsuarioActual() : null;
            TurnosGrid.DataSource = _negocio.ObtenerTurnos(pacienteId, profesionalId, desde, hasta);
            TurnosGrid.DataBind();
            ResumenFiltro.Text = HttpUtility.HtmlEncode(DescribirFiltro(desde, hasta, TurnosGrid.Rows.Count, "turno"));
        }

        private static string DescribirFiltro(DateTime? desde, DateTime? hasta, int cantidad, string objeto)
        {
            string periodo = !desde.HasValue && !hasta.HasValue ? "sin filtro de fecha"
                : desde.HasValue && hasta.HasValue && desde.Value == hasta.Value
                    ? "el " + desde.Value.ToString("dd/MM/yyyy")
                : (desde.HasValue ? "desde el " + desde.Value.ToString("dd/MM/yyyy") + " " : string.Empty)
                    + (hasta.HasValue ? "hasta el " + hasta.Value.ToString("dd/MM/yyyy") : string.Empty);
            return cantidad + (cantidad == 1 ? " " + objeto : " " + objeto + "s") + " (" + periodo.Trim() + ").";
        }

        private void PrepararReprogramacion(int turnoId, Guid pacienteId)
        {
            if (!EsAdministrativo && (!EsPaciente || pacienteId != ObtenerUsuarioActual()))
            {
                MostrarError("No tienes permiso para reprogramar este turno.");
                return;
            }

            TurnoDetalle turno = _negocio.ObtenerTurno(turnoId);
            if (turno == null || turno.FechaHora <= DateTime.Now
                || (turno.Estado != "Solicitado" && turno.Estado != "Confirmado"))
            {
                MostrarError("Solo se pueden reprogramar turnos futuros solicitados o confirmados.");
                return;
            }

            if (EsPaciente && !ReglasTurno.PacienteDentroDelPlazo(turno.FechaHora))
            {
                MostrarError("Los turnos solo se pueden reprogramar con al menos "
                    + ReglasTurno.HorasMinimasCancelacionPaciente + " horas de anticipacion. Comunicate con la clinica.");
                return;
            }

            ViewState["TurnoAReprogramar"] = turnoId;
            if (VistaActiva != null) VistaActiva.Value = "solicitar";
            if (EsAdministrativo)
            {
                Paciente.SelectedValue = turno.PacienteId.ToString();
            }

            ListItem especialidad = Especialidad.Items.FindByValue(turno.EspecialidadId.ToString());
            if (especialidad != null)
            {
                Especialidad.ClearSelection();
                especialidad.Selected = true;
            }

            CargarProfesionales();
            CargarResumenProfesionales();
            ListItem profesional = Profesional.Items.FindByValue(turno.ProfesionalId.ToString());
            if (profesional != null)
            {
                Profesional.ClearSelection();
                profesional.Selected = true;
            }

            FechaDisponibilidad.Text = turno.FechaHora.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            Motivo.Text = turno.Motivo;
            TituloSolicitud.Text = "Reprogramar turno " + turnoId;
            CancelarReprogramacion.Visible = true;
            ConfigurarPasosSolicitud();
            CargarDisponibilidad();
            MostrarExito("Elige un nuevo horario disponible. El turno actual se conserva si no se completa la reprogramación.");
        }

        private Guid ObtenerPacienteSeleccionado()
        {
            if (EsPaciente)
            {
                return ObtenerUsuarioActual();
            }

            Guid pacienteId;
            if (EsAdministrativo && Guid.TryParse(Paciente.SelectedValue, out pacienteId))
            {
                return pacienteId;
            }

            throw new InvalidOperationException("Selecciona un paciente para solicitar el turno.");
        }

        private bool EsPropioPaciente(Guid pacienteId)
        {
            return EsPaciente && pacienteId == ObtenerUsuarioActual();
        }

        private Guid ObtenerUsuarioActual()
        {
            Guid usuarioId;
            if (!Guid.TryParse(Convert.ToString(Session["UsuarioId"]), out usuarioId))
            {
                throw new InvalidOperationException("La sesión no contiene un identificador de usuario válido.");
            }

            return usuarioId;
        }

        private void EjecutarConManejoDeErrores(string contexto, Action accion)
        {
            try
            {
                accion();
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(ex.Message);
            }
            catch (SqlException ex)
            {
                MostrarError(contexto + " (error SQL " + ex.Number + ").");
            }
        }

        private void LimpiarDisponibilidad()
        {
            DisponibilidadGrid.DataSource = null;
            DisponibilidadGrid.DataBind();
        }

        private void LimpiarSeleccionHorario()
        {
            ViewState.Remove("SolicitudProfesionalId");
            ViewState.Remove("SolicitudEspecialidadId");
            ViewState.Remove("SolicitudFechaHora");
            ConfirmacionSolicitudPanel.Visible = false;
            MotivoPanel.Visible = false;
            ResumenSolicitud.Text = string.Empty;
        }

        private void FinalizarReprogramacion()
        {
            ViewState.Remove("TurnoAReprogramar");
            TituloSolicitud.Text = "Solicitar turno";
            CancelarReprogramacion.Visible = false;
        }

        private void MostrarExito(string mensaje)
        {
            Mensaje.Text = HttpUtility.HtmlEncode(mensaje);
            Mensaje.CssClass = "alert alert-success";
            Mensaje.Visible = true;
        }

        private void MostrarError(string mensaje)
        {
            Mensaje.Text = HttpUtility.HtmlEncode(mensaje);
            Mensaje.CssClass = "alert alert-error";
            Mensaje.Visible = true;
        }

        private void LimpiarMensaje()
        {
            Mensaje.Text = string.Empty;
            Mensaje.Visible = false;
        }
    }
}
