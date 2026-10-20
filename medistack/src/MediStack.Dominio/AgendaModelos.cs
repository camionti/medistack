using System;
using System.Collections.Generic;

namespace MediStack.Dominio
{
    public sealed class HorarioAtencion
    {
        public int HorarioAtencionId { get; set; }
        public Guid ProfesionalId { get; set; }
        public string Profesional { get; set; }
        public int EspecialidadId { get; set; }
        public string Especialidad { get; set; }
        public int DiaSemana { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public short DuracionMinutos { get; set; }
        public bool Activo { get; set; }
    }

    public sealed class TurnoAgenda
    {
        public int TurnoId { get; set; }
        public Guid PacienteId { get; set; }
        public string Paciente { get; set; }
        public DateTime FechaHora { get; set; }
        public string Estado { get; set; }
        public string Motivo { get; set; }
        public string Especialidad { get; set; }
        public short DuracionMinutos { get; set; }
        public string EstadoSena { get; set; }
        public decimal MontoSena { get; set; }
        public int? TurnoReprogramadoDesdeId { get; set; }
        public int? TurnoReprogramadoHaciaId { get; set; }
        public DateTime? FechaReprogramacion { get; set; }
        public bool EsReprogramado
        {
            get { return TurnoReprogramadoDesdeId.HasValue || TurnoReprogramadoHaciaId.HasValue; }
        }
    }

    public sealed class DiaAgendaSemanal
    {
        public DateTime Fecha { get; set; }
        public int DiaSemana { get; set; }
        public string NombreDia { get; set; }
        public bool Habilitado { get; set; }
        public bool TieneTurnosFueraDeHorario { get; set; }
        public IList<TurnoAgenda> Turnos { get; set; }
    }

    public sealed class FranjaAgenda
    {
        public DateTime FechaHora { get; set; }
        public DateTime Fin { get; set; }
        public string Especialidad { get; set; }
        public bool Disponible { get; set; }
        public int? TurnoId { get; set; }
        public Guid? PacienteId { get; set; }
        public string Paciente { get; set; }
        public string Estado { get; set; }
        public string Motivo { get; set; }
    }
}
