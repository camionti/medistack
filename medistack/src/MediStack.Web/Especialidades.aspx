<%@ Page Title="Especialidades" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Especialidades.aspx.cs" Inherits="MediStack.Web.Especialidades" %>
<asp:Content ID="EspecialidadesContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="page-heading">
        <p class="eyebrow">Gestión clínica</p>
        <h1>Especialidades</h1>
        <p>Administra las especialidades y la duración habitual de consulta.</p>
    </section>
    <asp:Label ID="Mensaje" runat="server" Visible="false" role="status" />
    <section class="panel management-panel" aria-labelledby="buscar-especialidad-title">
        <h2 id="buscar-especialidad-title" class="section-title">Buscar especialidades</h2>
        <div class="search-row">
            <asp:Label ID="BusquedaLabel" runat="server" AssociatedControlID="Busqueda" Text="Código o nombre" CssClass="visually-hidden" />
            <asp:TextBox ID="Busqueda" runat="server" CssClass="form-control" MaxLength="100" />
            <asp:Button ID="Buscar" runat="server" Text="Buscar" CssClass="button button-secondary" CausesValidation="false" OnClick="Buscar_Click" formnovalidate="formnovalidate" />
            <asp:Button ID="VerTodas" runat="server" Text="Ver todas" CssClass="button button-light" CausesValidation="false" OnClick="VerTodas_Click" formnovalidate="formnovalidate" />
        </div>
        <div class="table-wrap">
            <asp:GridView ID="EspecialidadesGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" DataKeyNames="EspecialidadId,Codigo,Nombre,Descripcion,DuracionEstandarMinutos,Activa"
                EmptyDataText="No hay especialidades para mostrar." OnRowCommand="EspecialidadesGrid_RowCommand">
                <Columns>
                    <asp:BoundField DataField="Codigo" HeaderText="Código" />
                    <asp:BoundField DataField="Nombre" HeaderText="Especialidad" />
                    <asp:BoundField DataField="DuracionEstandarMinutos" HeaderText="Duración (min)" />
                    <asp:BoundField DataField="Descripcion" HeaderText="Descripción" />
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
                                    CausesValidation="false" OnClientClick="return confirm('¿Cambiar el estado de la especialidad?');" />
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </section>
    <section class="panel management-panel" aria-labelledby="formulario-especialidad-title">
        <div class="section-heading-row">
            <h2 id="formulario-especialidad-title" class="section-title"><asp:Literal ID="TituloFormulario" runat="server" /></h2>
            <asp:Button ID="Nuevo" runat="server" Text="Nueva especialidad" CssClass="button button-light" CausesValidation="false" OnClick="Nuevo_Click" formnovalidate="formnovalidate" />
        </div>
        <div class="form-grid form-grid-short">
            <div class="form-group">
                <asp:Label ID="CodigoLabel" runat="server" AssociatedControlID="Codigo" Text="Código" />
                <asp:TextBox ID="Codigo" runat="server" CssClass="form-control" MaxLength="30" required="required" />
            </div>
            <div class="form-group">
                <asp:Label ID="NombreLabel" runat="server" AssociatedControlID="Nombre" Text="Nombre" />
                <asp:TextBox ID="Nombre" runat="server" CssClass="form-control" MaxLength="100" required="required" />
            </div>
            <div class="form-group">
                <asp:Label ID="DuracionLabel" runat="server" AssociatedControlID="Duracion" Text="Duración estándar (minutos)" />
                <asp:TextBox ID="Duracion" runat="server" CssClass="form-control" TextMode="Number" min="1" max="1440" required="required" />
            </div>
            <div class="form-group form-group-full">
                <asp:Label ID="DescripcionLabel" runat="server" AssociatedControlID="Descripcion" Text="Descripción" />
                <asp:TextBox ID="Descripcion" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="3" MaxLength="255" />
            </div>
        </div>
        <div class="form-actions">
            <asp:Button ID="Guardar" runat="server" Text="Guardar especialidad" CssClass="button button-primary" OnClick="Guardar_Click" />
            <asp:Button ID="Cancelar" runat="server" Text="Cancelar" CssClass="button button-light" CausesValidation="false" OnClick="Nuevo_Click" formnovalidate="formnovalidate" />
        </div>
    </section>
</asp:Content>
