<%@ Page Title="MediStack | Gestión Clínica Inclusiva" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="MediStack.Web.DefaultPage" %>

<asp:Content ID="DefaultHead" ContentPlaceHolderID="HeadContent" runat="server">
    <link href="<%: ResolveUrl("~/Content/landing.css") %>?v=<%: System.IO.File.GetLastWriteTimeUtc(Server.MapPath("~/Content/landing.css")).Ticks %>" rel="stylesheet" />
</asp:Content>

<asp:Content ID="DefaultContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="landing-page">
        <!-- Hero Section -->
        <section class="landing-hero" aria-labelledby="hero-title">
            <div class="hero-grid">
                <div class="hero-content">
                    <div class="hero-badge">
                        <span class="hero-badge-dot" aria-hidden="true"></span>
                        <span>MediStack · Salud Digital Inclusiva</span>
                    </div>
                    <h1 id="hero-title" class="hero-title">
                        Gestión clínica simple, ágil y <span class="gradient-text">para todas las personas</span>
                    </h1>
                    <p class="hero-lead">
                        Transformamos la atención en salud con una plataforma moderna y fácil de usar. Gestioná turnos, seguimientos médicos y administración con total comodidad y sin barreras.
                    </p>

                    <!-- Estado para usuario autenticado -->
                    <asp:Panel ID="PanelUsuarioAutenticado" runat="server" CssClass="hero-auth-box" Visible="false">
                        <div class="hero-auth-info">
                            <p class="hero-auth-greeting">Bienvenido de nuevo, <asp:Literal ID="NombreUsuarioLiteral" runat="server" /></p>
                            <p class="hero-auth-role">Perfil activo: <strong><asp:Literal ID="RolUsuarioLiteral" runat="server" /></strong></p>
                        </div>
                        <a href="<%: ResolveUrl("~/Dashboard.aspx") %>" class="btn-hero btn-hero-primary">
                            Ver tu espacio
                            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M5 12h14M12 5l7 7-7 7"/></svg>
                        </a>
                    </asp:Panel>

                    <!-- Estado para visitante anónimo -->
                    <asp:Panel ID="PanelUsuarioAnonimo" runat="server" CssClass="hero-actions" Visible="true">
                        <a href="<%: ResolveUrl("~/Account/Login.aspx") %>" class="btn-hero btn-hero-primary">
                            Ingresar
                            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4M10 17l5-5-5-5M15 12H3"/></svg>
                        </a>
                        <a href="<%: ResolveUrl("~/Account/Registro.aspx") %>" class="btn-hero btn-hero-secondary">
                            Registrarse como Paciente
                        </a>
                        <a href="#inclusion" class="btn-hero btn-hero-secondary">
                            Enfoque Inclusivo
                        </a>
                    </asp:Panel>
                </div>

                <!-- Hero Visual: Beneficios Clave -->
                <div class="hero-visual" aria-hidden="true">
                    <div class="hero-visual-card">
                        <div class="visual-card-header">
                            <h2 class="visual-card-title">Atención Médica Accesible</h2>
                        </div>
                        <div class="visual-metrics">
                            <div class="metric-item">
                                <div class="metric-item-value">Fácil</div>
                                <div class="metric-item-label">Uso intuitivo</div>
                            </div>
                            <div class="metric-item">
                                <div class="metric-item-value">Claro</div>
                                <div class="metric-item-label">Letra grande y legible</div>
                            </div>
                            <div class="metric-item">
                                <div class="metric-item-value">Inclusivo</div>
                                <div class="metric-item-label">Para todas las edades</div>
                            </div>
                            <div class="metric-item">
                                <div class="metric-item-value">Seguro</div>
                                <div class="metric-item-label">Privacidad cuidada</div>
                            </div>
                        </div>
                        <ul class="visual-feature-list">
                            <li>
                                <svg class="feature-check-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M20 6L9 17l-5-5"/></svg>
                                <span>Modo de alto contraste para leer mejor</span>
                            </li>
                            <li>
                                <svg class="feature-check-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M20 6L9 17l-5-5"/></svg>
                                <span>Botones grandes y fáciles de presionar</span>
                            </li>
                            <li>
                                <svg class="feature-check-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M20 6L9 17l-5-5"/></svg>
                                <span>Diseñado para usar desde cualquier pantalla</span>
                            </li>
                            <li>
                                <svg class="feature-check-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M20 6L9 17l-5-5"/></svg>
                                <span>Asistente que te orienta paso a paso</span>
                            </li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>

        <!-- Sección: Inclusión y Accesibilidad -->
        <section id="inclusion" class="inclusion-section" aria-labelledby="inclusion-title">
            <div class="section-header">
                <p class="section-eyebrow">Compromiso Inclusivo</p>
                <h2 id="inclusion-title" class="section-title">Tecnología pensada para todas las capacidades</h2>
                <p class="section-subtitle">
                    En MediStack creemos que la salud debe ser accesible para todos. Nuestra plataforma cuenta con herramientas prácticas para facilitar la lectura y el uso a personas con visión o movilidad reducida.
                </p>
            </div>

            <!-- Llamada interactiva para probar los ajustes visuales -->
            <aside class="accessibility-callout" role="note" aria-label="Ajustes visuales disponibles">
                <svg class="callout-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                    <circle cx="12" cy="12" r="10"/>
                    <path d="M12 16v-4M12 8h.01"/>
                </svg>
                <div class="callout-text">
                    <h4>Probá los ajustes visuales en cualquier momento</h4>
                    <p>
                        Hacé clic en el <strong>botón circular azul</strong> en la esquina inferior izquierda de la pantalla para activar el modo de <strong>Alto Contraste</strong> o <strong>agrandar la letra</strong> para leer con mayor comodidad.
                    </p>
                </div>
            </aside>

            <div class="accessibility-pillars">
                <!-- Pilar 1 -->
                <article class="pillar-card">
                    <div class="pillar-icon-box" aria-hidden="true">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z"/>
                            <circle cx="12" cy="12" r="3"/>
                        </svg>
                    </div>
                    <h3>Lectura cómoda y clara</h3>
                    <p>
                        Pensado para personas con vista cansada o dificultades para ver detalles. Podés cambiar a un fondo oscuro con letras de máxima nitidez o agrandar el texto sin desordenar la pantalla.
                    </p>
                    <div class="pillar-badges">
                        <span class="pillar-badge">Alto Contraste</span>
                        <span class="pillar-badge">Texto Grande</span>
                        <span class="pillar-badge">Fácil Lectura</span>
                    </div>
                </article>

                <!-- Pilar 2 -->
                <article class="pillar-card">
                    <div class="pillar-icon-box" aria-hidden="true">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <rect width="20" height="16" x="2" y="4" rx="2"/>
                            <path d="m10 8 4 4-4 4"/>
                        </svg>
                    </div>
                    <h3>Manejo simple y descansado</h3>
                    <p>
                        Diseñado para personas que necesitan mayor comodidad motriz. Toda la navegación se puede realizar usando solo el teclado, con botones amplios para evitar toques involuntarios.
                    </p>
                    <div class="pillar-badges">
                        <span class="pillar-badge">Botones Amplios</span>
                        <span class="pillar-badge">Uso por Teclado</span>
                        <span class="pillar-badge">Navegación Ágil</span>
                    </div>
                </article>

                <!-- Pilar 3 -->
                <article class="pillar-card">
                    <div class="pillar-icon-box" aria-hidden="true">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M12 2a3 3 0 0 0-3 3v7a3 3 0 0 0 6 0V5a3 3 0 0 0-3-3Z"/>
                            <path d="M19 10v2a7 7 0 0 1-14 0v-2"/>
                            <line x1="12" x2="12" y1="19" y2="22"/>
                        </svg>
                    </div>
                    <h3>Compatible con lectura por voz</h3>
                    <p>
                        El contenido está organizado de forma clara y ordenada para que los programas de lectura de pantalla describan las opciones y formularios con total fluidez.
                    </p>
                    <div class="pillar-badges">
                        <span class="pillar-badge">Lectura por Voz</span>
                        <span class="pillar-badge">Textos Claros</span>
                        <span class="pillar-badge">Formularios Guiados</span>
                    </div>
                </article>

               
            </div>
        </section>

        <!-- Sección: Empresa de Software Profesional -->
        <section id="empresa" class="company-section" aria-labelledby="empresa-title">
            <div class="company-card">
                <div class="company-content">
                    <p class="section-eyebrow">Sobre Nosotros</p>
                    <h3 id="empresa-title">Software confiable al servicio de la salud</h3>
                    <p>
                        En <strong>MediStack</strong> desarrollamos soluciones tecnológicas creadas para simplificar la atención clínica y conectar a las personas. Nos especializamos en brindar herramientas digitales donde la prioridad es la experiencia humana, la privacidad y la inclusión de cada paciente.
                    </p>
                    <p>
                        Acompañamos a consultorios, centros médicos y profesionales de la salud con un sistema moderno, estable y seguro para gestionar su actividad diaria con tranquilidad.
                    </p>
                </div>
                <div class="company-highlights">
                    <div class="highlight-box">
                        <h4 class="highlight-box-title">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect width="18" height="18" x="3" y="3" rx="2"/><path d="M3 9h18M9 21V9"/></svg>
                            Gestión Integral
                        </h4>
                        <p>Turnos, fichas de atención y administración unificados en una sola plataforma.</p>
                    </div>
                    <div class="highlight-box">
                        <h4 class="highlight-box-title">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/></svg>
                            Seguridad y Confianza
                        </h4>
                        <p>Protegemos la información y privacidad de cada paciente en todo momento.</p>
                    </div>
                    <div class="highlight-box">
                        <h4 class="highlight-box-title">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75"/></svg>
                            Diseño para Todos
                        </h4>
                        <p>Experiencias simples que se adaptan a usuarios de cualquier edad o condición.</p>
                    </div>
                    <div class="highlight-box">
                        <h4 class="highlight-box-title">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m18 16 4-4-4-4M6 8l-4 4 4 4M14.5 4l-5 16"/></svg>
                            Agilidad y Soporte
                        </h4>
                        <p>Un sistema veloz y preparado para resolver el trabajo del día a día sin demoras.</p>
                    </div>
                </div>
            </div>
        </section>

        <!-- Sección: Módulos de la Plataforma -->
        <section id="modulos" class="modules-section" aria-labelledby="modulos-title">
            <div class="section-header">
                <p class="section-eyebrow">Solución Integral</p>
                <h2 id="modulos-title" class="section-title">Una solución para cada rol en la clínica</h2>
                <p class="section-subtitle">
                    Optimizamos el día a día tanto de pacientes como de profesionales y personal administrativo.
                </p>
            </div>

            <div class="modules-grid">
                <!-- Módulo Paciente -->
                <article class="module-card">
                    <span class="module-number" aria-hidden="true">01</span>
                    <div class="module-icon-wrap paciente" aria-hidden="true">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2"/>
                            <circle cx="12" cy="7" r="4"/>
                        </svg>
                    </div>
                    <h3>Portal de Pacientes</h3>
                    <p>
                        Autonomía total: reservá turnos según la especialidad, consultá tu historial de atención y gestioná tus pagos de manera transparente.
                    </p>
                    <ul class="module-feature-list">
                        <li>Reserva de turnos con disponibilidad al instante</li>
                        <li>Ficha personal con obras sociales y coberturas</li>
                        <li>Historial de atenciones médicas y pagos</li>
                        <li>Asistente interactivo para resolver tus consultas</li>
                    </ul>
                </article>

                <!-- Módulo Profesional -->
                <article class="module-card">
                    <div class="module-number" aria-hidden="true">02</div>
                    <div class="module-icon-wrap profesional" aria-hidden="true">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M4.8 2.3A.3.3 0 1 0 5 2H4a2 2 0 0 0-2 2v5a6 6 0 0 0 6 6v0a6 6 0 0 0 6-6V4a2 2 0 0 0-2-2h-1a.2.2 0 1 0 .3.3"/>
                            <path d="M8 15v1a6 6 0 0 0 6 6v0a6 6 0 0 0 6-6v-4"/>
                            <circle cx="20" cy="10" r="2"/>
                        </svg>
                    </div>
                    <h3>Espacio para Profesionales</h3>
                    <p>
                        Organización clara para los especialistas de la salud: gestioná tu agenda diaria, consultá tus turnos asignados y accedé a la información de tus pacientes.
                    </p>
                    <ul class="module-feature-list">
                        <li>Agenda diaria clara y actualizada en tiempo real</li>
                        <li>Disponibilidad organizada por especialidad</li>
                        <li>Registro de atenciones y notas clínicas</li>
                        <li>Seguimiento ordenado de pacientes atendidos</li>
                    </ul>
                </article>

                <!-- Módulo Administrativo -->
                <article class="module-card">
                    <div class="module-number" aria-hidden="true">03</div>
                    <div class="module-icon-wrap admin" aria-hidden="true">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <rect width="20" height="14" x="2" y="7" rx="2" ry="2"/>
                            <path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16"/>
                        </svg>
                    </div>
                    <h3>Administración y Secretaría</h3>
                    <p>
                        Control ágil de la institución: administración de profesionales, obras sociales, convenios vigentes y control diario de caja.
                    </p>
                    <ul class="module-feature-list">
                        <li>Alta y gestión de pacientes y profesionales</li>
                        <li>Convenios con obras sociales y coberturas</li>
                        <li>Cobro de consultas y emisión de comprobantes</li>
                        <li>Cierre y control diario de caja</li>
                    </ul>
                </article>
            </div>
        </section>

        <!-- Sección: Banner Final de Llamada a la Acción -->
        <section class="landing-cta-banner" aria-labelledby="cta-title">
            <h2 id="cta-title">Una atención médica más conectada y humana</h2>
            <p>
                Descubrí cómo MediStack ayuda a centros de salud, médicos y pacientes a comunicarse y gestionarse sin dificultades.
            </p>

            <asp:Panel ID="PanelCtaAutenticado" runat="server" CssClass="cta-banner-actions" Visible="false">
                <a href="<%: ResolveUrl("~/Dashboard.aspx") %>" class="btn-hero btn-cta-white">
                    Ver tu espacio
                </a>
            </asp:Panel>

            <asp:Panel ID="PanelCtaAnonimo" runat="server" CssClass="cta-banner-actions" Visible="true">
                <a href="<%: ResolveUrl("~/Account/Login.aspx") %>" class="btn-hero btn-cta-white">
                    Ingresar
                </a>
                <a href="<%: ResolveUrl("~/Account/Registro.aspx") %>" class="btn-hero btn-cta-outline">
                    Registrarse como Paciente
                </a>
            </asp:Panel>
        </section>
    </div>
</asp:Content>
