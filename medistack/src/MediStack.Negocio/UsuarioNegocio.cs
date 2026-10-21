using System;
using MediStack.Datos;
using MediStack.Dominio;
using System.Data;

namespace MediStack.Negocio
{
    public class UsuarioNegocio
    {
        private readonly UsuarioDatos _usuarioDatos;
        private readonly GestionClinicaDatos _gestionDatos;
        private readonly EmailNegocio _email = new EmailNegocio();

        public UsuarioNegocio()
        {
            _usuarioDatos = new UsuarioDatos();
            _gestionDatos = new GestionClinicaDatos();
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

        public void CambiarPassword(Guid usuarioId, string actual, string nueva, string repetir)
        {
            if (usuarioId == Guid.Empty)
                throw new InvalidOperationException("No se pudo identificar tu cuenta. Volvé a iniciar sesión.");
            if (string.IsNullOrEmpty(actual) || string.IsNullOrEmpty(nueva) || string.IsNullOrEmpty(repetir))
                throw new InvalidOperationException("Completá los tres campos.");
            if (nueva != repetir)
                throw new InvalidOperationException("La contraseña nueva y su repetición no coinciden.");
            if (nueva.Length < 10 || nueva.Length > 128)
                throw new InvalidOperationException("La contraseña nueva debe tener entre 10 y 128 caracteres.");
            if (nueva == actual)
                throw new InvalidOperationException("La contraseña nueva debe ser distinta de la actual.");
            if (actual.Length > 128)
                throw new InvalidOperationException("La contraseña actual no es correcta.");

            Usuario usuario = _usuarioDatos.ObtenerPorId(usuarioId);
            if (usuario == null || !usuario.Activo)
                throw new InvalidOperationException("No se pudo identificar tu cuenta. Volvé a iniciar sesión.");
            if (usuario.BloqueadoHasta.HasValue && usuario.BloqueadoHasta.Value > DateTime.Now)
                throw new InvalidOperationException("Por seguridad, no podés cambiar la contraseña mientras tu cuenta esté bloqueada. Intentá más tarde.");
            if (string.Equals(nueva, usuario.NombreUsuario, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("La contraseña no puede ser igual a tu nombre de usuario.");

            if (!VerificadorPassword.Verificar(actual, usuario.PasswordHash))
            {
                _usuarioDatos.RegistrarIntentoFallido(usuarioId);
                throw new InvalidOperationException("La contraseña actual no es correcta.");
            }

            string hashNuevo = VerificadorPassword.CrearHash(nueva);
            if (!_gestionDatos.ActualizarPassword(usuarioId, usuario.PasswordHash, hashNuevo))
                throw new InvalidOperationException("Tu cuenta cambió mientras guardabas. Volvé a intentarlo.");

            _email.EnviarPasswordCambiada(usuario.Email, usuario.Nombre + " " + usuario.Apellido);
        }
    }
}