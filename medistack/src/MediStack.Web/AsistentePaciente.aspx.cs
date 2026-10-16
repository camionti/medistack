using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using MediStack.Dominio;
using MediStack.Negocio;

namespace MediStack.Web
{
    // Asistente conversacional para los tres roles (Requerimiento 11). El nombre de la clase se conserva
    // por compatibilidad con la primera version, que solo atendia a pacientes.
    public partial class AsistentePaciente : PaginaProtegida
    {
        private const string ClaveHistorial = "IA.Historial";
        private const string ClaveAccion = "IA.AccionTurnoId";
        private const string ClaveAccionTexto = "IA.AccionTexto";

        private readonly AgenteIAPacienteNegocio _agente = new AgenteIAPacienteNegocio();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TieneRol("PACIENTE") && !TieneRol("PROFESIONAL") && !TieneRol("ADMINISTRATIVO"))
            {
                throw new System.Web.HttpException(403, "El rol de esta cuenta no tiene acceso al asistente.");
            }

            IntroLiteral.Text = Server.HtmlEncode(
                TieneRol("PACIENTE")
                    ? "Preguntá por tus turnos, cobros u horarios disponibles. Las operaciones que modifiquen tus turnos siempre requieren confirmación."
                    : TieneRol("PROFESIONAL")
                        ? "Consultá tu agenda y tus próximos turnos."
                        : "Consultá turnos del día, turnos sin resolver y el resumen de caja.");
            if (!IsPostBack)
            {
                MostrarConversacion();
            }
        }

        protected void Enviar_Click(object sender, EventArgs e)
        {
            try
            {
                Guid usuarioId = (Guid)Session["UsuarioId"];
                string consulta = Consulta.Text;
                List<MensajeIA> historial = ObtenerHistorial();
                RespuestaAgenteIA respuesta = _agente.ProcesarMensaje(
                    usuarioId, Convert.ToString(Session["RolCodigo"]), consulta, CanalEntrada.Value, historial);

                historial.Add(new MensajeIA { Rol = "user", Contenido = consulta.Trim() });
                historial.Add(new MensajeIA { Rol = "assistant", Contenido = respuesta.Texto });

                if (respuesta.AccionPendienteTurnoId.HasValue && TieneRol("PACIENTE"))
                {
                    Session[ClaveAccion] = respuesta.AccionPendienteTurnoId.Value;
                    Session[ClaveAccionTexto] = respuesta.AccionPendienteDescripcion;
                }
                else
                {
                    Session.Remove(ClaveAccion);
                    Session.Remove(ClaveAccionTexto);
                }

                Consulta.Text = string.Empty;
                CanalEntrada.Value = "Texto";
                MostrarConversacion();
            }
            catch (InvalidOperationException ex)
            {
                MostrarMensaje(ex.Message, false);
                MostrarConversacion();
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                MostrarMensaje("No se pudo registrar la consulta (error SQL " + ex.Number + ").", false);
                MostrarConversacion();
            }
        }

        protected void ConfirmarAccion_Click(object sender, EventArgs e)
        {
            object turno = Session[ClaveAccion];
            Session.Remove(ClaveAccion);
            Session.Remove(ClaveAccionTexto);
            if (turno == null || !TieneRol("PACIENTE"))
            {
                MostrarMensaje("No hay ninguna acción pendiente para confirmar.", false);
                MostrarConversacion();
                return;
            }

            try
            {
                string texto = _agente.ConfirmarCancelacion((Guid)Session["UsuarioId"], (int)turno);
                ObtenerHistorial().Add(new MensajeIA { Rol = "assistant", Contenido = texto });
                MostrarMensaje(texto, true);
            }
            catch (InvalidOperationException ex)
            {
                MostrarMensaje(ex.Message, false);
            }

            MostrarConversacion();
        }

        protected void DescartarAccion_Click(object sender, EventArgs e)
        {
            Session.Remove(ClaveAccion);
            Session.Remove(ClaveAccionTexto);
            MostrarMensaje("La acción fue descartada. No se modificó ningún turno.", true);
            MostrarConversacion();
        }

        protected void NuevaConversacion_Click(object sender, EventArgs e)
        {
            Session.Remove(ClaveHistorial);
            Session.Remove(ClaveAccion);
            Session.Remove(ClaveAccionTexto);
            MostrarConversacion();
        }

        private List<MensajeIA> ObtenerHistorial()
        {
            List<MensajeIA> historial = Session[ClaveHistorial] as List<MensajeIA>;
            if (historial == null)
            {
                historial = new List<MensajeIA>();
                Session[ClaveHistorial] = historial;
            }

            return historial;
        }

        private void MostrarConversacion()
        {
            System.Text.StringBuilder html = new System.Text.StringBuilder();
            foreach (MensajeIA mensaje in ObtenerHistorial())
            {
                bool esUsuario = mensaje.Rol == "user";
                html.Append("<p class=\"form-note\"><strong>")
                    .Append(esUsuario ? "Vos" : "Asistente")
                    .Append(":</strong> ")
                    .Append(HttpUtility.HtmlEncode(mensaje.Contenido))
                    .Append("</p>");
            }

            Respuesta.Text = html.ToString();
            object accion = Session[ClaveAccionTexto];
            ConfirmacionPanel.Visible = accion != null;
            AccionLiteral.Text = accion == null ? string.Empty : Server.HtmlEncode(Convert.ToString(accion));
        }

        private void MostrarMensaje(string texto, bool exito)
        {
            Mensaje.Text = Server.HtmlEncode(texto);
            Mensaje.CssClass = exito ? "alert alert-success" : "alert alert-error";
            Mensaje.Visible = true;
        }
    }
}
