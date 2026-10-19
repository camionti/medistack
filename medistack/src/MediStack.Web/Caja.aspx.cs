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
                if (Request.QueryString["abrir"] == "1" && AbrirPanel.Visible)
                {
                    MostrarError("La caja de hoy no está abierta. Para poder cobrar, abrila indicando el fondo inicial.");
                }
                else if (Request.QueryString["abrir"] == "1" && Cerrar.Text != "Cerrar caja de hoy")
                {
                    MostrarError("Quedó una caja de un día anterior sin cerrar. Cerrala y después abrí la de hoy.");
                }
            }
        }

        protected void Abrir_Click(object sender, EventArgs e)
        {
            decimal fondo;
            if (!decimal.TryParse(FondoInicial.Text, NumberStyles.Number,
                    CultureInfo.InvariantCulture, out fondo))
            {
                MostrarError("Informa el fondo inicial con hasta dos decimales (puede ser 0).");
                return;
            }

            try
            {
                _negocio.AbrirCaja(ObtenerUsuarioActual(), fondo);
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(ex.Message);
                return;
            }
            catch (SqlException ex)
            {
                MostrarError("No se pudo abrir la caja (error SQL " + ex.Number + ").");
                return;
            }

            // Si llegó redirigido desde un cobro, vuelve a donde estaba (solo destinos conocidos).
            string volver = Request.QueryString["volver"];
            if (volver == "cobros" || volver == "turnos")
            {
                Response.Redirect(ResolveUrl(volver == "cobros" ? "~/Cobros.aspx" : "~/Turnos.aspx"), false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            MostrarExito("La caja de hoy quedó abierta con un fondo inicial de "
                + fondo.ToString("C2", CultureInfo.GetCultureInfo("es-AR")) + ".");
            FondoInicial.Text = string.Empty;
            EjecutarConManejoDeErrores("No se pudo cargar la caja", CargarCaja);
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
                DateTime fechaCerrada = _negocio.CerrarCaja(ObtenerUsuarioActual(), efectivo);
                MostrarExito("La caja del " + fechaCerrada.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("es-AR"))
                    + " se cerró correctamente.");
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

            // El "hoy" de la caja lo decide SQL Server, igual que las validaciones.
            CultureInfo ar = CultureInfo.GetCultureInfo("es-AR");
            EstadoCaja estado = _negocio.ObtenerEstadoCaja();
            FechaHoy.Text = HttpUtility.HtmlEncode(estado.Hoy.ToString("dd/MM/yyyy", ar));
            AbrirPanel.Visible = false;
            CerrarPanel.Visible = false;
            EgresoPanel.Visible = false;
            Cerrar.Text = "Cerrar caja de hoy";

            switch (estado.Estado)
            {
                case EstadoCajaDia.SinAbrir:
                    EstadoHoy.Text = "<span class=\"status-tag status-occupied\">Sin abrir</span>"
                        + " · No se puede cobrar en el mostrador hasta abrir la caja.";
                    AbrirPanel.Visible = true;
                    break;

                case EstadoCajaDia.PendienteDiaAnterior:
                    string fechaPendiente = estado.FechaCaja.Value.ToString("dd/MM/yyyy", ar);
                    EstadoHoy.Text = "<span class=\"status-tag status-occupied\">Cierre pendiente</span>"
                        + " · La caja del " + HttpUtility.HtmlEncode(fechaPendiente)
                        + " quedó sin cerrar (fondo inicial: "
                        + HttpUtility.HtmlEncode(estado.FondoInicial.ToString("C2", ar))
                        + "). Contá el efectivo y cerrala para poder abrir la de hoy.";
                    Cerrar.Text = "Cerrar caja del " + fechaPendiente;
                    CerrarPanel.Visible = true;
                    break;

                case EstadoCajaDia.Abierta:
                    EstadoHoy.Text = "<span class=\"status-tag status-available\">Abierta</span>"
                        + " · Desde las " + HttpUtility.HtmlEncode(estado.FechaHoraApertura.Value.ToString("HH:mm"))
                        + " por " + HttpUtility.HtmlEncode(estado.Administrativo)
                        + " · Fondo inicial: " + HttpUtility.HtmlEncode(estado.FondoInicial.ToString("C2", ar));
                    decimal egresosHoy = _negocio.ObtenerTotalEgresos(estado.Hoy);
                    if (egresosHoy > 0)
                    {
                        EstadoHoy.Text += " · Egresos de hoy: " + HttpUtility.HtmlEncode(egresosHoy.ToString("C2", ar));
                    }

                    CerrarPanel.Visible = true;
                    EgresoPanel.Visible = true;
                    break;

                default:
                    DataRow fila = _negocio.ObtenerCierreCaja(estado.Hoy).Rows[0];
                    EstadoHoy.Text = "<span class=\"status-tag status-occupied\">Cerrada</span>"
                        + " · Fondo inicial: " + HttpUtility.HtmlEncode(Convert.ToDecimal(fila["FondoInicial"]).ToString("C2", ar))
                        + " · Total: " + HttpUtility.HtmlEncode(Convert.ToDecimal(fila["TotalGeneral"]).ToString("C2", ar))
                        + " · Diferencia de efectivo: " + HttpUtility.HtmlEncode(Convert.ToDecimal(fila["Diferencia"]).ToString("C2", ar));
                    break;
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
