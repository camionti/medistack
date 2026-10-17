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
                if (appShell != null)
                {
                    appShell.Attributes["class"] = "app-shell is-authenticated";
                }
                if (SidebarHolder != null) SidebarHolder.Visible = true;
                if (TopbarAutenticado != null) TopbarAutenticado.Visible = true;
                if (HeaderAnonimo != null) HeaderAnonimo.Visible = false;

                string nombreCompleto = Convert.ToString(Session["NombreCompleto"]);
                string rolNombre = Convert.ToString(Session["RolNombre"]);
                string rolCodigo = Convert.ToString(Session["RolCodigo"]);

                if (NombreActual != null) NombreActual.Text = Server.HtmlEncode(nombreCompleto);
                if (RolActual != null) RolActual.Text = Server.HtmlEncode(rolNombre);

                if (NavegacionAdministrativa != null) NavegacionAdministrativa.Visible = EsRol(rolCodigo, "ADMINISTRATIVO");
                if (NavegacionPaciente != null) NavegacionPaciente.Visible = EsRol(rolCodigo, "PACIENTE");
                if (NavegacionProfesional != null) NavegacionProfesional.Visible = EsRol(rolCodigo, "PROFESIONAL");
                if (PhMiFicha != null) PhMiFicha.Visible = EsRol(rolCodigo, "PACIENTE");
            }
            else
            {
                if (appShell != null)
                {
                    appShell.Attributes["class"] = "app-shell is-anonymous";
                }
                if (SidebarHolder != null) SidebarHolder.Visible = false;
                if (TopbarAutenticado != null) TopbarAutenticado.Visible = false;
                if (HeaderAnonimo != null) HeaderAnonimo.Visible = true;
                if (NavegacionAdministrativa != null) NavegacionAdministrativa.Visible = false;
                if (NavegacionPaciente != null) NavegacionPaciente.Visible = false;
                if (NavegacionProfesional != null) NavegacionProfesional.Visible = false;
                if (PhMiFicha != null) PhMiFicha.Visible = false;
            }
        }

        private static bool EsRol(string rolCodigo, string rolEsperado)
        {
            return string.Equals(rolCodigo, rolEsperado, StringComparison.OrdinalIgnoreCase);
        }
    }
}
