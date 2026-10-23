using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;
using MediStack.Negocio;

namespace MediStack.Web
{
    public partial class RevisionDiaPagina : PaginaAdministrativa
    {
        private readonly TurnosNegocio _turnosNegocio = new TurnosNegocio();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                CargarFiltrosCatalogo();
                CargarTableroRecepcion();
                CargarPendientes();
            }
        }

        protected void BtnBuscar_Click(object sender, EventArgs e)
        {
            LimpiarMensaje(Mensaje);
            CargarTableroRecepcion();
        }

        protected void BtnLimpiar_Click(object sender, EventArgs e)
        {
            LimpiarMensaje(Mensaje);
            FiltroBusqueda.Text = string.Empty;
            FiltroEstado.SelectedValue = "Todos";
            if (FiltroProfesional.Items.Count > 0) FiltroProfesional.SelectedIndex = 0;
            if (FiltroEspecialidad.Items.Count > 0) FiltroEspecialidad.SelectedIndex = 0;
            CargarTableroRecepcion();
        }

        protected void Filtro_SelectedIndexChanged(object sender, EventArgs e)
        {
            LimpiarMensaje(Mensaje);
            CargarTableroRecepcion();
        }

        protected void RecepcionGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int turnoId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out turnoId))
            {
                return;
            }

            try
            {
                switch (e.CommandName)
                {
                    case "CheckIn":
                        _turnosNegocio.RegistrarLlegada(turnoId);
                        MostrarExito(Mensaje, "Se registró la llegada del paciente a la sala de espera.");
                        break;

                    case "DeshacerCheckIn":
                        _turnosNegocio.DeshacerLlegada(turnoId);
                        MostrarExito(Mensaje, "Se revirtió la llegada a sala de espera (el turno volvió a estado Confirmado).");
                        break;

                    case "Atendido":
                        _turnosNegocio.CambiarEstado(turnoId, "Atendido", true, null);
                        MostrarExito(Mensaje, "El turno fue marcado como atendido.");
                        break;

                    case "Ausente":
                        _turnosNegocio.CambiarEstado(turnoId, "Ausente", true, null);
                        MostrarExito(Mensaje, "El turno fue marcado como ausente.");
                        break;

                    case "Confirmado":
                        _turnosNegocio.CambiarEstado(turnoId, "Confirmado", true, null);
                        MostrarExito(Mensaje, "El turno fue confirmado.");
                        break;

                    case "Cancelado":
                        MediStack.Dominio.ResultadoCancelacion cancelacion =
                            _turnosNegocio.CambiarEstado(turnoId, "Cancelado", true, null);
                        string texto = "El turno fue cancelado.";
                        string textoSena = TurnosNegocio.DescribirSena(cancelacion);
                        if (!string.IsNullOrEmpty(textoSena))
                        {
                            texto += " " + textoSena;
                        }
                        MostrarExito(Mensaje, texto);
                        break;
                }
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(Mensaje, ex.Message);
            }
            catch (SqlException)
            {
                MostrarError(Mensaje, "No se pudo actualizar el estado del turno debido a un inconveniente en el servidor. Por favor, reintente en unos momentos.");
            }

            CargarTableroRecepcion();
            CargarPendientes();
        }

        protected void PendientesGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            string[] acciones = { "Atendido", "Ausente", "Confirmado", "Cancelado" };
            int turnoId;
            if (Array.IndexOf(acciones, e.CommandName) < 0
                || !int.TryParse(Convert.ToString(e.CommandArgument), out turnoId))
            {
                return;
            }

            try
            {
                MediStack.Dominio.ResultadoCancelacion cancelacion =
                    _turnosNegocio.CambiarEstado(turnoId, e.CommandName, true, null);
                string texto = e.CommandName == "Atendido" ? "El turno fue marcado como atendido."
                    : e.CommandName == "Ausente" ? "El turno fue marcado como ausente."
                    : e.CommandName == "Confirmado" ? "El turno fue confirmado."
                    : "El turno fue cancelado.";
                string textoSena = TurnosNegocio.DescribirSena(cancelacion);
                if (!string.IsNullOrEmpty(textoSena))
                {
                    texto += " " + textoSena;
                }
                MostrarExito(Mensaje, texto);
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(Mensaje, ex.Message);
            }
            catch (SqlException)
            {
                MostrarError(Mensaje, "No se pudo actualizar el turno debido a un inconveniente con el servidor de base de datos.");
            }

            CargarTableroRecepcion();
            CargarPendientes();
        }

        private void CargarFiltrosCatalogo()
        {
            CargarConManejoDeErrores(Mensaje, "No se pudieron cargar los catálogos de filtro", delegate
            {
                FiltroProfesional.Items.Clear();
                FiltroProfesional.Items.Add(new ListItem("Todos los profesionales", string.Empty));
                DataTable profesionales = _turnosNegocio.ObtenerProfesionalesActivos();
                foreach (DataRow fila in profesionales.Rows)
                {
                    FiltroProfesional.Items.Add(new ListItem(Convert.ToString(fila["Nombre"]), Convert.ToString(fila["ProfesionalId"])));
                }

                FiltroEspecialidad.Items.Clear();
                FiltroEspecialidad.Items.Add(new ListItem("Todas las especialidades", string.Empty));
                DataTable especialidades = _turnosNegocio.ObtenerEspecialidadesActivas();
                foreach (DataRow fila in especialidades.Rows)
                {
                    FiltroEspecialidad.Items.Add(new ListItem(Convert.ToString(fila["Nombre"]), Convert.ToString(fila["EspecialidadId"])));
                }
            });
        }

        private void CargarTableroRecepcion()
        {
            CargarConManejoDeErrores(Mensaje, "No se pudo cargar la recepción del día", delegate
            {
                string filtro = FiltroBusqueda.Text.Trim();
                string estado = FiltroEstado.SelectedValue;

                Guid? profesionalId = null;
                Guid profParsed;
                if (Guid.TryParse(FiltroProfesional.SelectedValue, out profParsed))
                {
                    profesionalId = profParsed;
                }

                int? especialidadId = null;
                int espParsed;
                if (int.TryParse(FiltroEspecialidad.SelectedValue, out espParsed))
                {
                    especialidadId = espParsed;
                }

                // Turnos filtrados para el grid
                DataTable turnos = _turnosNegocio.ObtenerTurnosRecepcion(
                    DateTime.Today, filtro, especialidadId, profesionalId, estado);
                RecepcionGrid.DataSource = turnos;
                RecepcionGrid.DataBind();

                // Métricas generales del día (sin filtro de estado ni búsqueda para que el resumen sea global)
                DataTable turnosDiaCompleto = _turnosNegocio.ObtenerTurnosRecepcion(
                    DateTime.Today, null, null, null, null);

                int cantEnEspera = 0;
                int cantAtendidos = 0;
                int cantTotal = turnosDiaCompleto.Rows.Count;

                foreach (DataRow row in turnosDiaCompleto.Rows)
                {
                    string est = Convert.ToString(row["Estado"]);
                    if (est == "EnEspera") cantEnEspera++;
                    else if (est == "Atendido") cantAtendidos++;
                }

                LitEnEspera.Text = cantEnEspera.ToString(CultureInfo.InvariantCulture);
                LitTotalHoy.Text = cantTotal.ToString(CultureInfo.InvariantCulture);
                LitAtendidosHoy.Text = cantAtendidos.ToString(CultureInfo.InvariantCulture);

                LitResumenTurnos.Text = turnos.Rows.Count + (turnos.Rows.Count == 1 ? " turno listado" : " turnos listados")
                    + " para hoy " + DateTime.Today.ToString("dd/MM/yyyy") + ".";
            });
        }

        private void CargarPendientes()
        {
            CargarConManejoDeErrores(Mensaje, "No se pudieron cargar los turnos pendientes", delegate
            {
                DataTable pendientes = _turnosNegocio.ObtenerTurnosSinResolver();
                PendientesGrid.DataSource = pendientes;
                PendientesGrid.DataBind();
                PendientesPanel.Visible = pendientes.Rows.Count > 0;
            });
        }

        protected string TextoEstado(object estado)
        {
            string est = Convert.ToString(estado);
            switch (est)
            {
                case "EnEspera": return "En sala de espera";
                case "Confirmado": return "Confirmado";
                case "Solicitado": return "Solicitado";
                case "Atendido": return "Atendido";
                case "Ausente": return "Ausente";
                case "Cancelado": return "Cancelado";
                default: return est;
            }
        }

        protected string MotivoCorto(object motivo)
        {
            string m = Convert.ToString(motivo);
            if (string.IsNullOrWhiteSpace(m)) return string.Empty;
            return m.Length > 40 ? m.Substring(0, 37) + "..." : m;
        }

        protected string BadgeSena(object estadoSena, object montoSena, object turnoId)
        {
            string estado = Convert.ToString(estadoSena);
            decimal monto = montoSena != null && montoSena != DBNull.Value ? Convert.ToDecimal(montoSena) : 0m;
            int id = Convert.ToInt32(turnoId);

            if (estado == "Pagada")
            {
                return "<span class=\"badge-sena badge-sena-pagada\">Pagada (" + monto.ToString("C", CultureInfo.GetCultureInfo("es-AR")) + ")</span>";
            }
            if (estado == "Pendiente" && monto > 0)
            {
                string urlCobro = ResolveUrl("~/Cobros.aspx");
                return "<span class=\"badge-sena badge-sena-pendiente\">Debe " + monto.ToString("C", CultureInfo.GetCultureInfo("es-AR")) + "</span> "
                    + "<a href=\"" + urlCobro + "\" class=\"button button-secondary button-sm\" title=\"Ir a cobrar seña\">Cobrar</a>";
            }
            if (estado == "Retenida")
            {
                return "<span class=\"badge-sena badge-sena-retenida\">Retenida</span>";
            }

            return "<span class=\"badge-sena badge-sena-norequerida\">No requerida</span>";
        }
    }
}
