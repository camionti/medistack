<%@ Page Title="Pacientes" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Pacientes.aspx.cs" Inherits="MediStack.Web.Pacientes" %>
<asp:Content ID="PacientesContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="page-heading">
        <p class="eyebrow">Gestión clínica</p>
        <h1>Pacientes</h1>
        <p>Consulta y mantiene los datos de los pacientes de la clínica.</p>
    </section>
    <asp:Label ID="Mensaje" runat="server" Visible="false" role="status" />
    <section class="panel management-panel" aria-labelledby="buscar-paciente-title">
        <h2 id="buscar-paciente-title" class="section-title">Buscar pacientes</h2>
        <div class="search-row">
            <asp:Label ID="BusquedaLabel" runat="server" AssociatedControlID="Busqueda" Text="Nombre, documento, usuario o correo" CssClass="visually-hidden" />
            <asp:TextBox ID="Busqueda" runat="server" CssClass="form-control" MaxLength="100" />
            <asp:Button ID="Buscar" runat="server" Text="Buscar" CssClass="button button-secondary" CausesValidation="false" OnClick="Buscar_Click" formnovalidate="formnovalidate" />
            <asp:Button ID="LimpiarBusqueda" runat="server" Text="Ver todos" CssClass="button button-light" CausesValidation="false" OnClick="LimpiarBusqueda_Click" formnovalidate="formnovalidate" />
        </div>
        <div class="table-wrap">
            <asp:GridView ID="PacientesGrid" runat="server" AutoGenerateColumns="false" CssClass="data-table"
                GridLines="None" DataKeyNames="PacienteId,NombreUsuario,Nombre,Apellido,NumeroDocumento,FechaNacimiento,Email,Telefono,ObraSocialId,ObraSocial,NumeroAfiliado,ContactoEmergenciaNombre,ContactoEmergenciaTelefono,Activo"
                EmptyDataText="No hay pacientes para mostrar." OnRowCommand="PacientesGrid_RowCommand">
                <Columns>
                    <asp:BoundField DataField="Nombre" HeaderText="Nombre" />
                    <asp:BoundField DataField="Apellido" HeaderText="Apellido" />
                    <asp:BoundField DataField="NumeroDocumento" HeaderText="Documento" />
                    <asp:BoundField DataField="Email" HeaderText="Correo" />
                    <asp:BoundField DataField="ObraSocial" HeaderText="Obra social" />
                    <asp:TemplateField HeaderText="Estado">
                        <ItemTemplate><%# Convert.ToBoolean(Eval("Activo")) ? "Activo" : "Inactivo" %></ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Acciones">
                        <ItemTemplate>
                            <div class="row-actions">
                                <asp:HyperLink ID="VerFicha" runat="server" Text="Ficha"
                                    NavigateUrl='<%# ResolveUrl("~/FichaPaciente.aspx?PacienteId=" + Eval("PacienteId")) %>' />
                                <asp:LinkButton ID="Editar" runat="server" Text="Editar" CommandName="Editar"
                                    CommandArgument="<%# Container.DataItemIndex %>" CausesValidation="false" />
                                <asp:LinkButton ID="CambiarEstado" runat="server"
                                    Text='<%# Convert.ToBoolean(Eval("Activo")) ? "Desactivar" : "Reactivar" %>'
                                    CommandName="CambiarEstado" CommandArgument="<%# Container.DataItemIndex %>"
                                    CausesValidation="false" OnClientClick="return confirm('¿Cambiar el estado del paciente?');" />
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </section>
    <section class="panel management-panel" aria-labelledby="formulario-paciente-title">
        <div class="section-heading-row">
            <h2 id="formulario-paciente-title" class="section-title"><asp:Literal ID="TituloFormulario" runat="server" /></h2>
            <asp:Button ID="Nuevo" runat="server" Text="Nuevo paciente" CssClass="button button-light" CausesValidation="false" OnClick="Nuevo_Click" formnovalidate="formnovalidate" />
        </div>
        <div class="form-grid">
            <div class="form-group">
                <asp:Label ID="NombreLabel" runat="server" AssociatedControlID="Nombre" Text="Nombre" />
                <asp:TextBox ID="Nombre" runat="server" CssClass="form-control" MaxLength="100" required="required" />
            </div>
            <div class="form-group">
                <asp:Label ID="ApellidoLabel" runat="server" AssociatedControlID="Apellido" Text="Apellido" />
                <asp:TextBox ID="Apellido" runat="server" CssClass="form-control" MaxLength="100" required="required" />
            </div>
            <div class="form-group">
                <asp:Label ID="DocumentoLabel" runat="server" AssociatedControlID="Documento" Text="Documento" />
                <asp:TextBox ID="Documento" runat="server" CssClass="form-control" MaxLength="10" inputmode="numeric" required="required" pattern="[0-9]{7,8}|[0-9]{1,2}\.[0-9]{3}\.[0-9]{3}" title="Ingresa solo numeros: 7 u 8 digitos (por ejemplo 41736377)." />
            </div>
            <div class="form-group">
                <asp:Label ID="NacimientoLabel" runat="server" AssociatedControlID="Nacimiento" Text="Fecha de nacimiento" />
                <asp:TextBox ID="Nacimiento" runat="server" CssClass="form-control" TextMode="Date" required="required" />
            </div>
            <div class="form-group">
                <asp:Label ID="EmailLabel" runat="server" AssociatedControlID="Email" Text="Correo electrónico" />
                <asp:TextBox ID="Email" runat="server" CssClass="form-control" TextMode="Email" MaxLength="256" required="required" />
            </div>
            <div class="form-group">
                <asp:Label ID="TelefonoLabel" runat="server" AssociatedControlID="Telefono" Text="Teléfono" />
                <asp:TextBox ID="Telefono" runat="server" CssClass="form-control" MaxLength="30" />
            </div>
            <div class="form-group">
                <asp:Label ID="ObraSocialLabel" runat="server" AssociatedControlID="ObraSocial" Text="Obra social" />
                <asp:DropDownList ID="ObraSocial" runat="server" CssClass="form-control" />
            </div>
            <div class="form-group">
                <asp:Label ID="AfiliadoLabel" runat="server" AssociatedControlID="Afiliado" Text="Número de afiliado" />
                <asp:TextBox ID="Afiliado" runat="server" CssClass="form-control" MaxLength="50" />
            </div>
            <div class="form-group">
                <asp:Label ID="EmergenciaNombreLabel" runat="server" AssociatedControlID="EmergenciaNombre" Text="Contacto de emergencia" />
                <asp:TextBox ID="EmergenciaNombre" runat="server" CssClass="form-control" MaxLength="100" />
            </div>
            <div class="form-group">
                <asp:Label ID="EmergenciaTelefonoLabel" runat="server" AssociatedControlID="EmergenciaTelefono" Text="Teléfono de emergencia" />
                <asp:TextBox ID="EmergenciaTelefono" runat="server" CssClass="form-control" MaxLength="30" />
            </div>
            <asp:Panel ID="CuentaNueva" runat="server" CssClass="form-grid form-grid-nested">
                <div class="form-group">
                    <asp:Label ID="UsuarioLabel" runat="server" AssociatedControlID="Usuario" Text="Usuario para iniciar sesión" />
                    <asp:TextBox ID="Usuario" runat="server" CssClass="form-control" MaxLength="50" autocomplete="off" />
                </div>
                <div class="form-group">
                    <asp:Label ID="ContrasenaLabel" runat="server" AssociatedControlID="Contrasena" Text="Contraseña inicial (mínimo 10 caracteres)" />
                    <asp:TextBox ID="Contrasena" runat="server" CssClass="form-control" TextMode="Password" MaxLength="128" autocomplete="new-password" />
                </div>
            </asp:Panel>
        </div>
        <div class="form-actions">
            <asp:Button ID="Guardar" runat="server" Text="Guardar paciente" CssClass="button button-primary" OnClick="Guardar_Click" />
            <asp:Button ID="Cancelar" runat="server" Text="Cancelar" CssClass="button button-light" CausesValidation="false" OnClick="Nuevo_Click" formnovalidate="formnovalidate" />
        </div>
    </section>
</asp:Content>
