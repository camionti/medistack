using System;
using System.Web.Security;
using System.Web.UI;

namespace MediStack.Web
{
    public partial class DefaultPage : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Context.User.Identity.IsAuthenticated && Session["UsuarioId"] == null)
            {
                FormsAuthentication.SignOut();
                Session.Clear();
            }

            if (Context.User.Identity.IsAuthenticated && Session["UsuarioId"] != null)
            {
                Response.Redirect("~/Dashboard.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
        }
    }
}
