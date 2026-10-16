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

            if (!string.IsNullOrWhiteSpace(solicitud.Instrucciones))
            {
                cuerpo["systemInstruction"] = new Dictionary<string, object>
                {
                    {
                        "parts",
                        new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "text", solicitud.Instrucciones } }
                        }
                    }
                };
            }

            if (solicitud.Herramientas != null && solicitud.Herramientas.Count > 0)
            {
                cuerpo["tools"] = CrearHerramientas(solicitud.Herramientas);
            }

            string respuestaJson = Enviar(cuerpo);
            JavaScriptSerializer serializador = new JavaScriptSerializer();
            Dictionary<string, object> respuesta = serializador.DeserializeObject(respuestaJson)
                as Dictionary<string, object>;
            if (respuesta == null)
            {
                throw new InvalidOperationException("Gemini devolvio una respuesta invalida.");
            }

            LlamadaHerramientaIA llamada;
            string texto = ExtraerTexto(respuesta, out llamada);
            return new RespuestaIA
            {
                Contenido = texto,
                Herramienta = llamada,
                GastoTokens = ExtraerTokens(respuesta)
            };
        }

        private static IList<Dictionary<string, object>> CrearHerramientas(IList<DefinicionHerramientaIA> herramientas)
        {
            JavaScriptSerializer serializador = new JavaScriptSerializer();
            List<Dictionary<string, object>> declaraciones = new List<Dictionary<string, object>>();
            foreach (DefinicionHerramientaIA herramienta in herramientas)
            {
                Dictionary<string, object> declaracion = new Dictionary<string, object>
                {
                    { "name", herramienta.Nombre },
                    { "description", herramienta.Descripcion }
                };
                if (!string.IsNullOrWhiteSpace(herramienta.EsquemaEntradaJson))
                {
                    declaracion["parameters"] = serializador.DeserializeObject(herramienta.EsquemaEntradaJson);
                }

                declaraciones.Add(declaracion);
            }

            return new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { { "functionDeclarations", declaraciones } }
            };
        }

        private void ValidarConfiguracion()
        {
            if (_apiKey.Length == 0)
            {
                throw new InvalidOperationException("Falta configurar Gemini.ApiKey en secrets.config.");
            }

            if (_modelo.Length == 0)
            {
                throw new InvalidOperationException("Falta configurar Gemini.Modelo en secrets.config.");
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
            JavaScriptSerializer serializador = new JavaScriptSerializer();
            foreach (MensajeIA mensaje in mensajes)
            {
                if (mensaje != null && mensaje.Llamada != null)
                {
                    object argumentos = string.IsNullOrWhiteSpace(mensaje.Llamada.ArgumentosJson)
                        ? new Dictionary<string, object>()
                        : serializador.DeserializeObject(mensaje.Llamada.ArgumentosJson);

                    Dictionary<string, object> parteLlamada = new Dictionary<string, object>
                    {
                        {
                            "functionCall",
                            new Dictionary<string, object>
                            {
                                { "name", mensaje.Llamada.Nombre },
                                { "args", argumentos }
                            }
                        }
                    };

                    if (!string.IsNullOrEmpty(mensaje.Llamada.Firma))
                    {
                        parteLlamada["thoughtSignature"] = mensaje.Llamada.Firma;
                    }

                    contenidos.Add(new Dictionary<string, object>
                    {
                        { "role", "model" },
                        { "parts", new List<Dictionary<string, object>> { parteLlamada } }
                    });
                    continue;
                }

                if (mensaje != null && mensaje.ResultadoJson != null)
                {
                    contenidos.Add(new Dictionary<string, object>
                    {
                        { "role", "user" },
                        {
                            "parts",
                            new List<Dictionary<string, object>>
                            {
                                new Dictionary<string, object>
                                {
                                    {
                                        "functionResponse",
                                        new Dictionary<string, object>
                                        {
                                            { "name", mensaje.NombreResultado },
                                            {
                                                "response",
                                                new Dictionary<string, object>
                                                {
                                                    { "resultado", serializador.DeserializeObject(mensaje.ResultadoJson) }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    });
                    continue;
                }

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

        private static string ExtraerTexto(Dictionary<string, object> respuesta, out LlamadaHerramientaIA llamada)
        {
            llamada = null;
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
                object llamadaObjeto;
                if (parte != null && parte.TryGetValue("text", out textoParte))
                {
                    texto.Append(Convert.ToString(textoParte));
                }
                else if (parte != null && llamada == null && parte.TryGetValue("functionCall", out llamadaObjeto))
                {
                    Dictionary<string, object> datos = llamadaObjeto as Dictionary<string, object>;
                    object nombre;
                    object argumentos;
                    if (datos != null && datos.TryGetValue("name", out nombre))
                    {
                        datos.TryGetValue("args", out argumentos);

                        object firmaObjeto;
                        parte.TryGetValue("thoughtSignature", out firmaObjeto);

                        llamada = new LlamadaHerramientaIA
                        {
                            Nombre = Convert.ToString(nombre),
                            ArgumentosJson = new JavaScriptSerializer().Serialize(
                                argumentos ?? new Dictionary<string, object>()),
                            Firma = firmaObjeto == null ? null : Convert.ToString(firmaObjeto)
                        };
                    }
                }
            }

            if (texto.Length == 0 && llamada != null)
            {
                return string.Empty;
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
                int codigo = (int)respuesta.StatusCode;
                if (codigo == 401 || codigo == 403)
                    return new InvalidOperationException("Gemini rechazo la API key configurada.");
                if (codigo == 429)
                    return new InvalidOperationException("Gemini informo que se alcanzo el limite de solicitudes.");
                if (codigo >= 500)
                    return new InvalidOperationException("Gemini no esta disponible en este momento.");

                string detalle = string.Empty;
                try
                {
                    using (StreamReader lector = new StreamReader(respuesta.GetResponseStream(), Encoding.UTF8))
                    {
                        detalle = lector.ReadToEnd();
                    }
                }
                catch { }

                System.Diagnostics.Trace.WriteLine("Gemini HTTP " + codigo + ": " + detalle);
                return new InvalidOperationException("Gemini respondio con error " + codigo + ".");
            }

            if (ex.Status == WebExceptionStatus.Timeout)
                return new InvalidOperationException("Gemini no respondio dentro del tiempo permitido.");

            return new InvalidOperationException("No se pudo comunicarse con Gemini (" + ex.Status + ").");
        }
    }
}