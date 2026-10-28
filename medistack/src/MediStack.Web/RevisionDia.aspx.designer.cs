namespace MediStack.Web
{
    public partial class RevisionDiaPagina
    {
        protected global::System.Web.UI.WebControls.Label Mensaje;
        protected global::System.Web.UI.WebControls.Literal LitEnEspera;
        protected global::System.Web.UI.WebControls.Literal LitTotalHoy;
        protected global::System.Web.UI.WebControls.Literal LitAtendidosHoy;
        protected global::System.Web.UI.WebControls.Literal LitResumenTurnos;
        protected global::System.Web.UI.WebControls.Label FiltroBusquedaLabel;
        protected global::System.Web.UI.WebControls.TextBox FiltroBusqueda;
        protected global::System.Web.UI.WebControls.Label FiltroEstadoLabel;
        protected global::System.Web.UI.WebControls.DropDownList FiltroEstado;
        protected global::System.Web.UI.WebControls.Label FiltroProfesionalLabel;
        protected global::System.Web.UI.WebControls.DropDownList FiltroProfesional;
        protected global::System.Web.UI.WebControls.Label FiltroEspecialidadLabel;
        protected global::System.Web.UI.WebControls.DropDownList FiltroEspecialidad;
        protected global::System.Web.UI.WebControls.Button BtnBuscar;
        protected global::System.Web.UI.WebControls.Button BtnLimpiar;
        protected global::System.Web.UI.WebControls.GridView RecepcionGrid;
        protected global::System.Web.UI.WebControls.Panel PendientesPanel;
        protected global::System.Web.UI.WebControls.GridView PendientesGrid;

        // Modal de Cobro RÃ¡pido en Mostrador
        protected global::System.Web.UI.WebControls.Panel ModalCobro;
        protected global::System.Web.UI.WebControls.Literal LitCobroSubtitulo;
        protected global::System.Web.UI.WebControls.Button BtnCerrarModalCobro;
        protected global::System.Web.UI.WebControls.Label MensajeModalCobro;
        protected global::System.Web.UI.WebControls.HiddenField HdnCobroTurnoId;
        protected global::System.Web.UI.WebControls.HiddenField HdnMontoCobrado;
        protected global::System.Web.UI.WebControls.Label LblCobroTipo;
        protected global::System.Web.UI.WebControls.DropDownList DdlCobroTipo;
        protected global::System.Web.UI.WebControls.Label LblCobroMedioPago;
        protected global::System.Web.UI.WebControls.DropDownList DdlCobroMedioPago;
        protected global::System.Web.UI.WebControls.Panel PnlDesgloseConsulta;
        protected global::System.Web.UI.WebControls.Literal LitCobroValorBase;
        protected global::System.Web.UI.WebControls.Literal LitCobroMontoOS;
        protected global::System.Web.UI.WebControls.Literal LitCobroCopago;
        protected global::System.Web.UI.WebControls.Panel PnlCobroSenaDescontada;
        protected global::System.Web.UI.WebControls.Literal LitCobroSenaDescontada;
        protected global::System.Web.UI.WebControls.Literal LitCobroTotalCobrar;
        protected global::System.Web.UI.WebControls.Label LblCuentaDevolucion;
        protected global::System.Web.UI.WebControls.TextBox TxtCuentaDevolucion;
        protected global::System.Web.UI.WebControls.Label LblCobroMontoRecibido;
        protected global::System.Web.UI.WebControls.TextBox TxtCobroMontoRecibido;
        protected global::System.Web.UI.WebControls.Panel PnlCobroExito;
        protected global::System.Web.UI.WebControls.Button BtnDescargarPdfModal;
        protected global::System.Web.UI.WebControls.Button BtnConfirmarCobro;
        protected global::System.Web.UI.WebControls.Button BtnCancelarCobro;
    }
}