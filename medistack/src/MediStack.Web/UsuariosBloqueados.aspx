<%@ Page Title="Usuarios bloqueados" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="UsuariosBloqueados.aspx.cs" Inherits="MediStack.Web.UsuariosBloqueados" %>
<asp:Content ID="UsuariosBloqueadosContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="page-heading">
        <p class="eyebrow">Seguridad</p>
        <h1>Usuarios bloqueados</h1>
        <p>Cuentas bloqueadas por intentos fallidos de ingreso. El bloqueo dura 15 minutos y podés levantarlo acá.</p>
    </section>
    <asp:Label ID="Mensaje" runat="server" Visible="false" role="status" />
    <section class="panel management-panel" aria-labelledby="bloqueados-title">
        <div class="section-heading-row">
            <h2 id="bloqueados-title" class="section-title">Cuentas bloqueadas ahora</h2>
            <asp:Button ID="Actualizar" runat="server" Text="Actualizar" CssClass="button button-light"
                CausesValidation="false" OnClick="Actualizar_Click" formnovalidate="formnovalidate" />
        </div>
        <div class="table-wrap">
            <asp:GridView ID="BloqueadosGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" DataKeyNames="UsuarioId,NombreUsuario"
                EmptyDataText="No hay cuentas bloqueadas en este momento."
                OnRowCommand="BloqueadosGrid_RowCommand">
                <Columns>
                    <asp:BoundField DataField="NombreUsuario" HeaderText="Usuario" />
                    <asp:BoundField DataField="NombreCompleto" HeaderText="Nombre" />
                    <asp:BoundField DataField="Rol" HeaderText="Rol" />
                    <asp:BoundField DataField="IntentosFallidos" HeaderText="Intentos fallidos" />
                    <asp:BoundField DataField="BloqueadoHasta" HeaderText="Bloqueado hasta"
                        DataFormatString="{0:dd/MM/yyyy HH:mm}" HtmlEncode="false" />
                    <asp:BoundField DataField="MinutosRestantes" HeaderText="Minutos restantes" />
                    <asp:TemplateField HeaderText="Acciones">
                        <ItemTemplate>
                            <div class="row-actions">
                                <asp:LinkButton ID="Desbloquear" runat="server" Text="Desbloquear"
                                    CommandName="Desbloquear" CommandArgument="<%# Container.DataItemIndex %>"
                                    CausesValidation="false"
                                    OnClientClick="return confirm('¿Desbloquear esta cuenta?');" />
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </section>
</asp:Content>