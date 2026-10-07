using System;
using System.Globalization;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace MediStack.Web
{
    public class PaginaProtegida : Page
    {
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            if (!Context.User.Identity.IsAuthenticated || Session["UsuarioId"] == null)
            {
                FormsAuthentication.SignOut();
                Session.Clear();
                Session.Abandon();
                Response.Redirect(ResolveUrl("~/Account/Login.aspx"), true);
            }
        }

        // Lee un filtro de fechas opcional. Ambos vacíos = sin filtro; solo "desde" = ese día;
        // solo "hasta" = todo hasta esa fecha.
        protected static bool TryLeerFiltroFechas(
            TextBox cajaDesde, TextBox cajaHasta, out DateTime? desde, out DateTime? hasta, out string error)
        {
            desde = null;
            hasta = null;
            error = null;
            string[] formatos = { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy" };
            DateTime valor;
            string textoDesde = (cajaDesde.Text ?? string.Empty).Trim();
            string textoHasta = (cajaHasta.Text ?? string.Empty).Trim();
            if (textoDesde.Length > 0)
            {
                if (!DateTime.TryParseExact(textoDesde, formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out valor))
                {
                    error = "La fecha \"Desde\" no es válida.";
                    return false;
                }

                desde = valor.Date;
            }

            if (textoHasta.Length > 0)
            {
                if (!DateTime.TryParseExact(textoHasta, formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out valor))
                {
                    error = "La fecha \"Hasta\" no es válida.";
                    return false;
                }

                hasta = valor.Date;
            }

            if (desde.HasValue && !hasta.HasValue)
            {
                hasta = desde;
            }

            if (desde.HasValue && hasta.HasValue && desde.Value > hasta.Value)
            {
                error = "La fecha inicial no puede ser posterior a la fecha final.";
                return false;
            }

            return true;
        }

        protected bool TieneRol(string codigoRol)
        {
            return string.Equals(
                Convert.ToString(Session["RolCodigo"]),
                codigoRol,
                StringComparison.OrdinalIgnoreCase);
        }

        protected string NombreCompleto
        {
            get { return HttpUtility.HtmlEncode(Convert.ToString(Session["NombreCompleto"])); }
        }

        protected string RolNombre
        {
            get { return HttpUtility.HtmlEncode(Convert.ToString(Session["RolNombre"])); }
        }
    }
}
