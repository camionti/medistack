# MediStack - Equipo 19

Aplicación web para la gestión de una clínica: pacientes, profesionales, agendas, turnos, cobros y caja.

## Tecnologías

- ASP.NET Web Forms y .NET Framework 4.8
- C# y ADO.NET
- Microsoft SQL Server
- Bootstrap y hojas de estilo propias

La solución separa las responsabilidades en cuatro proyectos: Web, Dominio, Datos y Negocio. La aplicación utiliza autenticación con contraseña almacenada mediante PBKDF2, sesiones y permisos por rol.

## Entregables

- Script SQL Server: [database/01_MediStackDB.sql](database/01_MediStackDB.sql)
- Diseño de tablas y relaciones: [spec/tablas.md](spec/tablas.md)
- Estructura prevista Web Forms: [spec/estructura-proyecto.md](spec/estructura-proyecto.md)
- Solución Visual Studio: [MediStack.sln](MediStack.sln)

La primera versión incluye autenticación con SQL Server, PBKDF2, bloqueo temporal después de intentos fallidos, cookie de autenticación y sesión para identidad y rol. La cadena de conexión de ejemplo está en `src/MediStack.Web/Web.config` y se debe ajustar para cada instancia.

## Compilación y ejecución

Abrir `MediStack.sln` en Visual Studio, seleccionar `MediStack.Web` como proyecto de inicio y ejecutar con IIS Express. La instancia de SQL Server configurada en `src/MediStack.Web/Web.config` debe estar disponible y tener creada la base `MediStackDB`.

También se puede compilar desde una terminal de desarrollador con:

```powershell
dotnet build MediStack.sln -c Debug
```

La compilación no inicia el sitio web; para verlo hay que ejecutarlo con IIS Express desde Visual Studio.

## Fase 2 - Gestión clínica inicial

Completada para los seis módulos administrativos iniciales:

- Pacientes: listado, búsqueda, alta transaccional de cuenta y perfil, edición y desactivación/reactivación.
- Profesionales: listado, búsqueda, alta transaccional de cuenta y perfil, edición y desactivación/reactivación.
- Especialidades: listado, búsqueda, alta, edición y desactivación/reactivación.
- Obras sociales: listado, búsqueda, alta, edición y desactivación/reactivación.
- Coberturas: listado, búsqueda, alta, edición y eliminación, respetando las claves foráneas de convenios.
- Convenios: listado, búsqueda, alta, edición y desactivación/reactivación; mantiene las relaciones con coberturas y profesionales-especialidades.

Las páginas de gestión requieren el rol Administrativo. La capa de datos usa ADO.NET con consultas parametrizadas y transacciones para las altas que crean usuario y perfil. No fue necesario modificar el esquema de `MediStackDB`.

Validación realizada: compilación completa de la solución y pruebas funcionales de búsqueda, edición, estados, altas de los seis módulos y rechazo claro al intentar borrar una cobertura vinculada.

## Fase 3 - Ficha, historial y agendas

Implementada sobre las relaciones que ya existen en `MediStackDB`, sin cambios de esquema:

- Ficha del paciente con información personal, contacto, obra social, afiliación, fecha de alta y porcentajes cubiertos por especialidad.
- Historial cronológico de turnos con profesional, especialidad, estado y los registros clínicos vinculados al turno.
- Historial de cobros consultado a través de los turnos del paciente.
- Acceso a la ficha para pacientes sobre su propia información, profesionales con turnos relacionados y personal administrativo.
- Agenda diaria de turnos y disponibilidad calculada a partir de `HorariosAtencion`, la duración de la especialidad y los turnos vigentes.
- Gestión administrativa de franjas semanales: alta, edición, baja/reactivación, prevención de superposiciones y protección de horarios que contienen turnos solicitados o confirmados.
- Vista de agenda propia, de solo lectura, para profesionales.

Validación realizada: compilación y precompilación de Web Forms; pruebas con datos de demostración para historial, cobros, permisos por rol, disponibilidad, altas y cambios de horarios. Los registros temporales usados para probar la agenda se retiraron.

## Fase 4 - Turnos

Implementada sobre el esquema existente de `MediStackDB`, sin migraciones:

- Solicitud de turnos para pacientes y para personal administrativo, con selección de paciente, profesional y especialidad según el rol.
- Disponibilidad calculada con la agenda activa, duración estándar de la especialidad y turnos solicitados/confirmados; muestra duración, estado y ocupante solo a los roles autorizados.
- Validación transaccional para impedir horarios fuera de agenda, superposiciones y más de un turno vigente del paciente por especialidad.
- Consulta de turnos limitada al paciente o profesional autenticado; el personal administrativo puede consultar y gestionar los turnos de la clínica.
- Confirmación administrativa, cancelación y reprogramación de turnos futuros; la reprogramación cancela el turno anterior y reserva el nuevo en una única transacción.
- Profesionales y personal administrativo pueden marcar como atendidos o ausentes los turnos confirmados una vez transcurrido el horario.

Validación realizada: rebuild de la solución, precompilación Web Forms, reserva y cancelación desde la página, reprogramación exitosa y rollback cuando el horario de destino está ocupado; rechazo de superposiciones, duplicados y horarios fuera de agenda desde la capa de negocio; comprobación de los alcances por rol. Los turnos creados como datos temporales de prueba se retiraron y no se modificó el esquema.

## Fase 5 - Cobros y Caja

Implementada sobre `Cobros`, `Turnos`, `Convenios` y `CierresCaja`, sin cambios de esquema:

- Personal administrativo puede registrar señas pendientes y consultas de turnos atendidos. Pacientes y profesionales consultan únicamente los cobros de sus propios turnos; solo el personal administrativo registra pagos y accede a Caja.
- El importe de consulta se calcula con el valor profesional-especialidad, la cobertura de un convenio vigente a la fecha del turno y la seña previamente pagada. Los importes recibidos, vuelto, medio de pago y relación con el turno se validan antes de persistir.
- La tabla `Cobros` impide más de un cobro por tipo y turno; el alta y la actualización del estado de seña se ejecutan en una transacción.
- Caja muestra los cobros como ingresos, totales por medio de pago y período, y los cierres existentes. Se puede cerrar el día una sola vez; el cierre calcula sus totales bajo transacción y, una vez cerrado, no admite nuevos cobros para ese día.
- El esquema no contiene egresos, movimientos de caja independientes, apertura formal ni observaciones por cobro. Por ello, la caja se considera abierta si no hay un registro en `CierresCaja`, el cierre es la operación diaria disponible y el estado de un cobro se deriva del estado de seña o de la consulta registrada.
- El script de datos de demostración crea un cierre para el día actual. En una base recién inicializada, Caja mostrará ese día como cerrado; el registro de cobros vuelve a habilitarse en el día siguiente. El cierre de demostración se conserva para no alterar los datos existentes.

Validación realizada: rebuild y precompilación de Web Forms; registro y consulta de señas y consultas; cálculo de cobertura/copago, validación de importe recibido y vuelto; rechazo de cobros duplicados y pagos posteriores al cierre; permisos de paciente, profesional y administración; totales por medio, conciliación y cierre diario. Se eliminaron los registros temporales y se restauró el cierre de demostración original de `MediStackDB`.

## Próximas etapas

- Liquidación de honorarios y reportes.
- Registro público y recuperación de contraseña.
- Pruebas integrales adicionales y revisión de accesibilidad.

## Etapa futura

El modulo de Inteligencia Artificial esta postergado y no forma parte de la aplicacion actual. La tabla `InteraccionesIA` del esquema SQL existente se conserva sin uso desde Web Forms; no se agregan paginas, logica ni dependencias de IA en esta etapa.

## Preparar para la presentacion

1. Preparar documentacion basica del proyecto     [ ]

2. Preparar una presentacion                     [ ]

3. Repartir partes y practicar                   [ ]
