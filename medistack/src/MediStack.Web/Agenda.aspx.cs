using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using MediStack.Dominio;
using MediStack.Negocio;

namespace MediStack.Web
{
    public partial class AgendaPagina : PaginaProtegida
    {
        private readonly AgendaNegocio _negocio = new AgendaNegocio();

        protected void Page_Load(object sender, EventArgs e)
        {
            bool esAdministrativo = TieneRol("ADMINISTRATIVO");
            bool esProfesional = TieneRol("PROFESIONAL");
            if (!esAdministrativo && !esProfesional)
            {
                Response.Redirect(ResolveUrl("~/Dashboard.aspx"), true);
            }

            GestionHorariosPanel.Visible = esAdministrativo;
            ProfesionalLabel.Visible = esAdministrativo;
            Profesional.Enabled = esAdministrativo;
            DisponibilidadPanel.Visible = !esProfesional;

            if (!IsPostBack)
            {
                EjecutarConManejoDeErrores("No se pudo cargar la agenda", () =>
                {
                    CargarProfesionales();
                    if (esProfesional)
                    {
                        Guid profesionalId = Guid.Parse(Convert.ToString(Session["UsuarioId"]));
                        SeleccionarProfesional(profesionalId);
                    }

                    Fecha.Text = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    CargarEspecialidadesHorario();
                    LimpiarFormularioHorario();
                    DateTime fechaInicial = ObtenerFechaConsulta();
                    Fecha.Text = fechaInicial.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    if (esProfesional && !EsVistaDiaria)
                    {
                        VistaSemanalPanel.Visible = true;
                        VistaDiariaPanel.Visible = false;
                        CargarAgendaSemanal(fechaInicial);
                    }
                    else
                    {
                        if (esProfesional)
                        {
                            VolverSemana.Visible = true;
                            VolverSemana.NavigateUrl = UrlSemana(InicioSemana(fechaInicial), string.Empty);
                        }
                        CargarDatosAgenda();
                    }
                });
            }
        }

        protected void Profesional_SelectedIndexChanged(object sender, EventArgs e)
        {
            EjecutarConManejoDeErrores("No se pudo cambiar el profesional", () =>
            {
                CargarEspecialidadesHorario();
                LimpiarFormularioHorario();
                CargarDatosAgenda();
            });
        }

        protected void Actualizar_Click(object sender, EventArgs e)
        {
            EjecutarConManejoDeErrores("No se pudo actualizar la agenda", CargarDatosAgenda);
        }

        private bool MostrarCanceladosDia
        {
            get { return ViewState["MostrarCanceladosDia"] != null && (bool)ViewState["MostrarCanceladosDia"]; }
            set { ViewState["MostrarCanceladosDia"] = value; }
        }

        protected void BotonVerCancelados_Click(object sender, EventArgs e)
        {
            MostrarCanceladosDia = !MostrarCanceladosDia;
            EjecutarConManejoDeErrores("No se pudo actualizar la vista de turnos", CargarDatosAgenda);
        }

        protected void NuevoHorario_Click(object sender, EventArgs e)
        {
            LimpiarMensaje();
            EjecutarConManejoDeErrores("No se pudo preparar el formulario", () =>
            {
                CargarEspecialidadesHorario();
                LimpiarFormularioHorario();
            });
        }

        protected void GuardarHorario_Click(object sender, EventArgs e)
        {
            try
            {
                Guid profesionalId;
                int especialidadId;
                int diaSemana;
                TimeSpan horaInicio;
                TimeSpan horaFin;
                bool esEdicion = ViewState["HorarioAtencionId"] != null;
                if (!int.TryParse(DiaSemana.SelectedValue, out diaSemana)
                    || !TimeSpan.TryParseExact(HoraInicio.Text, new[] { @"hh\:mm", @"hh\:mm\:ss" },
                        CultureInfo.InvariantCulture, out horaInicio)
                    || !TimeSpan.TryParseExact(HoraFin.Text, new[] { @"hh\:mm", @"hh\:mm\:ss" },
                        CultureInfo.InvariantCulture, out horaFin))
                {
                    MostrarError("Completa el día y el rango horario.");
                    return;
                }

                if (esEdicion)
                {
                    profesionalId = (Guid)ViewState["HorarioProfesionalId"];
                    especialidadId = (int)ViewState["HorarioEspecialidadId"];
                }
                else if (!Guid.TryParse(Profesional.SelectedValue, out profesionalId)
                    || !int.TryParse(EspecialidadHorario.SelectedValue, out especialidadId))
                {
                    MostrarError("Selecciona un profesional y una especialidad válidos.");
                    return;
                }
                else if (!EsPropioProfesional(profesionalId))
                {
                    MostrarError("No puedes modificar la agenda de otro profesional.");
                    return;
                }

                _negocio.GuardarHorario(new HorarioAtencion
                {
                    HorarioAtencionId = esEdicion ? (int)ViewState["HorarioAtencionId"] : 0,
                    ProfesionalId = profesionalId,
                    EspecialidadId = especialidadId,
                    DiaSemana = diaSemana,
                    HoraInicio = horaInicio,
                    HoraFin = horaFin
                });

                if (string.Equals(Profesional.SelectedValue, profesionalId.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    CargarDatosAgenda();
                }

                LimpiarFormularioHorario();
                MostrarExito("La franja semanal se guardo correctamente.");
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(ex.Message);
            }
            catch (SqlException ex)
            {
                MostrarError("No se pudo guardar la franja semanal (error SQL " + ex.Number + ").");
            }
        }

        protected void HorariosGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int indice;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out indice)
                || indice < 0 || indice >= HorariosGrid.DataKeys.Count)
            {
                return;
            }

            try
            {
                DataKey datos = HorariosGrid.DataKeys[indice];
                Guid profesionalId = Guid.Parse(ObtenerTextoDataKey(indice, "ProfesionalId"));
                int horarioId = Convert.ToInt32(HorariosGrid.DataKeys[indice].Values["HorarioAtencionId"]);
                if (!EsPropioProfesional(profesionalId))
                {
                    MostrarError("No puedes modificar la agenda de otro profesional.");
                    return;
                }

                if (e.CommandName == "EditarHorario")
                {
                    EditarHorario(indice, datos, horarioId, profesionalId);
                }
                else if (e.CommandName == "CambiarEstadoHorario")
                {
                    bool activo = Convert.ToBoolean(datos.Values["Activo"]);
                    _negocio.CambiarEstadoHorario(horarioId, !activo);
                    CargarDatosAgenda();
                    MostrarExito(activo ? "La franja fue desactivada." : "La franja fue reactivada.");
                }
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(ex.Message);
            }
            catch (SqlException ex)
            {
                MostrarError("No se pudo completar la operación (error SQL " + ex.Number + ").");
            }
        }

        protected string DiaSemanaTexto(object valor)
        {
            string[] dias = { string.Empty, "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };
            int numero;
            if (valor == null || valor == DBNull.Value || !int.TryParse(Convert.ToString(valor), out numero)
                || numero < 1 || numero >= dias.Length)
            {
                return string.Empty;
            }

            return dias[numero];
        }

        protected string UrlDia(object valor)
        {
            DateTime fecha = Convert.ToDateTime(valor);
            return ResolveUrl("~/Agenda.aspx?vista=dia&fecha="
                + fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        protected string MotivoCorto(object valor)
        {
            string motivo = Convert.ToString(valor);
            if (string.IsNullOrWhiteSpace(motivo))
            {
                return "Sin motivo";
            }

            motivo = motivo.Trim();
            return motivo.Length > 8 ? motivo.Substring(0, 8) + "..." : motivo;
        }

        protected string ClaseTurnoAgenda(object estado, object reprogramado)
        {
            string clase = "agenda-turno-card";
            if (string.Equals(Convert.ToString(estado), "Cancelado", StringComparison.OrdinalIgnoreCase))
            {
                clase += " agenda-turno-cancelado";
            }
            else if (Convert.ToBoolean(reprogramado))
            {
                clase += " agenda-turno-reprogramado";
            }

            return clase;
        }

        protected string EstadoAgenda(object estado, object reprogramado)
        {
            string texto = Convert.ToString(estado);
            if (Convert.ToBoolean(reprogramado))
            {
                texto += " · Reprogramado";
            }

            return texto;
        }

        protected string DetalleReprogramacion(
            object turnoDesde, object turnoHacia, object fechaHacia, object fechaActual)
        {
            if (turnoDesde != null && turnoDesde != DBNull.Value)
            {
                return "Reprogramado desde turno #" + Convert.ToString(turnoDesde);
            }

            if (turnoHacia != null && turnoHacia != DBNull.Value
                && fechaHacia != null && fechaHacia != DBNull.Value)
            {
                return "Reprogramado para el " + Convert.ToDateTime(fechaHacia).ToString("dd/MM HH:mm");
            }

            return string.Empty;
        }

        private void CargarProfesionales()
        {
            DataTable profesionales = _negocio.ObtenerProfesionales();
            Profesional.DataSource = profesionales;
            Profesional.DataTextField = "Nombre";
            Profesional.DataValueField = "ProfesionalId";
            Profesional.DataBind();
            Profesional.Items.Insert(0, new ListItem("Selecciona un profesional", string.Empty));

            foreach (DataRow fila in profesionales.Rows)
            {
                if (!Convert.ToBoolean(fila["Activo"]))
                {
                    ListItem item = Profesional.Items.FindByValue(Convert.ToString(fila["ProfesionalId"]));
                    if (item != null)
                    {
                        item.Text += " (inactivo)";
                    }
                }
            }

            if (TieneRol("ADMINISTRATIVO") && profesionales.Rows.Count > 0)
            {
                DataRow activo = profesionales.Select("Activo = true").Length > 0
                    ? profesionales.Select("Activo = true")[0] : profesionales.Rows[0];
                SeleccionarProfesional((Guid)activo["ProfesionalId"]);
            }
        }

        private void SeleccionarProfesional(Guid profesionalId)
        {
            ListItem item = Profesional.Items.FindByValue(profesionalId.ToString());
            if (item != null)
            {
                Profesional.ClearSelection();
                item.Selected = true;
            }
        }

        private void CargarEspecialidadesHorario()
        {
            Guid profesionalId;
            if (!Guid.TryParse(Profesional.SelectedValue, out profesionalId))
            {
                EspecialidadHorario.Items.Clear();
                EspecialidadHorario.Items.Insert(0, new ListItem("Selecciona una especialidad", string.Empty));
                return;
            }

            EspecialidadHorario.DataSource = _negocio.ObtenerEspecialidades(profesionalId);
            EspecialidadHorario.DataTextField = "Nombre";
            EspecialidadHorario.DataValueField = "EspecialidadId";
            EspecialidadHorario.DataBind();
            EspecialidadHorario.Items.Insert(0, new ListItem("Selecciona una especialidad", string.Empty));
        }

        private void CargarDatosAgenda()
        {
            Guid profesionalId;
            DateTime fecha;
            if (!Guid.TryParse(Profesional.SelectedValue, out profesionalId)
                || !DateTime.TryParseExact(Fecha.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out fecha))
            {
                DisponibilidadGrid.DataSource = null;
                DisponibilidadGrid.DataBind();
                TurnosGrid.DataSource = null;
                TurnosGrid.DataBind();
                HorariosGrid.DataSource = null;
                HorariosGrid.DataBind();
                ResumenAgenda.Text = string.Empty;
                MostrarError("Selecciona un profesional y una fecha válidos.");
                return;
            }

            if (!EsPropioProfesional(profesionalId))
            {
                MostrarError("No tienes acceso a la agenda de ese profesional.");
                return;
            }

            IList<FranjaAgenda> franjas = _negocio.ObtenerDisponibilidad(profesionalId, fecha);
            DataTable turnos = _negocio.ObtenerTurnos(profesionalId, fecha);
            DataTable semanal = _negocio.ObtenerHorarios(profesionalId);
            DisponibilidadGrid.DataSource = franjas;
            DisponibilidadGrid.DataBind();
            DataView vistaTurnos = turnos.DefaultView;
            int turnosCancelados = turnos.Rows.Cast<DataRow>()
                .Count(f => string.Equals(Convert.ToString(f["Estado"]), "Cancelado", StringComparison.OrdinalIgnoreCase));

            if (!MostrarCanceladosDia)
            {
                vistaTurnos.RowFilter = "Estado <> 'Cancelado'";
                BotonVerCancelados.Text = "Ver cancelados" + (turnosCancelados > 0 ? " (" + turnosCancelados + ")" : string.Empty);
                BotonVerCancelados.CssClass = "button button-light";
                TurnosGrid.EmptyDataText = turnosCancelados > 0
                    ? "No hay turnos vigentes para este profesional en la fecha seleccionada (" + turnosCancelados + " cancelado/s)."
                    : "No hay turnos asignados para este profesional en la fecha seleccionada.";
            }
            else
            {
                vistaTurnos.RowFilter = string.Empty;
                BotonVerCancelados.Text = "Ocultar cancelados";
                BotonVerCancelados.CssClass = "button button-secondary";
                TurnosGrid.EmptyDataText = "No hay turnos asignados para este profesional en la fecha seleccionada.";
            }

            TurnosGrid.DataSource = vistaTurnos;
            TurnosGrid.DataBind();
            HorariosGrid.DataSource = semanal;
            HorariosGrid.DataBind();
            if (TieneRol("PROFESIONAL"))
            {
                VolverSemana.Visible = true;
                VolverSemana.NavigateUrl = UrlSemana(InicioSemana(fecha), string.Empty);
            }

            ResumenAgenda.Text = HttpUtility.HtmlEncode(
                ArmarResumen(Profesional.SelectedItem.Text, fecha, franjas, turnos, semanal));
        }

        private void CargarAgendaSemanal(DateTime fechaReferencia)
        {
            Guid profesionalId;
            if (!Guid.TryParse(Profesional.SelectedValue, out profesionalId)
                || !EsPropioProfesional(profesionalId))
            {
                MostrarError("No tienes acceso a la agenda de ese profesional.");
                return;
            }

            DateTime lunes = InicioSemana(fechaReferencia);
            IList<DiaAgendaSemanal> dias = _negocio.ObtenerAgendaSemanal(profesionalId, lunes);
            string filtro = ObtenerFiltroAgenda();
            if (filtro == "cancelados")
            {
                foreach (DiaAgendaSemanal dia in dias)
                {
                    dia.Turnos = dia.Turnos.Where(t => t.Estado == "Cancelado").ToList();
                    if (dia.Turnos.Count == 0)
                    {
                        dia.TieneTurnosFueraDeHorario = false;
                    }
                }
            }
            else
            {
                foreach (DiaAgendaSemanal dia in dias)
                {
                    dia.Turnos = dia.Turnos.Where(t => t.Estado != "Cancelado").ToList();
                    if (dia.Turnos.Count == 0)
                    {
                        dia.TieneTurnosFueraDeHorario = false;
                    }
                }
            }

            AgendaSemanalRepeater.DataSource = dias;
            AgendaSemanalRepeater.DataBind();
            DateTime viernes = lunes.AddDays(4);
            ResumenSemana.Text = HttpUtility.HtmlEncode(
                "Semana del " + lunes.ToString("dd/MM/yyyy") + " al " + viernes.ToString("dd/MM/yyyy")
                + " · " + dias.Sum(d => d.Turnos.Count) + " turno/s visible/s.");
            ConfigurarEnlacesSemana(lunes, filtro);
            bool hayFinDeSemana = _negocio.TieneTurnosFinDeSemana(profesionalId, lunes);
            AvisoFinDeSemana.Visible = hayFinDeSemana;
            AvisoFinDeSemana.Text = hayFinDeSemana
                ? "Hay turnos registrados sábado o domingo. La grilla semanal muestra únicamente lunes a viernes."
                : string.Empty;
        }

        private void ConfigurarEnlacesSemana(DateTime lunes, string filtro)
        {
            SemanaAnterior.NavigateUrl = UrlSemana(lunes.AddDays(-7), filtro);
            SemanaActual.NavigateUrl = UrlSemana(DateTime.Today, filtro);
            SemanaSiguiente.NavigateUrl = UrlSemana(lunes.AddDays(7), filtro);
            FiltroVigentes.NavigateUrl = UrlSemana(lunes, string.Empty);
            FiltroCancelados.NavigateUrl = UrlSemana(lunes, "cancelados");

            FiltroVigentes.CssClass = ClaseFiltro(filtro, string.Empty);
            FiltroCancelados.CssClass = ClaseFiltro(filtro, "cancelados");

        }

        private string UrlSemana(DateTime fecha, string filtro)
        {
            string url = ResolveUrl("~/Agenda.aspx?vista=semana&fecha="
                + fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            return string.IsNullOrEmpty(filtro) ? url : url + "&filtro=" + HttpUtility.UrlEncode(filtro);
        }

        private static string ClaseFiltro(string filtroActual, string filtro)
        {
            return "turnos-tab-btn" + (string.Equals(filtroActual, filtro, StringComparison.OrdinalIgnoreCase)
                ? " is-active" : string.Empty);
        }

        private DateTime ObtenerFechaConsulta()
        {
            DateTime fecha;
            string valor = Request.QueryString["fecha"];
            return DateTime.TryParseExact(valor, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out fecha) ? fecha.Date : DateTime.Today;
        }

        private bool EsVistaDiaria
        {
            get { return string.Equals(Request.QueryString["vista"], "dia", StringComparison.OrdinalIgnoreCase); }
        }

        private static DateTime InicioSemana(DateTime fecha)
        {
            int desplazamiento = ((int)fecha.DayOfWeek + 6) % 7;
            return fecha.Date.AddDays(-desplazamiento);
        }

        private string ObtenerFiltroAgenda()
        {
            string filtro = Request.QueryString["filtro"] ?? string.Empty;
            filtro = filtro.ToLowerInvariant();
            return filtro == "cancelados" ? filtro : string.Empty;
        }

        private static string ArmarResumen(
            string profesional, DateTime fecha, IList<FranjaAgenda> franjas, DataTable turnos, DataTable semanal)
        {
            CultureInfo es = CultureInfo.GetCultureInfo("es-AR");
            string dia = fecha.ToString("dddd dd/MM/yyyy", es);
            int turnosVigentes = turnos.Rows.Cast<DataRow>()
                .Count(f => Convert.ToString(f["Estado"]) != "Cancelado");
            string turnosTexto = turnos.Rows.Count + (turnos.Rows.Count == 1 ? " turno asignado" : " turnos asignados")
                + (turnos.Rows.Count != turnosVigentes ? " (" + (turnos.Rows.Count - turnosVigentes) + " cancelado/s)" : string.Empty);

            if (franjas.Count > 0)
            {
                int libres = franjas.Count(f => f.Disponible);
                string especialidades = string.Join(", ", franjas.Select(f => f.Especialidad).Distinct());
                return "Profesional: " + profesional + " · Especialidad: " + especialidades + " · Día: " + dia
                    + " · " + franjas.Count + " horarios: " + libres + " disponibles y " + (franjas.Count - libres)
                    + " ocupados · " + turnosTexto + ".";
            }

            // Sin horarios ese día: explicar cuándo atiende realmente.
            string[] nombres = { string.Empty, "lunes", "martes", "miércoles", "jueves", "viernes", "sábado", "domingo" };
            var activos = semanal.Rows.Cast<DataRow>().Where(f => Convert.ToBoolean(f["Activo"])).ToList();
            string texto = profesional + " no tiene horarios de atención el " + dia + " · " + turnosTexto + ".";
            if (activos.Count == 0)
            {
                return texto + " No tiene franjas semanales activas.";
            }

            string atencion = string.Join("; ", activos.Select(f =>
                nombres[Convert.ToInt32(f["DiaSemana"])] + " " + Convert.ToString(f["Especialidad"]) + " "
                + ((TimeSpan)f["HoraInicio"]).ToString(@"hh\:mm") + "-" + ((TimeSpan)f["HoraFin"]).ToString(@"hh\:mm")));
            DateTime proxima = fecha.Date;
            for (int i = 1; i <= 14; i++)
            {
                proxima = fecha.Date.AddDays(i);
                int d = ((int)proxima.DayOfWeek + 6) % 7 + 1;
                if (activos.Any(f => Convert.ToInt32(f["DiaSemana"]) == d))
                {
                    break;
                }
            }

            return texto + " Atiende: " + atencion + ". Próximo día de atención: " + proxima.ToString("dddd dd/MM/yyyy", es) + ".";
        }

        private void EditarHorario(int indice, DataKey datos, int horarioId, Guid profesionalId)
        {
            int especialidadId = Convert.ToInt32(datos.Values["EspecialidadId"]);
            CargarEspecialidadesHorario();
            if (EspecialidadHorario.Items.FindByValue(especialidadId.ToString()) == null)
            {
                EspecialidadHorario.Items.Add(new ListItem(
                    ObtenerTextoDataKey(indice, "Especialidad") + " (inactiva)",
                    especialidadId.ToString(CultureInfo.InvariantCulture)));
            }

            EspecialidadHorario.SelectedValue = especialidadId.ToString(CultureInfo.InvariantCulture);
            EspecialidadHorario.Enabled = false;
            Profesional.Enabled = false;
            ViewState["HorarioAtencionId"] = horarioId;
            ViewState["HorarioProfesionalId"] = profesionalId;
            ViewState["HorarioEspecialidadId"] = especialidadId;
            DiaSemana.SelectedValue = Convert.ToString(datos.Values["DiaSemana"]);
            HoraInicio.Text = ((TimeSpan)datos.Values["HoraInicio"]).ToString(@"hh\:mm");
            HoraFin.Text = ((TimeSpan)datos.Values["HoraFin"]).ToString(@"hh\:mm");
            TituloHorario.Text = "Editar franja semanal";
            GuardarHorario.Text = "Guardar cambios";
            LimpiarMensaje();
        }

        private void LimpiarFormularioHorario()
        {
            ViewState.Remove("HorarioAtencionId");
            ViewState.Remove("HorarioProfesionalId");
            ViewState.Remove("HorarioEspecialidadId");
            Profesional.Enabled = TieneRol("ADMINISTRATIVO");
            EspecialidadHorario.Enabled = true;
            if (EspecialidadHorario.Items.Count > 0)
            {
                EspecialidadHorario.SelectedIndex = 0;
            }

            DiaSemana.SelectedValue = "1";
            HoraInicio.Text = string.Empty;
            HoraFin.Text = string.Empty;
            TituloHorario.Text = "Nueva franja semanal";
            GuardarHorario.Text = "Guardar franja";
        }

        private bool EsPropioProfesional(Guid profesionalId)
        {
            if (TieneRol("ADMINISTRATIVO"))
            {
                return true;
            }

            Guid usuarioActual;
            return TieneRol("PROFESIONAL")
                && Guid.TryParse(Convert.ToString(Session["UsuarioId"]), out usuarioActual)
                && usuarioActual == profesionalId;
        }

        private string ObtenerTextoDataKey(int fila, string clave)
        {
            object valor = HorariosGrid.DataKeys[fila].Values[clave];
            return valor == null || valor == DBNull.Value ? string.Empty : Convert.ToString(valor);
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
