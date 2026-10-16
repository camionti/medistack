using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using MediStack.Negocio;

namespace MediStack.Web
{
    public partial class CajaPagina : PaginaAdministrativa
    {
        private readonly CobrosNegocio _negocio = new CobrosNegocio();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                Desde.Text = DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                Hasta.Text = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                EjecutarConManejoDeErrores("No se pudo cargar la caja", CargarCaja);
            }
        }

        protected void Filtrar_Click(object sender, EventArgs e)
        {
            EjecutarConManejoDeErrores("No se pudo consultar la caja", CargarCaja);
        }

        protected void Cerrar_Click(object sender, EventArgs e)
        {
            decimal efectivo;
            if (!decimal.TryParse(EfectivoContado.Text, NumberStyles.Number,
                    CultureInfo.InvariantCulture, out efectivo))
            {
                MostrarError("Informa el importe de efectivo contado con hasta dos decimales.");
                return;
            }

            try
            {
                _negocio.CerrarCaja(DateTime.Today, ObtenerUsuarioActual(), efectivo);
                MostrarExito("La caja de hoy se cerró correctamente.");
                EfectivoContado.Text = string.Empty;
                CargarCaja();
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(ex.Message);
            }
            catch (SqlException ex)
            {
                MostrarError("No se pudo cerrar la caja (error SQL " + ex.Number + ").");
            }
        }

        protected void RegistrarEgreso_Click(object sender, EventArgs e)
        {
            decimal monto;
            if (!decimal.TryParse(EgresoMonto.Text, NumberStyles.Number,
                    CultureInfo.InvariantCulture, out monto))
            {
                MostrarError("Informa el monto del egreso con hasta dos decimales.");
                return;
            }

            try
            {
                _negocio.RegistrarEgreso(ObtenerUsuarioActual(), EgresoConcepto.Text, monto,
                    EgresoCategoria.SelectedValue, EgresoComprobante.Text);
                MostrarExito("El egreso se registró correctamente.");
                EgresoConcepto.Text = string.Empty;
                EgresoMonto.Text = string.Empty;
                EgresoComprobante.Text = string.Empty;
                CargarCaja();
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(ex.Message);
            }
            catch (SqlException ex)
            {
                MostrarError("No se pudo registrar el egreso (error SQL " + ex.Number + ").");
            }
        }

        private void CargarCaja()
        {
            DateTime desde;
            DateTime hasta;
            if (!DateTime.TryParseExact(Desde.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out desde)
                || !DateTime.TryParseExact(Hasta.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out hasta))
            {
                MostrarError("Selecciona un período de fechas válido.");
                return;
            }

            DataTable totales = _negocio.ObtenerTotalesCaja(desde, hasta);
            TotalesGrid.DataSource = totales;
            TotalesGrid.DataBind();
            MovimientosGrid.DataSource = _negocio.ObtenerMovimientosCaja(desde, hasta);
            MovimientosGrid.DataBind();
            EgresosGrid.DataSource = _negocio.ObtenerEgresos(desde, hasta);
            EgresosGrid.DataBind();
            CierresGrid.DataSource = _negocio.ObtenerCierresCaja(desde, hasta);
            CierresGrid.DataBind();

            FechaHoy.Text = HttpUtility.HtmlEncode(DateTime.Today.ToString(
                "dd/MM/yyyy", CultureInfo.GetCultureInfo("es-AR")));
            DataTable cierre = _negocio.ObtenerCierreCaja(DateTime.Today);
            if (cierre.Rows.Count == 0)
            {
                EstadoHoy.Text = "<span class=\"status-tag status-available\">Abierta</span>";
                CerrarPanel.Visible = true;
                EgresoPanel.Visible = true;
                decimal egresosHoy = _negocio.ObtenerTotalEgresos(DateTime.Today);
                if (egresosHoy > 0)
                {
                    EstadoHoy.Text += " · Egresos de hoy: " + HttpUtility.HtmlEncode(
                        egresosHoy.ToString("C2", CultureInfo.GetCultureInfo("es-AR")));
                }
            }
            else
            {
                DataRow fila = cierre.Rows[0];
                EstadoHoy.Text = "<span class=\"status-tag status-occupied\">Cerrada</span>"
                    + " · Total: " + HttpUtility.HtmlEncode(Convert.ToDecimal(fila["TotalGeneral"])
                        .ToString("C2", CultureInfo.GetCultureInfo("es-AR")))
                    + " · Diferencia de efectivo: " + HttpUtility.HtmlEncode(Convert.ToDecimal(fila["Diferencia"])
                        .ToString("C2", CultureInfo.GetCultureInfo("es-AR")));
                CerrarPanel.Visible = false;
                EgresoPanel.Visible = false;
            }
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
    }
}
