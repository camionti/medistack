using System;

namespace MediStack.Dominio
{
    public class Usuario
    {
        public Guid UsuarioId { get; set; }
        public string NombreUsuario { get; set; }
        public string PasswordHash { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string RolCodigo { get; set; }
        public string RolNombre { get; set; }
        public bool Activo { get; set; }
        public int IntentosFallidos { get; set; }
        public DateTime? BloqueadoHasta { get; set; }
        public string Email { get; set; } 
    }
}
