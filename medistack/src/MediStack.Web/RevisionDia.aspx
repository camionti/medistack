<%@ Page Title="Revisión del día" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RevisionDia.aspx.cs" Inherits="MediStack.Web.RevisionDiaPagina" %>
<asp:Content ID="RevisionContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="page-heading" aria-labelledby="revision-title">
        <p class="eyebrow">Caja y facturación</p>
        <h1 id="revision-title">Revisión del día</h1>
        <p>Turnos cuya hora ya pasó y todavía no se resolvieron. Marcá quién asistió y quién faltó; esta revisión no depende del cierre de caja.</p>
    </section>

    <asp:Label ID="Mensaje" runat="server" Visible="false" role="status" />

    <section class="panel management-panel" aria-labelledby="pendientes-title">
        <h2 id="pendientes-title" class="section-title">Turnos sin resolver</h2>
        <div class="table-wrap">
            <asp:GridView ID="PendientesGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" DataKeyNames="TurnoId" OnRowCommand="PendientesGrid_RowCommand"
                EmptyDataText="No hay turnos pendientes de resolver. ¡Todo al día!">
                <Columns>
                    <asp:BoundField DataField="FechaHora" HeaderText="Fecha y hora" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                    <asp:BoundField DataField="Paciente" HeaderText="Paciente" />
                    <asp:BoundField DataField="Profesional" HeaderText="Profesional" />
                    <asp:BoundField DataField="Especialidad" HeaderText="Especialidad" />
                    <asp:TemplateField HeaderText="Estado">
                        <ItemTemplate>
                            <span class="status-tag status-turno" data-estado='<%#: Eval("Estado") %>'><%#: Eval("Estado") %></span>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Acciones">
                        <ItemTemplate>
                            <asp:Button runat="server" Text="Asistió" CssClass="button button-primary button-sm"
                                CommandName="Atendido" CommandArgument='<%# Eval("TurnoId") %>'
                                Visible='<%# Convert.ToString(Eval("Estado")) == "Confirmado" %>' />
                            <asp:Button runat="server" Text="Ausente" CssClass="button button-secondary button-sm"
                                CommandName="Ausente" CommandArgument='<%# Eval("TurnoId") %>'
                                Visible='<%# Convert.ToString(Eval("Estado")) == "Confirmado" %>' />
                            <asp:Button runat="server" Text="Confirmar" CssClass="button button-primary button-sm"
                                CommandName="Confirmado" CommandArgument='<%# Eval("TurnoId") %>'
                                Visible='<%# Convert.ToString(Eval("Estado")) == "Solicitado" %>' />
                            <asp:Button runat="server" Text="Cancelar" CssClass="button button-secondary button-sm"
                                CommandName="Cancelado" CommandArgument='<%# Eval("TurnoId") %>'
                                Visible='<%# Convert.ToString(Eval("Estado")) == "Solicitado" %>' />
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
        <p class="form-note">Si el paciente vino, "Asistió" deja el turno listo para registrar el cobro. Un turno marcado como ausente nunca genera un cobro, por lo que no entra en el cierre de caja.</p>
    </section>
</asp:Content>
