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
                "El proveedor de IA todavía no está configurado. El asistente funciona actualmente con respuestas provisionales.");
        }
    }

    public sealed class ProveedorIAProvisional : IProveedorIA
    {
        public RespuestaIA Completar(SolicitudIA solicitud)
        {
            if (solicitud == null || solicitud.Mensajes == null || solicitud.Mensajes.Count == 0)
            {
                throw new InvalidOperationException("La solicitud al proveedor de IA no contiene mensajes.");
            }

            string consulta = solicitud.Mensajes[solicitud.Mensajes.Count - 1].Contenido ?? string.Empty;
            string normalizada = consulta.ToLowerInvariant();
            if (normalizada.Contains("mis turnos"))
            {
                return new RespuestaIA
                {
                    Herramienta = new LlamadaHerramientaIA { Nombre = "ConsultarMisTurnos" }
                };
            }

            if (normalizada.Contains("turno") || normalizada.Contains("disponibilidad"))
            {
                return new RespuestaIA
                {
                    Herramienta = new LlamadaHerramientaIA { Nombre = "ConsultarDisponibilidad" }
                };
            }

            return new RespuestaIA
            {
                Contenido = "Puedo ayudarte a consultar disponibilidad, solicitar un turno o revisar tus turnos. " +
                    "Para reservar, primero te voy a pedir especialidad, fecha y horario, y luego confirmaré la operación."
            };
        }
    }

    public static class ProveedorIAFactory
    {
        public static IProveedorIA Crear()
        {
            string nombre = ConfigurationManager.AppSettings["IA.Proveedor"];
            if (string.IsNullOrWhiteSpace(nombre)
                || string.Equals(nombre, "Provisional", StringComparison.OrdinalIgnoreCase))
            {
                return new ProveedorIAProvisional();
            }

            return new ProveedorIANoConfigurado();
        }
    }
}
