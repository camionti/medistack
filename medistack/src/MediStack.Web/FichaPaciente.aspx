<%@ Page Title="Ficha del paciente" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="FichaPaciente.aspx.cs" Inherits="MediStack.Web.FichaPacientePagina" %>
<asp:Content ID="FichaPacienteContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="page-heading">
        <p class="eyebrow">Atención clínica</p>
        <h1>Ficha del paciente</h1>
        <p>Datos personales, turnos, atenciones registradas y cobros vinculados.</p>
    </section>
    <asp:Label ID="Mensaje" runat="server" Visible="false" role="status" />
    <asp:Panel ID="FichaPanel" runat="server" Visible="false">
        <nav class="ms-page-shortcuts" aria-label="Secciones de la ficha">
            <a href="#datos-paciente-title">Datos personales</a>
            <a href="#cobertura-paciente-title">Cobertura</a>
            <a href="#historial-paciente-title">Atenciones</a>
            <a href="#cobros-paciente-title">Cobros</a>
        </nav>
        <section class="panel management-panel" aria-labelledby="datos-paciente-title">
            <div class="section-heading-row">
                <div>
                    <h2 id="datos-paciente-title" class="section-title"><asp:Literal ID="NombrePacienteLiteral" runat="server" /></h2>
                    <p class="form-note">Paciente desde <asp:Literal ID="FechaAlta" runat="server" /></p>
                </div>
                <asp:Literal ID="EstadoPaciente" runat="server" />
            </div>
            <dl class="record-grid">
                <div><dt>Documento</dt><dd><asp:Literal ID="Documento" runat="server" /></dd></div>
                <div><dt>Fecha de nacimiento</dt><dd><asp:Literal ID="Nacimiento" runat="server" /></dd></div>
                <div><dt>Correo electrónico</dt><dd><asp:Literal ID="Email" runat="server" /></dd></div>
                <div><dt>Teléfono</dt><dd><asp:Literal ID="Telefono" runat="server" /></dd></div>
                <div><dt>Obra social</dt><dd><asp:Literal ID="ObraSocial" runat="server" /></dd></div>
                <div><dt>Número de afiliado</dt><dd><asp:Literal ID="Afiliado" runat="server" /></dd></div>
                <div><dt>Contacto de emergencia</dt><dd><asp:Literal ID="EmergenciaNombre" runat="server" /></dd></div>
                <div><dt>Teléfono de emergencia</dt><dd><asp:Literal ID="EmergenciaTelefono" runat="server" /></dd></div>
            </dl>
        </section>

        <section class="panel management-panel" aria-labelledby="cobertura-paciente-title">
            <h2 id="cobertura-paciente-title" class="section-title">Cobertura por especialidad</h2>
            <div class="table-wrap">
                <asp:GridView ID="CoberturasGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                    GridLines="None" EmptyDataText="No hay coberturas registradas para la obra social del paciente.">
                    <Columns>
                        <asp:BoundField DataField="Especialidad" HeaderText="Especialidad" />
                        <asp:BoundField DataField="PorcentajeCobertura" HeaderText="Porcentaje" DataFormatString="{0:N2} %" />
                    </Columns>
                </asp:GridView>
            </div>
        </section>

        <section class="panel management-panel" aria-labelledby="historial-paciente-title">
            <h2 id="historial-paciente-title" class="section-title">Turnos e historial de atenciones</h2>
            <p class="form-note">Cronología por turno; los datos clínicos aparecen cuando existe un registro asociado.</p>
            <div class="table-wrap">
                <asp:GridView ID="HistorialGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                    GridLines="None" EmptyDataText="El paciente todavía no tiene turnos registrados.">
                    <Columns>
                        <asp:BoundField DataField="FechaHora" HeaderText="Fecha del turno" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                        <asp:BoundField DataField="Profesional" HeaderText="Profesional" />
                        <asp:BoundField DataField="Especialidad" HeaderText="Especialidad" />
                        <asp:BoundField DataField="Motivo" HeaderText="Motivo del turno" />
                        <asp:BoundField DataField="Estado" HeaderText="Estado" />
                        <asp:BoundField DataField="FechaRegistro" HeaderText="Atención registrada" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                        <asp:BoundField DataField="MotivoConsulta" HeaderText="Motivo / detalle" />
                        <asp:BoundField DataField="Diagnostico" HeaderText="Diagnóstico" />
                        <asp:BoundField DataField="Observaciones" HeaderText="Observaciones" />
                        <asp:BoundField DataField="EstadoSena" HeaderText="Estado de seña" />
                        <asp:BoundField DataField="MontoSena" HeaderText="Seña" DataFormatString="{0:C}" />
                    </Columns>
                </asp:GridView>
            </div>
        </section>

        <section class="panel management-panel" aria-labelledby="cobros-paciente-title">
            <h2 id="cobros-paciente-title" class="section-title">Historial de cobros</h2>
            <div class="table-wrap">
                <asp:GridView ID="CobrosGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                    GridLines="None" EmptyDataText="No hay cobros asociados a los turnos de este paciente.">
                    <Columns>
                        <asp:BoundField DataField="FechaHoraCobro" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                        <asp:BoundField DataField="TurnoId" HeaderText="Turno" />
                        <asp:BoundField DataField="TipoCobro" HeaderText="Concepto" />
                        <asp:BoundField DataField="MedioPago" HeaderText="Medio de pago" />
                        <asp:BoundField DataField="MontoCobrado" HeaderText="Importe" DataFormatString="{0:C}" />
                    </Columns>
                </asp:GridView>
            </div>
        </section>
    </asp:Panel>
</asp:Content>
