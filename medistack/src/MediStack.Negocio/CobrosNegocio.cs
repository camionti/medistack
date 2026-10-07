using System;
using System.Data;
using System.Data.SqlClient;
using MediStack.Datos;

namespace MediStack.Negocio
{
    public sealed class CobrosNegocio
    {
        private readonly CobrosDatos _datos = new CobrosDatos();

        public DataTable ObtenerCobros(
            Guid? pacienteId, Guid? profesionalId, DateTime? desde, DateTime? hasta)
        {
            if (desde.HasValue && hasta.HasValue)
            {
                ValidarPeriodo(desde.Value, hasta.Value);
            }

            return _datos.ObtenerCobros(pacienteId, profesionalId, desde, hasta);
        }

        public DataTable ObtenerTurnosCobrables(Guid? pacienteId, Guid? profesionalId)
        {
            return _datos.ObtenerTurnosCobrables(pacienteId, profesionalId);
        }

        public DataTable ObtenerImportesCobro(int turnoId)
        {
            if (turnoId <= 0)
            {
                throw new InvalidOperationException("Selecciona un turno válido.");
            }

            return _datos.ObtenerImportesCobro(turnoId);
        }

        public void RegistrarCobro(
            int turnoId, string tipoCobro, string medioPago, decimal montoRecibido, Guid cobradorId)
        {
            if (turnoId <= 0)
            {
                throw new InvalidOperationException("Selecciona un turno válido.");
            }

            if (tipoCobro != "Sena" && tipoCobro != "Consulta")
            {
                throw new InvalidOperationException("Selecciona un tipo de cobro válido.");
            }

            if (medioPago != "Efectivo" && medioPago != "Tarjeta" && medioPago != "Transferencia")
            {
                throw new InvalidOperationException("Selecciona un medio de pago válido.");
            }

            if (montoRecibido < 0 || montoRecibido > 9999999999.99m
                || decimal.Round(montoRecibido, 2) != montoRecibido)
            {
                throw new InvalidOperationException("El importe recibido debe ser válido y tener como máximo dos decimales.");
            }

            if (cobradorId == Guid.Empty)
            {
                throw new InvalidOperationException("No se pudo validar al usuario administrativo.");
            }

            EjecutarSeguro(() => _datos.RegistrarCobro(
                turnoId, tipoCobro, medioPago, montoRecibido, cobradorId));
        }

        public DataTable ObtenerMovimientosCaja(DateTime desde, DateTime hasta)
        {
            ValidarPeriodo(desde, hasta);
            return _datos.ObtenerMovimientosCaja(desde, hasta);
        }

        public DataTable ObtenerTotalesCaja(DateTime desde, DateTime hasta)
        {
            ValidarPeriodo(desde, hasta);
            return _datos.ObtenerTotalesCaja(desde, hasta);
        }

        public DataTable ObtenerCierresCaja(DateTime desde, DateTime hasta)
        {
            ValidarPeriodo(desde, hasta);
            return _datos.ObtenerCierresCaja(desde, hasta);
        }

        public DataTable ObtenerCierreCaja(DateTime fecha)
        {
            return _datos.ObtenerCierreCaja(fecha.Date);
        }

        public void CerrarCaja(DateTime fecha, Guid administrativoId, decimal efectivoContado)
        {
            if (fecha.Date != DateTime.Today)
            {
                throw new InvalidOperationException("La caja solo se puede cerrar para la fecha de hoy.");
            }

            if (administrativoId == Guid.Empty)
            {
                throw new InvalidOperationException("No se pudo validar al usuario administrativo.");
            }

            if (efectivoContado < 0 || efectivoContado > 9999999999.99m
                || decimal.Round(efectivoContado, 2) != efectivoContado)
            {
                throw new InvalidOperationException("El efectivo contado debe ser válido y tener como máximo dos decimales.");
            }

            EjecutarSeguro(() => _datos.CerrarCaja(fecha, administrativoId, efectivoContado));
        }

        private static void ValidarPeriodo(DateTime desde, DateTime hasta)
        {
            if (desde.Date > hasta.Date)
            {
                throw new InvalidOperationException("La fecha inicial no puede ser posterior a la fecha final.");
            }

            if (hasta.Date > DateTime.Today.AddYears(5))
            {
                throw new InvalidOperationException("El rango de fechas no puede superar cinco años.");
            }
        }

        private static void EjecutarSeguro(Action accion)
        {
            try
            {
                accion();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (SqlException ex)
            {
                if (ex.Number >= 51101 && ex.Number <= 51112)
                {
                    throw new InvalidOperationException(ex.Message);
                }

                if (ex.Number == 2601 || ex.Number == 2627)
                {
                    throw new InvalidOperationException("El turno ya tiene registrado ese tipo de cobro, o la caja de la fecha ya fue cerrada.");
                }

                if (ex.Number == 1205)
                {
                    throw new InvalidOperationException("La operación tuvo un conflicto con otro cobro o cierre. Actualiza la información e intenta nuevamente.");
                }

                throw new InvalidOperationException("No se pudo completar la operación en MediStackDB (error SQL " + ex.Number + ").");
            }
        }
    }
}
