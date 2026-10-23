using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MediStack.Dominio;
using MediStack.Negocio;

namespace MediStack.Web
{
    public partial class ConsultorioPagina : PaginaProtegida
    {
        private readonly ConsultorioNegocio _consultorioNegocio = new ConsultorioNegocio();

        protected void Page_Load(object sender, EventArgs e)
        {
            bool esAdministrativo = TieneRol("ADMINISTRATIVO");
            bool esProfesional = TieneRol("PROFESIONAL");

            if (!esAdministrativo && !esProfesional)
            {
                Response.Redirect(ResolveUrl("~/Dashboard.aspx"), true);
                return;
            }

            if (!IsPostBack)
            {
                FiltroFecha.Text = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                if (esAdministrativo)
                {
                    PanelSelectorProfesional.Visible = true;
                    CargarProfesionales();
                }
                else
                {
                    PanelSelectorProfesional.Visible = false;
                    Guid usuarioActualId;
                    if (Guid.TryParse(Convert.ToString(Session["UsuarioId"]), out usuarioActualId))
                    {
                        ViewState["ProfesionalId"] = usuarioActualId;
                    }
                }

                CargarConsultorio();
            }
        }

        private void CargarProfesionales()
        {
            DataTable profesionales = _consultorioNegocio.ObtenerProfesionales();
            FiltroProfesional.DataSource = profesionales;
            FiltroProfesional.DataTextField = "Nombre";
            FiltroProfesional.DataValueField = "ProfesionalId";
            FiltroProfesional.DataBind();

            if (profesionales.Rows.Count > 0)
            {
                Guid profesionalSeleccionado;
                if (!string.IsNullOrWhiteSpace(Request.QueryString["ProfesionalId"])
                    && Guid.TryParse(Request.QueryString["ProfesionalId"], out profesionalSeleccionado))
                {
                    ListItem item = FiltroProfesional.Items.FindByValue(profesionalSeleccionado.ToString());
                    if (item != null)
                    {
                        FiltroProfesional.ClearSelection();
                        item.Selected = true;
                    }
                }

                if (FiltroProfesional.SelectedIndex < 0)
                {
                    FiltroProfesional.SelectedIndex = 0;
                }

                ViewState["ProfesionalId"] = Guid.Parse(FiltroProfesional.SelectedValue);
            }
        }

        protected void FiltroProfesional_SelectedIndexChanged(object sender, EventArgs e)
        {
            Guid profId;
            if (Guid.TryParse(FiltroProfesional.SelectedValue, out profId))
            {
                ViewState["ProfesionalId"] = profId;
            }
            CargarConsultorio();
        }

        protected void FiltroFecha_TextChanged(object sender, EventArgs e)
        {
            CargarConsultorio();
        }

        protected void Filtro_SelectedIndexChanged(object sender, EventArgs e)
        {
            CargarConsultorio();
        }

        protected void BtnBuscar_Click(object sender, EventArgs e)
        {
            CargarConsultorio();
        }

        protected void BtnLimpiar_Click(object sender, EventArgs e)
        {
            FiltroBusqueda.Text = string.Empty;
            FiltroEstado.SelectedValue = "Todos";
            FiltroFecha.Text = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            CargarConsultorio();
        }

        private void CargarConsultorio()
        {
            LimpiarMensaje();

            Guid profesionalId = ObtenerProfesionalActivo();
            if (profesionalId == Guid.Empty)
            {
                MostrarError("No se ha seleccionado ningún profesional válido.");
                ConsultorioGrid.DataSource = null;
                ConsultorioGrid.DataBind();
                return;
            }

            DateTime fecha = ObtenerFechaFiltro();
            Guid usuarioActualId = Guid.Parse(Convert.ToString(Session["UsuarioId"]));
            string rolCodigo = Convert.ToString(Session["RolCodigo"]);

            try
            {
                IList<TurnoConsultorioItem> turnos = _consultorioNegocio.ObtenerTurnosConsultorio(
                    usuarioActualId, rolCodigo, profesionalId, fecha);

                // Métricas globales del día
                int enEspera = turnos.Count(t => t.Estado == "EnEspera");
                int citados = turnos.Count(t => t.Estado != "Cancelado");
                int atendidos = turnos.Count(t => t.Estado == "Atendido");
                int pendientesEvolucion = turnos.Count(t => t.RequiereEvolucion);

                LitEnEspera.Text = enEspera.ToString(CultureInfo.InvariantCulture);
                LitCitadosHoy.Text = citados.ToString(CultureInfo.InvariantCulture);
                LitAtendidosHoy.Text = atendidos.ToString(CultureInfo.InvariantCulture);
                LitEvolucionesPendientes.Text = pendientesEvolucion.ToString(CultureInfo.InvariantCulture);

                LitTituloConsultorio.Text = "Pacientes Citados — " + fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                LitResumenConsultorio.Text = HttpUtility.HtmlEncode(
                    string.Format(CultureInfo.InvariantCulture,
                        "{0} turnos citados en la fecha ({1} en sala de espera, {2} atendidos, {3} con evolución pendiente).",
                        citados, enEspera, atendidos, pendientesEvolucion));

                // Filtrado en memoria
                IEnumerable<TurnoConsultorioItem> turnosFiltrados = turnos;

                string estado = FiltroEstado.SelectedValue;
                if (estado == "EnEspera")
                {
                    turnosFiltrados = turnosFiltrados.Where(t => t.Estado == "EnEspera");
                }
                else if (estado == "Confirmado")
                {
                    turnosFiltrados = turnosFiltrados.Where(t => t.Estado == "Confirmado");
                }
                else if (estado == "AtendidoConEvolucion")
                {
                    turnosFiltrados = turnosFiltrados.Where(t => t.Estado == "Atendido" && t.TieneEvolucion);
                }
                else if (estado == "AtendidoSinEvolucion")
                {
                    turnosFiltrados = turnosFiltrados.Where(t => t.RequiereEvolucion);
                }
                else if (estado == "Ausente")
                {
                    turnosFiltrados = turnosFiltrados.Where(t => t.Estado == "Ausente");
                }

                string textoBusqueda = (FiltroBusqueda.Text ?? string.Empty).Trim().ToLowerInvariant();
                if (!string.IsNullOrEmpty(textoBusqueda))
                {
                    turnosFiltrados = turnosFiltrados.Where(t =>
                        (t.NombrePaciente != null && t.NombrePaciente.ToLowerInvariant().Contains(textoBusqueda))
                        || (t.NumeroDocumento != null && t.NumeroDocumento.Contains(textoBusqueda)));
                }

                ConsultorioGrid.DataSource = turnosFiltrados.ToList();
                ConsultorioGrid.DataBind();
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(ex.Message);
            }
            catch (Exception)
            {
                MostrarError("Ocurrió un error al obtener la lista de pacientes del consultorio.");
            }
        }

        protected void ConsultorioGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "Atender")
            {
                int turnoId;
                if (int.TryParse(Convert.ToString(e.CommandArgument), out turnoId))
                {
                    AbrirModalAtencion(turnoId);
                }
            }
            else if (e.CommandName == "VerFicha")
            {
                Guid pacienteId;
                if (Guid.TryParse(Convert.ToString(e.CommandArgument), out pacienteId))
                {
                    AbrirModalFicha(pacienteId);
                }
            }
        }

        private void AbrirModalAtencion(int turnoId)
        {
            Guid profesionalId = ObtenerProfesionalActivo();
            Guid usuarioActualId = Guid.Parse(Convert.ToString(Session["UsuarioId"]));
            string rolCodigo = Convert.ToString(Session["RolCodigo"]);
            DateTime fecha = ObtenerFechaFiltro();

            try
            {
                IList<TurnoConsultorioItem> turnos = _consultorioNegocio.ObtenerTurnosConsultorio(
                    usuarioActualId, rolCodigo, profesionalId, fecha);

                TurnoConsultorioItem turno = turnos.FirstOrDefault(t => t.TurnoId == turnoId);
                if (turno == null)
                {
                    MostrarError("No se encontró el turno seleccionado.");
                    return;
                }

                ResumenClinicoRapido resumen = _consultorioNegocio.ObtenerResumenClinicoRapido(
                    usuarioActualId, rolCodigo, turno.PacienteId, profesionalId);

                ModalTitulo.Text = turno.Estado == "Atendido" ? "Evolución Clínica del Turno" : "Atención en Consultorio";
                ModalPacienteNombre.Text = HttpUtility.HtmlEncode(turno.NombrePaciente);
                ModalPacienteDni.Text = HttpUtility.HtmlEncode(turno.NumeroDocumento ?? "—");
                ModalHorarioTurno.Text = turno.FechaHora.ToString("HH:mm", CultureInfo.InvariantCulture) + " hs";

                ModalPacienteEdad.Text = resumen.Edad.ToString(CultureInfo.InvariantCulture);
                ModalPacienteTelefono.Text = HttpUtility.HtmlEncode(resumen.Telefono ?? "—");
                ModalPacienteObraSocial.Text = HttpUtility.HtmlEncode(resumen.ObraSocial ?? "Particular");
                ModalPacienteAfiliado.Text = HttpUtility.HtmlEncode(resumen.NumeroAfiliado ?? "—");
                ModalPacienteEmergencia.Text = HttpUtility.HtmlEncode(
                    !string.IsNullOrWhiteSpace(resumen.ContactoEmergenciaNombre)
                        ? resumen.ContactoEmergenciaNombre + " (" + (resumen.ContactoEmergenciaTelefono ?? "—") + ")"
                        : "—");

                ModalAntecedentesRepeater.DataSource = resumen.AntecedentesRecientes;
                ModalAntecedentesRepeater.DataBind();
                PanelSinAntecedentes.Visible = resumen.AntecedentesRecientes == null || resumen.AntecedentesRecientes.Count == 0;

                ModalLinkFichaCompleta.NavigateUrl = ResolveUrl("~/FichaPaciente.aspx?PacienteId=" + turno.PacienteId);

                ModalTurnoId.Value = turno.TurnoId.ToString(CultureInfo.InvariantCulture);
                ModalPacienteId.Value = turno.PacienteId.ToString();
                ModalMotivoConsulta.Text = turno.MotivoTurno ?? string.Empty;
                ModalDiagnostico.Text = turno.Diagnostico ?? string.Empty;
                ModalObservaciones.Text = turno.Observaciones ?? string.Empty;

                ModalMensajeError.Visible = false;
                ModalAtencionPanel.Visible = true;
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(ex.Message);
            }
            catch (Exception)
            {
                MostrarError("No se pudo preparar la ficha de atención.");
            }
        }

        private void AbrirModalFicha(Guid pacienteId)
        {
            Guid profesionalId = ObtenerProfesionalActivo();
            Guid usuarioActualId = Guid.Parse(Convert.ToString(Session["UsuarioId"]));
            string rolCodigo = Convert.ToString(Session["RolCodigo"]);

            try
            {
                ResumenClinicoRapido resumen = _consultorioNegocio.ObtenerResumenClinicoRapido(
                    usuarioActualId, rolCodigo, pacienteId, profesionalId);

                ModalTitulo.Text = "Resumen Clínico Rápido";
                ModalPacienteNombre.Text = HttpUtility.HtmlEncode(resumen.NombreCompleto);
                ModalPacienteDni.Text = HttpUtility.HtmlEncode(resumen.NumeroDocumento ?? "—");
                ModalHorarioTurno.Text = "Consulta Informativa";

                ModalPacienteEdad.Text = resumen.Edad.ToString(CultureInfo.InvariantCulture);
                ModalPacienteTelefono.Text = HttpUtility.HtmlEncode(resumen.Telefono ?? "—");
                ModalPacienteObraSocial.Text = HttpUtility.HtmlEncode(resumen.ObraSocial ?? "Particular");
                ModalPacienteAfiliado.Text = HttpUtility.HtmlEncode(resumen.NumeroAfiliado ?? "—");
                ModalPacienteEmergencia.Text = HttpUtility.HtmlEncode(
                    !string.IsNullOrWhiteSpace(resumen.ContactoEmergenciaNombre)
                        ? resumen.ContactoEmergenciaNombre + " (" + (resumen.ContactoEmergenciaTelefono ?? "—") + ")"
                        : "—");

                ModalAntecedentesRepeater.DataSource = resumen.AntecedentesRecientes;
                ModalAntecedentesRepeater.DataBind();
                PanelSinAntecedentes.Visible = resumen.AntecedentesRecientes == null || resumen.AntecedentesRecientes.Count == 0;

                ModalLinkFichaCompleta.NavigateUrl = ResolveUrl("~/FichaPaciente.aspx?PacienteId=" + pacienteId);

                // En modo consulta informativa, no hay turno asociado activo para mutar
                ModalTurnoId.Value = string.Empty;
                ModalPacienteId.Value = pacienteId.ToString();
                ModalMotivoConsulta.Text = string.Empty;
                ModalDiagnostico.Text = string.Empty;
                ModalObservaciones.Text = string.Empty;

                ModalMensajeError.Visible = false;
                ModalAtencionPanel.Visible = true;
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(ex.Message);
            }
            catch (Exception)
            {
                MostrarError("No se pudo obtener el resumen clínico del paciente.");
            }
        }

        protected void BtnGuardarAtencion_Click(object sender, EventArgs e)
        {
            int turnoId;
            if (!int.TryParse(ModalTurnoId.Value, out turnoId) || turnoId <= 0)
            {
                ModalMensajeError.Text = "Esta vista es solo de consulta informativa. No hay un turno activo seleccionado para guardar.";
                ModalMensajeError.Visible = true;
                return;
            }

            string diagnostico = (ModalDiagnostico.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(diagnostico))
            {
                ModalMensajeError.Text = "El diagnóstico es obligatorio para registrar la atención médica.";
                ModalMensajeError.Visible = true;
                return;
            }

            Guid usuarioActualId = Guid.Parse(Convert.ToString(Session["UsuarioId"]));
            string rolCodigo = Convert.ToString(Session["RolCodigo"]);

            AtencionClinicaEntrada entrada = new AtencionClinicaEntrada
            {
                TurnoId = turnoId,
                MotivoConsulta = (ModalMotivoConsulta.Text ?? string.Empty).Trim(),
                Diagnostico = diagnostico,
                Observaciones = (ModalObservaciones.Text ?? string.Empty).Trim()
            };

            try
            {
                _consultorioNegocio.GuardarAtencionClinica(usuarioActualId, rolCodigo, entrada);
                ModalAtencionPanel.Visible = false;
                MostrarExito("Atención médica y evolución clínica registradas correctamente.");
                CargarConsultorio();
            }
            catch (InvalidOperationException ex)
            {
                ModalMensajeError.Text = HttpUtility.HtmlEncode(ex.Message);
                ModalMensajeError.Visible = true;
            }
            catch (Exception)
            {
                ModalMensajeError.Text = "Ocurrió un error inesperado al guardar la evolución médica.";
                ModalMensajeError.Visible = true;
            }
        }

        protected void BtnCerrarModal_Click(object sender, EventArgs e)
        {
            ModalAtencionPanel.Visible = false;
        }

        private Guid ObtenerProfesionalActivo()
        {
            if (TieneRol("ADMINISTRATIVO"))
            {
                Guid id;
                if (Guid.TryParse(FiltroProfesional.SelectedValue, out id))
                {
                    return id;
                }
            }

            if (ViewState["ProfesionalId"] != null)
            {
                return (Guid)ViewState["ProfesionalId"];
            }

            Guid usuarioId;
            if (Guid.TryParse(Convert.ToString(Session["UsuarioId"]), out usuarioId))
            {
                return usuarioId;
            }

            return Guid.Empty;
        }

        private DateTime ObtenerFechaFiltro()
        {
            DateTime fecha;
            if (DateTime.TryParseExact(FiltroFecha.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out fecha))
            {
                return fecha.Date;
            }

            return DateTime.Today;
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

        private void LimpiarMensaje()
        {
            Mensaje.Text = string.Empty;
            Mensaje.Visible = false;
        }
    }
}
