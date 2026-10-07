<%@ Page Title="Obras sociales" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ObrasSociales.aspx.cs" Inherits="MediStack.Web.ObrasSociales" %>
<asp:Content ID="ObrasSocialesContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="page-heading">
        <p class="eyebrow">Gestión clínica</p>
        <h1>Obras sociales</h1>
        <p>Mantén el padrón de obras sociales y sus códigos.</p>
    </section>
    <asp:Label ID="Mensaje" runat="server" Visible="false" role="status" />
    <section class="panel management-panel" aria-labelledby="buscar-obra-title">
        <h2 id="buscar-obra-title" class="section-title">Buscar obras sociales</h2>
        <div class="search-row">
            <asp:Label ID="BusquedaLabel" runat="server" AssociatedControlID="Busqueda" Text="Nombre o CUIT/código" CssClass="visually-hidden" />
            <asp:TextBox ID="Busqueda" runat="server" CssClass="form-control" MaxLength="100" />
            <asp:Button ID="Buscar" runat="server" Text="Buscar" CssClass="button button-secondary" CausesValidation="false" OnClick="Buscar_Click" formnovalidate="formnovalidate" />
            <asp:Button ID="VerTodas" runat="server" Text="Ver todas" CssClass="button button-light" CausesValidation="false" OnClick="VerTodas_Click" formnovalidate="formnovalidate" />
        </div>
        <div class="table-wrap">
            <asp:GridView ID="ObrasSocialesGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" DataKeyNames="ObraSocialId,Nombre,CodigoCUIT,Activa"
                EmptyDataText="No hay obras sociales para mostrar." OnRowCommand="ObrasSocialesGrid_RowCommand">
                <Columns>
                    <asp:BoundField DataField="Nombre" HeaderText="Obra social" />
                    <asp:BoundField DataField="CodigoCUIT" HeaderText="CUIT / código" />
                    <asp:TemplateField HeaderText="Estado">
                        <ItemTemplate><%# Convert.ToBoolean(Eval("Activa")) ? "Activa" : "Inactiva" %></ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Acciones">
                        <ItemTemplate>
                            <div class="row-actions">
                                <asp:LinkButton ID="Editar" runat="server" Text="Editar" CommandName="Editar"
                                    CommandArgument="<%# Container.DataItemIndex %>" CausesValidation="false" />
                                <asp:LinkButton ID="CambiarEstado" runat="server"
                                    Text='<%# Convert.ToBoolean(Eval("Activa")) ? "Desactivar" : "Reactivar" %>'
                                    CommandName="CambiarEstado" CommandArgument="<%# Container.DataItemIndex %>"
                                    CausesValidation="false" OnClientClick="return confirm('¿Cambiar el estado de la obra social?');" />
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </section>
    <section class="panel management-panel" aria-labelledby="formulario-obra-title">
        <div class="section-heading-row">
            <h2 id="formulario-obra-title" class="section-title"><asp:Literal ID="TituloFormulario" runat="server" /></h2>
            <asp:Button ID="Nuevo" runat="server" Text="Nueva obra social" CssClass="button button-light" CausesValidation="false" OnClick="Nuevo_Click" formnovalidate="formnovalidate" />
        </div>
        <div class="form-grid form-grid-short">
            <div class="form-group">
                <asp:Label ID="NombreLabel" runat="server" AssociatedControlID="Nombre" Text="Nombre" />
                <asp:TextBox ID="Nombre" runat="server" CssClass="form-control" MaxLength="100" required="required" />
            </div>
            <div class="form-group">
                <asp:Label ID="CodigoLabel" runat="server" AssociatedControlID="CodigoCUIT" Text="CUIT / código" />
                <asp:TextBox ID="CodigoCUIT" runat="server" CssClass="form-control" MaxLength="20" required="required" />
            </div>
        </div>
        <div class="form-actions">
            <asp:Button ID="Guardar" runat="server" Text="Guardar obra social" CssClass="button button-primary" OnClick="Guardar_Click" />
            <asp:Button ID="Cancelar" runat="server" Text="Cancelar" CssClass="button button-light" CausesValidation="false" OnClick="Nuevo_Click" formnovalidate="formnovalidate" />
        </div>
    </section>
</asp:Content>
