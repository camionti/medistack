using System;
using System.Configuration;
using System.Web.Hosting;
using System.Web.Security;
using System.Web.UI;
using MediStack.Negocio;

namespace MediStack.Web.Account
{
    public partial class RecuperarCuenta : Page
    {
        private readonly GestionClinicaNegocio _negocio = new GestionClinicaNegocio();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Context.User.Identity.IsAuthenticated && Session["UsuarioId"] != null)
                {
                    Response.Redirect(ResolveUrl("~/Dashboard.aspx"), false);
                    Context.ApplicationInstance.CompleteRequest();
                }
                else if (Context.User.Identity.IsAuthenticated)
                {
                    FormsAuthentication.SignOut();
                    Session.Clear();
                }
            }
        }

        protected void Enviar_Click(object sender, EventArgs e)
        {
            ErrorMessage.Visible = false;

            string correo;
            try
            {
                correo = _negocio.ValidarCorreoRecuperacion(Email.Text);
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(ex.Message);
                return;
            }

            bool olvidoPassword = Opcion.SelectedValue == "password";
            string urlNuevaPassword = ConstruirUrlNuevaPassword();

            HostingEnvironment.QueueBackgroundWorkItem(ct =>
                new GestionClinicaNegocio().ProcesarRecuperacion(correo, olvidoPassword, urlNuevaPassword));

            FormularioPanel.Visible = false;
            ExitoMessage.Text = "Si el correo está registrado, recibirás las instrucciones en breve. Revisá también la carpeta de spam.";
            ExitoMessage.Visible = true;
        }

        private string ConstruirUrlNuevaPassword()
        {
            string baseConfigurada = ConfigurationManager.AppSettings["App.UrlBase"];
            string raiz = string.IsNullOrWhiteSpace(baseConfigurada)
                ? Request.Url.GetLeftPart(UriPartial.Authority)
                : baseConfigurada.Trim().TrimEnd('/');

            return raiz + ResolveUrl("~/Account/NuevaPassword.aspx");
        }

        private void MostrarError(string mensaje)
        {
            ErrorMessage.Text = mensaje;
            ErrorMessage.Visible = true;
        }
    }
}