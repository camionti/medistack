using System;
using System.Data;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;
using MediStack.Negocio;

namespace MediStack.Web
{
    public partial class LiquidacionesPagina : PaginaAdministrativa
    {
        private readonly HonorariosNegocio _negocio = new HonorariosNegocio();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                DateTime hoy = DateTime.Today;
                DateTime primerDiaMesAnterior = new DateTime(hoy.Year, hoy.Month, 1).AddMonths(-1);
                Desde.Text = primerDiaMesAnterior.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                Hasta.Text = primerDiaMesAnterior.AddMonths(1).AddDays(-1)
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                CargarConManejoDeErrores(Mensaje, "No se pudieron cargar las liquidaciones", delegate
                {
                    CargarProfesionales();
                    CargarHistorial();
                });
            }
        }

        protected void Calcular_Click(object sender, EventArgs e)
        {
            LimpiarMensaje(Mensaje);
            CargarConManejoDeErrores(Mensaje, "No se pudo calcular la liquidación", delegate
            {
                Guid profesionalId;
                DateTime desde;
                DateTime hasta;
                if (!LeerSeleccion(out profesionalId, out desde, out hasta))
                {
                    return;
                }

                MostrarDetalle(profesionalId, desde, hasta);
            });
        }

        protected void Liquidar_Click(object sender, EventArgs e)
        {
            LimpiarMensaje(Mensaje);
            Guid profesionalId;
            DateTime desde;
            DateTime hasta;
            if (!LeerSeleccion(out profesionalId, out desde, out hasta))
            {
                return;
            }

            CargarConManejoDeErrores(Mensaje, "No se pudo registrar la liquidación", delegate
            {
                decimal total = _negocio.Liquidar(profesionalId, desde, hasta);
                MostrarExito(Mensaje, "La liquidación se registró por "
                    + total.ToString("C2", CultureInfo.GetCultureInfo("es-AR")) + ".");
                DetallePanel.Visible = false;
                CargarHistorial();
            });
        }

        private bool LeerSeleccion(out Guid profesionalId, out DateTime desde, out DateTime hasta)
        {
            desde = DateTime.MinValue;
            hasta = DateTime.MinValue;
            if (!Guid.TryParse(Profesional.SelectedValue, out profesionalId))
            {
                MostrarError(Mensaje, "Selecciona un profesional.");
                return false;
            }

            if (!DateTime.TryParseExact(Desde.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out desde)
                || !DateTime.TryParseExact(Hasta.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out hasta))
            {
                MostrarError(Mensaje, "Selecciona un período de fechas válido.");
                return false;
            }

            return true;
        }

        private void MostrarDetalle(Guid profesionalId, DateTime desde, DateTime hasta)
        {
            DataTable detalle = _negocio.CalcularDetalle(profesionalId, desde, hasta);
            decimal total = 0m;
            foreach (DataRow fila in detalle.Rows)
            {
                total += Convert.ToDecimal(fila["Honorario"]);
            }

            DetalleGrid.DataSource = detalle;
            DetalleGrid.DataBind();
            TotalLiteral.Text = HttpUtility.HtmlEncode(total.ToString("C2", CultureInfo.GetCultureInfo("es-AR")));
            CantidadLiteral.Text = detalle.Rows.Count + " consulta(s) cobrada(s)";
            Liquidar.Visible = detalle.Rows.Count > 0;
            DetallePanel.Visible = true;
        }

        private void CargarProfesionales()
        {
            Profesional.DataSource = _negocio.ObtenerProfesionales();
            Profesional.DataTextField = "Nombre";
            Profesional.DataValueField = "ProfesionalId";
            Profesional.DataBind();
        }

        private void CargarHistorial()
        {
            HistorialGrid.DataSource = _negocio.ObtenerLiquidaciones(null, null, null);
            HistorialGrid.DataBind();
        }
    }
}
