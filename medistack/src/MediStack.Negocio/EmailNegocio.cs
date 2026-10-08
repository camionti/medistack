using System;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using MediStack.Dominio;

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
    }
}