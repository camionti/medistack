using System;
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
                EjecutarConManejoDeErrores("No se pudieron cargar los turnos", () =>
                {
                    CargarOpcionesIniciales();
                    FechaDisponibilidad.Text = DateTime.Today.AddDays(1)
                        .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    CargarTurnos();
                });
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
                if (Profesional.Items.Count == 2)
                {
                    Profesional.SelectedIndex = 1;
                    ProponerProximaFecha();
                    CargarDisponibilidad();
                }
                else if (Profesional.Items.Count == 1 && Especialidad.SelectedValue.Length > 0)
                {
                    MostrarError("No hay profesionales activos que atiendan esta especialidad.");
                }
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
                    ProponerProximaFecha();
                    CargarDisponibilidad();
                }
            });
        }

        protected void ConsultarDisponibilidad_Click(object sender, EventArgs e)
        {
            EjecutarConManejoDeErrores("No se pudo consultar la disponibilidad", CargarDisponibilidad);
        }

        protected void Filtrar_Click(object sender, EventArgs e)
        {
            EjecutarConManejoDeErrores("No se pudieron filtrar los turnos", CargarTurnos);
        }

        protected void LimpiarFiltro_Click(object sender, EventArgs e)
        {
            Desde.Text = string.Empty;
            Hasta.Text = string.Empty;
            EjecutarConManejoDeErrores("No se pudieron cargar los turnos", CargarTurnos);
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
                    MostrarError("Ese horario ya no está disponible. Consulta nuevamente la agenda.");
                    CargarDisponibilidad();
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

                SolicitudTurno solicitud = new SolicitudTurno
                {
                    PacienteId = pacienteId,
                    ProfesionalId = profesionalId,
                    EspecialidadId = especialidadId,
                    FechaHora = fechaHora,
                    Motivo = Motivo.Text
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
                    _negocio.SolicitarTurno(solicitud);
                    CargarDisponibilidad();
                    MostrarExito("La solicitud de turno se registro correctamente. El horario reservado ya no figura como disponible.");
                    Motivo.Text = string.Empty;
                    CargarTurnos();
                    return;
                }

                LimpiarDisponibilidad();
                CargarTurnos();
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(ex.Message);
                CargarDisponibilidad();
            }
            catch (SqlException ex)
            {
                MostrarError("No se pudo guardar el turno (error SQL " + ex.Number + "). Actualiza la disponibilidad e intenta nuevamente.");
                CargarDisponibilidad();
            }
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
                    && pacienteId == ObtenerUsuarioActual());
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
                Profesional.DataSource = _negocio.ObtenerProfesionalesPorEspecialidad(especialidadId);
                Profesional.DataTextField = "Nombre";
                Profesional.DataValueField = "ProfesionalId";
                Profesional.DataBind();
            }

            Profesional.Items.Insert(0, new ListItem(
                Especialidad.SelectedValue.Length == 0
                    ? "Primero selecciona una especialidad" : "Selecciona un profesional",
                string.Empty));
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
            MostrarDuracionSeleccionada();
            LimpiarMensaje();
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

            ViewState["TurnoAReprogramar"] = turnoId;
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
