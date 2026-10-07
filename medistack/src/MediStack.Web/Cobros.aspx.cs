using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;
using MediStack.Negocio;

namespace MediStack.Web
{
    public partial class CobrosPagina : PaginaProtegida
    {
        private readonly CobrosNegocio _negocio = new CobrosNegocio();

        protected void Page_Load(object sender, EventArgs e)
        {
            bool esPaciente = TieneRol("PACIENTE");
            bool esProfesional = TieneRol("PROFESIONAL");
            bool esAdministrativo = TieneRol("ADMINISTRATIVO");
            if (!esPaciente && !esProfesional && !esAdministrativo)
            {
                Response.Redirect(ResolveUrl("~/Dashboard.aspx"), true);
            }

            RegistroPanel.Visible = esAdministrativo;
            AlcanceCobros.Text = esPaciente
                ? "Consulta tus propios cobros."
                : esProfesional
                    ? "Consulta los cobros asociados a tus turnos."
                    : "Consulta los cobros de la clínica y registra pagos vinculados a turnos.";

            if (!IsPostBack)
            {
                if (esAdministrativo)
                {
                    CargarTurnosCobrables();
                    ActualizarOpcionesCobro();
                }

                EjecutarConManejoDeErrores("No se pudieron cargar los cobros", CargarCobros);
            }
        }

        protected void Filtrar_Click(object sender, EventArgs e)
        {
            EjecutarConManejoDeErrores("No se pudieron filtrar los cobros", CargarCobros);
        }

        protected void LimpiarFiltro_Click(object sender, EventArgs e)
        {
            Desde.Text = string.Empty;
            Hasta.Text = string.Empty;
            EjecutarConManejoDeErrores("No se pudieron cargar los cobros", CargarCobros);
        }

        protected void Turno_SelectedIndexChanged(object sender, EventArgs e)
        {
            LimpiarMensaje();
            EjecutarConManejoDeErrores("No se pudo consultar el turno", ActualizarOpcionesCobro);
        }

        protected void TipoCobro_SelectedIndexChanged(object sender, EventArgs e)
        {
            LimpiarMensaje();
            EjecutarConManejoDeErrores("No se pudo calcular el importe", MostrarImporteCobro);
        }

        protected void Registrar_Click(object sender, EventArgs e)
        {
            int turnoId;
            decimal montoRecibido;
            if (!int.TryParse(Turno.SelectedValue, out turnoId)
                || !decimal.TryParse(MontoRecibido.Text, NumberStyles.Number,
                    CultureInfo.InvariantCulture, out montoRecibido))
            {
                MostrarError("Selecciona un turno e informa un importe recibido válido.");
                return;
            }

            try
            {
                _negocio.RegistrarCobro(
                    turnoId,
                    TipoCobro.SelectedValue,
                    MedioPago.SelectedValue,
                    montoRecibido,
                    ObtenerUsuarioActual());
                MostrarExito("El cobro se registró correctamente.");
                MontoRecibido.Text = string.Empty;
                CargarTurnosCobrables();
                ActualizarOpcionesCobro();
                CargarCobros();
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(ex.Message);
                ActualizarOpcionesCobro();
            }
            catch (SqlException ex)
            {
                MostrarError("No se pudo registrar el cobro (error SQL " + ex.Number + ").");
            }
        }

        private void CargarCobros()
        {
            DateTime? desde;
            DateTime? hasta;
            string errorFiltro;
            if (!TryLeerFiltroFechas(Desde, Hasta, out desde, out hasta, out errorFiltro))
            {
                MostrarError(errorFiltro);
                return;
            }

            Guid? pacienteId = TieneRol("PACIENTE") ? (Guid?)ObtenerUsuarioActual() : null;
            Guid? profesionalId = TieneRol("PROFESIONAL") ? (Guid?)ObtenerUsuarioActual() : null;
            CobrosGrid.DataSource = _negocio.ObtenerCobros(pacienteId, profesionalId, desde, hasta);
            CobrosGrid.DataBind();
            string periodo = !desde.HasValue && !hasta.HasValue ? "sin filtro de fecha"
                : desde.HasValue && hasta.HasValue && desde.Value == hasta.Value
                    ? "el " + desde.Value.ToString("dd/MM/yyyy")
                : (desde.HasValue ? "desde el " + desde.Value.ToString("dd/MM/yyyy") + " " : string.Empty)
                    + (hasta.HasValue ? "hasta el " + hasta.Value.ToString("dd/MM/yyyy") : string.Empty);
            ResumenFiltro.Text = HttpUtility.HtmlEncode(
                CobrosGrid.Rows.Count + (CobrosGrid.Rows.Count == 1 ? " cobro" : " cobros") + " (" + periodo.Trim() + ").");
        }

        private void CargarTurnosCobrables()
        {
            Turno.Items.Clear();
            Turno.Items.Add(new ListItem("Selecciona un turno", string.Empty));
            DataTable turnos = _negocio.ObtenerTurnosCobrables(null, null);
            foreach (DataRow fila in turnos.Rows)
            {
                string texto = "#" + fila["TurnoId"] + " · "
                    + Convert.ToDateTime(fila["FechaTurno"]).ToString("dd/MM/yyyy HH:mm")
                    + " · " + fila["Paciente"] + " · " + fila["Profesional"]
                    + " · " + fila["Especialidad"];
                Turno.Items.Add(new ListItem(texto, Convert.ToString(fila["TurnoId"])));
            }
        }

        private void ActualizarOpcionesCobro()
        {
            TipoCobro.Items.Clear();
            TipoCobro.Items.Add(new ListItem("Selecciona un concepto", string.Empty));
            int turnoId;
            if (!int.TryParse(Turno.SelectedValue, out turnoId))
            {
                ImporteCobro.Text = "Selecciona un turno para ver los conceptos disponibles.";
                return;
            }

            DataTable turnos = _negocio.ObtenerTurnosCobrables(null, null);
            DataRow seleccionado = null;
            foreach (DataRow fila in turnos.Rows)
            {
                if (Convert.ToInt32(fila["TurnoId"]) == turnoId)
                {
                    seleccionado = fila;
                    break;
                }
            }

            if (seleccionado == null)
            {
                ImporteCobro.Text = "El turno ya no tiene conceptos pendientes de cobro. Actualiza la página.";
                return;
            }

            if (Convert.ToBoolean(seleccionado["PuedeCobrarSena"]))
            {
                TipoCobro.Items.Add(new ListItem("Seña", "Sena"));
            }

            if (Convert.ToBoolean(seleccionado["PuedeCobrarConsulta"]))
            {
                TipoCobro.Items.Add(new ListItem("Consulta", "Consulta"));
            }

            if (TipoCobro.Items.Count > 1)
            {
                TipoCobro.SelectedIndex = 1;
                MostrarImporteCobro();
            }
            else
            {
                ImporteCobro.Text = "No hay conceptos pendientes de cobro para este turno.";
            }
        }

        private void MostrarImporteCobro()
        {
            int turnoId;
            if (!int.TryParse(Turno.SelectedValue, out turnoId)
                || string.IsNullOrEmpty(TipoCobro.SelectedValue))
            {
                ImporteCobro.Text = "Selecciona un turno y un concepto.";
                return;
            }

            DataTable importes = _negocio.ObtenerImportesCobro(turnoId);
            if (importes.Rows.Count == 0)
            {
                throw new InvalidOperationException("El turno ya no está disponible para cobrar.");
            }

            DataRow fila = importes.Rows[0];
            decimal importe = TipoCobro.SelectedValue == "Sena"
                ? Convert.ToDecimal(fila["MontoSena"])
                : Convert.ToDecimal(fila["SaldoConsulta"]);
            ImporteCobro.Text = HttpUtility.HtmlEncode(importe.ToString("C2", CultureInfo.GetCultureInfo("es-AR")));
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

        private Guid ObtenerUsuarioActual()
        {
            Guid usuarioId;
            if (!Guid.TryParse(Convert.ToString(Session["UsuarioId"]), out usuarioId))
            {
                throw new InvalidOperationException("No se pudo validar el usuario de la sesión.");
            }

            return usuarioId;
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
