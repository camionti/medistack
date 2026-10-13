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
}
