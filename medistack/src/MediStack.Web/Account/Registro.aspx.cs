using System;
using System.Globalization;
using System.Web.Hosting;
using System.Web.UI;
using System.Web.UI.WebControls;
using MediStack.Dominio;
using MediStack.Negocio;

namespace MediStack.Web.Account
{
    public partial class Registro : Page
    {
        private readonly GestionClinicaNegocio _negocio = new GestionClinicaNegocio();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            if (Context.User.Identity.IsAuthenticated)
            {
                Response.Redirect(ResolveUrl("~/Dashboard.aspx"), false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            CargarObrasSociales();
        }

        private void CargarObrasSociales()
        {
            ObraSocial.DataSource = _negocio.ObtenerOpcionesObraSocialPaciente();
            ObraSocial.DataTextField = "Nombre";
            ObraSocial.DataValueField = "ObraSocialId";
            ObraSocial.DataBind();
            ObraSocial.Items.Insert(0, new ListItem("Particular / Sin obra social", ""));
        }

        protected void Registrarme_Click(object sender, EventArgs e)
        {
            ErrorMessage.Visible = false;

            if (Password.Text != ConfirmarPassword.Text)
            {
                MostrarError("Las contraseñas no coinciden.");
                return;
            }
            if (!Terminos.Checked)
            {
                MostrarError("Debes aceptar los términos para continuar.");
                return;
            }

            DateTime nacimiento;
            if (!DateTime.TryParseExact(FechaNacimiento.Text, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out nacimiento))
            {
                MostrarError("Ingresa una fecha de nacimiento válida.");
                return;
            }

            int obraSocialId;
            Paciente paciente = new Paciente
            {
                NombreUsuario = Usuario.Text,
                Nombre = Nombre.Text,
                Apellido = Apellido.Text,
                NumeroDocumento = Dni.Text,
                FechaNacimiento = nacimiento,
                Email = Email.Text,
                Telefono = Telefono.Text,
                ObraSocialId = int.TryParse(ObraSocial.SelectedValue, out obraSocialId) ? obraSocialId : (int?)null,
                NumeroAfiliado = NumeroAfiliado.Text
            };

            try
            {
                _negocio.GuardarPaciente(paciente, Password.Text);
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(ex.Message);
                return;
            }

            // 1) Mail de bienvenida en segundo plano: no demora la respuesta al usuario.
            //    EnviarBienvenida atrapa sus propias excepciones, asi que un fallo no rompe nada.
            Paciente datosMail = paciente;
            HostingEnvironment.QueueBackgroundWorkItem(ct => new EmailNegocio().EnviarBienvenida(datosMail));

            // 2) Login automatico reutilizando el login real (verifica hash y trae el rol desde la base).
            ResultadoLogin resultado = new UsuarioNegocio().IniciarSesion(paciente.NombreUsuario, Password.Text);
            string destino = "~/Account/Login.aspx";
            if (resultado.Exitoso)
            {
                SesionUsuario.Iniciar(Context, resultado.Usuario);
                destino = "~/Dashboard.aspx";
            }

            // 3) Redirect despues del POST: si el usuario aprieta F5 no se reenvia el formulario.
            Response.Redirect(ResolveUrl(destino), false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void MostrarError(string mensaje)
        {
            ErrorMessage.Text = mensaje;
            ErrorMessage.Visible = true;
        }
    }
}