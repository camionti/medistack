<%@ Page Title="Asistente de turnos" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="AsistentePaciente.aspx.cs" Inherits="MediStack.Web.AsistentePaciente" %>
<asp:Content ID="AsistenteContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="page-heading" aria-labelledby="asistente-title">
        <p class="eyebrow">Asistencia para pacientes</p>
        <h1 id="asistente-title">Asistente de turnos</h1>
        <p>Escribí lo que necesitás y te guiaré paso a paso. Las operaciones que modifiquen tus turnos siempre requieren confirmación.</p>
    </section>
    <asp:Label ID="Mensaje" runat="server" Visible="false" role="status" />
    <section class="panel management-panel" aria-labelledby="chat-title">
        <h2 id="chat-title" class="section-title">Conversación</h2>
        <asp:Literal ID="Respuesta" runat="server" />
        <div class="form-group">
            <asp:Label ID="ConsultaLabel" runat="server" AssociatedControlID="Consulta" Text="Tu mensaje" />
            <asp:TextBox ID="Consulta" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="4"
                MaxLength="2000" />
        </div>
        <div class="form-actions">
            <asp:Button ID="Enviar" runat="server" Text="Enviar mensaje" CssClass="button button-primary"
                OnClick="Enviar_Click" />
        </div>
    </section>
</asp:Content>
