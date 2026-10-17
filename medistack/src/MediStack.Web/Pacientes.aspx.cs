using MediStack.Dominio;
using MediStack.Negocio;
using System;
using System.Collections.Generic;
using System.Data;
using System.Web.UI.WebControls;

namespace MediStack.Web
{
    public partial class Pacientes : PaginaAdministrativa
    {
        private readonly GestionClinicaNegocio _negocio = new GestionClinicaNegocio();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                CargarConManejoDeErrores(Mensaje, "No se pudieron cargar los pacientes",
                    () =>
                    {
                        LimpiarFormulario();
                        CargarPacientes();
                    });
            }
        }

        protected void Buscar_Click(object sender, EventArgs e)
        {
            CargarConManejoDeErrores(Mensaje, "No se pudieron buscar los pacientes", CargarPacientes);
        }

        protected void LimpiarBusqueda_Click(object sender, EventArgs e)
        {
            Busqueda.Text = string.Empty;
            CargarConManejoDeErrores(Mensaje, "No se pudieron cargar los pacientes", CargarPacientes);
        }

        protected void btnExportarCsv_Click(object sender, EventArgs e)
        {
            CargarConManejoDeErrores(Mensaje, "No se pudo exportar a CSV", () =>
            {
                string subtitulo;
                DataTable tabla = ObtenerTablaPacientesParaExportar(out subtitulo);
                if (tabla == null) return;
                ExportadorUtil.ExportarCSV(tabla, "Padron_Pacientes_" + DateTime.Now.ToString("yyyyMMdd_HHmm"));
            });
        }

        protected void btnExportarPdf_Click(object sender, EventArgs e)
        {
            CargarConManejoDeErrores(Mensaje, "No se pudo exportar a PDF", () =>
            {
                string subtitulo;
                DataTable tabla = ObtenerTablaPacientesParaExportar(out subtitulo);
                if (tabla == null) return;
                ExportadorUtil.ExportarPDF(tabla, "Padron_Pacientes_" + DateTime.Now.ToString("yyyyMMdd_HHmm"), "Padrón de Pacientes Registrados", subtitulo);
            });
        }

        private DataTable ObtenerTablaPacientesParaExportar(out string subtitulo)
        {
            subtitulo = string.Empty;
            var resultadoBusqueda = _negocio.BuscarPacientes(Busqueda.Text);

            DataTable datosCrudos;
            if (resultadoBusqueda is DataTable dt)
            {
                datosCrudos = dt;
            }
            else
            {
                // Si la capa de negocio devuelve una lista genérica, la convertimos a DataTable de forma segura
                datosCrudos = new DataTable();
                datosCrudos.Columns.Add("Nombre", typeof(string));
                datosCrudos.Columns.Add("Apellido", typeof(string));
                datosCrudos.Columns.Add("NumeroDocumento", typeof(string));
                datosCrudos.Columns.Add("Email", typeof(string));
                datosCrudos.Columns.Add("Telefono", typeof(string));
                datosCrudos.Columns.Add("ObraSocial", typeof(string));
                datosCrudos.Columns.Add("NumeroAfiliado", typeof(string));
                datosCrudos.Columns.Add("Activo", typeof(bool));

                foreach (DataKey key in PacientesGrid.DataKeys)
                {
                    DataRow row = datosCrudos.NewRow();
                    row["Nombre"] = key.Values["Nombre"];
                    row["Apellido"] = key.Values["Apellido"];
                    row["NumeroDocumento"] = key.Values["NumeroDocumento"];
                    row["Email"] = key.Values["Email"];
                    row["Telefono"] = key.Values["Telefono"];
                    row["ObraSocial"] = key.Values["ObraSocial"];
                    row["NumeroAfiliado"] = key.Values["NumeroAfiliado"];
                    row["Activo"] = key.Values["Activo"];
                    datosCrudos.Rows.Add(row);
                }
            }

            string filtroInfo = string.IsNullOrWhiteSpace(Busqueda.Text) ? "todos los registros" : "búsqueda: \"" + Busqueda.Text.Trim() + "\"";
            subtitulo = datosCrudos.Rows.Count + (datosCrudos.Rows.Count == 1 ? " paciente" : " pacientes") + " (" + filtroInfo + ").";

            var columnas = new Dictionary<string, string>
            {
                { "Nombre", "Nombre" },
                { "Apellido", "Apellido" },
                { "NumeroDocumento", "Documento" },
                { "Email", "Correo electrónico" },
                { "Telefono", "Teléfono" },
                { "ObraSocial", "Obra social" },
                { "NumeroAfiliado", "N° Afiliado" },
                { "Activo", "Estado" }
            };

            return ExportadorUtil.PrepararTablaParaReporte(datosCrudos, columnas);
        }

        protected void Nuevo_Click(object sender, EventArgs e)
        {
            LimpiarMensaje(Mensaje);
            LimpiarFormulario();
        }

        protected void Guardar_Click(object sender, EventArgs e)
        {
            try
            {
                DateTime fecha;
                if (!DateTime.TryParse(Nacimiento.Text, out fecha))
                {
                    MostrarError(Mensaje, "Ingresa una fecha de nacimiento valida.");
                    return;
                }

                int obraSocialId;
                Paciente paciente = new Paciente
                {
                    UsuarioId = ViewState["PacienteId"] == null ? Guid.Empty : (Guid)ViewState["PacienteId"],
                    NombreUsuario = Usuario.Text,
                    Nombre = Nombre.Text,
                    Apellido = Apellido.Text,
                    NumeroDocumento = Documento.Text,
                    FechaNacimiento = fecha,
                    Email = Email.Text,
                    Telefono = Telefono.Text,
                    ObraSocialId = int.TryParse(ObraSocial.SelectedValue, out obraSocialId) ? (int?)obraSocialId : null,
                    NumeroAfiliado = Afiliado.Text,
                    ContactoEmergenciaNombre = EmergenciaNombre.Text,
                    ContactoEmergenciaTelefono = EmergenciaTelefono.Text
                };

                _negocio.GuardarPaciente(paciente, Contrasena.Text);
                LimpiarFormulario();
                CargarPacientes();
                MostrarExito(Mensaje, "Los datos del paciente se guardaron correctamente.");
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(Mensaje, ex.Message);
            }
            catch (FormatException)
            {
                MostrarError(Mensaje, "El paciente seleccionado no es valido. Actualiza la pagina e intenta nuevamente.");
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                MostrarError(Mensaje, "No se pudieron cargar o guardar los datos del paciente (error SQL " + ex.Number + ").");
            }
        }

        protected void PacientesGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int indice;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out indice)
                || indice < 0 || indice >= PacientesGrid.DataKeys.Count)
            {
                return;
            }

            try
            {
                Guid id = GuidDataKey(PacientesGrid, indice, "PacienteId");
                if (e.CommandName == "Editar")
                {
                    CargarPacienteEnFormulario(indice);
                }
                else if (e.CommandName == "CambiarEstado")
                {
                    bool activo = Convert.ToBoolean(PacientesGrid.DataKeys[indice].Values["Activo"]);
                    _negocio.CambiarEstadoPaciente(id, !activo);
                    CargarPacientes();
                    MostrarExito(Mensaje, activo ? "El paciente fue desactivado." : "El paciente fue reactivado.");
                }
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(Mensaje, ex.Message);
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                MostrarError(Mensaje, "No se pudo completar la operacion (error SQL " + ex.Number + ").");
            }
        }

        private void CargarPacientes()
        {
            PacientesGrid.DataSource = _negocio.BuscarPacientes(Busqueda.Text);
            PacientesGrid.DataBind();
        }

        private void CargarObrasSociales()
        {
            ObraSocial.DataSource = _negocio.ObtenerOpcionesObraSocialPaciente();
            ObraSocial.DataTextField = "Nombre";
            ObraSocial.DataValueField = "ObraSocialId";
            ObraSocial.DataBind();
            ObraSocial.Items.Insert(0, new ListItem("Particular / sin obra social", string.Empty));
        }

        private void CargarPacienteEnFormulario(int indice)
        {
            DataKey datos = PacientesGrid.DataKeys[indice];
            ViewState["PacienteId"] = Guid.Parse(Convert.ToString(datos.Values["PacienteId"]));
            Nombre.Text = TextoDataKey(PacientesGrid, indice, "Nombre");
            Apellido.Text = TextoDataKey(PacientesGrid, indice, "Apellido");
            Documento.Text = TextoDataKey(PacientesGrid, indice, "NumeroDocumento");
            Nacimiento.Text = FechaDataKey(PacientesGrid, indice, "FechaNacimiento").ToString("yyyy-MM-dd");
            Email.Text = TextoDataKey(PacientesGrid, indice, "Email");
            Telefono.Text = TextoDataKey(PacientesGrid, indice, "Telefono");
            Afiliado.Text = TextoDataKey(PacientesGrid, indice, "NumeroAfiliado");
            EmergenciaNombre.Text = TextoDataKey(PacientesGrid, indice, "ContactoEmergenciaNombre");
            EmergenciaTelefono.Text = TextoDataKey(PacientesGrid, indice, "ContactoEmergenciaTelefono");
            string obraSocialId = TextoDataKey(PacientesGrid, indice, "ObraSocialId");
            if (!string.IsNullOrEmpty(obraSocialId) && ObraSocial.Items.FindByValue(obraSocialId) == null)
            {
                ObraSocial.Items.Add(new ListItem(
                    TextoDataKey(PacientesGrid, indice, "ObraSocial") + " (inactiva)", obraSocialId));
            }

            SeleccionarValor(ObraSocial, obraSocialId);
            Usuario.Text = TextoDataKey(PacientesGrid, indice, "NombreUsuario");
            Usuario.Enabled = false;
            CuentaNueva.Visible = false;
            TituloFormulario.Text = "Editar paciente";
            Guardar.Text = "Guardar cambios";
            LimpiarMensaje(Mensaje);
        }

        private void LimpiarFormulario()
        {
            CargarObrasSociales();
            ViewState.Remove("PacienteId");
            Nombre.Text = string.Empty;
            Apellido.Text = string.Empty;
            Documento.Text = string.Empty;
            Nacimiento.Text = string.Empty;
            Email.Text = string.Empty;
            Telefono.Text = string.Empty;
            Afiliado.Text = string.Empty;
            EmergenciaNombre.Text = string.Empty;
            EmergenciaTelefono.Text = string.Empty;
            Usuario.Text = string.Empty;
            Usuario.Enabled = true;
            Contrasena.Text = string.Empty;
            SeleccionarValor(ObraSocial, string.Empty);
            CuentaNueva.Visible = true;
            TituloFormulario.Text = "Alta de paciente";
            Guardar.Text = "Guardar paciente";
        }

        private static void SeleccionarValor(DropDownList lista, string valor)
        {
            ListItem opcion = lista.Items.FindByValue(valor ?? string.Empty);
            lista.ClearSelection();
            if (opcion != null)
            {
                opcion.Selected = true;
            }
        }
    }
}
