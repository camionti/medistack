using System;
using System.Globalization;
using MediStack.Datos;
using MediStack.Dominio;

namespace MediStack.Negocio
{
    // Orquesta el pago online de la seña: junta los datos del turno (TurnosDatos),
    // Mercado Pago (MercadoPagoServicio) y el registro del cobro (CobrosNegocio).
    public sealed class PagoSenaNegocio
    {
        private readonly TurnosDatos _turnosDatos = new TurnosDatos();
        private readonly MercadoPagoServicio _mercadoPago = new MercadoPagoServicio();
        private readonly CobrosNegocio _cobros = new CobrosNegocio();

        // Datos de la seña para mostrar en el checkout. Solo el paciente dueño del turno puede verla.
        public SenaParaPagar ObtenerSenaParaPagar(int turnoId, Guid pacienteId)
        {
            if (turnoId <= 0 || pacienteId == Guid.Empty)
            {
                throw new InvalidOperationException("No encontramos el turno a pagar.");
            }

            // Limpieza perezosa: si la reserva ya vencio, que se vea como vencida.
            _turnosDatos.LiberarReservasVencidas();

            SenaParaPagar sena = _turnosDatos.ObtenerSenaDeTurno(turnoId);

            // Mismo mensaje si no existe o si es de otro paciente: no revelamos turnos ajenos.
            if (sena == null || sena.PacienteId != pacienteId)
            {
                throw new InvalidOperationException("No encontramos el turno a pagar.");
            }

            return sena;
        }

        // Crea el cobro en Mercado Pago y devuelve el link de pago.
        // Vuelve a leer y validar todo desde la base: no confia en lo que mostraba la pantalla.
        public string IniciarPago(int turnoId, Guid pacienteId)
        {
            SenaParaPagar sena = ObtenerSenaParaPagar(turnoId, pacienteId);
            if (!sena.PuedePagarse)
            {
                throw new InvalidOperationException(
                    "Esta seña ya no se puede pagar: el plazo vencio o el turno ya no la requiere.");
            }

            string descripcion = "Seña turno " + sena.Especialidad + " - "
                + sena.FechaHora.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
                + " - " + sena.Profesional;

            return _mercadoPago.CrearPreferenciaSena(
                sena.TurnoId, descripcion, sena.MontoSena, sena.VencimientoSena.Value);
        }

        // Procesa un pago informado por Mercado Pago. Lo usan la pagina de vuelta y el webhook.
        // Nunca confia en datos de la URL o del aviso: consulta el pago real a la API de Mercado Pago
        // y recien ahi acredita. Es idempotente: se puede llamar varias veces con el mismo pago.
        public ResultadoPagoSena ProcesarPago(string pagoId)
        {
            PagoMercadoPago pago = _mercadoPago.ObtenerPago(pagoId);

            if (!pago.TurnoId.HasValue)
            {
                return new ResultadoPagoSena { Estado = EstadoProcesamientoPago.NoCorresponde };
            }

            ResultadoPagoSena resultado = new ResultadoPagoSena { TurnoId = pago.TurnoId };
            switch (pago.Estado)
            {
                case "approved":
                    if (pago.Moneda != "ARS")
                    {
                        resultado.Estado = EstadoProcesamientoPago.RequiereRevision;
                        break;
                    }

                    ResultadoAcreditacionSena acreditacion =
                        _cobros.AcreditarSenaOnline(pago.TurnoId.Value, pago.Id, pago.Monto);
                    resultado.Estado = acreditacion == ResultadoAcreditacionSena.Acreditada
                        || acreditacion == ResultadoAcreditacionSena.YaAcreditada
                        ? EstadoProcesamientoPago.Acreditado
                        : EstadoProcesamientoPago.RequiereRevision;
                    break;

                case "pending":
                case "in_process":
                case "authorized":
                    resultado.Estado = EstadoProcesamientoPago.EnProceso;
                    break;

                default:
                    // rejected, cancelled, refunded, charged_back...
                    resultado.Estado = EstadoProcesamientoPago.Rechazado;
                    break;
            }

            return resultado;
        }
    }
}
