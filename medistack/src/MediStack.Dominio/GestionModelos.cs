using System;
using System.Collections.Generic;

namespace MediStack.Dominio
{
    public class Paciente
    {
        public Guid UsuarioId { get; set; }
        public string NombreUsuario { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string NumeroDocumento { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public string Email { get; set; }
        public string Telefono { get; set; }
        public int? ObraSocialId { get; set; }
        public string NumeroAfiliado { get; set; }
        public string ContactoEmergenciaNombre { get; set; }
        public string ContactoEmergenciaTelefono { get; set; }
        public bool Activo { get; set; }
    }

    public class Profesional
    {
        public Guid UsuarioId { get; set; }
        public string NombreUsuario { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string NumeroDocumento { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public string Email { get; set; }
        public string Telefono { get; set; }
        public string MatriculaProfesional { get; set; }
        public bool Activo { get; set; }
        public List<int> EspecialidadesIds { get; set; } = new List<int>();
    }

    public class Especialidad
    {
        public int EspecialidadId { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public short DuracionEstandarMinutos { get; set; }
        public bool Activa { get; set; }
    }

    public class ObraSocial
    {
        public int ObraSocialId { get; set; }
        public string Nombre { get; set; }
        public string CodigoCUIT { get; set; }
        public bool Activa { get; set; }
    }

    public class CoberturaEspecialidad
    {
        public int ObraSocialId { get; set; }
        public int EspecialidadId { get; set; }
        public decimal PorcentajeCobertura { get; set; }
    }

    public class Convenio
    {
        public int ConvenioId { get; set; }
        public Guid ProfesionalId { get; set; }
        public int EspecialidadId { get; set; }
        public int ObraSocialId { get; set; }
        public DateTime FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public bool Activo { get; set; }
        public decimal ValorConsulta { get; set; }
        public string EsquemaTipo { get; set; }
        public decimal EsquemaValor { get; set; }
        public bool EsEspecialidadPrincipal { get; set; }
    }
}
