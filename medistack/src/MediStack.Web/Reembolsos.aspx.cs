using System;
using System.Data.SqlClient;
using System.Globalization;
using System.Web.UI.WebControls;
using MediStack.Negocio;

namespace MediStack.Web
{
    public partial class ReembolsosPagina : PaginaAdministrativa
    {
        private readonly ReembolsosNegocio _negocio = new ReembolsosNegocio();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                Desde.Text = DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                Hasta.Text = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                Ejecutar("No se pudieron cargar los reembolsos", Cargar);
            }
        }

        protected void Filtrar_Click(object sender, EventArgs e)
        {
            Ejecutar("No se pudo consultar el historial", Cargar);
        }

        protected void PendientesGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int indice;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out indice)
                || indice < 0 || indice >= PendientesGrid.Rows.Count)
            {
                return;
            }

            int reembolsoId = Convert.ToInt32(PendientesGrid.DataKeys[indice].Value);
            Ejecutar("No se pudo completar el reembolso", () =>
            {
                switch (e.CommandName)
                {
                    case "Efectivo":
                        _negocio.DevolverEnEfectivo(reembolsoId, ObtenerUsuarioActual());
                        MostrarExito(Mensaje, "Se registró la devolución en efectivo y el egreso en la caja del día.");
                        break;

                    case "Manual":
                        TextBox comprobante = (TextBox)PendientesGrid.Rows[indice].FindControl("Comprobante");
                        _negocio.RegistrarDevolucionManual(reembolsoId, ObtenerUsuarioActual(), comprobante.Text);
                        MostrarExito(Mensaje, "Se registró la devolución con el comprobante " + comprobante.Text.Trim() + ".");
                        break;

                    default:
                        return;
                }

                Cargar();
            });
        }

        protected string TextoMedio(object medio)
        {
            switch (Convert.ToString(medio))
            {
                case "MercadoPago": return "Mercado Pago";
                case "Tarjeta": return "Tarjeta";
                case "Transferencia": return "Transferencia";
                default: return "Efectivo";
            }
        }

        // A donde hay que devolver la seña, segun como se pago.
        protected string DestinoDevolucion(object medio, object cuenta, object pagoMercadoPago)
        {
            string medioPago = Convert.ToString(medio);
            if (medioPago == "Efectivo")
            {
                return "En efectivo, en el mostrador";
            }

            if (medioPago == "MercadoPago")
            {
                string operacion = Convert.ToString(pagoMercadoPago);
                return string.IsNullOrWhiteSpace(operacion)
                    ? "Cuenta de Mercado Pago del paciente"
                    : "Cuenta de Mercado Pago del paciente · operación " + operacion;
            }

            string cuentaDevolucion = Convert.ToString(cuenta);
            return string.IsNullOrWhiteSpace(cuentaDevolucion)
                ? "Sin cuenta registrada: pedile el CBU o alias al paciente"
                : "CBU/alias " + cuentaDevolucion;
        }

        protected string TextoMotivo(object motivo)
        {
            return Convert.ToString(motivo) == "CancelacionClinica" ? "Canceló la clínica" : "Canceló el paciente";
        }

        private void Cargar()
        {
            DateTime desde;
            DateTime hasta;
            if (!DateTime.TryParseExact(Desde.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out desde)
                || !DateTime.TryParseExact(Hasta.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out hasta))
            {
                MostrarError(Mensaje, "Seleccioná un período de fechas válido.");
                return;
            }

            System.Data.DataTable pendientes = _negocio.ObtenerReembolsosPendientes();
            PendientesGrid.DataSource = pendientes;
            PendientesGrid.DataBind();
            HistorialGrid.DataSource = _negocio.ObtenerHistorialReembolsos(desde, hasta);
            HistorialGrid.DataBind();

            // Aviso si hay devoluciones en efectivo y la caja no esta abierta.
            bool hayEfectivo = pendientes.Select("MedioPago = 'Efectivo'").Length > 0;
            AvisoCajaPanel.Visible = hayEfectivo
                && new CobrosNegocio().ObtenerEstadoCajaHoy() != EstadoCajaDia.Abierta;
        }

        private void Ejecutar(string contexto, Action accion)
        {
            try
            {
                accion();
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(Mensaje, ex.Message);
            }
            catch (SqlException ex)
            {
                MostrarError(Mensaje, contexto + " (error SQL " + ex.Number + ").");
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
