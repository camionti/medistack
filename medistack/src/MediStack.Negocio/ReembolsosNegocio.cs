using System;
using System.Data;
using System.Data.SqlClient;
using MediStack.Datos;
using MediStack.Dominio;

namespace MediStack.Negocio
{
    // Reembolsos de señas: todos se completan a mano desde el panel de Reembolsos.
    public sealed class ReembolsosNegocio
    {
        private readonly ReembolsosDatos _datos = new ReembolsosDatos();

        public DataTable ObtenerReembolsosPendientes()
        {
            return _datos.ObtenerReembolsosPendientes();
        }

        public DataTable ObtenerHistorialReembolsos(DateTime desde, DateTime hasta)
        {
            if (desde.Date > hasta.Date)
            {
                throw new InvalidOperationException("La fecha inicial no puede ser posterior a la fecha final.");
            }

            return _datos.ObtenerHistorialReembolsos(desde, hasta);
        }

        public void DevolverEnEfectivo(int reembolsoId, Guid administrativoId)
        {
            ValidarIds(reembolsoId, administrativoId);
            EjecutarSeguro(() => _datos.CompletarReembolsoEfectivo(reembolsoId, administrativoId));
        }

        public void RegistrarDevolucionManual(int reembolsoId, Guid administrativoId, string comprobante)
        {
            ValidarIds(reembolsoId, administrativoId);
            comprobante = (comprobante ?? string.Empty).Trim();
            if (comprobante.Length == 0 || comprobante.Length > 100)
            {
                throw new InvalidOperationException(
                    "Ingresá el número de comprobante de la devolución (hasta 100 caracteres).");
            }

            EjecutarSeguro(() => _datos.CompletarReembolsoManual(reembolsoId, administrativoId, comprobante));
        }

        private static void ValidarIds(int reembolsoId, Guid administrativoId)
        {
            if (reembolsoId <= 0)
            {
                throw new InvalidOperationException("Seleccioná un reembolso válido.");
            }

            if (administrativoId == Guid.Empty)
            {
                throw new InvalidOperationException("No se pudo validar al usuario administrativo.");
            }
        }

        private static void EjecutarSeguro(Action accion)
        {
            try
            {
                accion();
            }
            catch (SqlException ex)
            {
                // 51111/51113: caja cerrada o sin abrir · 51112: usuario no habilitado ·
                // 51120-51122: reembolso inexistente, ya completado o de otro medio.
                if ((ex.Number >= 51111 && ex.Number <= 51113) || (ex.Number >= 51120 && ex.Number <= 51122))
                {
                    throw new InvalidOperationException(ex.Message);
                }

                throw;
            }
        }
    }
}
