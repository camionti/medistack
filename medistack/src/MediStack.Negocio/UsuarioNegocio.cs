using System;
using MediStack.Datos;
using MediStack.Dominio;
using System.Data;
namespace MediStack.Negocio
{
    public class UsuarioNegocio
    {
        private readonly UsuarioDatos _usuarioDatos;

        public UsuarioNegocio()
        {
            _usuarioDatos = new UsuarioDatos();
        }

        public ResultadoLogin IniciarSesion(string nombreUsuario, string password)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario)
                || nombreUsuario.Trim().Length > 50
                || string.IsNullOrEmpty(password)
                || password.Length > 128)
            {
                return ResultadoLogin.Fallido();
            }

            Usuario usuario = _usuarioDatos.ObtenerPorNombreUsuario(nombreUsuario.Trim());
            if (usuario == null || !usuario.Activo)
            {
                return ResultadoLogin.Fallido();
            }

            if (usuario.BloqueadoHasta.HasValue && usuario.BloqueadoHasta.Value > DateTime.Now)
            {
                return ResultadoLogin.Fallido();
            }

            if (!VerificadorPassword.Verificar(password, usuario.PasswordHash))
            {
                _usuarioDatos.RegistrarIntentoFallido(usuario.UsuarioId);
                return ResultadoLogin.Fallido();
            }

            _usuarioDatos.RegistrarAccesoCorrecto(usuario.UsuarioId);
            return ResultadoLogin.Correcto(usuario);
        }
        public DataTable ObtenerUsuariosBloqueados()
        {
            return _usuarioDatos.ListarBloqueados(); 
        }

        public void DesbloquearUsuario(Guid usuarioId)
        {
            if (usuarioId == Guid.Empty) 
            {
                throw new InvalidOperationException("Seleccioná una cuenta válida.");
            }

            int filas = _usuarioDatos.Desbloquear(usuarioId); 
            if (filas == 0)
            {
                throw new InvalidOperationException("La cuenta ya no estaba bloqueada."); 
            }
        }

    }
}
