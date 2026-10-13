using System;
using System.Web.UI;
using MediStack.Negocio;

namespace MediStack.Web.Account
{
    public partial class Login : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack && Context.User.Identity.IsAuthenticated)
            {
                Redirigir("~/Dashboard.aspx");
            }
        }

        protected void Ingresar_Click(object sender, EventArgs e)
        {
            Page.Validate();
            if (!Page.IsValid)
            {
                return;
            }

            ResultadoLogin resultado = new UsuarioNegocio().IniciarSesion(NombreUsuario.Text, Password.Text);
            if (!resultado.Exitoso)
            {
                ErrorMessage.Text = resultado.Mensaje;
                ErrorMessage.Visible = true;
                return;
            }

            SesionUsuario.Iniciar(Context, resultado.Usuario);
            Redirigir("~/Dashboard.aspx");
        }

        private void Redirigir(string url)
        {
            Response.Redirect(ResolveUrl(url), false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
