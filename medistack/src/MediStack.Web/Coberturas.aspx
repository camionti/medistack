<%@ Page Title="Coberturas" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Coberturas.aspx.cs" Inherits="MediStack.Web.Coberturas" %>
<asp:Content ID="CoberturasContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="page-heading">
        <p class="eyebrow">Gestión clínica</p>
        <h1>Coberturas</h1>
        <p>Define el porcentaje cubierto por cada obra social para una especialidad.</p>
    </section>
    <asp:Label ID="Mensaje" runat="server" Visible="false" role="status" />
    <section class="panel management-panel" aria-labelledby="buscar-cobertura-title">
        <h2 id="buscar-cobertura-title" class="section-title">Buscar coberturas</h2>
        <div class="search-row">
            <asp:Label ID="BusquedaLabel" runat="server" AssociatedControlID="Busqueda" Text="Obra social o especialidad" CssClass="visually-hidden" />
            <asp:TextBox ID="Busqueda" runat="server" CssClass="form-control" MaxLength="100" />
            <asp:Button ID="Buscar" runat="server" Text="Buscar" CssClass="button button-secondary" CausesValidation="false" OnClick="Buscar_Click" formnovalidate="formnovalidate" />
            <asp:Button ID="VerTodas" runat="server" Text="Ver todas" CssClass="button button-light" CausesValidation="false" OnClick="VerTodas_Click" formnovalidate="formnovalidate" />
        </div>
        <div class="table-wrap">
            <asp:GridView ID="CoberturasGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" DataKeyNames="ObraSocialId,EspecialidadId,ObraSocial,Especialidad,PorcentajeCobertura"
                EmptyDataText="No hay coberturas para mostrar." OnRowCommand="CoberturasGrid_RowCommand">
                <Columns>
                    <asp:BoundField DataField="ObraSocial" HeaderText="Obra social" />
                    <asp:BoundField DataField="Especialidad" HeaderText="Especialidad" />
                    <asp:BoundField DataField="PorcentajeCobertura" HeaderText="Cobertura" DataFormatString="{0:N2} %" />
                    <asp:TemplateField HeaderText="Acciones">
                        <ItemTemplate>
                            <div class="row-actions">
                                <asp:LinkButton ID="Editar" runat="server" Text="Editar" CommandName="Editar"
                                    CommandArgument="<%# Container.DataItemIndex %>" CausesValidation="false" />
                                <asp:LinkButton ID="Eliminar" runat="server" Text="Eliminar" CommandName="Eliminar"
                                    CommandArgument="<%# Container.DataItemIndex %>" CausesValidation="false"
                                    OnClientClick="return confirm('¿Eliminar esta cobertura? Los convenios que la usan deben darse de baja primero.');" />
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </section>
    <section class="panel management-panel" aria-labelledby="formulario-cobertura-title">
        <div class="section-heading-row">
            <h2 id="formulario-cobertura-title" class="section-title"><asp:Literal ID="TituloFormulario" runat="server" /></h2>
            <asp:Button ID="Nuevo" runat="server" Text="Nueva cobertura" CssClass="button button-light" CausesValidation="false" OnClick="Nuevo_Click" formnovalidate="formnovalidate" />
        </div>
        <div class="form-grid form-grid-short">
            <div class="form-group">
                <asp:Label ID="ObraSocialLabel" runat="server" AssociatedControlID="ObraSocial" Text="Obra social" />
                <asp:DropDownList ID="ObraSocial" runat="server" CssClass="form-control" />
            </div>
            <div class="form-group">
                <asp:Label ID="EspecialidadLabel" runat="server" AssociatedControlID="Especialidad" Text="Especialidad" />
                <asp:DropDownList ID="Especialidad" runat="server" CssClass="form-control" />
            </div>
            <div class="form-group">
                <asp:Label ID="PorcentajeLabel" runat="server" AssociatedControlID="Porcentaje" Text="Porcentaje cubierto (0 a 100)" />
                <asp:TextBox ID="Porcentaje" runat="server" CssClass="form-control" TextMode="Number" min="0" max="100" step="0.01" required="required" />
            </div>
        </div>
        <div class="form-actions">
            <asp:Button ID="Guardar" runat="server" Text="Guardar cobertura" CssClass="button button-primary" OnClick="Guardar_Click" />
            <asp:Button ID="Cancelar" runat="server" Text="Cancelar" CssClass="button button-light" CausesValidation="false" OnClick="Nuevo_Click" formnovalidate="formnovalidate" />
        </div>
    </section>
</asp:Content>
