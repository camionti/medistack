using System;
using System.Globalization;
using System.Web;
using MediStack.Dominio;
using MediStack.Negocio;

namespace MediStack.Web
{
    // Checkout de la seña: muestra el detalle del turno y manda al paciente a pagar a Mercado Pago.
    // URL: PagarSena.aspx?turnoId=45
    public partial class PagarSenaPagina : PaginaProtegida
    {
        private static readonly CultureInfo CulturaAr = CultureInfo.GetCultureInfo("es-AR");
        private readonly PagoSenaNegocio _negocio = new PagoSenaNegocio();

        protected void Page_Load(object sender, EventArgs e)
        {
            // Solo el paciente paga su propia seña.
            if (!TieneRol("PACIENTE"))
            {
                Response.Redirect(ResolveUrl("~/Dashboard.aspx"), true);
            }

            if (!IsPostBack)
            {
                CargarDetalle();
            }
        }

        protected void PagarMercadoPago_Click(object sender, EventArgs e)
        {
            int? turnoId = LeerTurnoId();
            if (!turnoId.HasValue)
            {
                MostrarSinDetalle("No se indicó el turno a pagar.");
                return;
            }

            string link;
            try
            {
                // Negocio vuelve a validar todo desde la base y crea la preferencia en Mercado Pago.
                link = _negocio.IniciarPago(turnoId.Value, ObtenerUsuarioActual());
            }
            catch (InvalidOperationException ex)
            {
                CargarDetalle();
                MostrarError(ex.Message);
                return;
            }

            // Sale de MediStack hacia la pantalla de pago de Mercado Pago.
            Response.Redirect(link, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void CargarDetalle()
        {
            int? turnoId = LeerTurnoId();
            if (!turnoId.HasValue)
            {
                MostrarSinDetalle("No se indicó el turno a pagar.");
                return;
            }

            SenaParaPagar sena;
            try
            {
                sena = _negocio.ObtenerSenaParaPagar(turnoId.Value, ObtenerUsuarioActual());
            }
            catch (InvalidOperationException ex)
            {
                MostrarSinDetalle(ex.Message);
                return;
            }

            DetallePanel.Visible = true;
            SinDetallePanel.Visible = false;
            EspecialidadTexto.Text = sena.Especialidad;
            ProfesionalTexto.Text = sena.Profesional;
            FechaHoraTexto.Text = sena.FechaHora.ToString("dddd dd/MM/yyyy HH:mm", CulturaAr);
            EstadoTexto.Text = sena.Estado;
            MontoTexto.Text = sena.MontoSena.ToString("C", CulturaAr);
            VencimientoTexto.Text = sena.VencimientoSena.HasValue
                ? sena.VencimientoSena.Value.ToString("HH:mm 'hs' 'del' dd/MM", CulturaAr)
                : "-";

            PagarMercadoPago.Visible = sena.PuedePagarse;
            TiempoRestante.Text = string.Empty;

            if (sena.PuedePagarse)
            {
                int minutos = (int)Math.Ceiling((sena.VencimientoSena.Value - DateTime.Now).TotalMinutes);
                TiempoRestante.Text = minutos <= 1
                    ? "Te queda menos de un minuto para pagar."
                    : "Te quedan " + minutos + " minutos para pagar.";
            }
            else if (sena.EstadoSena == "Pagada")
            {
                MostrarExito("La seña ya está pagada. Tu turno está confirmado.");
            }
            else if (sena.EstadoSena == "Vencida")
            {
                MostrarError("Venció el plazo para pagar la seña y el horario se liberó. Podés reservar un nuevo turno.");
            }
            else
            {
                MostrarError("Este turno no tiene una seña pendiente de pago.");
            }
        }

        private int? LeerTurnoId()
        {
            int turnoId;
            return int.TryParse(Request.QueryString["turnoId"], NumberStyles.None, CultureInfo.InvariantCulture, out turnoId)
                && turnoId > 0
                ? turnoId
                : (int?)null;
        }

        private Guid ObtenerUsuarioActual()
        {
            Guid usuarioId;
            if (!Guid.TryParse(Convert.ToString(Session["UsuarioId"]), out usuarioId))
            {
                throw new InvalidOperationException("La sesión no contiene un identificador de usuario válido.");
            }

            return usuarioId;
        }

        private void MostrarSinDetalle(string mensaje)
        {
            DetallePanel.Visible = false;
            SinDetallePanel.Visible = true;
            MostrarError(mensaje);
        }

        private void MostrarExito(string mensaje)
        {
            Mensaje.Text = HttpUtility.HtmlEncode(mensaje);
            Mensaje.CssClass = "alert alert-success";
            Mensaje.Visible = true;
        }

        private void MostrarError(string mensaje)
        {
            Mensaje.Text = HttpUtility.HtmlEncode(mensaje);
            Mensaje.CssClass = "alert alert-error";
            Mensaje.Visible = true;
        }
    }
}
