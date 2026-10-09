# Estructura del proyecto ASP.NET Web Forms

La solución de Web Forms se encuentra en `MediStack.sln`. La separación de proyectos mantiene las responsabilidades de interfaz, reglas de negocio y acceso a datos:

```text
MediStack.sln
├── database/
│   └── 01_MediStackDB.sql
└── src/
    ├── MediStack.Web/                 ASP.NET Web Application (.NET Framework 4.8)
    │   ├── Account/Login.aspx         Inicio de sesión
    │   ├── Content/site.css           Estilos responsivos y accesibles
    │   ├── Dashboard.aspx             Panel protegido por autenticación
    │   ├── FichaPaciente.aspx         Ficha e historial del paciente
    │   ├── Agenda.aspx                Disponibilidad y turnos por profesional y fecha
    │   ├── Turnos.aspx                Solicitud, disponibilidad y gestión de turnos
    │   ├── Cobros.aspx                Registro y consulta de cobros por rol
    │   ├── Caja.aspx                  Ingresos, totales del período y cierre diario
    │   ├── Site.Master                Diseño común y navegación
    │   └── Web.config                 SQL Server, sesión y Forms Authentication
    ├── MediStack.Dominio/             Modelos utilizados por las capas
    ├── MediStack.Datos/               ADO.NET y consultas parametrizadas a SQL Server
    │   ├── TurnosDatos.cs             Consultas y operaciones de turnos
    │   └── CobrosDatos.cs              Cobros y cierres transaccionales
    └── MediStack.Negocio/             Validaciones y reglas de negocio
        ├── TurnosNegocio.cs            Disponibilidad y reglas de turnos
        └── CobrosNegocio.cs            Validaciones de importes y períodos
```

## Referencias entre proyectos

- `MediStack.Web` referencia a `MediStack.Negocio` y `MediStack.Dominio`.
- `MediStack.Negocio` referencia a `MediStack.Datos` y `MediStack.Dominio`.
- `MediStack.Datos` referencia a `MediStack.Dominio`.
- `MediStack.Datos` utiliza `System.Data.SqlClient`; no se usara Entity Framework.

## Configuracion de SQL Server

La cadena de conexion se guardara en `Web.config`, dentro de `<connectionStrings>`. Ejemplo para SQL Server local con autenticacion integrada:

```xml
<connectionStrings>
  <add name="MediStackDB"
       connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=MediStackDB;Integrated Security=True"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

El nombre del servidor debe ajustarse a la instancia instalada en cada equipo. Si se usa autenticacion de SQL Server, las credenciales locales no deben publicarse en el repositorio.

## Responsabilidades basicas

- Las paginas `.aspx` presentan formularios y resultados; sus code-behind coordinan las llamadas.
- `UsuarioNegocio` valida credenciales, estado de la cuenta y el hash PBKDF2.
- `UsuarioDatos` ejecuta consultas SQL parametrizadas sobre `Usuarios` y `Roles`.
- Las entidades de `MediStack.Dominio` representan los datos que cruzan las capas.
- `GestionClinicaDatos` consulta pacientes, profesionales, horarios, turnos, registros clínicos y cobros mediante ADO.NET parametrizado.
- `FichaPacienteNegocio` construye la ficha desde `Pacientes`, `Usuarios`, `ObrasSociales`, `CoberturasEspecialidades`, `Turnos`, `RegistrosClinicos` y `Cobros`, sin copiar esos datos a tablas nuevas.
- `AgendaNegocio` calcula los espacios a partir de `HorariosAtencion`, la duración estándar de `Especialidades` y los turnos existentes. La gestión de franjas evita superposiciones y no permite dejar turnos vigentes fuera de su horario.
- `TurnosNegocio` calcula intervalos de turnos y valida solicitudes; `TurnosDatos` vuelve a validar dentro de transacciones serializables antes de reservar, cancelar o reprogramar. Al reprogramar, solo se excluye el turno original al presentar alternativas y el cambio se confirma como una unidad.
- `CobrosDatos` calcula el copago desde el valor de la relación profesional-especialidad y la cobertura del convenio vigente a la fecha del turno. Registra cobros y actualiza señas en una transacción serializable; también concilia y registra cierres diarios usando los ingresos de `Cobros`.
- `AgenteIAPacienteNegocio` orquesta la asistencia conversacional para pacientes sin reemplazar las validaciones de `TurnosNegocio`; `InteraccionesIADatos` persiste las consultas en `InteraccionesIA`.
- `Cobros.aspx` limita los resultados al paciente o profesional autenticado, mientras que solo el personal administrativo puede registrar cobros. `Caja.aspx` hereda la protección administrativa y limita a un cierre por fecha; un cierre bloquea cobros posteriores de ese día.
- La autenticacion usa Forms Authentication y conserva identificador, nombre y codigo de rol en `Session`.
- Las paginas que requieran inicio de sesion deben heredar de `PaginaProtegida`, que valida cookie y sesión antes de servir contenido.
- `FichaPaciente.aspx` limita las fichas de pacientes a su propia cuenta; un profesional solo accede a pacientes vinculados a sus turnos; el personal administrativo puede consultar todas.
- `Agenda.aspx` permite a personal administrativo gestionar horarios y a profesionales consultar únicamente su propia agenda.
- `Turnos.aspx` limita la consulta de pacientes a sus propios turnos y la de profesionales a los turnos de su agenda; el personal administrativo puede consultar y gestionar todos. Los nombres de pacientes ocupantes no se muestran a otros pacientes.
- `Caja.aspx` presenta cobros como ingresos porque no existe una tabla de movimientos independiente. El esquema tampoco representa egresos, una operación de apertura ni observaciones de cobro; la caja se considera abierta hasta que se registra el cierre diario y el estado del cobro se deriva de `Turnos.EstadoSena` o de la existencia del cobro de consulta.

Login, sesión, roles, dashboard, módulos administrativos, ficha/historial, agendas, turnos, cobros, cierres de caja y la primera pantalla del asistente de pacientes usan el esquema actual de `MediStackDB`; no requieren scripts de migración. Liquidaciones, reportes, registro y recuperación de contraseña quedan para próximas etapas. La integración con un proveedor externo y la ejecución conversacional de operaciones de turnos quedan pendientes.
