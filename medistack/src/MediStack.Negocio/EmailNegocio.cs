using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using MediStack.Dominio;

namespace MediStack.Negocio
{
    public class EmailNegocio
    {
        public bool EnviarBienvenida(Paciente paciente)
        {
            string nombre = WebUtility.HtmlEncode(paciente.Nombre + " " + paciente.Apellido); // HtmlEncode: un nombre con < > no rompe el HTML

            string contenido =
                  "<h2 style=\"margin:0 0 6px;font-size:20px;color:#0f172a\">¡Tu registro fue exitoso!</h2>" // título, igual que en los mails de turno
                + "<p style=\"margin:0 0 16px;color:#64748b;font-size:13px\">Te damos la bienvenida a MediStack</p>" // subtítulo gris
                + "<p style=\"margin:0 0 16px;color:#334155;font-size:15px;line-height:1.55\">Hola <b>" + nombre + "</b>,<br/>"
                + "tu registro se completó con éxito. Ya podés iniciar sesión con este usuario:</p>"
                + "<p style=\"margin:0 0 20px;padding:14px 18px;background:#f1f5ff;border:1px solid #d6e0ff;" // caja azul con el usuario (la misma de EnviarNombreUsuario)
                + "border-radius:10px;font-size:20px;font-weight:700;color:#2152ff;text-align:center\">"
                + WebUtility.HtmlEncode(paciente.NombreUsuario) + "</p>"
                + "<p style=\"margin:0 0 20px;color:#334155;font-size:15px;line-height:1.55\">"
                + "Desde tu cuenta vas a poder sacar y gestionar tus turnos médicos.</p>"
                + "<div style=\"margin:0 0 20px;padding:14px 18px;background:#fff8e6;border:1px solid #ffe2a3;border-radius:10px\">" // caja amarilla (la misma de EnviarPasswordCambiada)
                + "<p style=\"margin:0\"><b>¿No fuiste vos?</b> Si no creaste esta cuenta, avisá a la administración de la clínica.</p></div>"
                + "<p style=\"margin:0;color:#64748b;font-size:13px\">Si fuiste vos, no tenés que hacer nada más.</p>";

            return EnviarCorreo(paciente.Email, "¡Registro exitoso en MediStack!", // asunto con el mismo nombre que los demás mails
                ArmarPlantilla("Registro exitoso", contenido)); // ArmarPlantilla = banner azul + tarjeta + pie
        }

        public bool EnviarNombreUsuario(string destino, string nombreCompleto, string nombreUsuario)
        {
            string contenido =
                  "<p style=\"margin:0 0 16px\">Hola <b>" + WebUtility.HtmlEncode(nombreCompleto) + "</b>,</p>"
                + "<p style=\"margin:0 0 16px\">Recibimos una solicitud para recordar el <b>nombre de usuario</b> "
                + "asociado a este correo electrónico en MediStack.</p>"
                + "<p style=\"margin:0 0 8px\">Tu nombre de usuario es:</p>"
                + "<p style=\"margin:0 0 20px;padding:14px 18px;background:#f1f5ff;border:1px solid #d6e0ff;"
                + "border-radius:10px;font-size:20px;font-weight:700;color:#2152ff;text-align:center\">"
                + WebUtility.HtmlEncode(nombreUsuario) + "</p>"
                + "<p style=\"margin:0 0 16px\">Con ese usuario y tu contraseña ya podés iniciar sesión en MediStack.</p>"
                + "<p style=\"margin:0 0 16px\">Si además no recordás tu contraseña, entrá a <b>Recuperar cuenta</b> "
                + "y elegí la opción <i>Olvidé mi contraseña</i>.</p>"
                + "<p style=\"margin:0;color:#64748b;font-size:13px\">Si vos no hiciste esta solicitud, podés ignorar este mensaje: "
                + "tu cuenta sigue segura y no se modificó nada.</p>";

            return EnviarCorreo(destino, "Tu nombre de usuario en MediStack",
                ArmarPlantilla("Recordatorio de usuario", contenido));
        }

        public bool EnviarRecuperacionPassword(string destino, string nombreCompleto, string enlace)
        {
            string enlaceSeguro = WebUtility.HtmlEncode(enlace);

            string contenido =
                  "<p style=\"margin:0 0 16px\">Hola <b>" + WebUtility.HtmlEncode(nombreCompleto) + "</b>,</p>"
                + "<p style=\"margin:0 0 16px\">Recibimos una solicitud para <b>restablecer la contraseña</b> "
                + "de tu cuenta de MediStack. Para elegir una contraseña nueva, hacé clic en el siguiente botón:</p>"
                + "<p style=\"margin:24px 0;text-align:center\">"
                + "<a href=\"" + enlaceSeguro + "\" style=\"display:inline-block;padding:14px 28px;background:#2152ff;"
                + "color:#ffffff;text-decoration:none;font-weight:700;border-radius:10px\">Restablecer mi contraseña</a></p>"
                + "<div style=\"margin:0 0 20px;padding:14px 18px;background:#fff8e6;border:1px solid #ffe2a3;border-radius:10px\">"
                + "<p style=\"margin:0 0 8px;font-weight:700\">Antes de continuar, tené en cuenta que:</p>"
                + "<ul style=\"margin:0;padding-left:20px\">"
                + "<li>El enlace <b>caduca en 2 horas</b> desde que se solicitó.</li>"
                + "<li>Es de <b>un solo uso</b>: una vez que cambies tu contraseña, deja de funcionar.</li>"
                + "<li>Si necesitás otro, podés pedirlo de nuevo desde <i>Recuperar cuenta</i>.</li>"
                + "</ul></div>"
                + "<p style=\"margin:0 0 8px;color:#64748b;font-size:13px\">Si el botón no funciona, copiá y pegá esta dirección en tu navegador:</p>"
                + "<p style=\"margin:0 0 20px;font-size:12px;word-break:break-all;color:#475569\">" + enlaceSeguro + "</p>"
                + "<p style=\"margin:0;color:#64748b;font-size:13px\"><b>¿No fuiste vos?</b> Ignorá este correo: "
                + "tu contraseña actual sigue siendo la misma y nadie puede cambiarla sin este enlace. "
                + "Nunca compartas este mensaje con otras personas.</p>";

            return EnviarCorreo(destino, "Restablecé tu contraseña de MediStack",
                ArmarPlantilla("Restablecer contraseña", contenido));
        }

        public bool EnviarPasswordCambiada(string destino, string nombreCompleto)
        {
            string fecha = DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            string contenido =
                  "<p style=\"margin:0 0 16px\">Hola <b>" + WebUtility.HtmlEncode(nombreCompleto) + "</b>,</p>"
                + "<p style=\"margin:0 0 16px\">Te avisamos que la <b>contraseña</b> de tu cuenta de MediStack "
                + "fue modificada el <b>" + fecha + "</b>.</p>"
                + "<div style=\"margin:0 0 20px;padding:14px 18px;background:#fff8e6;border:1px solid #ffe2a3;border-radius:10px\">"
                + "<p style=\"margin:0\"><b>¿No fuiste vos?</b> Recuperá tu cuenta desde <i>Recuperar cuenta</i> "
                + "en la pantalla de ingreso y avisá a la administración de la clínica.</p></div>"
                + "<p style=\"margin:0;color:#64748b;font-size:13px\">Si fuiste vos, no tenés que hacer nada más.</p>";

            return EnviarCorreo(destino, "Tu contraseña de MediStack fue modificada",
                ArmarPlantilla("Contraseña modificada", contenido));
        }
        public bool EnviarConfirmacionTurno(NotificacionTurno datos)
        {
            return EnviarConfirmacionTurnoAsync(datos).GetAwaiter().GetResult();
        }

        public async Task<bool> EnviarConfirmacionTurnoAsync(NotificacionTurno datos)
        {
            if (datos == null || string.IsNullOrWhiteSpace(datos.PacienteEmail))
            {
                return false;
            }

            string contenido = ArmarCuerpoCorreoTurno(
                "¡Tu turno fue registrado con éxito!",
                "Confirmación de turno",
                "Te confirmamos que registramos la reserva de tu consulta médica en MediStack. A continuación encontrás todos los detalles de tu cita:",
                datos);

            return await EnviarCorreoAsync(datos.PacienteEmail, "Confirmación de turno - MediStack",
                ArmarPlantilla("Confirmación de turno", contenido));
        }

        public bool EnviarRecordatorioTurno(NotificacionTurno datos)
        {
            return EnviarRecordatorioTurnoAsync(datos).GetAwaiter().GetResult();
        }

        public async Task<bool> EnviarRecordatorioTurnoAsync(NotificacionTurno datos)
        {
            if (datos == null || string.IsNullOrWhiteSpace(datos.PacienteEmail))
            {
                return false;
            }

            string contenido = ArmarCuerpoCorreoTurno(
                "Recordatorio: Tenés un turno en 48 horas",
                "Recordatorio de consulta médica",
                "Te recordamos que en 48 horas tenés programada una consulta médica en MediStack. A continuación te compartimos los detalles de tu cita:",
                datos);

            return await EnviarCorreoAsync(datos.PacienteEmail, "Recordatorio de tu turno en 48 hs - MediStack",
                ArmarPlantilla("Recordatorio de turno", contenido));
        }

        public async Task<bool> EnviarCancelacionTurnoAsync(NotificacionTurno datos, string textoSena)
        {
            if (datos == null || string.IsNullOrWhiteSpace(datos.PacienteEmail))
            {
                return false;
            }

            string contenido = "<h2 style=\"margin:0 0 6px;font-size:20px;color:#0f172a\">Tu turno fue cancelado</h2>"
                + "<p style=\"margin:0 0 16px;color:#334155;font-size:15px;line-height:1.55\">Hola <b>"
                + WebUtility.HtmlEncode(datos.PacienteNombre) + "</b>,<br/>"
                + "te confirmamos que se canceló tu turno de <b>" + WebUtility.HtmlEncode(datos.Especialidad)
                + "</b> con " + WebUtility.HtmlEncode(datos.ProfesionalNombre) + " del "
                + WebUtility.HtmlEncode(FormatearFechaTurno(datos.FechaHora)) + ".</p>"
                + (string.IsNullOrWhiteSpace(textoSena)
                    ? string.Empty
                    : "<div style=\"background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;padding:14px 16px;margin:0 0 20px\">"
                      + "<p style=\"margin:0;font-size:14px;line-height:1.5;color:#1e293b\"><b>Seña:</b> "
                      + WebUtility.HtmlEncode(textoSena) + "</p></div>")
                + "<p style=\"margin:0;color:#64748b;font-size:13px\">Podés sacar un nuevo turno cuando quieras desde tu cuenta en MediStack.</p>";

            return await EnviarCorreoAsync(datos.PacienteEmail, "Turno cancelado - MediStack",
                ArmarPlantilla("Cancelación de turno", contenido));
        }

        private static string FormatearFechaTurno(DateTime fechaHora)
        {
            CultureInfo es = CultureInfo.GetCultureInfo("es-AR");
            return fechaHora.ToString("dddd d 'de' MMMM 'de' yyyy 'a las' HH:mm 'hs'", es);
        }

        private static string ArmarCuerpoCorreoTurno(
            string encabezado, string subtitulo, string introduccion, NotificacionTurno datos)
        {
            string paciente = WebUtility.HtmlEncode(datos.PacienteNombre);
            string profesional = WebUtility.HtmlEncode(datos.ProfesionalNombre);
            string especialidad = WebUtility.HtmlEncode(datos.Especialidad);
            string fechaHora = WebUtility.HtmlEncode(FormatearFechaTurno(datos.FechaHora));
            string estado = WebUtility.HtmlEncode(datos.Estado);
            string urlVer = WebUtility.HtmlEncode(datos.UrlVerTurno ?? "#");
            string urlCancelar = WebUtility.HtmlEncode(datos.UrlCancelarTurno ?? "#");

            return "<h2 style=\"margin:0 0 6px;font-size:20px;color:#0f172a\">" + WebUtility.HtmlEncode(encabezado) + "</h2>"
                 + "<p style=\"margin:0 0 16px;color:#64748b;font-size:13px\">" + WebUtility.HtmlEncode(subtitulo) + "</p>"
                 + "<p style=\"margin:0 0 16px;color:#334155;font-size:15px;line-height:1.55\">Hola <b>" + paciente + "</b>,<br/>"
                 + WebUtility.HtmlEncode(introduccion) + "</p>"
                 + "<table style=\"width:100%;border-collapse:collapse;margin:0 0 20px;background:#f8fafc;border-radius:10px;border:1px solid #e2e8f0;font-size:14px\">"
                 + "<tr><td style=\"padding:12px 16px;border-bottom:1px solid #edf2f7;color:#64748b;width:35%\">Fecha y hora</td>"
                 + "<td style=\"padding:12px 16px;border-bottom:1px solid #edf2f7;font-weight:700;color:#1e293b\">" + fechaHora + "</td></tr>"
                 + "<tr><td style=\"padding:12px 16px;border-bottom:1px solid #edf2f7;color:#64748b\">Especialidad</td>"
                 + "<td style=\"padding:12px 16px;border-bottom:1px solid #edf2f7;font-weight:600;color:#1e293b\">" + especialidad + "</td></tr>"
                 + "<tr><td style=\"padding:12px 16px;border-bottom:1px solid #edf2f7;color:#64748b\">Profesional</td>"
                 + "<td style=\"padding:12px 16px;border-bottom:1px solid #edf2f7;font-weight:600;color:#1e293b\">" + profesional + "</td></tr>"
                 + "<tr><td style=\"padding:12px 16px;border-bottom:1px solid #edf2f7;color:#64748b\">Estado</td>"
                 + "<td style=\"padding:12px 16px;border-bottom:1px solid #edf2f7;font-weight:600;color:#2152ff\">" + estado + "</td></tr>"
                 + "<tr><td style=\"padding:12px 16px;color:#64748b\">Ubicación</td>"
                 + "<td style=\"padding:12px 16px;color:#1e293b\">Clínica MediStack - Consultorios Centrales</td></tr>"
                 + "</table>"
                 + "<div style=\"background:#fffbeb;border:1px solid #fde68a;border-radius:8px;padding:14px 16px;margin:0 0 24px\">"
                 + "<p style=\"margin:0;font-size:13px;line-height:1.5;color:#92400e\">"
                 + "<b>Importante:</b> En caso de no poder asistir a tu consulta, por favor cancelá el turno con anticipación desde el botón a continuación para liberar el horario a otro paciente que lo necesite.</p>"
                 + "</div>"
                 + "<table style=\"width:100%;border-collapse:collapse;margin:0 0 20px\">"
                 + "<tr><td style=\"text-align:center;padding-bottom:12px\">"
                 + "<a href=\"" + urlVer + "\" style=\"display:inline-block;padding:12px 28px;background:#2152ff;color:#ffffff;text-decoration:none;font-weight:700;font-size:14px;border-radius:8px;min-width:180px\">Ver detalle de mi turno</a>"
                 + "</td></tr>"
                 + "<tr><td style=\"text-align:center\">"
                 + "<a href=\"" + urlCancelar + "\" style=\"display:inline-block;padding:10px 22px;background:#ffffff;border:1px solid #dc2626;color:#dc2626;text-decoration:none;font-weight:600;font-size:13px;border-radius:8px\">Cancelar turno</a>"
                 + "</td></tr>"
                 + "</table>"
                 + "<p style=\"margin:0;color:#64748b;font-size:12px;text-align:center\">Si los botones no funcionan en tu cliente de correo, podés gestionar tus citas ingresando a tu cuenta en MediStack.</p>";
        }

        private static string ArmarPlantilla(string titulo, string contenido)
        {
            return "<div style=\"background:#f4f6fb;padding:24px 12px;font-family:'Segoe UI',Arial,sans-serif;color:#1e293b\">"
                 + "<div style=\"max-width:560px;margin:0 auto;background:#ffffff;border-radius:16px;overflow:hidden;"
                 + "border:1px solid #e2e8f0\">"
                 + "<div style=\"background:#2152ff;padding:20px 28px;color:#ffffff\">"
                 + "<div style=\"font-size:22px;font-weight:800\">MediStack</div>"
                 + "<div style=\"font-size:14px;opacity:.9\">" + WebUtility.HtmlEncode(titulo) + "</div></div>"
                 + "<div style=\"padding:28px;font-size:15px;line-height:1.55\">" + contenido + "</div>"
                 + "<div style=\"padding:16px 28px;background:#f8fafc;border-top:1px solid #e2e8f0;color:#94a3b8;font-size:12px\">"
                 + "Este es un mensaje automático de MediStack, por favor no lo respondas.</div>"
                 + "</div></div>";
        }

        private bool EnviarCorreo(string destino, string asunto, string cuerpoHtml)
        {
          
            return Task.Run(() => EnviarCorreoAsync(destino, asunto, cuerpoHtml)).GetAwaiter().GetResult();
        }

        private async Task<bool> EnviarCorreoAsync(string destino, string asunto, string cuerpoHtml)
        {
            try
            {
                string servidor = ConfigurationManager.AppSettings["Email.Servidor"];
                int puerto = int.Parse(ConfigurationManager.AppSettings["Email.Puerto"]);
                string usuario = ConfigurationManager.AppSettings["Email.Usuario"];
                string clave = ConfigurationManager.AppSettings["Email.Clave"];
                string remitente = ConfigurationManager.AppSettings["Email.NombreRemitente"];

                if (string.IsNullOrEmpty(clave))
                {
                    Trace.TraceError("Falta Email.Clave: revisá que secrets.config esté en la carpeta de MediStack.Web");
                    return false;
                }

                using (MailMessage mensaje = new MailMessage())
                {
                    mensaje.From = new MailAddress(usuario, remitente);
                    mensaje.To.Add(destino);
                    mensaje.Subject = asunto;
                    mensaje.SubjectEncoding = Encoding.UTF8;
                    mensaje.IsBodyHtml = true;
                    mensaje.BodyEncoding = Encoding.UTF8;
                    mensaje.Body = cuerpoHtml;

                    using (SmtpClient smtp = new SmtpClient(servidor, puerto))
                    {
                        smtp.EnableSsl = true;
                        smtp.Credentials = new NetworkCredential(usuario, clave);
                        await smtp.SendMailAsync(mensaje);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError("No se pudo enviar el mail \"" + asunto + "\": " + ex);
                return false;
            }
        }
    }
}