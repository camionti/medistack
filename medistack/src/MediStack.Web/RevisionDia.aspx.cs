using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using MediStack.Dominio;
using MediStack.Negocio;

namespace MediStack.Web
{
    public partial class RevisionDiaPagina : PaginaAdministrativa
    {
        private readonly TurnosNegocio _turnosNegocio = new TurnosNegocio();
        private readonly CobrosNegocio _cobrosNegocio = new CobrosNegocio();

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
            int argumentoId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out argumentoId))
            {
                return;
            }

            try
            {
                switch (e.CommandName)
                {
                    case "CheckIn":
                        _turnosNegocio.RegistrarLlegada(argumentoId);
                        MostrarExito(Mensaje, "Se registró la llegada del paciente a la sala de espera.");
                        break;

                    case "DeshacerCheckIn":
                        _turnosNegocio.DeshacerLlegada(argumentoId);
                        MostrarExito(Mensaje, "Se revirtió la llegada a sala de espera (el turno volvió a estado Confirmado).");
                        break;

                    case "Atendido":
                        _turnosNegocio.CambiarEstado(argumentoId, "Atendido", true, null);
                        MostrarExito(Mensaje, "El turno fue marcado como atendido.");
                        break;

                    case "Ausente":
                        _turnosNegocio.CambiarEstado(argumentoId, "Ausente", true, null);
                        MostrarExito(Mensaje, "El turno fue marcado como ausente.");
                        break;

                    case "Confirmado":
                        _turnosNegocio.CambiarEstado(argumentoId, "Confirmado", true, null);
                        MostrarExito(Mensaje, "El turno fue confirmado.");
                        break;

                    case "Cancelado":
                        MediStack.Dominio.ResultadoCancelacion cancelacion =
                            _turnosNegocio.CambiarEstado(argumentoId, "Cancelado", true, null);
                        string texto = "El turno fue cancelado.";
                        string textoSena = TurnosNegocio.DescribirSena(cancelacion);
                        if (!string.IsNullOrEmpty(textoSena))
                        {
                            texto += " " + textoSena;
                        }
                        MostrarExito(Mensaje, texto);
                        break;

                    case "AbrirCobroSena":
                        AbrirModalCobro(argumentoId, "Sena");
                        return;

                    case "AbrirCobroConsulta":
                        AbrirModalCobro(argumentoId, "Consulta");
                        return;

                    case "DescargarComprobante":
                        DescargarComprobanteDirecto(argumentoId);
                        return;
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
                switch (e.CommandName)
                {
                    case "Atendido":
                        _turnosNegocio.CambiarEstado(turnoId, "Atendido", true, null);
                        MostrarExito(Mensaje, "El turno pendiente fue marcado como atendido.");
                        break;
                    case "Ausente":
                        _turnosNegocio.CambiarEstado(turnoId, "Ausente", true, null);
                        MostrarExito(Mensaje, "El turno pendiente fue marcado como ausente.");
                        break;
                    case "Confirmado":
                        _turnosNegocio.CambiarEstado(turnoId, "Confirmado", true, null);
                        MostrarExito(Mensaje, "El turno pendiente fue confirmado.");
                        break;
                    case "Cancelado":
                        _turnosNegocio.CambiarEstado(turnoId, "Cancelado", true, null);
                        MostrarExito(Mensaje, "El turno pendiente fue cancelado.");
                        break;
                }
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(Mensaje, ex.Message);
            }
            catch (SqlException)
            {
                MostrarError(Mensaje, "No se pudo actualizar el turno pendiente.");
            }

            CargarTableroRecepcion();
            CargarPendientes();
        }

        private void CargarFiltrosCatalogo()
        {
            CargarConManejoDeErrores(Mensaje, "No se pudieron cargar los catálogos de filtro", delegate
            {
                DataTable profs = _turnosNegocio.ObtenerProfesionalesActivos();
                FiltroProfesional.Items.Clear();
                FiltroProfesional.Items.Add(new ListItem("Todos los profesionales", ""));
                foreach (DataRow fila in profs.Rows)
                {
                    FiltroProfesional.Items.Add(new ListItem(Convert.ToString(fila["Nombre"]), Convert.ToString(fila["ProfesionalId"])));
                }

                DataTable esps = _turnosNegocio.ObtenerEspecialidadesActivas();
                FiltroEspecialidad.Items.Clear();
                FiltroEspecialidad.Items.Add(new ListItem("Todas las especialidades", ""));
                foreach (DataRow fila in esps.Rows)
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

                DataTable turnos = _turnosNegocio.ObtenerTurnosRecepcion(
                    DateTime.Today, filtro, especialidadId, profesionalId, estado);
                RecepcionGrid.DataSource = turnos;
                RecepcionGrid.DataBind();

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

                LitEnEspera.Text = cantEnEspera.ToString();
                LitTotalHoy.Text = cantTotal.ToString();
                LitAtendidosHoy.Text = cantAtendidos.ToString();

                LitResumenTurnos.Text = turnos.Rows.Count == 0
                    ? "Sin turnos que coincidan con la búsqueda."
                    : string.Format("Mostrando {0} turno(s) para hoy.", turnos.Rows.Count);
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
                case "Solicitado": return "Solicitado";
                case "Confirmado": return "Confirmado";
                case "EnEspera": return "En espera";
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

            if (estado == "Pagada")
            {
                return "<span class=\"badge-sena badge-sena-pagada\">Pagada (" + monto.ToString("C", CultureInfo.GetCultureInfo("es-AR")) + ")</span>";
            }
            if (estado == "Pendiente" && monto > 0)
            {
                return "<span class=\"badge-sena badge-sena-pendiente\">Debe " + monto.ToString("C", CultureInfo.GetCultureInfo("es-AR")) + "</span>";
            }
            if (estado == "Retenida")
            {
                return "<span class=\"badge-sena badge-sena-retenida\">Retenida</span>";
            }

            return "<span class=\"badge-sena badge-sena-norequerida\">No requerida</span>";
        }

        protected string RenderBadgePagos(object estadoObj, object estadoSenaObj, object montoSenaObj, object consultaCobradaObj)
        {
            string estado = Convert.ToString(estadoObj);
            string estadoSena = Convert.ToString(estadoSenaObj);
            decimal montoSena = montoSenaObj != null && montoSenaObj != DBNull.Value ? Convert.ToDecimal(montoSenaObj) : 0m;
            bool consultaCobrada = consultaCobradaObj != null && consultaCobradaObj != DBNull.Value && Convert.ToBoolean(consultaCobradaObj);
            CultureInfo ar = CultureInfo.GetCultureInfo("es-AR");

            StringBuilder sb = new StringBuilder();

            if (estadoSena == "Pagada")
            {
                sb.Append("<span class=\"badge-cobro badge-cobro-pagado\">Seña: " + montoSena.ToString("C2", ar) + "</span>");
            }
            else if (estadoSena == "Pendiente" && montoSena > 0)
            {
                sb.Append("<span class=\"badge-cobro badge-cobro-pendiente\">Debe seña: " + montoSena.ToString("C2", ar) + "</span>");
            }

            if (consultaCobrada)
            {
                sb.Append("<span class=\"badge-cobro badge-cobro-pagado\">Consulta Pagada</span>");
            }
            else if (estado == "EnEspera" || estado == "Atendido")
            {
                sb.Append("<span class=\"badge-cobro badge-cobro-pendiente\">Copago pendiente</span>");
            }

            if (sb.Length == 0)
            {
                return "<span style=\"color: #94a3b8; font-size: 0.8rem;\">—</span>";
            }

            return sb.ToString();
        }

        private void AbrirModalCobro(int turnoId, string conceptoPreferido)
        {
            LimpiarMensaje(Mensaje);
            LimpiarMensaje(MensajeModalCobro);

            EstadoCajaDia estadoCaja = _cobrosNegocio.ObtenerEstadoCajaHoy();
            if (estadoCaja == EstadoCajaDia.SinAbrir || estadoCaja == EstadoCajaDia.PendienteDiaAnterior)
            {
                MostrarError(Mensaje, "La caja del día no está abierta. Para registrar cobros en mostrador, abrí la caja desde Caja indicando el fondo inicial.");
                return;
            }
            if (estadoCaja == EstadoCajaDia.Cerrada)
            {
                MostrarError(Mensaje, "La caja de hoy ya está cerrada: no se pueden registrar cobros hasta mañana.");
                return;
            }

            DataTable cobrables = _cobrosNegocio.ObtenerTurnosCobrables(null, null);
            DataRow seleccionado = null;
            foreach (DataRow fila in cobrables.Rows)
            {
                if (Convert.ToInt32(fila["TurnoId"]) == turnoId)
                {
                    seleccionado = fila;
                    break;
                }
            }

            if (seleccionado == null)
            {
                MostrarError(Mensaje, "El turno ya no posee conceptos pendientes de cobro.");
                return;
            }

            HdnCobroTurnoId.Value = turnoId.ToString();
            DateTime fechaTurno = Convert.ToDateTime(seleccionado["FechaTurno"]);
            LitCobroSubtitulo.Text = string.Format("Turno #{0} ({1:dd/MM HH:mm} hs) · Paciente: {2} · Dr/a. {3} ({4})",
                turnoId, fechaTurno, seleccionado["Paciente"], seleccionado["Profesional"], seleccionado["Especialidad"]);

            DdlCobroTipo.Items.Clear();

            bool puedeCobrarSena = Convert.ToBoolean(seleccionado["PuedeCobrarSena"]);
            if (puedeCobrarSena)
            {
                DdlCobroTipo.Items.Add(new ListItem("Seña de Reserva", "Sena"));
            }

            bool puedeCobrarConsulta = Convert.ToBoolean(seleccionado["PuedeCobrarConsulta"]);
            if (puedeCobrarConsulta)
            {
                DdlCobroTipo.Items.Add(new ListItem("Consulta Médica (Copago)", "Consulta"));
            }

            if (DdlCobroTipo.Items.Count == 0)
            {
                MostrarError(Mensaje, "El turno no tiene conceptos pendientes de cobro.");
                return;
            }

            ListItem itemPreferido = DdlCobroTipo.Items.FindByValue(conceptoPreferido);
            if (itemPreferido != null)
            {
                DdlCobroTipo.SelectedValue = conceptoPreferido;
            }
            else
            {
                DdlCobroTipo.SelectedIndex = 0;
            }

            DdlCobroTipo.Enabled = true;
            DdlCobroMedioPago.Enabled = true;
            DdlCobroMedioPago.SelectedValue = "Efectivo";
            TxtCobroMontoRecibido.Enabled = true;
            TxtCuentaDevolucion.Enabled = true;
            TxtCuentaDevolucion.Text = string.Empty;
            BtnConfirmarCobro.Visible = true;
            PnlCobroExito.Visible = false;

            CargarDatosCobroModal(turnoId, DdlCobroTipo.SelectedValue);
            ModalCobro.Visible = true;
        }

        private void CargarDatosCobroModal(int turnoId, string tipoCobro)
        {
            DataTable importes = _cobrosNegocio.ObtenerImportesCobro(turnoId);
            if (importes == null || importes.Rows.Count == 0)
            {
                MostrarError(MensajeModalCobro, "No se pudieron obtener los importes de liquidación para este turno.");
                return;
            }

            DataRow fila = importes.Rows[0];
            CultureInfo ar = CultureInfo.GetCultureInfo("es-AR");

            if (tipoCobro == "Consulta")
            {
                PnlDesgloseConsulta.Visible = true;
                decimal montoBase = Convert.ToDecimal(fila["MontoBase"]);
                decimal montoOS = Convert.ToDecimal(fila["MontoObraSocial"]);
                decimal copago = Convert.ToDecimal(fila["Copago"]);
                decimal senaDescontada = Convert.ToDecimal(fila["SenaDescontada"]);
                decimal saldoConsulta = Convert.ToDecimal(fila["SaldoConsulta"]);

                LitCobroValorBase.Text = montoBase.ToString("C2", ar);
                LitCobroMontoOS.Text = montoOS.ToString("C2", ar);
                LitCobroCopago.Text = copago.ToString("C2", ar);

                if (senaDescontada > 0)
                {
                    PnlCobroSenaDescontada.Visible = true;
                    LitCobroSenaDescontada.Text = senaDescontada.ToString("C2", ar);
                }
                else
                {
                    PnlCobroSenaDescontada.Visible = false;
                }

                LitCobroTotalCobrar.Text = saldoConsulta.ToString("C2", ar);
                HdnMontoCobrado.Value = saldoConsulta.ToString(CultureInfo.InvariantCulture);
                TxtCobroMontoRecibido.Text = saldoConsulta.ToString("F2", CultureInfo.InvariantCulture);
            }
            else
            {
                PnlDesgloseConsulta.Visible = false;
                decimal montoSena = Convert.ToDecimal(fila["MontoSena"]);
                LitCobroTotalCobrar.Text = montoSena.ToString("C2", ar);
                HdnMontoCobrado.Value = montoSena.ToString(CultureInfo.InvariantCulture);
                TxtCobroMontoRecibido.Text = montoSena.ToString("F2", CultureInfo.InvariantCulture);
            }
        }

        protected void DdlCobroTipo_SelectedIndexChanged(object sender, EventArgs e)
        {
            int turnoId;
            if (int.TryParse(HdnCobroTurnoId.Value, out turnoId))
            {
                CargarDatosCobroModal(turnoId, DdlCobroTipo.SelectedValue);
            }
        }

        protected void BtnCerrarModalCobro_Click(object sender, EventArgs e)
        {
            ModalCobro.Visible = false;
            LimpiarMensaje(MensajeModalCobro);
            CargarTableroRecepcion();
        }

        protected void BtnConfirmarCobro_Click(object sender, EventArgs e)
        {
            int turnoId;
            if (!int.TryParse(HdnCobroTurnoId.Value, out turnoId))
            {
                MostrarError(MensajeModalCobro, "Turno inválido.");
                return;
            }

            decimal montoRecibido;
            if (!decimal.TryParse(TxtCobroMontoRecibido.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out montoRecibido))
            {
                MostrarError(MensajeModalCobro, "Ingresa un importe recibido válido.");
                return;
            }

            string tipoCobro = DdlCobroTipo.SelectedValue;
            string medioPago = DdlCobroMedioPago.SelectedValue;
            string cuentaDevolucion = TxtCuentaDevolucion.Text;

            try
            {
                Guid cajeroId = ObtenerUsuarioActual();
                int nuevoCobroId = _cobrosNegocio.RegistrarCobro(
                    turnoId, tipoCobro, medioPago, montoRecibido, cajeroId, cuentaDevolucion);

                Session["UltimoCobroIdEmitido"] = nuevoCobroId;
                PnlCobroExito.Visible = true;
                BtnConfirmarCobro.Visible = false;
                DdlCobroTipo.Enabled = false;
                DdlCobroMedioPago.Enabled = false;
                TxtCobroMontoRecibido.Enabled = false;
                TxtCuentaDevolucion.Enabled = false;

                MostrarExito(Mensaje, "Cobro registrado correctamente. Se habilitó la emisión del comprobante.");
                CargarTableroRecepcion();
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(MensajeModalCobro, ex.Message);
            }
            catch (SqlException ex)
            {
                MostrarError(MensajeModalCobro, "No se pudo registrar el cobro en la base de datos (error SQL " + ex.Number + ").");
            }
        }

        protected void BtnDescargarPdfModal_Click(object sender, EventArgs e)
        {
            if (Session["UltimoCobroIdEmitido"] != null)
            {
                int cobroId = Convert.ToInt32(Session["UltimoCobroIdEmitido"]);
                DescargarComprobanteDirecto(cobroId);
            }
        }

        private void DescargarComprobanteDirecto(int cobroId)
        {
            try
            {
                var comprobante = _cobrosNegocio.ObtenerComprobanteCobro(cobroId, ObtenerUsuarioActual(), true);
                ExportadorUtil.DescargarComprobantePDF(comprobante);
            }
            catch (Exception ex)
            {
                MostrarError(Mensaje, "No se pudo generar el comprobante: " + ex.Message);
            }
        }

        private Guid ObtenerUsuarioActual()
        {
            Guid usuarioId;
            if (!Guid.TryParse(Convert.ToString(Session["UsuarioId"]), out usuarioId))
            {
                throw new InvalidOperationException("No se pudo validar el usuario de la sesión.");
            }

            return usuarioId;
        }
    }
}
