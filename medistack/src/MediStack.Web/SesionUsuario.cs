using System.Web;
using System.Web.Security;
using MediStack.Dominio;

namespace MediStack.Web
{
    // Unico lugar donde se define que significa "iniciar sesion" en MediStack:
    // datos del usuario en Session + cookie de Forms Authentication.
    // Lo usan Login (despues de verificar la contrasena) y Registro (login automatico).
    // Si se necesita una clave nueva en Session, se agrega solo aca.
    public static class SesionUsuario
    {
        public static void Iniciar(HttpContext contexto, Usuario usuario)
        {
            contexto.Session.Clear();
            contexto.Session["UsuarioId"] = usuario.UsuarioId;
            contexto.Session["NombreUsuario"] = usuario.NombreUsuario;
            contexto.Session["NombreCompleto"] = usuario.Nombre + " " + usuario.Apellido;
            contexto.Session["RolCodigo"] = usuario.RolCodigo;
            contexto.Session["RolNombre"] = usuario.RolNombre;

            FormsAuthentication.SetAuthCookie(usuario.NombreUsuario, false);
        }
    }
}
