using System;
using System.Data;
using MediStack.Datos;

namespace MediStack.Negocio
{
    public sealed class ReportesNegocio
    {
        private readonly ReportesDatos _datos = new ReportesDatos();

        public DataTable TurnosPorEstado(DateTime desde, DateTime hasta)
        {
            ValidarPeriodo(desde, hasta);
            return _datos.TurnosPorEstado(desde, hasta);
        }

        public DataTable TurnosPorProfesional(DateTime desde, DateTime hasta)
        {
            ValidarPeriodo(desde, hasta);
            return _datos.TurnosPorProfesional(desde, hasta);
        }

        public DataTable TurnosPorEspecialidad(DateTime desde, DateTime hasta)
        {
            ValidarPeriodo(desde, hasta);
            return _datos.TurnosPorEspecialidad(desde, hasta);
        }

        public DataTable IngresosPorMedioPago(DateTime desde, DateTime hasta)
        {
            ValidarPeriodo(desde, hasta);
            return _datos.IngresosPorMedioPago(desde, hasta);
        }

        public DataTable IngresosPorEspecialidad(DateTime desde, DateTime hasta)
        {
            ValidarPeriodo(desde, hasta);
            return _datos.IngresosPorEspecialidad(desde, hasta);
        }

        public DataTable ConsultasPorObraSocial(DateTime desde, DateTime hasta)
        {
            ValidarPeriodo(desde, hasta);
            return _datos.ConsultasPorObraSocial(desde, hasta);
        }

        public DataTable HonorariosLiquidados(DateTime desde, DateTime hasta)
        {
            ValidarPeriodo(desde, hasta);
            return _datos.HonorariosLiquidados(desde, hasta);
        }

        public DataTable EgresosPorCategoria(DateTime desde, DateTime hasta)
        {
            ValidarPeriodo(desde, hasta);
            return _datos.EgresosPorCategoria(desde, hasta);
        }

        public DataTable InteraccionesIA(DateTime desde, DateTime hasta)
        {
            ValidarPeriodo(desde, hasta);
            return _datos.InteraccionesIA(desde, hasta);
        }

        private static void ValidarPeriodo(DateTime desde, DateTime hasta)
        {
            if (desde.Date > hasta.Date)
            {
                throw new InvalidOperationException("La fecha inicial no puede ser posterior a la fecha final.");
            }

            if (desde.Date < new DateTime(2000, 1, 1) || hasta.Date > DateTime.Today.AddYears(5))
            {
                throw new InvalidOperationException("El rango de fechas no es válido.");
            }
        }
    }
}
