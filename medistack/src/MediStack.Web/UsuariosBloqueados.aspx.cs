using System;
using System.Data.SqlClient;
using System.Web.UI.WebControls;
using MediStack.Negocio;

namespace MediStack.Web
{
    public partial class UsuariosBloqueados : PaginaAdministrativa 
    {
        private readonly UsuarioNegocio _negocio = new UsuarioNegocio();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) 
            {
                CargarConManejoDeErrores(Mensaje, "No se pudieron cargar las cuentas bloqueadas", CargarBloqueados);
            }
        }

        protected void Actualizar_Click(object sender, EventArgs e)
        {
            LimpiarMensaje(Mensaje);
            CargarConManejoDeErrores(Mensaje, "No se pudieron cargar las cuentas bloqueadas", CargarBloqueados);
        }

        protected void BloqueadosGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Desbloquear") { return; } 

            int indice;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out indice)
                || indice < 0 || indice >= BloqueadosGrid.DataKeys.Count)
            {
                return; 
            }

            try
            {
                Guid usuarioId = GuidDataKey(BloqueadosGrid, indice, "UsuarioId");
                string nombreUsuario = TextoDataKey(BloqueadosGrid, indice, "NombreUsuario");

                _negocio.DesbloquearUsuario(usuarioId);
                CargarBloqueados(); 
                MostrarExito(Mensaje, "La cuenta " + nombreUsuario + " fue desbloqueada.");
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(Mensaje, ex.Message);
                CargarConManejoDeErrores(Mensaje, "No se pudo actualizar la lista", CargarBloqueados);
            }
            catch (SqlException ex)
            {
                MostrarError(Mensaje, "No se pudo desbloquear la cuenta (error SQL " + ex.Number + ").");
            }
        }

        private void CargarBloqueados()
        {
            BloqueadosGrid.DataSource = _negocio.ObtenerUsuariosBloqueados();
            BloqueadosGrid.DataBind(); 
        }
    }
}