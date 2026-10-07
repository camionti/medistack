<%@ Page Title="Inicio" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="MediStack.Web.Dashboard" %>
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
                <article class="panel dashboard-card">
                    <span class="card-number" aria-hidden="true">02</span>
                    <h3>Pacientes</h3>
                    <p>Accede a las fichas vinculadas con tus atenciones.</p>
                    <a href="<%: ResolveUrl("~/Turnos.aspx") %>">Consultar turnos y pacientes</a>
                    <a href="<%: ResolveUrl("~/Cobros.aspx") %>">Consultar cobros asociados</a>
                </article>
            </div>
        </section>
    </asp:Panel>
    <asp:Panel ID="AdministrativoPanel" runat="server" Visible="false">
        <section aria-labelledby="administrativo-title">
            <h2 id="administrativo-title" class="section-title">Área administrativa</h2>
            <div class="card-grid">
                <article class="panel dashboard-card">
                    <span class="card-number" aria-hidden="true">01</span>
                    <h3>Gestión clínica</h3>
                    <p>Pacientes, profesionales, especialidades y agendas.</p>
                    <a href="<%: ResolveUrl("~/Pacientes.aspx") %>">Pacientes</a>
                    <a href="<%: ResolveUrl("~/Turnos.aspx") %>">Turnos</a>
                    <a href="<%: ResolveUrl("~/Agenda.aspx") %>">Agendas</a>
                    <a href="<%: ResolveUrl("~/Profesionales.aspx") %>">Profesionales</a>
                    <a href="<%: ResolveUrl("~/Especialidades.aspx") %>">Especialidades</a>
                </article>
                <article class="panel dashboard-card">
                    <span class="card-number" aria-hidden="true">02</span>
                    <h3>Caja y cobros</h3>
                    <p>Registra pagos, consulta ingresos y controla los cierres diarios.</p>
                    <a href="<%: ResolveUrl("~/Cobros.aspx") %>">Administrar cobros</a>
                    <a href="<%: ResolveUrl("~/Caja.aspx") %>">Consultar y cerrar caja</a>
                </article>
                <article class="panel dashboard-card">
                    <span class="card-number" aria-hidden="true">03</span>
                    <h3>Obras sociales y reportes</h3>
                    <p>Convenios, coberturas y estadísticas de la clínica.</p>
                    <a href="<%: ResolveUrl("~/ObrasSociales.aspx") %>">Obras sociales</a>
                    <a href="<%: ResolveUrl("~/Coberturas.aspx") %>">Coberturas</a>
                    <a href="<%: ResolveUrl("~/Convenios.aspx") %>">Convenios</a>
                </article>
            </div>
        </section>
    </asp:Panel>
</asp:Content>
