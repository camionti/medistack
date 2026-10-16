using System;
using System.Configuration;
using System.Web;

namespace MediStack.Web
{
    public static class ConfiguracionSitio
    {
        private static volatile string _ultimaUrlBaseDetectada;

        public static void RegistrarUrlBaseDesdeRequest(HttpRequest request)
        {
            if (request == null || request.Url == null)
            {
                return;
            }

            try
            {
                string autoridad = request.Url.GetLeftPart(UriPartial.Authority);
                string appPath = request.ApplicationPath;
                if (!string.IsNullOrEmpty(appPath) && appPath != "/")
                {
                    autoridad += appPath.TrimEnd('/');
                }

                _ultimaUrlBaseDetectada = autoridad;
            }
            catch
            {
                // Si el contexto del request esta en transicion o no disponible, no afecta el flujo.
            }
        }

        public static string ObtenerUrlBase()
        {
            string configurada = ConfigurationManager.AppSettings["Sitio.UrlBase"];
            if (!string.IsNullOrWhiteSpace(configurada))
            {
                return configurada.TrimEnd('/');
            }

            if (HttpContext.Current != null && HttpContext.Current.Request != null)
            {
                try
                {
                    HttpRequest request = HttpContext.Current.Request;
                    string autoridad = request.Url.GetLeftPart(UriPartial.Authority);
                    string appPath = request.ApplicationPath;
                    if (!string.IsNullOrEmpty(appPath) && appPath != "/")
                    {
                        autoridad += appPath.TrimEnd('/');
                    }

                    _ultimaUrlBaseDetectada = autoridad;
                    return autoridad;
                }
                catch
                {
                }
            }

            if (!string.IsNullOrEmpty(_ultimaUrlBaseDetectada))
            {
                return _ultimaUrlBaseDetectada;
            }

            return "http://localhost:59999";
        }
    }
}
