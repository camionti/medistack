<%@ Page Title="Asistente" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="AsistentePaciente.aspx.cs" Inherits="MediStack.Web.AsistentePaciente" %>
<asp:Content ID="AsistenteContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="page-heading" aria-labelledby="asistente-title">
        <p class="eyebrow">Asistencia inteligente</p>
        <h1 id="asistente-title">Asistente</h1>
        <p><asp:Literal ID="IntroLiteral" runat="server" /></p>
    </section>
    <asp:Label ID="Mensaje" runat="server" Visible="false" role="status" />
    <section class="panel management-panel" aria-labelledby="chat-title">
        <h2 id="chat-title" class="section-title">Conversación</h2>
        <asp:Literal ID="Respuesta" runat="server" />
        <asp:Panel ID="ConfirmacionPanel" runat="server" Visible="false" CssClass="alert alert-success">
            <p><strong>Acción pendiente:</strong> <asp:Literal ID="AccionLiteral" runat="server" /></p>
            <asp:Button ID="ConfirmarAccion" runat="server" Text="Confirmar" CssClass="button button-primary"
                OnClick="ConfirmarAccion_Click" CausesValidation="false" />
            <asp:Button ID="DescartarAccion" runat="server" Text="Descartar" CssClass="button button-secondary"
                OnClick="DescartarAccion_Click" CausesValidation="false" />
        </asp:Panel>
        <div class="form-group">
            <asp:Label ID="ConsultaLabel" runat="server" AssociatedControlID="Consulta" Text="Tu mensaje" />
            <asp:TextBox ID="Consulta" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="4"
                MaxLength="2000" />
        </div>
        <asp:HiddenField ID="CanalEntrada" runat="server" Value="Texto" />
        <div class="form-actions">
            <asp:Button ID="Enviar" runat="server" Text="Enviar mensaje" CssClass="button button-primary"
                OnClick="Enviar_Click" />
            <button type="button" id="ms-dictar" class="button button-secondary" hidden>Dictar por voz</button>
            <asp:Button ID="NuevaConversacion" runat="server" Text="Nueva conversación"
                CssClass="button button-secondary" OnClick="NuevaConversacion_Click" CausesValidation="false" />
        </div>
        <p class="form-note">El asistente consulta datos reales del sistema según tu rol. Las operaciones que modifican turnos siempre requieren tu confirmación.</p>
    </section>
    <script>
        (function () {
            var boton = document.getElementById('ms-dictar');
            var Reconocimiento = window.SpeechRecognition || window.webkitSpeechRecognition;
            if (!boton || !Reconocimiento) { return; }
            var texto = document.getElementById('<%= Consulta.ClientID %>');
            var canal = document.getElementById('<%= CanalEntrada.ClientID %>');
            boton.hidden = false;
            boton.addEventListener('click', function () {
                var r = new Reconocimiento();
                r.lang = 'es-AR';
                r.interimResults = false;
                r.onresult = function (e) {
                    texto.value = (texto.value + ' ' + e.results[0][0].transcript).trim();
                    canal.value = 'Voz';
                };
                r.onend = function () { boton.textContent = 'Dictar por voz'; };
                boton.textContent = 'Escuchando…';
                r.start();
            });
        })();
    </script>
</asp:Content>
