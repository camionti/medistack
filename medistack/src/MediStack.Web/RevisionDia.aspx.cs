using System;
using System.Data.SqlClient;
using System.Web.UI.WebControls;
using MediStack.Negocio;

namespace MediStack.Web
{
    public partial class RevisionDiaPagina : PaginaAdministrativa
    {
        private readonly TurnosNegocio _negocio = new TurnosNegocio();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                CargarPendientes();
            }
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
                    _negocio.CambiarEstado(turnoId, e.CommandName, true, null);
                string texto = e.CommandName == "Atendido" ? "El turno fue marcado como atendido."
                    : e.CommandName == "Ausente" ? "El turno fue marcado como ausente."
                    : e.CommandName == "Confirmado" ? "El turno fue confirmado."
                    : "El turno fue cancelado.";
                string textoSena = TurnosNegocio.DescribirSena(cancelacion);
                if (textoSena.Length > 0)
                {
                    texto += " " + textoSena;
                }
                MostrarExito(Mensaje, texto);
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(Mensaje, ex.Message);
            }
            catch (SqlException ex)
            {
                MostrarError(Mensaje, "No se pudo actualizar el turno (error SQL " + ex.Number + ").");
            }

            CargarPendientes();
        }

        private void CargarPendientes()
        {
            CargarConManejoDeErrores(Mensaje, "No se pudo cargar la revisión del día", delegate
            {
                PendientesGrid.DataSource = _negocio.ObtenerTurnosSinResolver();
                PendientesGrid.DataBind();
            });
        }
    }
}
