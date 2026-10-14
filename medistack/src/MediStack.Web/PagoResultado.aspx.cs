using System;
using System.Web;
using MediStack.Dominio;
using MediStack.Negocio;

namespace MediStack.Web
{
    // Pagina a la que Mercado Pago devuelve al paciente (back_urls).
    // Mercado Pago agrega a la URL datos como ?payment_id=123&status=approved&external_reference=45.
    // NO se confia en esos datos: solo se toma el payment_id y se consulta el pago real a Mercado Pago.
    public partial class PagoResultadoPagina : PaginaProtegida
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack)
            {
                return;
            }

            // payment_id es el nombre actual; collection_id, el historico. Si no pago, llega vacio o "null".
            string pagoId = Request.QueryString["payment_id"];
            if (string.IsNullOrWhiteSpace(pagoId) || pagoId == "null")
            {
                pagoId = Request.QueryString["collection_id"];
            }

            if (string.IsNullOrWhiteSpace(pagoId) || pagoId == "null")
            {
                MostrarPagoNoRealizado();
                return;
            }

            ResultadoPagoSena resultado;
            try
            {
                resultado = new PagoSenaNegocio().ProcesarPago(pagoId);
            }
            catch (InvalidOperationException)
            {
                // Mercado Pago no respondio o el pago no existe. Si el pago fue real, el webhook lo acredita igual.
                Mostrar("No pudimos verificar el pago", "alert alert-error",
                    "No pudimos confirmar el pago con Mercado Pago en este momento. Si lo realizaste, "
                    + "se va a acreditar automáticamente en unos minutos: revisá el estado en Mis turnos.");
                return;
            }

            switch (resultado.Estado)
            {
                case EstadoProcesamientoPago.Acreditado:
                    Mostrar("¡Pago aprobado!", "alert alert-success",
                        "Recibimos el pago de la seña. Tu turno quedó confirmado.");
                    break;

                case EstadoProcesamientoPago.EnProceso:
                    Mostrar("Pago en proceso", "alert alert-success",
                        "Mercado Pago está procesando tu pago. Cuando se apruebe, tu turno se confirma solo.");
                    break;

                case EstadoProcesamientoPago.Rechazado:
                    Mostrar("Pago rechazado", "alert alert-error",
                        "El pago no se pudo realizar y no se cobró nada. Podés intentarlo de nuevo con otro medio de pago "
                        + "mientras tu reserva siga vigente.");
                    MostrarReintento(resultado.TurnoId);
                    break;

                case EstadoProcesamientoPago.RequiereRevision:
                    Mostrar("Pago recibido, turno no disponible", "alert alert-error",
                        "Recibimos tu pago, pero el turno ya no estaba disponible (por ejemplo, venció el plazo de la reserva). "
                        + "La clínica se va a comunicar con vos para reintegrarte el dinero.");
                    break;

                default:
                    MostrarPagoNoRealizado();
                    break;
            }
        }

        private void MostrarPagoNoRealizado()
        {
            Mostrar("Pago no realizado", "alert alert-error",
                "No se completó el pago. Si tu reserva sigue vigente, podés pagar la seña desde Mis turnos.");
        }

        private void MostrarReintento(int? turnoId)
        {
            if (turnoId.HasValue)
            {
                ReintentarPago.NavigateUrl = ResolveUrl("~/PagarSena.aspx?turnoId=" + turnoId.Value);
                ReintentarPago.Visible = true;
            }
        }

        private void Mostrar(string titulo, string clase, string detalle)
        {
            Titulo.Text = titulo;
            Mensaje.Text = HttpUtility.HtmlEncode(titulo);
            Mensaje.CssClass = clase;
            Mensaje.Visible = true;
            Detalle.Text = detalle;
        }
    }
}
