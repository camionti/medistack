using System;
using System.Web.UI;

namespace MediStack.Web
{
    public partial class Dashboard : PaginaProtegida
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            NombreUsuarioLiteral.Text = NombreCompleto;
            RolLiteral.Text = RolNombre;

            if (TieneRol("PACIENTE"))
            {
                PacientePanel.Visible = true;
            }
            else if (TieneRol("PROFESIONAL"))
            {
                ProfesionalPanel.Visible = true;
                JornadaHoyLink.NavigateUrl = ResolveUrl("~/Agenda.aspx?vista=dia&fecha="
                    + DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
            }
            else if (TieneRol("ADMINISTRATIVO"))
            {
                AdministrativoPanel.Visible = true;
            }
            else
            {
                throw new System.Web.HttpException(403, "El rol de esta cuenta no tiene acceso al panel.");
            }
        }
    }
}
