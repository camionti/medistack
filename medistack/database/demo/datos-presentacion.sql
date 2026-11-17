/*
    MediStackDB - Datos para la presentacion (grabacion de clips)

    QUE HACE
    1. BORRA los datos de movimiento de la base: turnos, cobros, registros clinicos,
       reembolsos, cajas (aperturas, egresos y cierres), liquidaciones de honorarios y
       conversaciones con el asistente. NO borra usuarios, pacientes, profesionales,
       especialidades, obras sociales, convenios ni horarios.
       A las cuentas demo (admin y las de 01 y 08) les pone la contrasena MediStack2026!
       y las desbloquea, por si alguien la cambio o se bloqueo probando.
    2. Carga un escenario completo y realista para mostrar el sistema:
      - Historial de 60 dias (aprox. 200 turnos) con atenciones, ausentes, cancelaciones,
        cobros por todos los medios, registros clinicos y cierres de caja por dia.
        Sirve para que Reportes, Honorarios, Caja y Exportar muestren numeros con sentido.
      - La agenda de HOY armada alrededor de la hora en que se corre el script:
        atendidos, un ausente, un turno pasado sin resolver, pacientes en sala de espera
        y turnos que todavia no llegaron (entre ellos el de Ana Perez, con la sena pagada
        por Mercado Pago).
      - Senas a devolver en Reembolsos (Mercado Pago, transferencia y efectivo) y algunas
        devoluciones ya completadas en el historial.
      - La caja de hoy abierta, con un egreso.

    COMO USARLO
    Requisito: tener corridos los scripts 01 a 09 (incluido 08_DatosPrueba.sql).
      - DBeaver: abrir el archivo, Ctrl+A (seleccionar todo) y Ctrl+Enter.
      - SSMS: abrir el archivo y F5.
    Se puede correr todas las veces que haga falta: cada vez deja la base igual.
    Correrlo EL MISMO DIA en que se graba (mejor despues de las 11): la agenda de hoy
    se arma con la fecha y hora del momento en que se ejecuta.

    USUARIOS (contrasena de todos: MediStack2026!)
      admin            Administrativo (Paula Admin)
      ana.perez        Paciente, obra social Salud Union. Hoy tiene turno de Dermatologia
                       con la dra. Dominguez, sena pagada por Mercado Pago, todavia sin llegar.
      dra.dominguez    Profesional (Dermatologia). Hoy tiene a Florencia Castro en sala de
                       espera y despues a Ana.
      carla.ruiz       Paciente particular (sin obra social), para mostrar la sena en el mostrador.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

USE [MediStackDB];

-- ============================================================================
-- 0. VALIDACIONES
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE UsuarioId = '11111111-1111-4111-8111-111111111111')
    THROW 51899, N'No se encontro la cuenta admin creada por 01_MediStackDB.sql.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE NombreUsuario = N'dra.dominguez')
    THROW 51900, N'Falta correr 08_DatosPrueba.sql antes de este script.', 1;

IF COL_LENGTH(N'dbo.Turnos', N'FechaHoraLlegada') IS NULL
    THROW 51901, N'Falta correr 09_RecepcionSalaEspera.sql antes de este script.', 1;

IF COL_LENGTH(N'dbo.Turnos', N'TurnoReprogramadoDesdeId') IS NULL
    THROW 51902, N'Falta correr 07_ReprogramacionesAgenda.sql antes de este script.', 1;

BEGIN TRANSACTION;

-- ============================================================================
-- 0.b LIMPIEZA DE DATOS DE MOVIMIENTO (en orden, por las claves foraneas)
-- ============================================================================
DELETE FROM dbo.Reembolsos;
DELETE FROM dbo.Cobros;
DELETE FROM dbo.RegistrosClinicos;
UPDATE dbo.Turnos SET TurnoReprogramadoDesdeId = NULL WHERE TurnoReprogramadoDesdeId IS NOT NULL;
DELETE FROM dbo.Turnos;
DELETE FROM dbo.CierresCaja;
DELETE FROM dbo.Egresos;
DELETE FROM dbo.AperturasCaja;
DELETE FROM dbo.LiquidacionesHonorarios;
DELETE FROM dbo.InteraccionesIA;

-- ============================================================================
-- 0.c CUENTAS DEMO: misma contrasena para todas (MediStack2026!), activas y desbloqueadas
--     admin, ana.perez, dra.lopez, dr.fernandez y los usuarios de 08_DatosPrueba.
-- ============================================================================
UPDATE dbo.Usuarios
SET PasswordHash = N'PBKDF2-SHA256$120000$uQAATWSh0/U7vy2FX4WbPA==$4DtXmtzJXrYmDWXH9YW1HaP4X4gsnL6x5DA+1ClDyOk=',
    Activo = 1,
    IntentosFallidos = 0,
    BloqueadoHasta = NULL
WHERE CONVERT(NVARCHAR(36), UsuarioId) LIKE N'11111111-%'
   OR CONVERT(NVARCHAR(36), UsuarioId) LIKE N'22222222-%'
   OR CONVERT(NVARCHAR(36), UsuarioId) LIKE N'33333333-%'
   OR CONVERT(NVARCHAR(36), UsuarioId) LIKE N'44444444-%'
   OR CONVERT(NVARCHAR(36), UsuarioId) LIKE N'55555555-%';

-- ============================================================================
-- 1. REFERENCIAS
-- ============================================================================
DECLARE @AdminId UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'admin');

DECLARE @Hoy DATE = CONVERT(date, SYSDATETIME());

-- Hora base de la agenda de hoy: la hora actual redondeada a la media hora,
-- acotada entre las 11:00 y las 18:00 para que entren los turnos pasados y futuros.
DECLARE @MinutosAhora INT = DATEDIFF(minute, CONVERT(datetime2(0), @Hoy), SYSDATETIME());
SET @MinutosAhora = (@MinutosAhora / 30) * 30;
IF @MinutosAhora < 11 * 60 SET @MinutosAhora = 11 * 60;
IF @MinutosAhora > 18 * 60 SET @MinutosAhora = 18 * 60;
DECLARE @Base DATETIME2(0) = DATEADD(minute, @MinutosAhora, CONVERT(datetime2(0), @Hoy));

-- Profesionales con su especialidad principal
DECLARE @Docs TABLE (N INT IDENTITY(1,1), ProfesionalId UNIQUEIDENTIFIER, Usuario NVARCHAR(50), EspecialidadId INT, EspCodigo NVARCHAR(20));
INSERT INTO @Docs (ProfesionalId, Usuario, EspecialidadId, EspCodigo)
SELECT pe.ProfesionalId, u.NombreUsuario, pe.EspecialidadId, e.Codigo
FROM dbo.ProfesionalesEspecialidades pe
JOIN dbo.Usuarios u ON u.UsuarioId = pe.ProfesionalId
JOIN dbo.Especialidades e ON e.EspecialidadId = pe.EspecialidadId
WHERE pe.EsEspecialidadPrincipal = 1
  -- solo los profesionales de 01 y 08 (asi todos generan los mismos datos)
  AND (CONVERT(NVARCHAR(36), pe.ProfesionalId) LIKE N'33333333-%' OR CONVERT(NVARCHAR(36), pe.ProfesionalId) LIKE N'55555555-%')
ORDER BY u.NombreUsuario;
DECLARE @CantDocs INT = (SELECT COUNT(*) FROM @Docs);

-- Pacientes
DECLARE @Pacs TABLE (N INT IDENTITY(1,1), PacienteId UNIQUEIDENTIFIER, Usuario NVARCHAR(50));
INSERT INTO @Pacs (PacienteId, Usuario)
SELECT p.PacienteId, u.NombreUsuario
FROM dbo.Pacientes p JOIN dbo.Usuarios u ON u.UsuarioId = p.PacienteId
-- solo los pacientes de 01 y 08 (los que cada uno se haya registrado probando no entran)
WHERE CONVERT(NVARCHAR(36), p.PacienteId) LIKE N'22222222-%' OR CONVERT(NVARCHAR(36), p.PacienteId) LIKE N'44444444-%'
ORDER BY u.NombreUsuario;
DECLARE @CantPacs INT = (SELECT COUNT(*) FROM @Pacs);

-- Motivos y diagnosticos por especialidad (3 por especialidad)
DECLARE @Clinica TABLE (EspCodigo NVARCHAR(20), N INT, Motivo NVARCHAR(500), Diagnostico NVARCHAR(500));
INSERT INTO @Clinica VALUES
    (N'CARDIO', 0, N'Control de presion arterial', N'Hipertension arterial controlada. Continua con medicacion habitual.'),
    (N'CARDIO', 1, N'Palpitaciones ocasionales', N'Extrasistoles aisladas benignas. Se indica Holter de control.'),
    (N'CARDIO', 2, N'Apto fisico para actividad deportiva', N'Examen cardiovascular normal. Apto fisico otorgado.'),
    (N'DERMA', 0, N'Control de lunares', N'Nevos benignos. Control anual y fotoproteccion.'),
    (N'DERMA', 1, N'Acne persistente', N'Acne inflamatorio leve. Tratamiento topico por 8 semanas.'),
    (N'DERMA', 2, N'Picazon y enrojecimiento en manos', N'Dermatitis de contacto. Se indica crema con corticoide.'),
    (N'PED', 0, N'Control de nino sano', N'Crecimiento y desarrollo acordes a la edad.'),
    (N'PED', 1, N'Fiebre y dolor de garganta', N'Faringitis viral. Tratamiento sintomatico e hidratacion.'),
    (N'PED', 2, N'Certificado escolar', N'Apto fisico escolar otorgado. Vacunas al dia.'),
    (N'TRAUMA', 0, N'Dolor de rodilla al correr', N'Tendinitis rotuliana. Reposo deportivo y kinesiologia.'),
    (N'TRAUMA', 1, N'Dolor lumbar', N'Lumbalgia mecanica. Antiinflamatorios y ejercicios.'),
    (N'TRAUMA', 2, N'Control post esguince de tobillo', N'Buena evolucion. Alta progresiva a la actividad.'),
    (N'OFTALMO', 0, N'Vision borrosa de cerca', N'Presbicia. Se indican lentes para lectura.'),
    (N'OFTALMO', 1, N'Ardor y ojo rojo', N'Conjuntivitis alergica. Colirio antihistaminico.'),
    (N'OFTALMO', 2, N'Control visual anual', N'Agudeza visual normal. Fondo de ojos sin alteraciones.'),
    (N'GINEC', 0, N'Control ginecologico anual', N'Examen normal. PAP y ecografia solicitados.'),
    (N'GINEC', 1, N'Consulta por dolor menstrual', N'Dismenorrea primaria. Analgesicos y control.'),
    (N'GINEC', 2, N'Consulta preconcepcional', N'Se indica acido folico y laboratorio de rutina.'),
    (N'CLINICA', 0, N'Chequeo anual', N'Examen clinico normal. Laboratorio de rutina solicitado.'),
    (N'CLINICA', 1, N'Cuadro gripal', N'Infeccion respiratoria alta viral. Reposo y control.'),
    (N'CLINICA', 2, N'Control de colesterol', N'Dislipemia leve. Dieta y control en 3 meses.');

-- ============================================================================
-- 2. PLAN DE TURNOS
--    Cada fila es un turno. El bucle del punto 3 calcula cobertura, sena,
--    cobros, registros clinicos y reembolsos de forma consistente.
-- ============================================================================
DECLARE @Plan TABLE
(
    N INT IDENTITY(1,1),
    PacienteId UNIQUEIDENTIFIER NOT NULL,
    ProfesionalId UNIQUEIDENTIFIER NOT NULL,
    EspecialidadId INT NOT NULL,
    FechaHora DATETIME2(0) NOT NULL,
    Estado NVARCHAR(20) NOT NULL,
    Motivo NVARCHAR(500) NULL,
    Diagnostico NVARCHAR(500) NULL,
    Llegada DATETIME2(0) NULL,          -- para EnEspera
    EstadoSenaForzado NVARCHAR(20) NULL, -- ReembolsoPendiente / Reembolsada
    MedioSena NVARCHAR(20) NULL,
    MedioConsulta NVARCHAR(20) NULL,
    MotivoReembolso NVARCHAR(30) NULL
);

-- 2.a Historial: ultimos 60 dias, de lunes a viernes, 5 turnos por dia
DECLARE @d INT = -60;
WHILE @d <= -1
BEGIN
    DECLARE @Dia DATE = DATEADD(day, @d, @Hoy);
    IF DATEDIFF(day, CONVERT(date, '19000101'), @Dia) % 7 < 5   -- 0 = lunes ... 4 = viernes
    BEGIN
        DECLARE @k INT = 1;
        WHILE @k <= 5
        BEGIN
            DECLARE @DocN INT = ABS(CHECKSUM(@d, @k, N'doc')) % @CantDocs + 1;
            DECLARE @PacN INT = ABS(CHECKSUM(@d, @k, N'pac')) % @CantPacs + 1;
            DECLARE @Tiro INT = ABS(CHECKSUM(@d, @k, N'est')) % 100;
            DECLARE @EspCod NVARCHAR(20) = (SELECT EspCodigo FROM @Docs WHERE N = @DocN);
            DECLARE @ClinN INT = ABS(CHECKSUM(@d, @k, N'cli')) % 3;

            INSERT INTO @Plan (PacienteId, ProfesionalId, EspecialidadId, FechaHora, Estado, Motivo, Diagnostico)
            SELECT p.PacienteId, doc.ProfesionalId, doc.EspecialidadId,
                   DATEADD(minute, (6 + 2 * @k) * 60 + (ABS(@d) % 2) * 30, CONVERT(datetime2(0), @Dia)),
                   CASE WHEN @Tiro < 72 THEN N'Atendido' WHEN @Tiro < 84 THEN N'Ausente' ELSE N'Cancelado' END,
                   c.Motivo, c.Diagnostico
            FROM @Docs doc
            CROSS JOIN @Pacs p
            JOIN @Clinica c ON c.EspCodigo = @EspCod AND c.N = @ClinN
            WHERE doc.N = @DocN AND p.N = @PacN;

            SET @k = @k + 1;
        END;
    END;
    SET @d = @d + 1;
END;

-- Pacientes y profesionales de los escenarios
DECLARE @Ana UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'ana.perez');
DECLARE @PSantiago UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'santiago.morales');
DECLARE @PLuciana UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'luciana.sosa');
DECLARE @PJoaquin UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'joaquin.torres');
DECLARE @PAgustina UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'agustina.alvarez');
DECLARE @PNicolas UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'nicolas.medina');
DECLARE @PFlorencia UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'florencia.castro');
DECLARE @PTomas UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'tomas.ortiz');
DECLARE @PCamila UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'camila.silva');
DECLARE @PMicaela UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'micaela.suarez');
DECLARE @PJulieta UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'julieta.acuna');
DECLARE @PFelipe UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'felipe.mendez');
DECLARE @PFranco UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'franco.dominguez');
DECLARE @PSofia UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'sofia.navarro');
DECLARE @PBruno UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'bruno.rossi');
DECLARE @PCatalina UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'catalina.vargas');
DECLARE @PMartina UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'martina.guerrero');

DECLARE @DLopez UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'dra.lopez');
DECLARE @DFernandez UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'dr.fernandez');
DECLARE @DAlvarez UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'dr.alvarez');
DECLARE @DBenitez UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'dra.benitez');
DECLARE @DCastro UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'dr.castro');
DECLARE @DDominguez UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'dra.dominguez');
DECLARE @DEspinosa UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'dr.espinosa');
DECLARE @DFlores UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'dra.flores');
DECLARE @DGutierrez UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'dr.gutierrez');
DECLARE @DHerrera UNIQUEIDENTIFIER = (SELECT UsuarioId FROM dbo.Usuarios WHERE NombreUsuario = N'dra.herrera');

DECLARE @Cardio INT = (SELECT EspecialidadId FROM dbo.Especialidades WHERE Codigo = N'CARDIO');
DECLARE @Derma INT = (SELECT EspecialidadId FROM dbo.Especialidades WHERE Codigo = N'DERMA');
DECLARE @Ped INT = (SELECT EspecialidadId FROM dbo.Especialidades WHERE Codigo = N'PED');
DECLARE @Trauma INT = (SELECT EspecialidadId FROM dbo.Especialidades WHERE Codigo = N'TRAUMA');
DECLARE @Oftalmo INT = (SELECT EspecialidadId FROM dbo.Especialidades WHERE Codigo = N'OFTALMO');
DECLARE @Ginec INT = (SELECT EspecialidadId FROM dbo.Especialidades WHERE Codigo = N'GINEC');
DECLARE @ClinicaEsp INT = (SELECT EspecialidadId FROM dbo.Especialidades WHERE Codigo = N'CLINICA');

-- 2.b Historial propio de Ana (para Mis turnos, Mi ficha y el asistente)
INSERT INTO @Plan (PacienteId, ProfesionalId, EspecialidadId, FechaHora, Estado, Motivo, Diagnostico, MedioSena, MedioConsulta)
VALUES
    (@Ana, @DLopez, @Cardio, DATEADD(hour, 9, CONVERT(datetime2(0), DATEADD(day, -45, @Hoy))), N'Atendido',
     N'Control de presion arterial', N'Presion arterial en rango normal. Control en 6 meses.', NULL, N'Tarjeta'),
    (@Ana, @DGutierrez, @ClinicaEsp, DATEADD(minute, 630, CONVERT(datetime2(0), DATEADD(day, -18, @Hoy))), N'Atendido',
     N'Chequeo anual', N'Examen clinico normal. Laboratorio de rutina solicitado.', NULL, N'Efectivo');

-- 2.c Agenda de HOY (alrededor de la hora en que se corre el script)
INSERT INTO @Plan (PacienteId, ProfesionalId, EspecialidadId, FechaHora, Estado, Motivo, Diagnostico, Llegada, MedioSena, MedioConsulta)
VALUES
    -- Ya atendidos hoy (dan movimiento a la caja del dia)
    (@PJulieta,   @DAlvarez,   @Cardio,     DATEADD(minute, -180, @Base), N'Atendido', N'Control de presion arterial', N'Hipertension arterial controlada. Continua con medicacion habitual.', NULL, NULL, N'Efectivo'),
    (@PNicolas,   @DCastro,    @Trauma,     DATEADD(minute, -150, @Base), N'Atendido', N'Dolor lumbar', N'Lumbalgia mecanica. Antiinflamatorios y ejercicios.', NULL, N'Transferencia', N'Tarjeta'),
    (@PLuciana,   @DGutierrez, @ClinicaEsp, DATEADD(minute, -90,  @Base), N'Atendido', N'Cuadro gripal', N'Infeccion respiratoria alta viral. Reposo y control.', NULL, NULL, N'Efectivo'),
    -- No vino (sena retenida)
    (@PFranco,    @DBenitez,   @Ped,        DATEADD(minute, -120, @Base), N'Ausente',  N'Control de nino sano', NULL, NULL, N'Efectivo', NULL),
    -- Hora pasada sin resolver (aparece en "Turnos de horas pasadas sin resolver")
    (@PTomas,     @DAlvarez,   @Cardio,     DATEADD(minute, -60,  @Base), N'Confirmado', N'Palpitaciones ocasionales', NULL, NULL, NULL, NULL),
    -- En sala de espera
    (@PCamila,    @DFernandez, @Derma,      DATEADD(minute, -30,  @Base), N'EnEspera', N'Acne persistente', NULL, DATEADD(minute, -45, @Base), NULL, NULL),
    (@PFlorencia, @DDominguez, @Derma,      @Base,                        N'EnEspera', N'Control de lunares', NULL, DATEADD(minute, -10, @Base), NULL, NULL),
    (@PAgustina,  @DHerrera,   @Ped,        @Base,                        N'EnEspera', N'Certificado escolar', NULL, DATEADD(minute, -5, @Base), NULL, NULL),
    -- Todavia no llegaron
    (@Ana,        @DDominguez, @Derma,      DATEADD(minute, 30,  @Base),  N'Confirmado', N'Control de lunar que cambio de color', NULL, NULL, N'MercadoPago', NULL),
    (@PSantiago,  @DAlvarez,   @Cardio,     DATEADD(minute, 30,  @Base),  N'Confirmado', N'Apto fisico para actividad deportiva', NULL, NULL, NULL, NULL),
    (@PBruno,     @DFlores,    @Ginec,      DATEADD(minute, 60,  @Base),  N'Confirmado', N'Control ginecologico anual', NULL, NULL, NULL, NULL),
    (@PMicaela,   @DEspinosa,  @Oftalmo,    DATEADD(minute, 90,  @Base),  N'Confirmado', N'Vision borrosa de cerca', NULL, NULL, N'Efectivo', NULL),
    (@PJoaquin,   @DCastro,    @Trauma,     DATEADD(minute, 120, @Base),  N'Confirmado', N'Dolor de rodilla al correr', NULL, NULL, N'Tarjeta', NULL);

-- 2.d Senas a devolver (turnos futuros cancelados) y devoluciones ya hechas
INSERT INTO @Plan (PacienteId, ProfesionalId, EspecialidadId, FechaHora, Estado, Motivo, EstadoSenaForzado, MedioSena, MotivoReembolso)
VALUES
    (@PSofia,    @DFlores,   @Ginec,   DATEADD(hour, 10, CONVERT(datetime2(0), DATEADD(day, 3, @Hoy))),   N'Cancelado', N'Consulta por dolor menstrual', N'ReembolsoPendiente', N'MercadoPago',   N'CancelacionClinica'),
    (@PCatalina, @DEspinosa, @Oftalmo, DATEADD(hour, 11, CONVERT(datetime2(0), DATEADD(day, 5, @Hoy))),   N'Cancelado', N'Ardor y ojo rojo',             N'ReembolsoPendiente', N'Transferencia', N'CancelacionClinica'),
    (@PFelipe,   @DFernandez, @Derma,  DATEADD(hour, 9,  CONVERT(datetime2(0), DATEADD(day, 2, @Hoy))),   N'Cancelado', N'Picazon y enrojecimiento en manos', N'ReembolsoPendiente', N'Efectivo', N'CancelacionPaciente'),
    (@PMartina,  @DCastro,   @Trauma,  DATEADD(hour, 15, CONVERT(datetime2(0), DATEADD(day, -12, @Hoy))), N'Cancelado', N'Dolor lumbar',                 N'Reembolsada',        N'MercadoPago',   N'CancelacionClinica'),
    (@PNicolas,  @DFlores,   @Ginec,   DATEADD(hour, 16, CONVERT(datetime2(0), DATEADD(day, -26, @Hoy))), N'Cancelado', N'Consulta preconcepcional',     N'Reembolsada',        N'Efectivo',      N'CancelacionClinica');

-- ============================================================================
-- 3. INSERCION DE TURNOS, COBROS, REGISTROS CLINICOS Y REEMBOLSOS
-- ============================================================================
DECLARE @i INT = 1, @Total INT = (SELECT COUNT(*) FROM @Plan);
WHILE @i <= @Total
BEGIN
    DECLARE @pac UNIQUEIDENTIFIER, @doc UNIQUEIDENTIFIER, @esp INT, @fh DATETIME2(0), @estado NVARCHAR(20),
            @motivo NVARCHAR(500), @diag NVARCHAR(500), @llegada DATETIME2(0), @senaForzada NVARCHAR(20),
            @medioSena NVARCHAR(20), @medioConsulta NVARCHAR(20), @motivoReemb NVARCHAR(30);

    SELECT @pac = PacienteId, @doc = ProfesionalId, @esp = EspecialidadId, @fh = FechaHora, @estado = Estado,
           @motivo = Motivo, @diag = Diagnostico, @llegada = Llegada, @senaForzada = EstadoSenaForzado,
           @medioSena = MedioSena, @medioConsulta = MedioConsulta, @motivoReemb = MotivoReembolso
    FROM @Plan WHERE N = @i;

    -- Valor de la consulta y cobertura (solo si hay convenio vigente con la obra social del paciente)
    DECLARE @valor DECIMAL(12,2) = (SELECT ValorConsulta FROM dbo.ProfesionalesEspecialidades WHERE ProfesionalId = @doc AND EspecialidadId = @esp);
    DECLARE @os INT = (SELECT ObraSocialId FROM dbo.Pacientes WHERE PacienteId = @pac);
    DECLARE @pct DECIMAL(5,2) = NULL;
    IF @os IS NOT NULL AND EXISTS (
        SELECT 1 FROM dbo.Convenios c
        WHERE c.ProfesionalId = @doc AND c.EspecialidadId = @esp AND c.ObraSocialId = @os AND c.Activo = 1
          AND c.FechaDesde <= CONVERT(date, @fh) AND (c.FechaHasta IS NULL OR c.FechaHasta >= CONVERT(date, @fh)))
        SET @pct = (SELECT PorcentajeCobertura FROM dbo.CoberturasEspecialidades WHERE ObraSocialId = @os AND EspecialidadId = @esp);

    -- Sena: 50% del valor cuando no hay cobertura
    DECLARE @montoSena DECIMAL(12,2) = CASE WHEN @pct IS NULL THEN ROUND(@valor * 0.5, 0) ELSE 0 END;
    DECLARE @estadoSena NVARCHAR(20) =
        CASE
            WHEN @montoSena = 0 THEN N'NoRequerida'
            WHEN @senaForzada IS NOT NULL THEN @senaForzada
            WHEN @estado = N'Ausente' THEN N'Retenida'
            WHEN @estado = N'Cancelado' THEN N'Vencida'
            ELSE N'Pagada'
        END;

    IF @montoSena > 0 AND @medioSena IS NULL
        SET @medioSena = CASE ABS(CHECKSUM(@i, N'ms')) % 20 WHEN 0 THEN N'Transferencia' WHEN 1 THEN N'Transferencia'
                              WHEN 2 THEN N'Efectivo' WHEN 3 THEN N'Efectivo' WHEN 4 THEN N'Efectivo' WHEN 5 THEN N'Efectivo'
                              ELSE N'MercadoPago' END;
    IF @medioConsulta IS NULL
        SET @medioConsulta = CASE ABS(CHECKSUM(@i, N'mc')) % 20 WHEN 0 THEN N'Transferencia' WHEN 1 THEN N'Transferencia'
                                  WHEN 2 THEN N'Transferencia' WHEN 3 THEN N'Tarjeta' WHEN 4 THEN N'Tarjeta' WHEN 5 THEN N'Tarjeta'
                                  WHEN 6 THEN N'Tarjeta' WHEN 7 THEN N'Tarjeta' WHEN 8 THEN N'Tarjeta' WHEN 9 THEN N'Tarjeta'
                                  ELSE N'Efectivo' END;

    -- Online (Mercado Pago) se paga la noche anterior; en el mostrador, tres dias antes
    DECLARE @fhSena DATETIME2(0) = CASE WHEN @medioSena = N'MercadoPago'
                                        THEN DATEADD(minute, 21 * 60 + ABS(CHECKSUM(@i, N'h')) % 120, CONVERT(datetime2(0), DATEADD(day, -1, CONVERT(date, @fh))))
                                        ELSE DATEADD(minute, 10 * 60 + ABS(CHECKSUM(@i, N'h')) % 300, CONVERT(datetime2(0), DATEADD(day, -3, CONVERT(date, @fh)))) END;
    IF @fhSena > SYSDATETIME() SET @fhSena = DATEADD(hour, -1, SYSDATETIME());

    INSERT INTO dbo.Turnos (PacienteId, ProfesionalId, EspecialidadId, FechaHora, Estado, Motivo, MontoSena, EstadoSena, FechaHoraSena, FechaHoraLlegada)
    VALUES (@pac, @doc, @esp, @fh, @estado, @motivo, @montoSena, @estadoSena,
            CASE WHEN @estadoSena IN (N'Pagada', N'Retenida', N'ReembolsoPendiente', N'Reembolsada') THEN @fhSena END,
            CASE WHEN @estado = N'EnEspera' THEN @llegada
                 WHEN @estado = N'Atendido' THEN DATEADD(minute, -15, @fh) END);
    DECLARE @turnoId INT = SCOPE_IDENTITY();

    -- Cobro de la sena
    DECLARE @cobroSenaId INT = NULL;
    IF @estadoSena IN (N'Pagada', N'Retenida', N'ReembolsoPendiente', N'Reembolsada')
    BEGIN
        INSERT INTO dbo.Cobros (TurnoId, TipoCobro, MontoBase, MontoObraSocial, Copago, SenaDescontada, MontoCobrado,
                                MedioPago, MontoRecibido, Vuelto, FechaHoraCobro, UsuarioCobradorId, MercadoPagoPagoId)
        VALUES (@turnoId, N'Sena', 0, 0, 0, 0, @montoSena, @medioSena, @montoSena, 0, @fhSena,
                CASE WHEN @medioSena = N'MercadoPago' THEN NULL ELSE @AdminId END,
                CASE WHEN @medioSena = N'MercadoPago' THEN CONCAT(N'DEMO-', 1350000000 + @turnoId) END);
        SET @cobroSenaId = SCOPE_IDENTITY();
    END;

    -- Cobro de la consulta, registro clinico (turnos atendidos)
    IF @estado = N'Atendido'
    BEGIN
        DECLARE @montoOS DECIMAL(12,2) = CASE WHEN @pct IS NULL THEN 0 ELSE ROUND(@valor * @pct / 100, 0) END;
        DECLARE @copago DECIMAL(12,2) = @valor - @montoOS;
        DECLARE @cobrado DECIMAL(12,2) = @copago - @montoSena;
        DECLARE @recibido DECIMAL(12,2) = CASE WHEN @medioConsulta = N'Efectivo' THEN CEILING(@cobrado / 1000) * 1000 ELSE @cobrado END;

        INSERT INTO dbo.Cobros (TurnoId, TipoCobro, MontoBase, MontoObraSocial, Copago, SenaDescontada, MontoCobrado,
                                MedioPago, MontoRecibido, Vuelto, FechaHoraCobro, UsuarioCobradorId)
        VALUES (@turnoId, N'Consulta', @valor, @montoOS, @copago, @montoSena, @cobrado,
                @medioConsulta, @recibido, @recibido - @cobrado, DATEADD(minute, -10, @fh), @AdminId);

        INSERT INTO dbo.RegistrosClinicos (TurnoId, FechaRegistro, MotivoConsulta, Diagnostico, Observaciones)
        VALUES (@turnoId, DATEADD(minute, 25, @fh), @motivo, @diag, N'Se explican pautas de alarma. Control segun evolucion.');
    END;

    -- Reembolsos
    IF @estadoSena IN (N'ReembolsoPendiente', N'Reembolsada')
    BEGIN
        DECLARE @fhSolicitud DATETIME2(0) = CASE WHEN @estadoSena = N'ReembolsoPendiente'
                                                 THEN DATEADD(hour, -3, SYSDATETIME())
                                                 ELSE DATEADD(day, -2, @fh) END;
        DECLARE @egresoId INT = NULL;
        IF @estadoSena = N'Reembolsada' AND @medioSena = N'Efectivo'
        BEGIN
            INSERT INTO dbo.Egresos (Fecha, FechaHoraRegistro, Concepto, Monto, Categoria, Comprobante, AdministrativoId)
            VALUES (CONVERT(date, DATEADD(day, 1, @fhSolicitud)), DATEADD(day, 1, @fhSolicitud),
                    CONCAT(N'Devolucion de sena del turno ', @turnoId), @montoSena, N'Reembolso', NULL, @AdminId);
            SET @egresoId = SCOPE_IDENTITY();
        END;

        INSERT INTO dbo.Reembolsos (TurnoId, CobroId, Monto, MedioPago, Motivo, Estado, FechaHoraSolicitud, SolicitadoPorId,
                                    FechaHoraCompletado, CompletadoPorId, MercadoPagoReembolsoId, EgresoId, Comprobante)
        VALUES (@turnoId, @cobroSenaId, @montoSena, @medioSena, @motivoReemb,
                CASE WHEN @estadoSena = N'Reembolsada' THEN N'Completado' ELSE N'Pendiente' END,
                @fhSolicitud, @AdminId,
                CASE WHEN @estadoSena = N'Reembolsada' THEN DATEADD(day, 1, @fhSolicitud) END,
                CASE WHEN @estadoSena = N'Reembolsada' THEN @AdminId END,
                CASE WHEN @estadoSena = N'Reembolsada' AND @medioSena = N'MercadoPago' THEN CONCAT(N'DEMO-REF-', @turnoId) END,
                @egresoId,
                CASE WHEN @estadoSena = N'Reembolsada' AND @medioSena IN (N'Transferencia', N'Tarjeta') THEN CONCAT(N'TRF-', @turnoId) END);
    END;

    SET @i = @i + 1;
END;

-- ============================================================================
-- 4. CAJA: aperturas, egresos y cierres de cada dia habil del historial
-- ============================================================================
DECLARE @Conceptos TABLE (N INT, Concepto NVARCHAR(200), Monto DECIMAL(12,2), Categoria NVARCHAR(20));
INSERT INTO @Conceptos VALUES
    (0, N'Compra de resmas y toner', 6200, N'Insumos'),
    (1, N'Guantes, alcohol y descartables', 8400, N'Insumos'),
    (2, N'Cadeteria: envio de muestras al laboratorio', 3500, N'Viaticos'),
    (3, N'Arreglo de la camilla del consultorio 2', 12000, N'Mantenimiento'),
    (4, N'Cafe, azucar y vasos para sala de espera', 4100, N'Otros');

-- Se recalculan todos los cierres pasados (08 deja algunos calculados antes de este historial)
DELETE FROM dbo.CierresCaja WHERE Fecha >= DATEADD(day, -60, @Hoy) AND Fecha < @Hoy;

SET @d = -60;
WHILE @d <= 0
BEGIN
    DECLARE @Fecha DATE = DATEADD(day, @d, @Hoy);
    IF DATEDIFF(day, CONVERT(date, '19000101'), @Fecha) % 7 < 5
    BEGIN
        DECLARE @Fondo DECIMAL(12,2) = 20000 + (ABS(CHECKSUM(@d, N'fondo')) % 3) * 2500;

        IF NOT EXISTS (SELECT 1 FROM dbo.AperturasCaja WHERE Fecha = @Fecha)
            INSERT INTO dbo.AperturasCaja (Fecha, FondoInicial, AdministrativoId, FechaHoraApertura)
            VALUES (@Fecha, @Fondo, @AdminId, DATEADD(minute, 7 * 60 + 40, CONVERT(datetime2(0), @Fecha)));
        SET @Fondo = (SELECT FondoInicial FROM dbo.AperturasCaja WHERE Fecha = @Fecha);

        -- Un egreso cada dos dias habiles mas o menos (y siempre uno hoy)
        DECLARE @Eg INT = ABS(CHECKSUM(@d, N'eg')) % 10;
        IF (@Eg < 5 OR @d = 0)
           AND NOT EXISTS (SELECT 1 FROM dbo.Egresos WHERE Fecha = @Fecha AND Categoria <> N'Reembolso' AND AdministrativoId = @AdminId
                           AND Concepto IN (SELECT Concepto FROM @Conceptos))
            INSERT INTO dbo.Egresos (Fecha, FechaHoraRegistro, Concepto, Monto, Categoria, Comprobante, AdministrativoId)
            SELECT @Fecha, DATEADD(minute, 12 * 60 + @Eg * 7, CONVERT(datetime2(0), @Fecha)), Concepto, Monto, Categoria,
                   CONCAT(N'FAC-B-0003-', 4000 + ABS(@d) * 7 + @Eg), @AdminId
            FROM @Conceptos WHERE N = @Eg % 5;

        -- Cierre (no para hoy: la caja de hoy queda abierta para mostrarla en el clip)
        IF @d < 0 AND NOT EXISTS (SELECT 1 FROM dbo.CierresCaja WHERE Fecha = @Fecha)
        BEGIN
            DECLARE @Ef DECIMAL(12,2) = ISNULL((SELECT SUM(MontoCobrado) FROM dbo.Cobros WHERE CONVERT(date, FechaHoraCobro) = @Fecha AND MedioPago = N'Efectivo'), 0);
            DECLARE @Tj DECIMAL(12,2) = ISNULL((SELECT SUM(MontoCobrado) FROM dbo.Cobros WHERE CONVERT(date, FechaHoraCobro) = @Fecha AND MedioPago = N'Tarjeta'), 0);
            DECLARE @Tr DECIMAL(12,2) = ISNULL((SELECT SUM(MontoCobrado) FROM dbo.Cobros WHERE CONVERT(date, FechaHoraCobro) = @Fecha AND MedioPago IN (N'Transferencia', N'MercadoPago')), 0);
            DECLARE @Cant INT = (SELECT COUNT(*) FROM dbo.Cobros WHERE CONVERT(date, FechaHoraCobro) = @Fecha);
            DECLARE @TotEg DECIMAL(12,2) = ISNULL((SELECT SUM(Monto) FROM dbo.Egresos WHERE Fecha = @Fecha), 0);
            DECLARE @Esperado DECIMAL(12,2) = @Fondo + @Ef - @TotEg;
            -- Casi siempre cierra justo; algunos dias falta o sobra un poco
            DECLARE @Dif DECIMAL(12,2) = CASE ABS(CHECKSUM(@d, N'dif')) % 8 WHEN 0 THEN -500 WHEN 1 THEN 200 ELSE 0 END;
            IF @Esperado + @Dif < 0 SET @Dif = 0;

            INSERT INTO dbo.CierresCaja (Fecha, AdministrativoId, TotalEfectivo, TotalTarjeta, TotalTransferencia, TotalGeneral,
                                         CantidadCobros, EfectivoContado, Diferencia, FechaHoraCierre, TotalEgresos, FondoInicial)
            VALUES (@Fecha, @AdminId, @Ef, @Tj, @Tr, @Ef + @Tj + @Tr, @Cant, @Esperado + @Dif, @Dif,
                    DATEADD(minute, 19 * 60 + 15, CONVERT(datetime2(0), @Fecha)), @TotEg, @Fondo);
        END;
    END;
    SET @d = @d + 1;
END;

-- Por si hoy es sabado o domingo: igual se abre la caja para poder grabar
IF NOT EXISTS (SELECT 1 FROM dbo.AperturasCaja WHERE Fecha = @Hoy)
    INSERT INTO dbo.AperturasCaja (Fecha, FondoInicial, AdministrativoId, FechaHoraApertura)
    VALUES (@Hoy, 25000, @AdminId, DATEADD(minute, 7 * 60 + 40, CONVERT(datetime2(0), @Hoy)));

COMMIT TRANSACTION;

-- ============================================================================
-- 5. RESUMEN
-- ============================================================================
SELECT
    (SELECT COUNT(*) FROM dbo.Turnos) AS Turnos,
    (SELECT COUNT(*) FROM dbo.Turnos WHERE CONVERT(date, FechaHora) = CONVERT(date, SYSDATETIME())) AS TurnosHoy,
    (SELECT COUNT(*) FROM dbo.Turnos WHERE Estado = N'EnEspera') AS EnSalaDeEspera,
    (SELECT COUNT(*) FROM dbo.Cobros) AS Cobros,
    (SELECT COUNT(*) FROM dbo.RegistrosClinicos) AS RegistrosClinicos,
    (SELECT COUNT(*) FROM dbo.Reembolsos WHERE Estado = N'Pendiente') AS ReembolsosPendientes,
    (SELECT COUNT(*) FROM dbo.CierresCaja) AS CierresCaja,
    (SELECT COUNT(*) FROM dbo.Egresos) AS Egresos;
