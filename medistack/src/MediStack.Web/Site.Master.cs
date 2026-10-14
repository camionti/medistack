using System;
using System.Web.UI;

namespace MediStack.Web
{
    public partial class SiteMaster : MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Context.User.Identity.IsAuthenticated && Session["UsuarioId"] != null)
            {
                UsuarioActual.Visible = true;
                UsuarioAnonimo.Visible = false;
                NombreActual.Text = Server.HtmlEncode(Convert.ToString(Session["NombreCompleto"]));
                RolActual.Text = Server.HtmlEncode(Convert.ToString(Session["RolNombre"]));
                string rolCodigo = Convert.ToString(Session["RolCodigo"]);
                NavegacionAdministrativa.Visible = EsRol(rolCodigo, "ADMINISTRATIVO");
                NavegacionPaciente.Visible = EsRol(rolCodigo, "PACIENTE");
                NavegacionProfesional.Visible = EsRol(rolCodigo, "PROFESIONAL");
            }
            else
            {
                UsuarioActual.Visible = false;
                UsuarioAnonimo.Visible = true;
            }
        }

        private static bool EsRol(string rolCodigo, string rolEsperado)
        {
            return string.Equals(rolCodigo, rolEsperado, StringComparison.OrdinalIgnoreCase);
        }
    }
}
