using System;
using System.Web.UI;
using MediStack.Dominio;
using MediStack.Negocio;

namespace MediStack.Web
{
    public partial class AsistentePaciente : PaginaProtegida
    {
        private readonly AgenteIAPacienteNegocio _agente = new AgenteIAPacienteNegocio();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TieneRol("PACIENTE"))
            {
                throw new System.Web.HttpException(403, "El asistente solo esta disponible para pacientes.");
            }
        }

        protected void Enviar_Click(object sender, EventArgs e)
        {
            try
            {
                Guid pacienteId = (Guid)Session["UsuarioId"];
                RespuestaAgenteIA respuesta = _agente.ProcesarMensaje(pacienteId, Consulta.Text, "Texto");
                Respuesta.Text = "<p class=\"form-note\"><strong>Asistente:</strong> " +
                    Server.HtmlEncode(respuesta.Texto) + "</p>";
                Consulta.Text = string.Empty;
            }
            catch (InvalidOperationException ex)
            {
                Mensaje.Text = Server.HtmlEncode(ex.Message);
                Mensaje.CssClass = "alert alert-error";
                Mensaje.Visible = true;
            }
        }
    }
}
