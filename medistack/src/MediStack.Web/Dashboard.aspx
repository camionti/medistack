<%@ Page Title="Tu espacio" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="MediStack.Web.Dashboard" %>
<asp:Content ID="DashboardContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="page-heading" aria-labelledby="dashboard-title">
        <p class="eyebrow">Panel principal</p>
        <h1 id="dashboard-title">Hola, <asp:Literal ID="NombreUsuarioLiteral" runat="server" /></h1>
        <p>Tu perfil: <strong><asp:Literal ID="RolLiteral" runat="server" /></strong></p>
    </section>

    <asp:Panel ID="PacientePanel" runat="server" Visible="false">
        <section aria-labelledby="paciente-title">
            <h2 id="paciente-title" class="section-title">Tu espacio</h2>
            <div class="card-grid">
                <article class="panel dashboard-card">
                    <span class="card-number" aria-hidden="true">01</span>
                    <h3>Mis turnos</h3>
                    <p>Consulta y administra tus próximos turnos.</p>
                    <a href="<%: ResolveUrl("~/Turnos.aspx") %>">Consultar y solicitar turnos</a>
                </article>
                <article class="panel dashboard-card">
                    <span class="card-number" aria-hidden="true">02</span>
                    <h3>Mi historial</h3>
                    <p>Consulta tus datos, turnos, atenciones y cobros asociados.</p>
                    <a href="<%: ResolveUrl("~/FichaPaciente.aspx") %>">Ver mi ficha e historial</a>
                    <a href="<%: ResolveUrl("~/Cobros.aspx") %>">Consultar mis cobros</a>
                    <a href="<%: ResolveUrl("~/AsistentePaciente.aspx") %>">Hablar con el asistente</a>
                </article>
            </div>
        </section>
    </asp:Panel>
    <asp:Panel ID="ProfesionalPanel" runat="server" Visible="false">
        <section aria-labelledby="profesional-title">
            <h2 id="profesional-title" class="section-title">Área profesional</h2>
            <div class="card-grid">
                <article class="panel dashboard-card">
                    <span class="card-number" aria-hidden="true">01</span>
                    <h3>Mi agenda</h3>
                    <p>Consulta tus horarios, disponibilidad y turnos del día.</p>
                    <a href="<%: ResolveUrl("~/Agenda.aspx") %>">Ver mi agenda</a>
                </article>
                <article class="panel dashboard-card dashboard-card-highlight">
                    <span class="card-number" aria-hidden="true">02</span>
                    <h3>Jornada de hoy</h3>
                    <p>Accede directamente al detalle de los turnos de tu jornada actual.</p>
                    <asp:HyperLink ID="JornadaHoyLink" runat="server" Text="Ver jornada de hoy" />
                </article>
                <article class="panel dashboard-card">
                    <span class="card-number" aria-hidden="true">03</span>
                    <h3>Pacientes</h3>
                    <p>Accede a las fichas vinculadas con tus atenciones.</p>
                    <a href="<%: ResolveUrl("~/Turnos.aspx") %>">Consultar turnos y pacientes</a>
                    <a href="<%: ResolveUrl("~/Cobros.aspx") %>">Consultar cobros asociados</a>
                    <a href="<%: ResolveUrl("~/AsistentePaciente.aspx") %>">Hablar con el asistente</a>
                </article>
            </div>
        </section>
    </asp:Panel>
    <asp:Panel ID="AdministrativoPanel" runat="server" Visible="false">
        <section aria-labelledby="administrativo-title">
            <h2 id="administrativo-title" class="section-title">Panel operativo administrativo</h2>
            <div class="card-grid">
                <article class="panel dashboard-card dashboard-card-highlight">
                    <span class="card-number" aria-hidden="true">01</span>
                    <h3>En foco hoy</h3>
                    <p>Atención directa en mostrador, recepción de pacientes y cobranza del día.</p>
                    <asp:HyperLink ID="AdminTurnosHoyLink" runat="server" Text="Turnos de hoy" />
                    <a href="<%: ResolveUrl("~/RevisionDia.aspx") %>">Revisión del día</a>
                    <a href="<%: ResolveUrl("~/Cobros.aspx") %>">Cobros</a>
                    <a href="<%: ResolveUrl("~/Caja.aspx") %>">Caja diaria</a>
                    <a href="<%: ResolveUrl("~/Pacientes.aspx") %>">Recepción de pacientes</a>
                </article>
                <article class="panel dashboard-card">
                    <span class="card-number" aria-hidden="true">02</span>
                    <h3>Gestión semanal</h3>
                    <p>Seguimiento rutinario de agendas médicas, reprogramaciones y honorarios.</p>
                    <asp:HyperLink ID="AdminAgendaSemanaLink" runat="server" Text="Agendas de la semana" />
                    <a href="<%: ResolveUrl("~/Reembolsos.aspx") %>">Reembolsos pendientes</a>
                    <a href="<%: ResolveUrl("~/Liquidaciones.aspx") %>">Honorarios del período</a>
                    <a href="<%: ResolveUrl("~/AsistentePaciente.aspx") %>">Asistente de turnos</a>
                </article>
                <article class="panel dashboard-card">
                    <span class="card-number" aria-hidden="true">03</span>
                    <h3>Archivo y catálogos</h3>
                    <p>Búsquedas históricas globales, métricas y mantenimiento de maestros.</p>
                    <a href="<%: ResolveUrl("~/Pacientes.aspx") %>">Padrón histórico de pacientes</a>
                    <a href="<%: ResolveUrl("~/Turnos.aspx") %>">Historial de turnos</a>
                    <a href="<%: ResolveUrl("~/Reportes.aspx") %>">Reportes y estadísticas</a>
                    <a href="<%: ResolveUrl("~/ObrasSociales.aspx") %>">Obras sociales</a>
                    <a href="<%: ResolveUrl("~/Convenios.aspx") %>">Convenios y coberturas</a>
                </article>
            </div>
        </section>
    </asp:Panel>
</asp:Content>
