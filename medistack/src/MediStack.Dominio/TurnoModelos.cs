using System;

namespace MediStack.Dominio
{
    public sealed class TurnoDisponible
    {
        public Guid ProfesionalId { get; set; }
        public int EspecialidadId { get; set; }
        public string Especialidad { get; set; }
        public DateTime FechaHora { get; set; }
        public DateTime Fin { get; set; }
        public short DuracionMinutos { get; set; }
        public bool Disponible { get; set; }
        public int? TurnoId { get; set; }
        public string Paciente { get; set; }
        public string Estado { get; set; }
    }

    public sealed class SolicitudTurno
    {
        public Guid PacienteId { get; set; }
        public Guid ProfesionalId { get; set; }
        public int EspecialidadId { get; set; }
        public DateTime FechaHora { get; set; }
        public string Motivo { get; set; }

        // true = la reserva la hace el propio paciente desde la web (canal online).
        // false = la carga el administrativo (canal presencial): nunca exige seña al reservar.
        public bool EsReservaOnline { get; set; }

        // Solo canal presencial: con que medio el administrativo cobra la seña en el mostrador
        // (Efectivo, Tarjeta o Transferencia) y quien la cobra. Si el paciente no tiene cobertura
        // para el turno, la seña es obligatoria y se cobra en el mismo momento de reservar.
        public string MedioPagoSena { get; set; }
        public Guid? CobradorId { get; set; }
    }

    // Reglas de turnos compartidas por Datos (la regla real) y Web (para mostrar u ocultar botones).
    // Estan en Dominio porque es el unico proyecto que referencian ambos.
    public static class ReglasTurno
    {
        public const int HorasMinimasCancelacionPaciente = 24;

        // true si al turno le faltan al menos 24 hs: el paciente todavia puede cancelarlo o reprogramarlo.
        public static bool PacienteDentroDelPlazo(DateTime fechaHoraTurno)
        {
            return fechaHoraTurno >= DateTime.Now.AddHours(HorasMinimasCancelacionPaciente);
        }

        // --- Seña de reservas online ---
        // Un paciente sin cobertura para el turno (sin obra social o sin convenio vigente con ese
        // profesional y especialidad) debe pagar una seña para reservar online.
        public const decimal PorcentajeSenaParticular = 50m;

        // Minutos que tiene el paciente para pagar la seña antes de que la reserva se libere.
        public const int MinutosParaPagarSena = 15;

        // Margen extra antes de liberar el horario: el paciente y Mercado Pago ven 15 minutos, pero la
        // limpieza perezosa recien libera a los 15 + 5. Asi un pago hecho en el ultimo segundo, cuyo aviso
        // llega un poco despues, todavia encuentra la seña Pendiente y se acredita.
        public const int MinutosMargenPagoSena = 5;

        // Monto de la seña: un porcentaje del valor de la consulta, redondeado a centavos.
        public static decimal CalcularSena(decimal valorConsulta)
        {
            if (valorConsulta <= 0)
            {
                return 0m;
            }

            return Math.Round(valorConsulta * PorcentajeSenaParticular / 100m, 2, MidpointRounding.AwayFromZero);
        }

        // Hasta cuando se guarda el lugar de una reserva con seña pendiente.
        public static DateTime CalcularVencimientoSena(DateTime momentoReserva)
        {
            return momentoReserva.AddMinutes(MinutosParaPagarSena);
        }
    }

    // Profesional que atiende una especialidad, visto desde un paciente concreto:
    // si su obra social lo cubre y, si no, cuanto pagaria de seña al reservar online.
    public sealed class ProfesionalParaPaciente
    {
        public Guid ProfesionalId { get; set; }
        public string Nombre { get; set; }
        public decimal ValorConsulta { get; set; }
        public bool TieneCobertura { get; set; }
        public decimal MontoSena { get; set; }
    }

    public sealed class ResumenDisponibilidadProfesional
    {
        public Guid ProfesionalId { get; set; }
        public string Nombre { get; set; }
        public DateTime? ProximaFecha { get; set; }
        public int CantidadFechas { get; set; }
        public bool TieneDisponibilidad { get; set; }
        public bool TieneCobertura { get; set; }
        public decimal MontoSena { get; set; }
        public string MotivoSinDisponibilidad { get; set; }
    }

    public sealed class FechaDisponible
    {
        public Guid ProfesionalId { get; set; }
        public int EspecialidadId { get; set; }
        public DateTime Fecha { get; set; }
        public int CantidadHorarios { get; set; }
    }

    // Resultado de reservar un turno: su Id y la seña (0 si no requiere).
    public sealed class ReservaTurno
    {
        public int TurnoId { get; set; }
        public decimal MontoSena { get; set; }

        // true si la seña ya se cobró en el mostrador al reservar (canal presencial).
        public bool SenaCobradaEnMostrador { get; set; }
        public string MedioPagoSena { get; set; }

        // Hay una seña pendiente que el paciente todavia tiene que pagar (canal online).
        public bool RequierePagoSena
        {
            get { return MontoSena > 0 && !SenaCobradaEnMostrador; }
        }
    }

    public sealed class TurnoDetalle
    {
        public int TurnoId { get; set; }
        public Guid PacienteId { get; set; }
        public Guid ProfesionalId { get; set; }
        public int EspecialidadId { get; set; }
        public DateTime FechaHora { get; set; }
        public string Estado { get; set; }
        public string Motivo { get; set; }
    }

    public sealed class NotificacionTurno
    {
        public int TurnoId { get; set; }
        public Guid PacienteId { get; set; }
        public string PacienteNombre { get; set; }
        public string PacienteEmail { get; set; }
        public string ProfesionalNombre { get; set; }
        public string Especialidad { get; set; }
        public DateTime FechaHora { get; set; }
        public string Estado { get; set; }
        public decimal MontoSena { get; set; }
        public string EstadoSena { get; set; }
        public string UrlVerTurno { get; set; }
        public string UrlCancelarTurno { get; set; }
    }
}
