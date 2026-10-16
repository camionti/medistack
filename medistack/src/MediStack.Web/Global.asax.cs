using System;
using System.Web;

namespace MediStack.Web
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            RecordatorioTurnosServicio.Iniciar();
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            ConfiguracionSitio.RegistrarUrlBaseDesdeRequest(Request);
        }

        protected void Application_End(object sender, EventArgs e)
        {
            RecordatorioTurnosServicio.Detener();
        }
    }
}
