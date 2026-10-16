using System;
using System.Data;
using System.Data.SqlClient;
using MediStack.Datos;

namespace MediStack.Negocio
{
    public sealed class HonorariosNegocio
    {
        private readonly HonorariosDatos _datos = new HonorariosDatos();

        public DataTable ObtenerProfesionales()
        {
            return _datos.ObtenerProfesionales();
        }

        public DataTable CalcularDetalle(Guid profesionalId, DateTime desde, DateTime hasta)
        {
            ValidarSeleccion(profesionalId, desde, hasta);
            return _datos.CalcularDetalle(profesionalId, desde, hasta);
        }

        public DataTable ObtenerLiquidaciones(Guid? profesionalId, DateTime? desde, DateTime? hasta)
        {
            if (desde.HasValue && hasta.HasValue && desde.Value.Date > hasta.Value.Date)
            {
                throw new InvalidOperationException("La fecha inicial no puede ser posterior a la fecha final.");
            }

            return _datos.ObtenerLiquidaciones(profesionalId, desde, hasta);
        }

        public decimal Liquidar(Guid profesionalId, DateTime desde, DateTime hasta)
        {
            ValidarSeleccion(profesionalId, desde, hasta);
            try
            {
                return _datos.Liquidar(profesionalId, desde, hasta);
            }
            catch (SqlException ex)
            {
                if (ex.Number >= 51120 && ex.Number <= 51122)
                {
                    throw new InvalidOperationException(ex.Message);
                }

                if (ex.Number == 2601 || ex.Number == 2627)
                {
                    throw new InvalidOperationException("Ya existe una liquidación para ese profesional y período.");
                }

                if (ex.Number == 1205)
                {
                    throw new InvalidOperationException("La operación tuvo un conflicto con otra liquidación o cobro. Intenta nuevamente.");
                }

                throw new InvalidOperationException("No se pudo registrar la liquidación (error SQL " + ex.Number + ").");
            }
        }

        private static void ValidarSeleccion(Guid profesionalId, DateTime desde, DateTime hasta)
        {
            if (profesionalId == Guid.Empty)
            {
                throw new InvalidOperationException("Selecciona un profesional.");
            }

            if (desde.Date > hasta.Date)
            {
                throw new InvalidOperationException("La fecha inicial no puede ser posterior a la fecha final.");
            }

            if (hasta.Date > DateTime.Today)
            {
                throw new InvalidOperationException("El período a liquidar no puede incluir fechas futuras.");
            }

            if (desde.Date < new DateTime(2000, 1, 1))
            {
                throw new InvalidOperationException("La fecha inicial no es válida.");
            }
        }
    }
}
