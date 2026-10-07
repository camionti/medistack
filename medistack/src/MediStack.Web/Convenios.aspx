<%@ Page Title="Convenios" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Convenios.aspx.cs" Inherits="MediStack.Web.Convenios" %>
<asp:Content ID="ConveniosContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="page-heading">
        <p class="eyebrow">Gestión clínica</p>
        <h1>Convenios</h1>
        <p>Relaciona profesionales y especialidades con las coberturas contratadas.</p>
    </section>
    <asp:Label ID="Mensaje" runat="server" Visible="false" role="status" />
    <section class="panel management-panel" aria-labelledby="buscar-convenio-title">
        <h2 id="buscar-convenio-title" class="section-title">Buscar convenios</h2>
        <div class="search-row">
            <asp:Label ID="BusquedaLabel" runat="server" AssociatedControlID="Busqueda" Text="Profesional, especialidad u obra social" CssClass="visually-hidden" />
            <asp:TextBox ID="Busqueda" runat="server" CssClass="form-control" MaxLength="100" />
            <asp:Button ID="Buscar" runat="server" Text="Buscar" CssClass="button button-secondary" CausesValidation="false" OnClick="Buscar_Click" formnovalidate="formnovalidate" />
            <asp:Button ID="VerTodos" runat="server" Text="Ver todos" CssClass="button button-light" CausesValidation="false" OnClick="VerTodos_Click" formnovalidate="formnovalidate" />
        </div>
        <div class="table-wrap">
            <asp:GridView ID="ConveniosGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" DataKeyNames="ConvenioId,ProfesionalId,EspecialidadId,ObraSocialId,FechaDesde,FechaHasta,Activo,ValorConsulta,EsquemaTipo,EsquemaValor,EsEspecialidadPrincipal"
                EmptyDataText="No hay convenios para mostrar." OnRowCommand="ConveniosGrid_RowCommand">
                <Columns>
                    <asp:BoundField DataField="Profesional" HeaderText="Profesional" />
                    <asp:BoundField DataField="Especialidad" HeaderText="Especialidad" />
                    <asp:BoundField DataField="ObraSocial" HeaderText="Obra social" />
                    <asp:BoundField DataField="FechaDesde" HeaderText="Desde" DataFormatString="{0:dd/MM/yyyy}" />
                    <asp:BoundField DataField="FechaHasta" HeaderText="Hasta" DataFormatString="{0:dd/MM/yyyy}" />
                    <asp:TemplateField HeaderText="Estado">
                        <ItemTemplate><%# Convert.ToBoolean(Eval("Activo")) ? "Activo" : "Inactivo" %></ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Acciones">
                        <ItemTemplate>
                            <div class="row-actions">
                                <asp:LinkButton ID="Editar" runat="server" Text="Editar" CommandName="Editar"
                                    CommandArgument="<%# Container.DataItemIndex %>" CausesValidation="false" />
                                <asp:LinkButton ID="CambiarEstado" runat="server"
                                    Text='<%# Convert.ToBoolean(Eval("Activo")) ? "Desactivar" : "Reactivar" %>'
                                    CommandName="CambiarEstado" CommandArgument="<%# Container.DataItemIndex %>"
                                    CausesValidation="false" OnClientClick="return confirm('¿Cambiar el estado del convenio?');" />
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </section>
    <section class="panel management-panel" aria-labelledby="formulario-convenio-title">
        <div class="section-heading-row">
            <h2 id="formulario-convenio-title" class="section-title"><asp:Literal ID="TituloFormulario" runat="server" /></h2>
            <asp:Button ID="Nuevo" runat="server" Text="Nuevo convenio" CssClass="button button-light" CausesValidation="false" OnClick="Nuevo_Click" formnovalidate="formnovalidate" />
        </div>
        <div class="form-grid">
            <div class="form-group">
                <asp:Label ID="ProfesionalLabel" runat="server" AssociatedControlID="Profesional" Text="Profesional" />
                <asp:DropDownList ID="Profesional" runat="server" CssClass="form-control" />
            </div>
            <div class="form-group">
                <asp:Label ID="CoberturaLabel" runat="server" AssociatedControlID="Cobertura" Text="Obra social y especialidad" />
                <asp:DropDownList ID="Cobertura" runat="server" CssClass="form-control" />
            </div>
            <div class="form-group">
                <asp:Label ID="FechaDesdeLabel" runat="server" AssociatedControlID="FechaDesde" Text="Vigente desde" />
                <asp:TextBox ID="FechaDesde" runat="server" CssClass="form-control" TextMode="Date" required="required" />
            </div>
            <div class="form-group">
                <asp:Label ID="FechaHastaLabel" runat="server" AssociatedControlID="FechaHasta" Text="Vigente hasta (opcional)" />
                <asp:TextBox ID="FechaHasta" runat="server" CssClass="form-control" TextMode="Date" />
            </div>
            <div class="form-group">
                <asp:Label ID="ValorConsultaLabel" runat="server" AssociatedControlID="ValorConsulta" Text="Valor de consulta" />
                <asp:TextBox ID="ValorConsulta" runat="server" CssClass="form-control" TextMode="Number" min="0" step="0.01" required="required" />
            </div>
            <div class="form-group">
                <asp:Label ID="EsquemaTipoLabel" runat="server" AssociatedControlID="EsquemaTipo" Text="Esquema de honorarios" />
                <asp:DropDownList ID="EsquemaTipo" runat="server" CssClass="form-control">
                    <asp:ListItem Text="Porcentaje" Value="Porcentaje" />
                    <asp:ListItem Text="Importe fijo" Value="Fijo" />
                </asp:DropDownList>
            </div>
            <div class="form-group">
                <asp:Label ID="EsquemaValorLabel" runat="server" AssociatedControlID="EsquemaValor" Text="Porcentaje o importe de honorarios" />
                <asp:TextBox ID="EsquemaValor" runat="server" CssClass="form-control" TextMode="Number" min="0" step="0.01" required="required" />
            </div>
            <div class="form-group form-check">
                <asp:CheckBox ID="Principal" runat="server" Text=" Especialidad principal del profesional" />
            </div>
        </div>
        <p class="form-note">El valor de consulta y el esquema se guardan en la relación profesional-especialidad y aplican a esa combinación.</p>
        <div class="form-actions">
            <asp:Button ID="Guardar" runat="server" Text="Guardar convenio" CssClass="button button-primary" OnClick="Guardar_Click" />
            <asp:Button ID="Cancelar" runat="server" Text="Cancelar" CssClass="button button-light" CausesValidation="false" OnClick="Nuevo_Click" formnovalidate="formnovalidate" />
        </div>
    </section>
</asp:Content>
