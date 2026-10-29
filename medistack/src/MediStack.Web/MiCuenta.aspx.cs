using System;
using System.Data.SqlClient;
using System.Web;
using MediStack.Negocio;

namespace MediStack.Web
{
    public partial class MiCuenta : PaginaProtegida
    {
        private readonly UsuarioNegocio _negocio = new UsuarioNegocio();

        protected void Cambiar_Click(object sender, EventArgs e)
        {
            try
            {
                Guid usuarioId = (Guid)Session["UsuarioId"];
                bool avisoEnviado = _negocio.CambiarPassword(usuarioId, ClaveActual.Text, ClaveNueva.Text, ClaveRepetir.Text); 
                MostrarMensaje(true, avisoEnviado
                    ? "Tu contraseña se actualizó correctamente." // 
                    : "Tu contraseña se actualizó, pero no pudimos enviarte el aviso por correo."); 
            }
            catch (InvalidOperationException ex)
            {
                MostrarMensaje(false, ex.Message);
            }
            catch (SqlException ex)
            {
                MostrarMensaje(false, "No se pudo cambiar la contraseña (error SQL " + ex.Number + ").");
            }
        }

        private void MostrarMensaje(bool exito, string texto)
        {
            Mensaje.Text = HttpUtility.HtmlEncode(texto);
            Mensaje.CssClass = exito ? "alert alert-success" : "alert alert-error";
            Mensaje.Visible = true;
        }
    }
}