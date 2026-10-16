<%@ Page Title="Reportes" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Reportes.aspx.cs" Inherits="MediStack.Web.ReportesPagina" %>
<asp:Content ID="ReportesContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="page-heading" aria-labelledby="reportes-title">
        <p class="eyebrow">Administración</p>
        <h1 id="reportes-title">Reportes y estadísticas</h1>
        <p>Indicadores de turnos, ingresos, obras sociales y honorarios para el período elegido.</p>
    </section>

    <asp:Label ID="Mensaje" runat="server" Visible="false" role="status" />

    <section class="panel management-panel" aria-labelledby="filtro-title">
        <h2 id="filtro-title" class="section-title">Período</h2>
        <div class="search-row agenda-filter">
            <div class="form-group">
                <asp:Label ID="DesdeLabel" runat="server" AssociatedControlID="Desde" Text="Desde" />
                <asp:TextBox ID="Desde" runat="server" CssClass="form-control" TextMode="Date" />
            </div>
            <div class="form-group">
                <asp:Label ID="HastaLabel" runat="server" AssociatedControlID="Hasta" Text="Hasta" />
                <asp:TextBox ID="Hasta" runat="server" CssClass="form-control" TextMode="Date" />
            </div>
            <asp:Button ID="Filtrar" runat="server" Text="Generar reportes" CssClass="button button-secondary"
                CausesValidation="false" OnClick="Filtrar_Click" formnovalidate="formnovalidate" />
        </div>
    </section>

    <section class="panel management-panel" aria-labelledby="TurnosEstado-title">
        <h2 id="TurnosEstado-title" class="section-title">Turnos por estado</h2>
        <p class="form-note">Cantidad de turnos del período según su estado.</p>
        <div class="table-wrap">
            <asp:GridView ID="TurnosEstadoGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" EmptyDataText="Sin datos para el período seleccionado.">
                <Columns>
                    <asp:BoundField DataField="Estado" HeaderText="Estado" />
                    <asp:BoundField DataField="Cantidad" HeaderText="Cantidad" />
                </Columns>
            </asp:GridView>
        </div>
    </section>

    <section class="panel management-panel" aria-labelledby="TurnosProfesional-title">
        <h2 id="TurnosProfesional-title" class="section-title">Turnos y ausentismo por profesional</h2>
        <p class="form-note">El ausentismo se calcula sobre turnos atendidos y ausentes.</p>
        <div class="table-wrap">
            <asp:GridView ID="TurnosProfesionalGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" EmptyDataText="Sin datos para el período seleccionado.">
                <Columns>
                    <asp:BoundField DataField="Profesional" HeaderText="Profesional" />
                    <asp:BoundField DataField="Total" HeaderText="Total" />
                    <asp:BoundField DataField="Atendidos" HeaderText="Atendidos" />
                    <asp:BoundField DataField="Ausentes" HeaderText="Ausentes" />
                    <asp:BoundField DataField="Cancelados" HeaderText="Cancelados" />
                    <asp:BoundField DataField="Pendientes" HeaderText="Pendientes" />
                    <asp:BoundField DataField="PorcentajeAusentismo" HeaderText="% ausentismo" DataFormatString="{0:0.0}%" />
                </Columns>
            </asp:GridView>
        </div>
    </section>

    <section class="panel management-panel" aria-labelledby="TurnosEspecialidad-title">
        <h2 id="TurnosEspecialidad-title" class="section-title">Turnos por especialidad</h2>
        <div class="table-wrap">
            <asp:GridView ID="TurnosEspecialidadGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" EmptyDataText="Sin datos para el período seleccionado.">
                <Columns>
                    <asp:BoundField DataField="Especialidad" HeaderText="Especialidad" />
                    <asp:BoundField DataField="Total" HeaderText="Total" />
                    <asp:BoundField DataField="Atendidos" HeaderText="Atendidos" />
                    <asp:BoundField DataField="Ausentes" HeaderText="Ausentes" />
                    <asp:BoundField DataField="Cancelados" HeaderText="Cancelados" />
                </Columns>
            </asp:GridView>
        </div>
    </section>

    <section class="panel management-panel" aria-labelledby="IngresosMedio-title">
        <h2 id="IngresosMedio-title" class="section-title">Ingresos por medio de pago</h2>
        <p class="form-note">Cobros registrados en el período (señas y consultas).</p>
        <div class="table-wrap">
            <asp:GridView ID="IngresosMedioGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" EmptyDataText="Sin datos para el período seleccionado.">
                <Columns>
                    <asp:BoundField DataField="MedioPago" HeaderText="Medio de pago" />
                    <asp:BoundField DataField="Cantidad" HeaderText="Cobros" />
                    <asp:BoundField DataField="Total" HeaderText="Total" DataFormatString="{0:C2}" />
                </Columns>
            </asp:GridView>
        </div>
    </section>

    <section class="panel management-panel" aria-labelledby="IngresosEspecialidad-title">
        <h2 id="IngresosEspecialidad-title" class="section-title">Ingresos por especialidad</h2>
        <div class="table-wrap">
            <asp:GridView ID="IngresosEspecialidadGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" EmptyDataText="Sin datos para el período seleccionado.">
                <Columns>
                    <asp:BoundField DataField="Especialidad" HeaderText="Especialidad" />
                    <asp:BoundField DataField="Cobros" HeaderText="Cobros" />
                    <asp:BoundField DataField="Total" HeaderText="Cobrado" DataFormatString="{0:C2}" />
                    <asp:BoundField DataField="CubiertoObraSocial" HeaderText="Cubierto por obra social" DataFormatString="{0:C2}" />
                </Columns>
            </asp:GridView>
        </div>
    </section>

    <section class="panel management-panel" aria-labelledby="ObrasSociales-title">
        <h2 id="ObrasSociales-title" class="section-title">Consultas por obra social</h2>
        <p class="form-note">Consultas cobradas, con lo que cubrió la obra social y el copago del paciente.</p>
        <div class="table-wrap">
            <asp:GridView ID="ObrasSocialesGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" EmptyDataText="Sin datos para el período seleccionado.">
                <Columns>
                    <asp:BoundField DataField="ObraSocial" HeaderText="Obra social" />
                    <asp:BoundField DataField="Consultas" HeaderText="Consultas" />
                    <asp:BoundField DataField="MontoBase" HeaderText="Valor de consultas" DataFormatString="{0:C2}" />
                    <asp:BoundField DataField="CubiertoObraSocial" HeaderText="Cubierto" DataFormatString="{0:C2}" />
                    <asp:BoundField DataField="CopagoPacientes" HeaderText="Copago" DataFormatString="{0:C2}" />
                </Columns>
            </asp:GridView>
        </div>
    </section>

    <section class="panel management-panel" aria-labelledby="Honorarios-title">
        <h2 id="Honorarios-title" class="section-title">Honorarios liquidados</h2>
        <p class="form-note">Liquidaciones registradas cuyo período se cruza con el filtro.</p>
        <div class="table-wrap">
            <asp:GridView ID="HonorariosGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" EmptyDataText="Sin datos para el período seleccionado.">
                <Columns>
                    <asp:BoundField DataField="Profesional" HeaderText="Profesional" />
                    <asp:BoundField DataField="Liquidaciones" HeaderText="Liquidaciones" />
                    <asp:BoundField DataField="Total" HeaderText="Total" DataFormatString="{0:C2}" />
                </Columns>
            </asp:GridView>
        </div>
    </section>

    <section class="panel management-panel" aria-labelledby="Egresos-title">
        <h2 id="Egresos-title" class="section-title">Egresos de caja por categoría</h2>
        <div class="table-wrap">
            <asp:GridView ID="EgresosGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" EmptyDataText="Sin datos para el período seleccionado.">
                <Columns>
                    <asp:BoundField DataField="Categoria" HeaderText="Categoría" />
                    <asp:BoundField DataField="Cantidad" HeaderText="Egresos" />
                    <asp:BoundField DataField="Total" HeaderText="Total" DataFormatString="{0:C2}" />
                </Columns>
            </asp:GridView>
        </div>
    </section>

    <section class="panel management-panel" aria-labelledby="auditoria-title">
        <h2 id="auditoria-title" class="section-title">Auditoría del asistente de IA</h2>
        <p class="form-note">Últimas 200 consultas del período: quién consultó, por qué canal y qué función ejecutó el asistente.</p>
        <div class="table-wrap">
            <asp:GridView ID="AuditoriaIAGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" EmptyDataText="Sin consultas al asistente en el período seleccionado.">
                <Columns>
                    <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                    <asp:BoundField DataField="Usuario" HeaderText="Usuario" />
                    <asp:BoundField DataField="Rol" HeaderText="Rol" />
                    <asp:BoundField DataField="Canal" HeaderText="Canal" />
                    <asp:BoundField DataField="Consulta" HeaderText="Consulta" />
                    <asp:BoundField DataField="FuncionEjecutada" HeaderText="Función" />
                    <asp:BoundField DataField="GastoTokens" HeaderText="Tokens" />
                </Columns>
            </asp:GridView>
        </div>
    </section>
</asp:Content>
