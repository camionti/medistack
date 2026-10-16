using System;
using System.Globalization;
using MediStack.Negocio;

namespace MediStack.Web
{
    public partial class ReportesPagina : PaginaAdministrativa
    {
        private readonly ReportesNegocio _negocio = new ReportesNegocio();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                Desde.Text = DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                Hasta.Text = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                Generar();
            }
        }

        protected void Filtrar_Click(object sender, EventArgs e)
        {
            Generar();
        }

        private void Generar()
        {
            LimpiarMensaje(Mensaje);
            DateTime desde;
            DateTime hasta;
            if (!DateTime.TryParseExact(Desde.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out desde)
                || !DateTime.TryParseExact(Hasta.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out hasta))
            {
                MostrarError(Mensaje, "Selecciona un período de fechas válido.");
                return;
            }

            CargarConManejoDeErrores(Mensaje, "No se pudieron generar los reportes", delegate
            {
                TurnosEstadoGrid.DataSource = _negocio.TurnosPorEstado(desde, hasta);
                TurnosEstadoGrid.DataBind();
                TurnosProfesionalGrid.DataSource = _negocio.TurnosPorProfesional(desde, hasta);
                TurnosProfesionalGrid.DataBind();
                TurnosEspecialidadGrid.DataSource = _negocio.TurnosPorEspecialidad(desde, hasta);
                TurnosEspecialidadGrid.DataBind();
                IngresosMedioGrid.DataSource = _negocio.IngresosPorMedioPago(desde, hasta);
                IngresosMedioGrid.DataBind();
                IngresosEspecialidadGrid.DataSource = _negocio.IngresosPorEspecialidad(desde, hasta);
                IngresosEspecialidadGrid.DataBind();
                ObrasSocialesGrid.DataSource = _negocio.ConsultasPorObraSocial(desde, hasta);
                ObrasSocialesGrid.DataBind();
                HonorariosGrid.DataSource = _negocio.HonorariosLiquidados(desde, hasta);
                HonorariosGrid.DataBind();
                EgresosGrid.DataSource = _negocio.EgresosPorCategoria(desde, hasta);
                EgresosGrid.DataBind();
                AuditoriaIAGrid.DataSource = _negocio.InteraccionesIA(desde, hasta);
                AuditoriaIAGrid.DataBind();
            });
        }
    }
}
