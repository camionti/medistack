using System;
using System.Collections.Generic;

namespace MediStack.Dominio
{
    public sealed class TurnoConsultorioItem
    {
        public int TurnoId { get; set; }
        public DateTime FechaHora { get; set; }
        public Guid PacienteId { get; set; }
        public string NombrePaciente { get; set; }
        public string NumeroDocumento { get; set; }
        public string ObraSocial { get; set; }
        public string NumeroAfiliado { get; set; }
        public string Especialidad { get; set; }
        public string MotivoTurno { get; set; }
        public string Estado { get; set; }
        public DateTime? FechaHoraLlegada { get; set; }
        public bool TieneEvolucion { get; set; }
        public string Diagnostico { get; set; }
        public string Observaciones { get; set; }
        public DateTime? FechaRegistroEvolucion { get; set; }

        public bool RequiereEvolucion
        {
            get { return Estado == "Atendido" && !TieneEvolucion; }
        }

        public string EstadoVisual
        {
            get
            {
                if (Estado == "EnEspera") return "En Sala de Espera";
                if (Estado == "Confirmado") return "Citado (No ingresó)";
                if (Estado == "Atendido" && !TieneEvolucion) return "Atendido (Evolución pendiente)";
                if (Estado == "Atendido") return "Atendido";
                if (Estado == "Ausente") return "Ausente";
                return Estado;
            }
        }
    }

    public sealed class ResumenClinicoRapido
    {
        public Guid PacienteId { get; set; }
        public string NombreCompleto { get; set; }
        public string NumeroDocumento { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public int Edad { get; set; }
        public string Telefono { get; set; }
        public string ObraSocial { get; set; }
        public string NumeroAfiliado { get; set; }
        public string ContactoEmergenciaNombre { get; set; }
        public string ContactoEmergenciaTelefono { get; set; }
        public IList<AntecedenteClinicoItem> AntecedentesRecientes { get; set; }
    }

    public sealed class AntecedenteClinicoItem
    {
        public DateTime Fecha { get; set; }
        public string Profesional { get; set; }
        public string Especialidad { get; set; }
        public string MotivoConsulta { get; set; }
        public string Diagnostico { get; set; }
        public string Observaciones { get; set; }
    }

    public sealed class AtencionClinicaEntrada
    {
        public int TurnoId { get; set; }
        public string MotivoConsulta { get; set; }
        public string Diagnostico { get; set; }
        public string Observaciones { get; set; }
    }
}
