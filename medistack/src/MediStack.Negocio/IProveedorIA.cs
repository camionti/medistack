using System;
using System.Configuration;
using MediStack.Dominio;

namespace MediStack.Negocio
{
    public interface IProveedorIA
    {
        RespuestaIA Completar(SolicitudIA solicitud);
    }

    public sealed class ProveedorIANoConfigurado : IProveedorIA
    {
        public RespuestaIA Completar(SolicitudIA solicitud)
        {
            throw new InvalidOperationException(
                "El proveedor de IA todavía no está configurado. No se puede procesar la consulta.");
        }
    }

    public static class ProveedorIAFactory
    {
        public static IProveedorIA Crear()
        {
            string proveedor = ConfigurationManager.AppSettings["IA.Proveedor"];
            if (string.Equals(proveedor, "Gemini", StringComparison.OrdinalIgnoreCase))
            {
                return new GeminiProveedorIA(
                    ConfigurationManager.AppSettings["Gemini.ApiKey"],
                    ConfigurationManager.AppSettings["Gemini.Modelo"],
                    LeerTimeout());
            }

            return new ProveedorIANoConfigurado();
        }

        private static int LeerTimeout()
        {
            int timeout;
            return int.TryParse(ConfigurationManager.AppSettings["Gemini.TimeoutMs"], out timeout)
                && timeout >= 1000
                && timeout <= 120000
                ? timeout
                : 30000;
        }
    }
}
