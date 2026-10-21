using System;
using System.Collections.Generic;
using System.Data;
using System.Web.UI.WebControls;
using MediStack.Dominio;
using MediStack.Negocio;

namespace MediStack.Web
{
    public partial class Profesionales : PaginaAdministrativa
    {
        private readonly GestionClinicaNegocio _negocio = new GestionClinicaNegocio();
        private readonly AgendaNegocio _agendaNegocio = new AgendaNegocio();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                CargarConManejoDeErrores(Mensaje, "No se pudieron cargar los profesionales",
                    () =>
                    {
                        LimpiarFormulario();
                        CargarProfesionales();
                    });
            }
        }

        protected void Buscar_Click(object sender, EventArgs e)
        {
            CargarConManejoDeErrores(Mensaje, "No se pudieron buscar los profesionales", CargarProfesionales);
        }

        protected void VerTodos_Click(object sender, EventArgs e)
        {
            Busqueda.Text = string.Empty;
            CargarConManejoDeErrores(Mensaje, "No se pudieron cargar los profesionales", CargarProfesionales);
        }

        protected void btnExportarCsv_Click(object sender, EventArgs e)
        {
            CargarConManejoDeErrores(Mensaje, "No se pudo exportar a CSV", () =>
            {
                string subtitulo;
                DataTable tabla = ObtenerTablaProfesionalesParaExportar(out subtitulo);
                if (tabla == null) return;
                ExportadorUtil.ExportarCSV(tabla, "Padron_Profesionales_" + DateTime.Now.ToString("yyyyMMdd_HHmm"));
            });
        }

        protected void btnExportarPdf_Click(object sender, EventArgs e)
        {
            CargarConManejoDeErrores(Mensaje, "No se pudo exportar a PDF", () =>
            {
                string subtitulo;
                DataTable tabla = ObtenerTablaProfesionalesParaExportar(out subtitulo);
                if (tabla == null) return;
                ExportadorUtil.ExportarPDF(tabla, "Padron_Profesionales_" + DateTime.Now.ToString("yyyyMMdd_HHmm"), "Padrón de Profesionales", subtitulo);
            });
        }

        private DataTable ObtenerTablaProfesionalesParaExportar(out string subtitulo)
        {
            subtitulo = string.Empty;
            DataTable datosCrudos = _negocio.BuscarProfesionales(Busqueda.Text);
            string filtroInfo = string.IsNullOrWhiteSpace(Busqueda.Text) ? "todos los registros" : "búsqueda: \"" + Busqueda.Text.Trim() + "\"";
            subtitulo = datosCrudos.Rows.Count + (datosCrudos.Rows.Count == 1 ? " profesional" : " profesionales") + " (" + filtroInfo + ").";

            var columnas = new Dictionary<string, string>
            {
                { "Nombre", "Nombre" },
                { "Apellido", "Apellido" },
                { "MatriculaProfesional", "Matrícula" },
                { "Especialidades", "Especialidades" },
                { "NumeroDocumento", "Documento" },
                { "Email", "Correo electrónico" },
                { "Telefono", "Teléfono" },
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

                List<int> especialidadesSeleccionadas = new List<int>();
                foreach (ListItem item in EspecialidadesList.Items)
                {
                    int espId;
                    if (item.Selected && int.TryParse(item.Value, out espId))
                    {
                        especialidadesSeleccionadas.Add(espId);
                    }
                }

                bool esAlta = ViewState["ProfesionalId"] == null;
                Profesional profesional = new Profesional
                {
                    UsuarioId = esAlta ? Guid.Empty : (Guid)ViewState["ProfesionalId"],
                    NombreUsuario = Usuario.Text,
                    Nombre = Nombre.Text,
                    Apellido = Apellido.Text,
                    NumeroDocumento = Documento.Text,
                    FechaNacimiento = fecha,
                    Email = Email.Text,
                    Telefono = Telefono.Text,
                    MatriculaProfesional = Matricula.Text,
                    EspecialidadesIds = especialidadesSeleccionadas
                };

                _negocio.GuardarProfesional(profesional, Contrasena.Text);

                if (esAlta)
                {
                    GuardarHorariosInicialesSiCorresponde(profesional.UsuarioId, especialidadesSeleccionadas);
                }

                LimpiarFormulario();
                CargarProfesionales();
                MostrarExito(Mensaje, "Los datos del profesional se guardaron correctamente.");
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(Mensaje, ex.Message);
            }
            catch (FormatException)
            {
                MostrarError(Mensaje, "El profesional seleccionado no es valido. Actualiza la pagina e intenta nuevamente.");
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                MostrarError(Mensaje, "No se pudieron cargar o guardar los datos del profesional (error SQL " + ex.Number + ").");
            }
        }

        private void GuardarHorariosInicialesSiCorresponde(Guid profesionalId, List<int> especialidadesIds)
        {
            if (profesionalId == Guid.Empty)
            {
                // Si el metodo GuardarProfesional no asigno el Guid en la instancia, lo buscamos por matricula
                DataTable busqueda = _negocio.BuscarProfesionales(Matricula.Text.Trim());
                foreach (DataRow fila in busqueda.Rows)
                {
                    if (string.Equals(Convert.ToString(fila["MatriculaProfesional"]), Matricula.Text.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        profesionalId = (Guid)fila["ProfesionalId"];
                        break;
                    }
                }
            }

            if (profesionalId == Guid.Empty) return;

            List<int> diasSeleccionados = new List<int>();
            foreach (ListItem item in DiasInicialesList.Items)
            {
                int dia;
                if (item.Selected && int.TryParse(item.Value, out dia))
                {
                    diasSeleccionados.Add(dia);
                }
            }

            if (diasSeleccionados.Count == 0) return;

            TimeSpan horaInicio;
            TimeSpan horaFin;
            if (!TimeSpan.TryParse(HoraInicioInicial.Text, out horaInicio)
                || !TimeSpan.TryParse(HoraFinInicial.Text, out horaFin)
                || horaInicio >= horaFin)
            {
                return;
            }

            foreach (int espId in especialidadesIds)
            {
                foreach (int dia in diasSeleccionados)
                {
                    _agendaNegocio.GuardarHorario(new HorarioAtencion
                    {
                        ProfesionalId = profesionalId,
                        EspecialidadId = espId,
                        DiaSemana = (byte)dia,
                        HoraInicio = horaInicio,
                        HoraFin = horaFin
                    });
                }
            }
        }

        protected void ProfesionalesGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int indice;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out indice)
                || indice < 0 || indice >= ProfesionalesGrid.DataKeys.Count)
            {
                return;
            }

            try
            {
                Guid id = GuidDataKey(ProfesionalesGrid, indice, "ProfesionalId");
                if (e.CommandName == "Editar")
                {
                    CargarProfesionalEnFormulario(indice);
                }
                else if (e.CommandName == "CambiarEstado")
                {
                    bool activo = Convert.ToBoolean(ProfesionalesGrid.DataKeys[indice].Values["Activo"]);
                    _negocio.CambiarEstadoProfesional(id, !activo);
                    CargarProfesionales();
                    MostrarExito(Mensaje, activo ? "El profesional fue desactivado." : "El profesional fue reactivado.");
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

        private void CargarProfesionales()
        {
            ProfesionalesGrid.DataSource = _negocio.BuscarProfesionales(Busqueda.Text);
            ProfesionalesGrid.DataBind();
        }

        private void CargarOpcionesEspecialidades()
        {
            EspecialidadesList.DataSource = _negocio.ObtenerEspecialidadesActivas();
            EspecialidadesList.DataTextField = "Nombre";
            EspecialidadesList.DataValueField = "EspecialidadId";
            EspecialidadesList.DataBind();
        }

        private void CargarProfesionalEnFormulario(int indice)
        {
            CargarOpcionesEspecialidades();
            Guid profesionalId = GuidDataKey(ProfesionalesGrid, indice, "ProfesionalId");
            ViewState["ProfesionalId"] = profesionalId;
            Nombre.Text = TextoDataKey(ProfesionalesGrid, indice, "Nombre");
            Apellido.Text = TextoDataKey(ProfesionalesGrid, indice, "Apellido");
            Documento.Text = TextoDataKey(ProfesionalesGrid, indice, "NumeroDocumento");
            Nacimiento.Text = FechaDataKey(ProfesionalesGrid, indice, "FechaNacimiento").ToString("yyyy-MM-dd");
            Email.Text = TextoDataKey(ProfesionalesGrid, indice, "Email");
            Telefono.Text = TextoDataKey(ProfesionalesGrid, indice, "Telefono");
            Matricula.Text = TextoDataKey(ProfesionalesGrid, indice, "MatriculaProfesional");

            DataTable especialidadesAsignadas = _negocio.ObtenerEspecialidadesProfesional(profesionalId);
            foreach (DataRow fila in especialidadesAsignadas.Rows)
            {
                string idStr = Convert.ToString(fila["EspecialidadId"]);
                ListItem item = EspecialidadesList.Items.FindByValue(idStr);
                if (item == null)
                {
                    item = new ListItem(Convert.ToString(fila["Nombre"]) + " (inactiva)", idStr);
                    EspecialidadesList.Items.Add(item);
                }
                item.Selected = true;
            }

            Usuario.Text = TextoDataKey(ProfesionalesGrid, indice, "NombreUsuario");
            Usuario.Enabled = false;
            CuentaNueva.Visible = false;
            TituloFormulario.Text = "Editar profesional";
            Guardar.Text = "Guardar cambios";
            LimpiarMensaje(Mensaje);
        }

        private void LimpiarFormulario()
        {
            CargarOpcionesEspecialidades();
            ViewState.Remove("ProfesionalId");
            Nombre.Text = string.Empty;
            Apellido.Text = string.Empty;
            Documento.Text = string.Empty;
            Nacimiento.Text = string.Empty;
            Email.Text = string.Empty;
            Telefono.Text = string.Empty;
            Matricula.Text = string.Empty;
            Usuario.Text = string.Empty;
            Usuario.Enabled = true;
            Contrasena.Text = string.Empty;
            foreach (ListItem item in DiasInicialesList.Items)
            {
                item.Selected = false;
            }
            HoraInicioInicial.Text = "08:00";
            HoraFinInicial.Text = "14:00";
            CuentaNueva.Visible = true;
            TituloFormulario.Text = "Alta de profesional";
            Guardar.Text = "Guardar profesional";
        }
    }
}