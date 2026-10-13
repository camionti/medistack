using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using MediStack.Dominio;

namespace MediStack.Negocio
{
    public sealed class GeminiProveedorIA : IProveedorIA
    {
        private const string EndpointBase = "https://generativelanguage.googleapis.com/v1beta/models/";
        private readonly string _apiKey;
        private readonly string _modelo;
        private readonly int _timeoutMilisegundos;

        public GeminiProveedorIA(string apiKey, string modelo, int timeoutMilisegundos)
        {
            _apiKey = (apiKey ?? string.Empty).Trim();
            _modelo = (modelo ?? string.Empty).Trim();
            _timeoutMilisegundos = timeoutMilisegundos;
        }

        public RespuestaIA Completar(SolicitudIA solicitud)
        {
            ValidarConfiguracion();
            if (solicitud == null || solicitud.Mensajes == null || solicitud.Mensajes.Count == 0)
            {
                throw new InvalidOperationException("La solicitud al proveedor de IA no contiene mensajes.");
            }

            Dictionary<string, object> cuerpo = new Dictionary<string, object>
            {
                { "contents", CrearContenidos(solicitud.Mensajes) }
            };

            string respuestaJson = Enviar(cuerpo);
            JavaScriptSerializer serializador = new JavaScriptSerializer();
            Dictionary<string, object> respuesta = serializador.DeserializeObject(respuestaJson)
                as Dictionary<string, object>;
            if (respuesta == null)
            {
                throw new InvalidOperationException("Gemini devolvio una respuesta invalida.");
            }

            string texto = ExtraerTexto(respuesta);
            return new RespuestaIA
            {
                Contenido = texto,
                GastoTokens = ExtraerTokens(respuesta)
            };
        }

        private void ValidarConfiguracion()
        {
            if (_apiKey.Length == 0)
            {
                throw new InvalidOperationException(
                    "Falta configurar Gemini.ApiKey en secrets.config.");
            }

            if (_modelo.Length == 0)
            {
                throw new InvalidOperationException(
                    "Falta configurar Gemini.Modelo en secrets.config.");
            }
        }

        private string Enviar(Dictionary<string, object> cuerpo)
        {
            JavaScriptSerializer serializador = new JavaScriptSerializer();
            byte[] datos = Encoding.UTF8.GetBytes(serializador.Serialize(cuerpo));
            string url = EndpointBase + Uri.EscapeDataString(_modelo) + ":generateContent";

            HttpWebRequest solicitud = WebRequest.CreateHttp(url);
            solicitud.Method = "POST";
            solicitud.ContentType = "application/json; charset=utf-8";
            solicitud.Accept = "application/json";
            solicitud.Headers["x-goog-api-key"] = _apiKey;
            solicitud.Timeout = _timeoutMilisegundos;
            solicitud.ReadWriteTimeout = _timeoutMilisegundos;
            solicitud.ContentLength = datos.Length;

            try
            {
                using (Stream flujo = solicitud.GetRequestStream())
                {
                    flujo.Write(datos, 0, datos.Length);
                }

                using (WebResponse respuesta = solicitud.GetResponse())
                using (StreamReader lector = new StreamReader(respuesta.GetResponseStream(), Encoding.UTF8))
                {
                    return lector.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                throw CrearErrorProveedor(ex);
            }
        }

        private static IList<Dictionary<string, object>> CrearContenidos(IList<MensajeIA> mensajes)
        {
            List<Dictionary<string, object>> contenidos = new List<Dictionary<string, object>>();
            foreach (MensajeIA mensaje in mensajes)
            {
                if (mensaje == null || string.IsNullOrWhiteSpace(mensaje.Contenido))
                {
                    continue;
                }

                string rol = string.Equals(mensaje.Rol, "assistant", StringComparison.OrdinalIgnoreCase)
                    ? "model"
                    : "user";
                contenidos.Add(new Dictionary<string, object>
                {
                    { "role", rol },
                    {
                        "parts",
                        new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "text", mensaje.Contenido } }
                        }
                    }
                });
            }

            if (contenidos.Count == 0)
            {
                throw new InvalidOperationException("La solicitud al proveedor de IA no contiene texto.");
            }

            return contenidos;
        }

        private static string ExtraerTexto(Dictionary<string, object> respuesta)
        {
            object candidatosObjeto;
            if (!respuesta.TryGetValue("candidates", out candidatosObjeto))
            {
                throw new InvalidOperationException("Gemini no devolvio candidatos de respuesta.");
            }

            object[] candidatos = candidatosObjeto as object[];
            if (candidatos == null || candidatos.Length == 0)
            {
                throw new InvalidOperationException("Gemini no devolvio contenido.");
            }

            Dictionary<string, object> candidato = candidatos[0] as Dictionary<string, object>;
            object contenidoObjeto;
            if (candidato == null || !candidato.TryGetValue("content", out contenidoObjeto))
            {
                throw new InvalidOperationException("Gemini devolvio un candidato sin contenido.");
            }

            Dictionary<string, object> contenido = contenidoObjeto as Dictionary<string, object>;
            object partesObjeto;
            if (contenido == null || !contenido.TryGetValue("parts", out partesObjeto))
            {
                throw new InvalidOperationException("Gemini devolvio contenido sin partes.");
            }

            object[] partes = partesObjeto as object[];
            if (partes == null)
            {
                throw new InvalidOperationException("Gemini devolvio partes invalidas.");
            }

            StringBuilder texto = new StringBuilder();
            foreach (object parteObjeto in partes)
            {
                Dictionary<string, object> parte = parteObjeto as Dictionary<string, object>;
                object textoParte;
                if (parte != null && parte.TryGetValue("text", out textoParte))
                {
                    texto.Append(Convert.ToString(textoParte));
                }
            }

            if (texto.Length == 0)
            {
                throw new InvalidOperationException("Gemini no devolvio texto util.");
            }

            return texto.ToString();
        }

        private static int ExtraerTokens(Dictionary<string, object> respuesta)
        {
            object usoObjeto;
            if (!respuesta.TryGetValue("usageMetadata", out usoObjeto))
            {
                return 0;
            }

            Dictionary<string, object> uso = usoObjeto as Dictionary<string, object>;
            object totalObjeto;
            if (uso == null || !uso.TryGetValue("totalTokenCount", out totalObjeto))
            {
                return 0;
            }

            int tokens;
            return int.TryParse(Convert.ToString(totalObjeto), out tokens) && tokens >= 0 ? tokens : 0;
        }

        private static InvalidOperationException CrearErrorProveedor(WebException ex)
        {
            HttpWebResponse respuesta = ex.Response as HttpWebResponse;
            if (respuesta != null)
            {
                if ((int)respuesta.StatusCode == 401 || (int)respuesta.StatusCode == 403)
                {
                    return new InvalidOperationException("Gemini rechazo la API key configurada.");
                }

                if ((int)respuesta.StatusCode == 429)
                {
                    return new InvalidOperationException("Gemini informo que se alcanzo el limite de solicitudes.");
                }

                if ((int)respuesta.StatusCode >= 500)
                {
                    return new InvalidOperationException("Gemini no esta disponible en este momento.");
                }
            }

            if (ex.Status == WebExceptionStatus.Timeout)
            {
                return new InvalidOperationException("Gemini no respondio dentro del tiempo permitido.");
            }

            return new InvalidOperationException("No se pudo comunicarse con Gemini.");
        }
    }
}
