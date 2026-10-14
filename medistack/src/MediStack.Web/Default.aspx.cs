using System;
using System.Web.UI;

namespace MediStack.Web
{
    public partial class DefaultPage : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                bool estaAutenticado = Context.User.Identity.IsAuthenticated && Session["UsuarioId"] != null;

                PanelUsuarioAutenticado.Visible = estaAutenticado;
                PanelUsuarioAnonimo.Visible = !estaAutenticado;
                PanelCtaAutenticado.Visible = estaAutenticado;
                PanelCtaAnonimo.Visible = !estaAutenticado;

                if (estaAutenticado)
                {
                    string nombre = Convert.ToString(Session["NombreCompleto"]);
                    string rol = Convert.ToString(Session["RolNombre"]);

                    if (string.IsNullOrWhiteSpace(nombre))
                    {
                        nombre = Context.User.Identity.Name;
                    }

                    NombreUsuarioLiteral.Text = Server.HtmlEncode(nombre);
                    RolUsuarioLiteral.Text = Server.HtmlEncode(rol ?? string.Empty);
                }
            }
        }
    }
}
