using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using MediStack.Negocio;

namespace MediStack.Web
{
    // Webhook de Mercado Pago: el SERVIDOR de Mercado Pago llama a esta direccion cuando cambia un pago,
    // aunque el paciente haya cerrado la pestaña. No hay usuario logueado: por eso es un handler (.ashx)
    // y no hereda de PaginaProtegida.
    //
    // Seguridad: el aviso solo trae el Id del pago. Nunca se confia en su contenido: ProcesarPago
    // consulta el pago real a la API de Mercado Pago (con nuestro Access Token) antes de acreditar.
    // Un aviso falso no puede acreditar nada.
    //
    // Respuesta: 200 = recibido. Si algo falla (base caida, Mercado Pago sin responder) se devuelve 500
    // para que Mercado Pago reintente mas tarde. Acreditar es idempotente, asi que reintentar es seguro.
    public class MercadoPagoWebhook : IHttpHandler
    {
        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";

            string tipo;
            string id;
            LeerNotificacion(context.Request, out tipo, out id);

            // Solo interesan los avisos de pagos (tambien llegan, por ejemplo, de "merchant_order").
            if (tipo != "payment" || string.IsNullOrWhiteSpace(id))
            {
                context.Response.StatusCode = 200;
                context.Response.Write("ignorado");
                return;
            }

            try
            {
                new PagoSenaNegocio().ProcesarPago(id);
                context.Response.StatusCode = 200;
                context.Response.Write("ok");
            }
            catch (Exception ex)
            {
                Trace.TraceError("Webhook Mercado Pago: no se pudo procesar el pago " + id + ": " + ex.Message);
                context.Response.StatusCode = 500;
                context.Response.Write("error");
            }
        }

        // Mercado Pago avisa en dos formatos:
        //   Webhooks: ?data.id=123&type=payment  + cuerpo JSON {"type":"payment","data":{"id":"123"}}
        //   IPN (viejo): ?topic=payment&id=123
        private static void LeerNotificacion(HttpRequest request, out string tipo, out string id)
        {
            tipo = request.QueryString["type"] ?? request.QueryString["topic"];
            id = request.QueryString["data.id"] ?? request.QueryString["id"];

            if (!string.IsNullOrWhiteSpace(tipo) && !string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            // Si no vino en la URL, se busca en el cuerpo JSON.
            try
            {
                string cuerpo;
                using (StreamReader lector = new StreamReader(request.InputStream, Encoding.UTF8))
                {
                    cuerpo = lector.ReadToEnd();
                }

                Dictionary<string, object> json = new JavaScriptSerializer().DeserializeObject(cuerpo)
                    as Dictionary<string, object>;
                if (json == null)
                {
                    return;
                }

                object valor;
                if (string.IsNullOrWhiteSpace(tipo) && json.TryGetValue("type", out valor))
                {
                    tipo = Convert.ToString(valor);
                }

                object datos;
                Dictionary<string, object> data;
                if (string.IsNullOrWhiteSpace(id) && json.TryGetValue("data", out datos)
                    && (data = datos as Dictionary<string, object>) != null
                    && data.TryGetValue("id", out valor))
                {
                    id = Convert.ToString(valor);
                }
            }
            catch (ArgumentException)
            {
                // Cuerpo que no es JSON valido: se ignora el aviso.
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
