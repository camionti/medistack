using System;
using System.Web;
using System.Web.UI;
using MediStack.Negocio;

namespace MediStack.Web.Account
{
    public partial class NuevaPassword : Page
    {
        private readonly GestionClinicaNegocio _negocio = new GestionClinicaNegocio();
        private string _token;

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            _token = Request.QueryString["token"];

            ResultadoToken resultado = _negocio.ValidarTokenRecuperacion(_token);
            if (resultado.Estado != EstadoToken.Valido)
            {
                FormularioPanel.Visible = false;
                MostrarError(resultado.Mensaje);
                return;
            }

            if (!IsPostBack)
            {
                CorreoCuenta.Text = Server.HtmlEncode(EnmascararCorreo(resultado.Email));
            }
        }

        protected void Guardar_Click(object sender, EventArgs e)
        {
            ErrorMessage.Visible = false;

            try
            {
                _negocio.RestablecerPassword(_token, NuevaClave.Text, RepetirClave.Text);
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(ex.Message);
                return;
            }

            FormularioPanel.Visible = false;
            ExitoPanel.Visible = true;
        }

        private static string EnmascararCorreo(string correo)
        {
            int arroba = correo.IndexOf('@');
            if (arroba <= 0)
            {
                return correo;
            }

            string local = correo.Substring(0, arroba);
            int visibles = Math.Min(2, Math.Max(1, local.Length - 1));
            return local.Substring(0, visibles) + new string('*', Math.Max(local.Length - visibles, 3)) + correo.Substring(arroba);
        }

        private void MostrarError(string mensaje)
        {
            ErrorMessage.Text = mensaje;
            ErrorMessage.Visible = true;
        }
    }
}