using System;

namespace MediStack.Dominio
{
    // Modelos de pagos online (Mercado Pago).

    // Seña de un turno vista desde el checkout (PagarSena.aspx). Todo sale de la base.
    public sealed class SenaParaPagar
    {
        public int TurnoId { get; set; }
        public Guid PacienteId { get; set; }
        public string Profesional { get; set; }
        public string Especialidad { get; set; }
        public DateTime FechaHora { get; set; }
        public string Estado { get; set; }
        public string EstadoSena { get; set; }
        public decimal MontoSena { get; set; }
        public DateTime? VencimientoSena { get; set; }

        // Se puede pagar: seña pendiente, turno vigente y todavia dentro del plazo.
        public bool PuedePagarse
        {
            get
            {
                return EstadoSena == "Pendiente"
                    && (Estado == "Solicitado" || Estado == "Confirmado")
                    && MontoSena > 0
                    && VencimientoSena.HasValue
                    && VencimientoSena.Value > DateTime.Now;
            }
        }
    }

    // Un pago tal como lo informa la API de Mercado Pago (GET /v1/payments/{id}).
    // Solo los datos que MediStack necesita para verificar y acreditar una seña.
    public sealed class PagoMercadoPago
    {
        public string Id { get; set; }

        // approved, pending, in_process, rejected, cancelled, refunded, ...
        public string Estado { get; set; }

        // Nuestro TurnoId (lo mandamos como external_reference al crear la preferencia).
        // null si el pago no trae una referencia valida.
        public int? TurnoId { get; set; }

        public decimal Monto { get; set; }

        public string Moneda { get; set; }

        public bool EstaAprobado
        {
            get { return Estado == "approved"; }
        }
    }

    // Que paso al procesar un pago de Mercado Pago (vuelta del navegador o webhook).
    public enum EstadoProcesamientoPago
    {
        // Pago aprobado y seña acreditada (ahora o en un aviso anterior): turno confirmado.
        Acreditado,

        // Mercado Pago todavia lo esta procesando (pending / in_process): se acredita cuando llegue el aviso.
        EnProceso,

        // Rechazado, cancelado o devuelto: no se cobro nada.
        Rechazado,

        // Se cobro pero no se pudo acreditar (turno vencido/cancelado o monto distinto): revisar y reembolsar.
        RequiereRevision,

        // El pago no tiene relacion con un turno de MediStack: se ignora.
        NoCorresponde
    }

    public sealed class ResultadoPagoSena
    {
        public EstadoProcesamientoPago Estado { get; set; }
        public int? TurnoId { get; set; }
    }

    // Resultado de acreditar una seña pagada online (Mercado Pago).
    public enum ResultadoAcreditacionSena
    {
        // Se registro ahora: cobro creado, seña 'Pagada' y turno 'Confirmado'.
        Acreditada,

        // Ese mismo pago ya se habia registrado antes (aviso repetido): no se hace nada.
        YaAcreditada,

        // El turno ya no espera este pago (no existe, se cancelo, la seña vencio o ya se pago
        // por otro medio). El pago quedo aprobado en Mercado Pago: hay que revisarlo y reembolsarlo.
        TurnoNoDisponible,

        // El importe pagado no coincide con la seña del turno: no se acredita, hay que revisarlo.
        MontoIncorrecto
    }

    // Datos completos para la emisión de un comprobante / recibo oficial de pago (PDF).
    public sealed class ComprobanteCobroModel
    {
        public int CobroId { get; set; }
        public int TurnoId { get; set; }
        public DateTime FechaHoraCobro { get; set; }
        public string TipoCobro { get; set; }
        public string MedioPago { get; set; }
        public decimal MontoBase { get; set; }
        public decimal MontoObraSocial { get; set; }
        public decimal Copago { get; set; }
        public decimal SenaDescontada { get; set; }
        public decimal MontoCobrado { get; set; }
        public decimal MontoRecibido { get; set; }
        public decimal Vuelto { get; set; }

        public Guid PacienteId { get; set; }
        public string PacienteNombreCompleto { get; set; }
        public string PacienteDni { get; set; }
        public string ObraSocialNombre { get; set; }
        public string NumeroAfiliado { get; set; }

        public DateTime FechaHoraTurno { get; set; }
        public string ProfesionalNombreCompleto { get; set; }
        public string ProfesionalMatricula { get; set; }
        public string EspecialidadNombre { get; set; }

        public string CajeroNombreCompleto { get; set; }
    }
}
