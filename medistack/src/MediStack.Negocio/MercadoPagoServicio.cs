using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using MediStack.Dominio;

namespace MediStack.Negocio
{
    // Unica clase de MediStack que habla con la API de Mercado Pago (Checkout Pro, API de Preferences).
    // Usa HttpWebRequest + JavaScriptSerializer, igual que GeminiProveedorIA: el SDK oficial de
    // Mercado Pago no es compatible con .NET Framework 4.8 y asi no hace falta instalar paquetes.
    // El Access Token se lee de secrets.config y NUNCA sale del servidor.
    public sealed class MercadoPagoServicio
    {
        private const string UrlApi = "https://api.mercadopago.com";
        private const int TimeoutMilisegundos = 20000;

        // Paginas de MediStack a las que vuelve Mercado Pago.
        private const string PaginaResultado = "/PagoResultado.aspx";
        private const string PaginaWebhook = "/MercadoPagoWebhook.ashx";

        private readonly string _accessToken;
        private readonly string _urlBase;

        public MercadoPagoServicio()
        {
            _accessToken = (ConfigurationManager.AppSettings["MercadoPago.AccessToken"] ?? string.Empty).Trim();
            _urlBase = (ConfigurationManager.AppSettings["App.UrlBase"] ?? string.Empty).Trim().TrimEnd('/');
        }

        // Crea la preferencia (el "cobro" en Mercado Pago) para la seña de un turno y devuelve
        // el link de pago al que hay que redirigir al paciente.
        // Todos los datos tienen que venir de la base, nunca de la pantalla.
        public string CrearPreferenciaSena(int turnoId, string descripcion, decimal monto, DateTime vencimiento)
        {
            ValidarConfiguracion();
            if (turnoId <= 0 || monto <= 0)
            {
                throw new InvalidOperationException("No se puede generar el pago: el turno o el monto no son validos.");
            }

            string referencia = turnoId.ToString(CultureInfo.InvariantCulture);
            string urlResultado = _urlBase + PaginaResultado;

            Dictionary<string, object> cuerpo = new Dictionary<string, object>
            {
                {
                    "items", new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object>
                        {
                            { "id", "SENA-" + referencia },
                            { "title", string.IsNullOrWhiteSpace(descripcion) ? "Seña de turno MediStack" : descripcion },
                            { "quantity", 1 },
                            { "currency_id", "ARS" },
                            { "unit_price", monto }
                        }
                    }
                },
                // Nuestro TurnoId: cuando vuelva el pago, sabemos a que turno corresponde.
                { "external_reference", referencia },
                {
                    "back_urls", new Dictionary<string, object>
                    {
                        { "success", urlResultado },
                        { "pending", urlResultado },
                        { "failure", urlResultado }
                    }
                },
                // Si el pago se aprueba, Mercado Pago devuelve al paciente solo a nuestra pagina.
                { "auto_return", "approved" },
                // A donde Mercado Pago avisa "por detras" (aunque el paciente cierre la pestaña).
                { "notification_url", _urlBase + PaginaWebhook },
                // La preferencia vence junto con la reserva: despues no se puede pagar.
                { "expires", true },
                { "expiration_date_to", FormatearFecha(vencimiento) },
                {
                    // Sin efectivo (Rapipago/Pago Facil) ni cajeros: no son inmediatos y la reserva dura minutos.
                    "payment_methods", new Dictionary<string, object>
                    {
                        {
                            "excluded_payment_types", new List<Dictionary<string, object>>
                            {
                                new Dictionary<string, object> { { "id", "ticket" } },
                                new Dictionary<string, object> { { "id", "atm" } }
                            }
                        },
                        { "installments", 1 }
                    }
                }
            };

            Dictionary<string, object> respuesta = Enviar("POST", "/checkout/preferences", cuerpo);

            string link = LeerTexto(respuesta, "init_point");
            if (string.IsNullOrWhiteSpace(link))
            {
                link = LeerTexto(respuesta, "sandbox_init_point");
            }

            if (string.IsNullOrWhiteSpace(link))
            {
                throw new InvalidOperationException("Mercado Pago no devolvio el link de pago.");
            }

            return link;
        }

        // Consulta un pago en Mercado Pago. Es la fuente de verdad: nunca se confia en los datos
        // que llegan en la URL de vuelta o en el aviso del webhook, siempre se pregunta aca.
        public PagoMercadoPago ObtenerPago(string pagoId)
        {
            ValidarConfiguracion();
            pagoId = (pagoId ?? string.Empty).Trim();
            long numero;
            if (!long.TryParse(pagoId, NumberStyles.None, CultureInfo.InvariantCulture, out numero))
            {
                throw new InvalidOperationException("El identificador del pago no es valido.");
            }

            Dictionary<string, object> respuesta = Enviar(
                "GET", "/v1/payments/" + numero.ToString(CultureInfo.InvariantCulture), null);

            int turnoId;
            string referencia = LeerTexto(respuesta, "external_reference");
            bool referenciaValida = int.TryParse(referencia, NumberStyles.None, CultureInfo.InvariantCulture, out turnoId)
                && turnoId > 0;

            object montoObjeto;
            decimal monto = respuesta.TryGetValue("transaction_amount", out montoObjeto) && montoObjeto != null
                ? Convert.ToDecimal(montoObjeto, CultureInfo.InvariantCulture)
                : 0m;

            return new PagoMercadoPago
            {
                Id = numero.ToString(CultureInfo.InvariantCulture),
                Estado = LeerTexto(respuesta, "status"),
                TurnoId = referenciaValida ? (int?)turnoId : null,
                Monto = monto,
                Moneda = LeerTexto(respuesta, "currency_id")
            };
        }

        private void ValidarConfiguracion()
        {
            if (_accessToken.Length == 0)
            {
                throw new InvalidOperationException("Falta configurar MercadoPago.AccessToken en secrets.config.");
            }

            if (_urlBase.Length == 0 || !_urlBase.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Falta configurar App.UrlBase en secrets.config con la URL publica https (por ejemplo, la de ngrok).");
            }
        }

        private Dictionary<string, object> Enviar(string metodo, string ruta, Dictionary<string, object> cuerpo)
        {
            // Mercado Pago exige TLS 1.2 o superior.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            HttpWebRequest solicitud = WebRequest.CreateHttp(UrlApi + ruta);
            solicitud.Method = metodo;
            solicitud.Accept = "application/json";
            solicitud.Headers[HttpRequestHeader.Authorization] = "Bearer " + _accessToken;
            solicitud.Timeout = TimeoutMilisegundos;
            solicitud.ReadWriteTimeout = TimeoutMilisegundos;

            JavaScriptSerializer serializador = new JavaScriptSerializer();
            try
            {
                if (cuerpo != null)
                {
                    byte[] datos = Encoding.UTF8.GetBytes(serializador.Serialize(cuerpo));
                    solicitud.ContentType = "application/json; charset=utf-8";
                    solicitud.ContentLength = datos.Length;
                    using (Stream flujo = solicitud.GetRequestStream())
                    {
                        flujo.Write(datos, 0, datos.Length);
                    }
                }

                using (WebResponse respuesta = solicitud.GetResponse())
                using (StreamReader lector = new StreamReader(respuesta.GetResponseStream(), Encoding.UTF8))
                {
                    Dictionary<string, object> json = serializador.DeserializeObject(lector.ReadToEnd())
                        as Dictionary<string, object>;
                    if (json == null)
                    {
                        throw new InvalidOperationException("Mercado Pago devolvio una respuesta invalida.");
                    }

                    return json;
                }
            }
            catch (WebException ex)
            {
                throw CrearError(ex);
            }
        }

        // Traduce los errores HTTP de Mercado Pago a un mensaje entendible (sin exponer el token).
        private static InvalidOperationException CrearError(WebException ex)
        {
            HttpWebResponse respuesta = ex.Response as HttpWebResponse;
            if (respuesta == null)
            {
                return new InvalidOperationException(
                    "No se pudo conectar con Mercado Pago. Revisa la conexion a internet e intenta nuevamente.", ex);
            }

            int codigo = (int)respuesta.StatusCode;
            string detalle = string.Empty;
            try
            {
                using (StreamReader lector = new StreamReader(respuesta.GetResponseStream(), Encoding.UTF8))
                {
                    detalle = lector.ReadToEnd();
                }
            }
            catch (IOException)
            {
            }

            if (codigo == 401 || codigo == 403)
            {
                return new InvalidOperationException(
                    "Mercado Pago rechazo las credenciales. Revisa MercadoPago.AccessToken en secrets.config.", ex);
            }

            if (codigo == 404)
            {
                return new InvalidOperationException("Mercado Pago no encontro el pago o recurso solicitado.", ex);
            }

            if (detalle.Length > 300)
            {
                detalle = detalle.Substring(0, 300);
            }

            return new InvalidOperationException(
                "Mercado Pago respondio con error " + codigo + ". Detalle: " + detalle, ex);
        }

        private static string LeerTexto(Dictionary<string, object> json, string clave)
        {
            object valor;
            return json.TryGetValue(clave, out valor) && valor != null
                ? Convert.ToString(valor, CultureInfo.InvariantCulture)
                : null;
        }

        // Formato que pide Mercado Pago: 2026-10-14T13:05:00.000-03:00
        private static string FormatearFecha(DateTime fecha)
        {
            DateTimeOffset conZona = new DateTimeOffset(
                DateTime.SpecifyKind(fecha, DateTimeKind.Unspecified), TimeZoneInfo.Local.GetUtcOffset(fecha));
            return conZona.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture);
        }
    }
}
