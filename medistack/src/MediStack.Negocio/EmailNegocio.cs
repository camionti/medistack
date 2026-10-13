using System;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using MediStack.Dominio;
using System.Text;
namespace MediStack.Negocio
{
    public class EmailNegocio
    {
        public bool EnviarBienvenida(Paciente paciente)
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
                    Trace.TraceError("Falta Email.Clave: revisá que secrets.config esté en la carpeta de MediStack.Web"); // ahora deja rastro
                    return false;
                }

                string nombre = WebUtility.HtmlEncode(paciente.Nombre + " " + paciente.Apellido);

                using (MailMessage mensaje = new MailMessage())  
                {
                    mensaje.From = new MailAddress(usuario, remitente); 
                    mensaje.To.Add(paciente.Email);                      
                    mensaje.Subject = "¡Registro exitoso en Medistack!"; 
                    mensaje.IsBodyHtml = true;                           
                    mensaje.Body = "<h2>¡Hola " + nombre + "!</h2>"
                                 + "<p>Tu registro en <b>Medistack</b> se completó con éxito.</p>"
                                 + "<p>Ya podés iniciar sesión con tu usuario <b>"
                                 + WebUtility.HtmlEncode(paciente.NombreUsuario) + "</b>.</p>";

                    using (SmtpClient smtp = new SmtpClient(servidor, puerto)) 
                    {
                        smtp.EnableSsl = true;                                  
                        smtp.Credentials = new NetworkCredential(usuario, clave); 
                        smtp.Send(mensaje);                                     
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError("No se pudo enviar el mail de bienvenida: " + ex);
                return false;
            }
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
                        smtp.Send(mensaje);
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