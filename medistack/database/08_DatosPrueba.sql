/*
    MediStackDB - Migracion 08: datos de prueba e integracion (seed mock)
    Ejecutar sobre MediStackDB tras aplicar las migraciones 01 a 07.

    Como ejecutarlo:
      - DBeaver: Seleccionar todo (Ctrl+A) y ejecutar como script (Alt+X o Ctrl+Enter).
      - SSMS: F5.
    Es estrictamente idempotente: puede ejecutarse reiteradas veces sin duplicar registros ni fallar.

    Contenido incorporado:
    - 4 especialidades adicionales (Traumatologia, Oftalmologia, Ginecologia, Medicina General).
    - 8 profesionales medicos con matricula, usuarios, especialidad asignada, convenios y horarios de atencion.
    - 20 pacientes con cobertura en obras sociales (o particulares) y datos de contacto.
    - Todos los nuevos usuarios comparten la contrasena demo: MediStack2026!
    - Turnos distribuidos entre 1 mes hacia el pasado y 1 mes hacia el futuro (al menos un turno por cada paciente).
    - Registros clinicos de atencion para consultas pasadas.
    - Cobros de senas y de consultas (Efectivo, Tarjeta, Transferencia y Mercado Pago).
    - Ciclos de caja: aperturas de caja con fondo inicial, egresos operativos y cierres de caja balanceados.
    - Apertura de caja para la fecha actual (si aun no existiera).
*/

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

USE [MediStackDB];

-- ============================================================================
-- 1. ROLES Y USUARIO ADMINISTRATIVO EXISTENTE
-- ============================================================================
DECLARE @RolPaciente INT = (SELECT RolId FROM dbo.Roles WHERE Codigo = N'PACIENTE');
DECLARE @RolProfesional INT = (SELECT RolId FROM dbo.Roles WHERE Codigo = N'PROFESIONAL');
DECLARE @RolAdministrativo INT = (SELECT RolId FROM dbo.Roles WHERE Codigo = N'ADMINISTRATIVO');

DECLARE @AdminId UNIQUEIDENTIFIER = (SELECT TOP (1) UsuarioId FROM dbo.Usuarios WHERE RolId = @RolAdministrativo ORDER BY FechaRegistro ASC);
IF @AdminId IS NULL
    SET @AdminId = '11111111-1111-4111-8111-111111111111';

-- Hash para 'MediStack2026!'
DECLARE @PasswordHashDemo NVARCHAR(256) = N'PBKDF2-SHA256$120000$uQAATWSh0/U7vy2FX4WbPA==$4DtXmtzJXrYmDWXH9YW1HaP4X4gsnL6x5DA+1ClDyOk=';

-- ============================================================================
-- 2. OBRAS SOCIALES Y ESPECIALIDADES
-- ============================================================================
DECLARE @ObraSocial1 INT = (SELECT ObraSocialId FROM dbo.ObrasSociales WHERE CodigoCUIT = N'30-70000001-1');
DECLARE @ObraSocial2 INT = (SELECT ObraSocialId FROM dbo.ObrasSociales WHERE CodigoCUIT = N'30-70000002-2');

IF NOT EXISTS (SELECT 1 FROM dbo.Especialidades WHERE Codigo = N'TRAUMA')
    INSERT INTO dbo.Especialidades (Codigo, Nombre, Descripcion, DuracionEstandarMinutos, Activa)
    VALUES (N'TRAUMA', N'Traumatologia', N'Atencion y seguimiento de lesiones osteoarticulares.', 30, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Especialidades WHERE Codigo = N'OFTALMO')
    INSERT INTO dbo.Especialidades (Codigo, Nombre, Descripcion, DuracionEstandarMinutos, Activa)
    VALUES (N'OFTALMO', N'Oftalmologia', N'Salud visual, fondo de ojos y agudeza visual.', 30, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Especialidades WHERE Codigo = N'GINEC')
    INSERT INTO dbo.Especialidades (Codigo, Nombre, Descripcion, DuracionEstandarMinutos, Activa)
    VALUES (N'GINEC', N'Ginecologia', N'Control ginecologico y salud reproductiva.', 30, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Especialidades WHERE Codigo = N'CLINICA')
    INSERT INTO dbo.Especialidades (Codigo, Nombre, Descripcion, DuracionEstandarMinutos, Activa)
    VALUES (N'CLINICA', N'Medicina General', N'Atencion clinica primaria y medicina preventiva.', 30, 1);

DECLARE @CardioId INT = (SELECT EspecialidadId FROM dbo.Especialidades WHERE Codigo = N'CARDIO');
DECLARE @DermaId INT = (SELECT EspecialidadId FROM dbo.Especialidades WHERE Codigo = N'DERMA');
DECLARE @PedId INT = (SELECT EspecialidadId FROM dbo.Especialidades WHERE Codigo = N'PED');
DECLARE @TraumaId INT = (SELECT EspecialidadId FROM dbo.Especialidades WHERE Codigo = N'TRAUMA');
DECLARE @OftalmoId INT = (SELECT EspecialidadId FROM dbo.Especialidades WHERE Codigo = N'OFTALMO');
DECLARE @GinecId INT = (SELECT EspecialidadId FROM dbo.Especialidades WHERE Codigo = N'GINEC');
DECLARE @ClinicaId INT = (SELECT EspecialidadId FROM dbo.Especialidades WHERE Codigo = N'CLINICA');

-- Coberturas de obras sociales para especialidades nuevas
IF @ObraSocial1 IS NOT NULL AND @TraumaId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.CoberturasEspecialidades WHERE ObraSocialId = @ObraSocial1 AND EspecialidadId = @TraumaId)
    INSERT INTO dbo.CoberturasEspecialidades (ObraSocialId, EspecialidadId, PorcentajeCobertura) VALUES (@ObraSocial1, @TraumaId, 60.00);

IF @ObraSocial1 IS NOT NULL AND @ClinicaId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.CoberturasEspecialidades WHERE ObraSocialId = @ObraSocial1 AND EspecialidadId = @ClinicaId)
    INSERT INTO dbo.CoberturasEspecialidades (ObraSocialId, EspecialidadId, PorcentajeCobertura) VALUES (@ObraSocial1, @ClinicaId, 80.00);

IF @ObraSocial2 IS NOT NULL AND @OftalmoId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.CoberturasEspecialidades WHERE ObraSocialId = @ObraSocial2 AND EspecialidadId = @OftalmoId)
    INSERT INTO dbo.CoberturasEspecialidades (ObraSocialId, EspecialidadId, PorcentajeCobertura) VALUES (@ObraSocial2, @OftalmoId, 50.00);

IF @ObraSocial2 IS NOT NULL AND @GinecId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.CoberturasEspecialidades WHERE ObraSocialId = @ObraSocial2 AND EspecialidadId = @GinecId)
    INSERT INTO dbo.CoberturasEspecialidades (ObraSocialId, EspecialidadId, PorcentajeCobertura) VALUES (@ObraSocial2, @GinecId, 70.00);

-- ============================================================================
-- 3. 8 PROFESIONALES (DOCTORES)
-- ============================================================================
DECLARE @Doc1 UNIQUEIDENTIFIER = '55555555-5555-4555-8555-000000000001';
DECLARE @Doc2 UNIQUEIDENTIFIER = '55555555-5555-4555-8555-000000000002';
DECLARE @Doc3 UNIQUEIDENTIFIER = '55555555-5555-4555-8555-000000000003';
DECLARE @Doc4 UNIQUEIDENTIFIER = '55555555-5555-4555-8555-000000000004';
DECLARE @Doc5 UNIQUEIDENTIFIER = '55555555-5555-4555-8555-000000000005';
DECLARE @Doc6 UNIQUEIDENTIFIER = '55555555-5555-4555-8555-000000000006';
DECLARE @Doc7 UNIQUEIDENTIFIER = '55555555-5555-4555-8555-000000000007';
DECLARE @Doc8 UNIQUEIDENTIFIER = '55555555-5555-4555-8555-000000000008';

-- Tabla temporal con definicion de los 8 doctores
DECLARE @DoctoresDef TABLE
(
    Id UNIQUEIDENTIFIER,
    Usuario NVARCHAR(50),
    Nombre NVARCHAR(100),
    Apellido NVARCHAR(100),
    Dni NVARCHAR(20),
    Nacimiento DATE,
    Email NVARCHAR(256),
    Telefono NVARCHAR(30),
    Matricula NVARCHAR(50),
    EspecialidadId INT,
    ValorConsulta DECIMAL(12,2),
    EsquemaTipo NVARCHAR(20),
    EsquemaValor DECIMAL(7,2)
);

INSERT INTO @DoctoresDef VALUES
    (@Doc1, N'dr.alvarez', N'Julian', N'Alvarez', N'27101101', '1982-05-14', N'julian.alvarez@medistack.test', N'1144001001', N'MN-61201', @CardioId, 32000.00, N'Porcentaje', 70.00),
    (@Doc2, N'dra.benitez', N'Mariana', N'Benitez', N'27101102', '1984-08-22', N'mariana.benitez@medistack.test', N'1144001002', N'MN-61202', @PedId, 26000.00, N'Porcentaje', 65.00),
    (@Doc3, N'dr.castro', N'Roberto', N'Castro', N'27101103', '1978-11-30', N'roberto.castro@medistack.test', N'1144001003', N'MN-61203', @TraumaId, 35000.00, N'Fijo', 22000.00),
    (@Doc4, N'dra.dominguez', N'Lucia', N'Dominguez', N'27101104', '1986-03-17', N'lucia.dominguez@medistack.test', N'1144001004', N'MN-61204', @DermaId, 29000.00, N'Porcentaje', 65.00),
    (@Doc5, N'dr.espinosa', N'Gabriel', N'Espinosa', N'27101105', '1980-09-09', N'gabriel.espinosa@medistack.test', N'1144001005', N'MN-61205', @OftalmoId, 31000.00, N'Porcentaje', 70.00),
    (@Doc6, N'dra.flores', N'Valeria', N'Flores', N'27101106', '1985-07-25', N'valeria.flores@medistack.test', N'1144001006', N'MN-61206', @GinecId, 34000.00, N'Porcentaje', 70.00),
    (@Doc7, N'dr.gutierrez', N'Esteban', N'Gutierrez', N'27101107', '1975-01-12', N'esteban.gutierrez@medistack.test', N'1144001007', N'MN-61207', @ClinicaId, 24000.00, N'Fijo', 16000.00),
    (@Doc8, N'dra.herrera', N'Camila', N'Herrera', N'27101108', '1988-12-04', N'camila.herrera@medistack.test', N'1144001008', N'MN-61208', @PedId, 27000.00, N'Porcentaje', 65.00);

-- Insercion de Usuarios para Profesionales
INSERT INTO dbo.Usuarios (UsuarioId, RolId, NombreUsuario, PasswordHash, Nombre, Apellido, NumeroDocumento, FechaNacimiento, Email, Telefono)
SELECT d.Id, @RolProfesional, d.Usuario, @PasswordHashDemo, d.Nombre, d.Apellido, d.Dni, d.Nacimiento, d.Email, d.Telefono
FROM @DoctoresDef d
WHERE NOT EXISTS (SELECT 1 FROM dbo.Usuarios u WHERE u.UsuarioId = d.Id OR u.NombreUsuario = d.Usuario OR u.NumeroDocumento = d.Dni OR u.Email = d.Email);

-- Insercion de Profesionales
INSERT INTO dbo.Profesionales (ProfesionalId, MatriculaProfesional)
SELECT d.Id, d.Matricula
FROM @DoctoresDef d
WHERE NOT EXISTS (SELECT 1 FROM dbo.Profesionales p WHERE p.ProfesionalId = d.Id OR p.MatriculaProfesional = d.Matricula);

-- Asignacion de Especialidad Principal
INSERT INTO dbo.ProfesionalesEspecialidades (ProfesionalId, EspecialidadId, EsEspecialidadPrincipal, ValorConsulta, EsquemaTipo, EsquemaValor)
SELECT d.Id, d.EspecialidadId, 1, d.ValorConsulta, d.EsquemaTipo, d.EsquemaValor
FROM @DoctoresDef d
WHERE NOT EXISTS (SELECT 1 FROM dbo.ProfesionalesEspecialidades pe WHERE pe.ProfesionalId = d.Id AND pe.EspecialidadId = d.EspecialidadId);

-- Convenios para doctores con obras sociales
INSERT INTO dbo.Convenios (ProfesionalId, EspecialidadId, ObraSocialId, FechaDesde)
SELECT d.Id, d.EspecialidadId, @ObraSocial1, '2024-01-01'
FROM @DoctoresDef d
WHERE @ObraSocial1 IS NOT NULL
  AND EXISTS (SELECT 1 FROM dbo.CoberturasEspecialidades ce WHERE ce.ObraSocialId = @ObraSocial1 AND ce.EspecialidadId = d.EspecialidadId)
  AND NOT EXISTS (SELECT 1 FROM dbo.Convenios c WHERE c.ProfesionalId = d.Id AND c.EspecialidadId = d.EspecialidadId AND c.ObraSocialId = @ObraSocial1);

INSERT INTO dbo.Convenios (ProfesionalId, EspecialidadId, ObraSocialId, FechaDesde)
SELECT d.Id, d.EspecialidadId, @ObraSocial2, '2024-01-01'
FROM @DoctoresDef d
WHERE @ObraSocial2 IS NOT NULL
  AND EXISTS (SELECT 1 FROM dbo.CoberturasEspecialidades ce WHERE ce.ObraSocialId = @ObraSocial2 AND ce.EspecialidadId = d.EspecialidadId)
  AND NOT EXISTS (SELECT 1 FROM dbo.Convenios c WHERE c.ProfesionalId = d.Id AND c.EspecialidadId = d.EspecialidadId AND c.ObraSocialId = @ObraSocial2);

-- Horarios de atencion (Lunes a Viernes de 08:00 a 12:00 y 13:00 a 17:00 distribuidos)
DECLARE @DiasSemana TABLE (Dia TINYINT);
INSERT INTO @DiasSemana VALUES (1), (2), (3), (4), (5);

INSERT INTO dbo.HorariosAtencion (ProfesionalId, EspecialidadId, DiaSemana, HoraInicio, HoraFin)
SELECT d.Id, d.EspecialidadId, s.Dia, '08:00', '13:00'
FROM @DoctoresDef d
CROSS JOIN @DiasSemana s
WHERE (d.Dni % 2 = 1)
  AND NOT EXISTS (SELECT 1 FROM dbo.HorariosAtencion ha WHERE ha.ProfesionalId = d.Id AND ha.EspecialidadId = d.EspecialidadId AND ha.DiaSemana = s.Dia AND ha.HoraInicio = '08:00');

INSERT INTO dbo.HorariosAtencion (ProfesionalId, EspecialidadId, DiaSemana, HoraInicio, HoraFin)
SELECT d.Id, d.EspecialidadId, s.Dia, '13:00', '18:00'
FROM @DoctoresDef d
CROSS JOIN @DiasSemana s
WHERE (d.Dni % 2 = 0)
  AND NOT EXISTS (SELECT 1 FROM dbo.HorariosAtencion ha WHERE ha.ProfesionalId = d.Id AND ha.EspecialidadId = d.EspecialidadId AND ha.DiaSemana = s.Dia AND ha.HoraInicio = '13:00');

-- ============================================================================
-- 4. 20 PACIENTES
-- ============================================================================
DECLARE @PacientesDef TABLE
(
    Id UNIQUEIDENTIFIER,
    Usuario NVARCHAR(50),
    Nombre NVARCHAR(100),
    Apellido NVARCHAR(100),
    Dni NVARCHAR(20),
    Nacimiento DATE,
    Email NVARCHAR(256),
    Telefono NVARCHAR(30),
    ObraSocialId INT,
    NumeroAfiliado NVARCHAR(50),
    ContactoNombre NVARCHAR(100),
    ContactoTelefono NVARCHAR(30)
);

INSERT INTO @PacientesDef VALUES
    ('44444444-4444-4444-8444-000000000001', N'santiago.morales', N'Santiago', N'Morales', N'35102001', '1995-02-10', N'santiago.morales@medistack.test', N'1155002001', @ObraSocial1, N'SU-5001', N'Marta Morales', N'1155990001'),
    ('44444444-4444-4444-8444-000000000002', N'valentina.rodriguez', N'Valentina', N'Rodriguez', N'35102002', '1998-06-25', N'valentina.rodriguez@medistack.test', N'1155002002', @ObraSocial2, N'CF-6002', N'Carlos Rodriguez', N'1155990002'),
    ('44444444-4444-4444-8444-000000000003', N'matias.romero', N'Matias', N'Romero', N'35102003', '1992-11-14', N'matias.romero@medistack.test', N'1155002003', NULL, NULL, N'Laura Romero', N'1155990003'),
    ('44444444-4444-4444-8444-000000000004', N'luciana.sosa', N'Luciana', N'Sosa', N'35102004', '2000-04-03', N'luciana.sosa@medistack.test', N'1155002004', @ObraSocial1, N'SU-5004', N'Andres Sosa', N'1155990004'),
    ('44444444-4444-4444-8444-000000000005', N'joaquin.torres', N'Joaquin', N'Torres', N'35102005', '1989-09-18', N'joaquin.torres@medistack.test', N'1155002005', @ObraSocial2, N'CF-6005', N'Silvia Torres', N'1155990005'),
    ('44444444-4444-4444-8444-000000000006', N'agustina.alvarez', N'Agustina', N'Alvarez', N'35102006', '1996-01-30', N'agustina.alvarez@medistack.test', N'1155002006', @ObraSocial1, N'SU-5006', N'Jorge Alvarez', N'1155990006'),
    ('44444444-4444-4444-8444-000000000007', N'nicolas.medina', N'Nicolas', N'Medina', N'35102007', '1991-07-12', N'nicolas.medina@medistack.test', N'1155002007', NULL, NULL, N'Beatriz Medina', N'1155990007'),
    ('44444444-4444-4444-8444-000000000008', N'florencia.castro', N'Florencia', N'Castro', N'35102008', '1994-10-08', N'florencia.castro@medistack.test', N'1155002008', @ObraSocial2, N'CF-6008', N'Lucas Castro', N'1155990008'),
    ('44444444-4444-4444-8444-000000000009', N'tomas.ortiz', N'Tomas', N'Ortiz', N'35102009', '1987-03-21', N'tomas.ortiz@medistack.test', N'1155002009', @ObraSocial1, N'SU-5009', N'Patricia Ortiz', N'1155990009'),
    ('44444444-4444-4444-8444-000000000010', N'camila.silva', N'Camila', N'Silva', N'35102010', '1999-12-15', N'camila.silva@medistack.test', N'1155002010', @ObraSocial2, N'CF-6010', N'Mario Silva', N'1155990010'),
    ('44444444-4444-4444-8444-000000000011', N'lucas.nunez', N'Lucas', N'Nunez', N'35102011', '1993-08-05', N'lucas.nunez@medistack.test', N'1155002011', @ObraSocial1, N'SU-5011', N'Daniela Nunez', N'1155990011'),
    ('44444444-4444-4444-8444-000000000012', N'micaela.suarez', N'Micaela', N'Suarez', N'35102012', '1997-05-19', N'micaela.suarez@medistack.test', N'1155002012', NULL, NULL, N'Claudio Suarez', N'1155990012'),
    ('44444444-4444-4444-8444-000000000013', N'benjamin.blanco', N'Benjamin', N'Blanco', N'35102013', '2001-02-27', N'benjamin.blanco@medistack.test', N'1155002013', @ObraSocial2, N'CF-6013', N'Rosa Blanco', N'1155990013'),
    ('44444444-4444-4444-8444-000000000014', N'julieta.acuna', N'Julieta', N'Acuna', N'35102014', '1990-10-11', N'julieta.acuna@medistack.test', N'1155002014', @ObraSocial1, N'SU-5014', N'Gonzalo Acuna', N'1155990014'),
    ('44444444-4444-4444-8444-000000000015', N'felipe.mendez', N'Felipe', N'Mendez', N'35102015', '1985-04-16', N'felipe.mendez@medistack.test', N'1155002015', @ObraSocial1, N'SU-5015', N'Lorena Mendez', N'1155990015'),
    ('44444444-4444-4444-8444-000000000016', N'martina.guerrero', N'Martina', N'Guerrero', N'35102016', '1994-01-23', N'martina.guerrero@medistack.test', N'1155002016', @ObraSocial2, N'CF-6016', N'Sergio Guerrero', N'1155990016'),
    ('44444444-4444-4444-8444-000000000017', N'franco.dominguez', N'Franco', N'Dominguez', N'35102017', '1996-09-02', N'franco.dominguez@medistack.test', N'1155002017', NULL, NULL, N'Victoria Dominguez', N'1155990017'),
    ('44444444-4444-4444-8444-000000000018', N'sofia.navarro', N'Sofia', N'Navarro', N'35102018', '1998-11-28', N'sofia.navarro@medistack.test', N'1155002018', @ObraSocial1, N'SU-5018', N'Estela Navarro', N'1155990018'),
    ('44444444-4444-4444-8444-000000000019', N'bruno.rossi', N'Bruno', N'Rossi', N'35102019', '1993-07-07', N'bruno.rossi@medistack.test', N'1155002019', @ObraSocial2, N'CF-6019', N'Oscar Rossi', N'1155990019'),
    ('44444444-4444-4444-8444-000000000020', N'catalina.vargas', N'Catalina', N'Vargas', N'35102020', '2002-03-14', N'catalina.vargas@medistack.test', N'1155002020', @ObraSocial1, N'SU-5020', N'Mirta Vargas', N'1155990020');

-- Insercion de Usuarios para Pacientes
INSERT INTO dbo.Usuarios (UsuarioId, RolId, NombreUsuario, PasswordHash, Nombre, Apellido, NumeroDocumento, FechaNacimiento, Email, Telefono)
SELECT p.Id, @RolPaciente, p.Usuario, @PasswordHashDemo, p.Nombre, p.Apellido, p.Dni, p.Nacimiento, p.Email, p.Telefono
FROM @PacientesDef p
WHERE NOT EXISTS (SELECT 1 FROM dbo.Usuarios u WHERE u.UsuarioId = p.Id OR u.NombreUsuario = p.Usuario OR u.NumeroDocumento = p.Dni OR u.Email = p.Email);

-- Insercion en Pacientes
INSERT INTO dbo.Pacientes (PacienteId, ObraSocialId, NumeroAfiliado, ContactoEmergenciaNombre, ContactoEmergenciaTelefono)
SELECT p.Id, p.ObraSocialId, p.NumeroAfiliado, p.ContactoNombre, p.ContactoTelefono
FROM @PacientesDef p
WHERE NOT EXISTS (SELECT 1 FROM dbo.Pacientes pa WHERE pa.PacienteId = p.Id);

-- ============================================================================
-- 5. FECHAS RELATIVAS Y GENERACION DE TURNOS
-- ============================================================================
DECLARE @Hoy DATE = CONVERT(date, SYSDATETIME());
DECLARE @LunesActual DATE = DATEADD(day, -(DATEDIFF(day, CONVERT(date, '19000101'), @Hoy) % 7), @Hoy);

-- Tabla de planificacion de turnos mock
DECLARE @TurnosPlan TABLE
(
    KeyRef INT IDENTITY(1,1),
    PacienteId UNIQUEIDENTIFIER,
    ProfesionalId UNIQUEIDENTIFIER,
    EspecialidadId INT,
    FechaTurno DATETIME2(0),
    Estado NVARCHAR(20),
    Motivo NVARCHAR(500),
    MontoSena DECIMAL(12,2),
    EstadoSena NVARCHAR(20),
    MedioPagoSena NVARCHAR(20),
    MontoBase DECIMAL(12,2),
    MontoOS DECIMAL(12,2),
    Copago DECIMAL(12,2),
    MedioPagoConsulta NVARCHAR(20),
    Diagnostico NVARCHAR(500)
);

-- Pacientes 1 a 10: Turnos PASADOS (hace entre 1 y 25 dias)
INSERT INTO @TurnosPlan VALUES
    -- Paciente 1 (Atendido en Cardio con Dr. Alvarez hace 21 dias)
    ('44444444-4444-4444-8444-000000000001', @Doc1, @CardioId, DATEADD(hour, 9, CONVERT(datetime2(0), DATEADD(day, -21, @LunesActual))),
     N'Atendido', N'Chequeo cardiovascular por antecedentes familiares', 0.00, N'NoRequerida', NULL, 32000.00, 19200.00, 12800.00, N'Efectivo', N'Electrocardiograma normal. Presion arterial controlada.'),
    
    -- Paciente 2 (Atendido en Pediatria con Dra. Benitez hace 20 dias)
    ('44444444-4444-4444-8444-000000000002', @Doc2, @PedId, DATEADD(hour, 14, CONVERT(datetime2(0), DATEADD(day, -20, @LunesActual))),
     N'Atendido', N'Control de rutina y vacunacion', 4000.00, N'Pagada', N'Tarjeta', 26000.00, 13000.00, 13000.00, N'Tarjeta', N'Crecimiento acorde a percentiles. Calendario de vacunacion al dia.'),

    -- Paciente 3 (Atendido particular en Traumatologia con Dr. Castro hace 15 dias)
    ('44444444-4444-4444-8444-000000000003', @Doc3, @TraumaId, DATEADD(hour, 10, CONVERT(datetime2(0), DATEADD(day, -15, @LunesActual))),
     N'Atendido', N'Dolor lumbar persistente post esfuerzo laboral', 5000.00, N'Pagada', N'Transferencia', 35000.00, 0.00, 35000.00, N'Efectivo', N'Lumbalgia mecanica sin compromiso radicular. Reposo e ibuprofeno.'),

    -- Paciente 4 (Atendido en Dermatologia con Dra. Dominguez hace 14 dias)
    ('44444444-4444-4444-8444-000000000004', @Doc4, @DermaId, DATEADD(hour, 15, CONVERT(datetime2(0), DATEADD(day, -14, @LunesActual))),
     N'Atendido', N'Control de lunares en extremidades', 0.00, N'NoRequerida', NULL, 29000.00, 17400.00, 11600.00, N'Efectivo', N'Nevi melanociticos benignos. Pautas de fotoproteccion solar.'),

    -- Paciente 5 (Atendido en Oftalmologia con Dr. Espinosa hace 11 dias)
    ('44444444-4444-4444-8444-000000000005', @Doc5, @OftalmoId, DATEADD(hour, 11, CONVERT(datetime2(0), DATEADD(day, -11, @LunesActual))),
     N'Atendido', N'Disminucion de agudeza visual para lectura', 4500.00, N'Pagada', N'MercadoPago', 31000.00, 15500.00, 15500.00, N'Transferencia', N'Presbicia incipiente. Receta de lentes aereos.'),

    -- Paciente 6 (Atendido en Ginecologia con Dra. Flores hace 8 dias)
    ('44444444-4444-4444-8444-000000000006', @Doc6, @GinecId, DATEADD(hour, 16, CONVERT(datetime2(0), DATEADD(day, -8, @LunesActual))),
     N'Atendido', N'Control ginecologico anual y PAP', 0.00, N'NoRequerida', NULL, 34000.00, 23800.00, 10200.00, N'Efectivo', N'Examen fisico adecuado. Muestra PAP y colposcopia remitidas.'),

    -- Paciente 7 (Atendido particular en Clinica con Dr. Gutierrez hace 7 dias)
    ('44444444-4444-4444-8444-000000000007', @Doc7, @ClinicaId, DATEADD(hour, 9, CONVERT(datetime2(0), DATEADD(day, -7, @LunesActual))),
     N'Atendido', N'Sindrome febril y congestion de 3 dias', 0.00, N'NoRequerida', NULL, 24000.00, 0.00, 24000.00, N'Efectivo', N'Infeccion respiratoria alta viral. Tratamiento sintomatico.'),

    -- Paciente 8 (Atendido en Pediatria con Dra. Herrera hace 5 dias)
    ('44444444-4444-4444-8444-000000000008', @Doc8, @PedId, DATEADD(hour, 14, CONVERT(datetime2(0), DATEADD(day, -5, @LunesActual))),
     N'Atendido', N'Certificado de aptitud fisica escolar', 0.00, N'NoRequerida', NULL, 27000.00, 13500.00, 13500.00, N'Efectivo', N'Apto fisico pediatrico otorgado.'),

    -- Paciente 9 (Cancelado con seña reembolsada hace 7 dias)
    ('44444444-4444-4444-8444-000000000009', @Doc1, @CardioId, DATEADD(hour, 11, CONVERT(datetime2(0), DATEADD(day, -7, @LunesActual))),
     N'Cancelado', N'Consulta cancelada por viaje laboral anticipado', 4000.00, N'Reembolsada', N'Transferencia', 0.00, 0.00, 0.00, NULL, NULL),

    -- Paciente 10 (Ausente hace 4 dias)
    ('44444444-4444-4444-8444-000000000010', @Doc3, @TraumaId, DATEADD(hour, 12, CONVERT(datetime2(0), DATEADD(day, -4, @LunesActual))),
     N'Ausente', N'Dolor en tobillo tras esguince leve', 5000.00, N'Retenida', N'Tarjeta', 0.00, 0.00, 0.00, NULL, NULL);

-- Pacientes 11 a 20: Turnos FUTUROS (proximos 2 a 28 dias)
INSERT INTO @TurnosPlan VALUES
    -- Paciente 11 (Confirmado proximo martes, seña abonada)
    ('44444444-4444-4444-8444-000000000011', @Doc1, @CardioId, DATEADD(hour, 10, CONVERT(datetime2(0), DATEADD(day, 8, @LunesActual))),
     N'Confirmado', N'Evaluacion prequirurgica cardiovascular', 5000.00, N'Pagada', N'Transferencia', 0.00, 0.00, 0.00, NULL, NULL),

    -- Paciente 12 (Solicitado particular proximo miercoles, seña pendiente)
    ('44444444-4444-4444-8444-000000000012', @Doc4, @DermaId, DATEADD(hour, 14, CONVERT(datetime2(0), DATEADD(day, 9, @LunesActual))),
     N'Solicitado', N'Consulta por dermatitis de contacto', 4000.00, N'Pendiente', NULL, 0.00, 0.00, 0.00, NULL, NULL),

    -- Paciente 13 (Confirmado proximo jueves, seña por Mercado Pago)
    ('44444444-4444-4444-8444-000000000013', @Doc2, @PedId, DATEADD(hour, 15, CONVERT(datetime2(0), DATEADD(day, 10, @LunesActual))),
     N'Confirmado', N'Consulta por broncoespasmo recurrente', 3500.00, N'Pagada', N'MercadoPago', 0.00, 0.00, 0.00, NULL, NULL),

    -- Paciente 14 (Solicitado proximo viernes, seña pendiente)
    ('44444444-4444-4444-8444-000000000014', @Doc3, @TraumaId, DATEADD(hour, 11, CONVERT(datetime2(0), DATEADD(day, 11, @LunesActual))),
     N'Solicitado', N'Evaluacion de dolor en rodilla izquierda', 5000.00, N'Pendiente', NULL, 0.00, 0.00, 0.00, NULL, NULL),

    -- Paciente 15 (Confirmado dentro de 15 dias, seña no requerida por convenio)
    ('44444444-4444-4444-8444-000000000015', @Doc7, @ClinicaId, DATEADD(hour, 8, CONVERT(datetime2(0), DATEADD(day, 15, @LunesActual))),
     N'Confirmado', N'Chequeo clinico periodico laboral', 0.00, N'NoRequerida', NULL, 0.00, 0.00, 0.00, NULL, NULL),

    -- Paciente 16 (Confirmado dentro de 16 dias, seña en efectivo en mostrador)
    ('44444444-4444-4444-8444-000000000016', @Doc5, @OftalmoId, DATEADD(hour, 9, CONVERT(datetime2(0), DATEADD(day, 16, @LunesActual))),
     N'Confirmado', N'Fondo de ojos por indicacion diabetologica', 4000.00, N'Pagada', N'Efectivo', 0.00, 0.00, 0.00, NULL, NULL),

    -- Paciente 17 (Solicitado particular dentro de 18 dias)
    ('44444444-4444-4444-8444-000000000017', @Doc6, @GinecId, DATEADD(hour, 15, CONVERT(datetime2(0), DATEADD(day, 18, @LunesActual))),
     N'Solicitado', N'Consulta preconcepcional', 4500.00, N'Pendiente', NULL, 0.00, 0.00, 0.00, NULL, NULL),

    -- Paciente 18 (Confirmado dentro de 22 dias, seña abonada con tarjeta)
    ('44444444-4444-4444-8444-000000000018', @Doc1, @CardioId, DATEADD(hour, 8, CONVERT(datetime2(0), DATEADD(day, 22, @LunesActual))),
     N'Confirmado', N'Prueba de esfuerzo ergometrica', 6000.00, N'Pagada', N'Tarjeta', 0.00, 0.00, 0.00, NULL, NULL),

    -- Paciente 19 (Confirmado dentro de 24 dias, seña abonada por transferencia)
    ('44444444-4444-4444-8444-000000000019', @Doc3, @TraumaId, DATEADD(hour, 10, CONVERT(datetime2(0), DATEADD(day, 24, @LunesActual))),
     N'Confirmado', N'Seguimiento post inmovilizacion de muneca', 5000.00, N'Pagada', N'Transferencia', 0.00, 0.00, 0.00, NULL, NULL),

    -- Paciente 20 (Confirmado dentro de 26 dias)
    ('44444444-4444-4444-8444-000000000020', @Doc7, @ClinicaId, DATEADD(hour, 11, CONVERT(datetime2(0), DATEADD(day, 26, @LunesActual))),
     N'Confirmado', N'Seguimiento de laboratorio y perfil lipidico', 0.00, N'NoRequerida', NULL, 0.00, 0.00, 0.00, NULL, NULL);

-- Turnos adicionales futuros para pacientes 1 a 5 (segundas consultas en distinta especialidad)
INSERT INTO @TurnosPlan VALUES
    ('44444444-4444-4444-8444-000000000001', @Doc7, @ClinicaId, DATEADD(hour, 10, CONVERT(datetime2(0), DATEADD(day, 12, @LunesActual))),
     N'Confirmado', N'Control clinico complementario', 0.00, N'NoRequerida', NULL, 0.00, 0.00, 0.00, NULL, NULL),

    ('44444444-4444-4444-8444-000000000002', @Doc4, @DermaId, DATEADD(hour, 16, CONVERT(datetime2(0), DATEADD(day, 17, @LunesActual))),
     N'Solicitado', N'Consulta por resequedad en piel', 3500.00, N'Pendiente', NULL, 0.00, 0.00, 0.00, NULL, NULL),

    ('44444444-4444-4444-8444-000000000003', @Doc5, @OftalmoId, DATEADD(hour, 10, CONVERT(datetime2(0), DATEADD(day, 19, @LunesActual))),
     N'Confirmado', N'Control visual anual', 4000.00, N'Pagada', N'Transferencia', 0.00, 0.00, 0.00, NULL, NULL),

    ('44444444-4444-4444-8444-000000000004', @Doc6, @GinecId, DATEADD(hour, 14, CONVERT(datetime2(0), DATEADD(day, 23, @LunesActual))),
     N'Confirmado', N'Seguimiento ecografico ginecologico', 4500.00, N'Pagada', N'MercadoPago', 0.00, 0.00, 0.00, NULL, NULL),

    ('44444444-4444-4444-8444-000000000005', @Doc1, @CardioId, DATEADD(hour, 9, CONVERT(datetime2(0), DATEADD(day, 25, @LunesActual))),
     N'Solicitado', N'Control anual de presion arterial', 4500.00, N'Pendiente', NULL, 0.00, 0.00, 0.00, NULL, NULL);

-- Cursor / Bucle para insercion idempotente de turnos, cobros y atenciones
DECLARE @Idx INT = 1;
DECLARE @TotalTurnos INT = (SELECT COUNT(*) FROM @TurnosPlan);

WHILE @Idx <= @TotalTurnos
BEGIN
    DECLARE @pId UNIQUEIDENTIFIER, @docId UNIQUEIDENTIFIER, @espId INT, @fHora DATETIME2(0);
    DECLARE @est NVARCHAR(20), @mot NVARCHAR(500), @mSena DECIMAL(12,2), @estSena NVARCHAR(20);
    DECLARE @medSena NVARCHAR(20), @mBase DECIMAL(12,2), @mOS DECIMAL(12,2), @copago DECIMAL(12,2);
    DECLARE @medConsulta NVARCHAR(20), @diag NVARCHAR(500);

    SELECT
        @pId = PacienteId, @docId = ProfesionalId, @espId = EspecialidadId, @fHora = FechaTurno,
        @est = Estado, @mot = Motivo, @mSena = MontoSena, @estSena = EstadoSena,
        @medSena = MedioPagoSena, @mBase = MontoBase, @mOS = MontoOS, @copago = Copago,
        @medConsulta = MedioPagoConsulta, @diag = Diagnostico
    FROM @TurnosPlan
    WHERE KeyRef = @Idx;

    -- Validacion de existencia previa del turno
    DECLARE @TurnoIdActual INT = (
        SELECT TurnoId FROM dbo.Turnos
        WHERE PacienteId = @pId AND ProfesionalId = @docId AND FechaHora = @fHora
    );

    IF @TurnoIdActual IS NULL
    BEGIN
        INSERT INTO dbo.Turnos
            (PacienteId, ProfesionalId, EspecialidadId, FechaHora, Estado, Motivo, MontoSena, EstadoSena, FechaHoraSena)
        VALUES
            (@pId, @docId, @espId, @fHora, @est, @mot, @mSena, @estSena,
             CASE WHEN @estSena IN (N'Pagada', N'Reembolsada', N'Retenida') THEN DATEADD(day, -1, @fHora) ELSE NULL END);

        SET @TurnoIdActual = SCOPE_IDENTITY();
    END;

    -- 1) Cobro de Seña (si corresponde y no existe)
    IF @mSena > 0 AND @estSena IN (N'Pagada', N'Reembolsada', N'Retenida') AND @medSena IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.Cobros WHERE TurnoId = @TurnoIdActual AND TipoCobro = N'Sena')
        BEGIN
            INSERT INTO dbo.Cobros
                (TurnoId, TipoCobro, MontoBase, MontoObraSocial, Copago, SenaDescontada, MontoCobrado, MedioPago, MontoRecibido, Vuelto, FechaHoraCobro, UsuarioCobradorId, MercadoPagoPagoId)
            VALUES
                (@TurnoIdActual, N'Sena', 0.00, 0.00, 0.00, 0.00, @mSena, @medSena, @mSena, 0.00, DATEADD(hour, -2, @fHora), @AdminId,
                 CASE WHEN @medSena = N'MercadoPago' THEN CONCAT(N'MP-SEED-', @TurnoIdActual) ELSE NULL END);
        END;
    END;

    -- 2) Reembolso para turnos cancelados con seña devuelta
    IF @est = N'Cancelado' AND @estSena = N'Reembolsada'
    BEGIN
        DECLARE @CobroSenaId INT = (SELECT CobroId FROM dbo.Cobros WHERE TurnoId = @TurnoIdActual AND TipoCobro = N'Sena');
        IF NOT EXISTS (SELECT 1 FROM dbo.Reembolsos WHERE TurnoId = @TurnoIdActual)
        BEGIN
            INSERT INTO dbo.Reembolsos
                (TurnoId, CobroId, Monto, MedioPago, Motivo, Estado, FechaHoraSolicitud, SolicitadoPorId, FechaHoraCompletado, CompletadoPorId, Comprobante)
            VALUES
                (@TurnoIdActual, @CobroSenaId, @mSena, @medSena, N'CancelacionPaciente', N'Completado', DATEADD(hour, 1, @fHora), @AdminId, DATEADD(hour, 2, @fHora), @AdminId, CONCAT(N'TRANSF-DEV-', @TurnoIdActual));
        END;
    END;

    -- 3) Cobro de Consulta médica para turnos Atendidos
    IF @est = N'Atendido' AND @mBase > 0 AND @medConsulta IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.Cobros WHERE TurnoId = @TurnoIdActual AND TipoCobro = N'Consulta')
        BEGIN
            DECLARE @SenaADescontar DECIMAL(12,2) = CASE WHEN @estSena = N'Pagada' THEN @mSena ELSE 0.00 END;
            DECLARE @MontoACobrar DECIMAL(12,2) = @copago - @SenaADescontar;

            INSERT INTO dbo.Cobros
                (TurnoId, TipoCobro, MontoBase, MontoObraSocial, Copago, SenaDescontada, MontoCobrado, MedioPago, MontoRecibido, Vuelto, FechaHoraCobro, UsuarioCobradorId)
            VALUES
                (@TurnoIdActual, N'Consulta', @mBase, @mOS, @copago, @SenaADescontar, @MontoACobrar, @medConsulta,
                 CASE WHEN @medConsulta = N'Efectivo' THEN @MontoACobrar + 2000.00 ELSE @MontoACobrar END,
                 CASE WHEN @medConsulta = N'Efectivo' THEN 2000.00 ELSE 0.00 END,
                 DATEADD(minute, 35, @fHora), @AdminId);
        END;
    END;

    -- 4) Registro Clinico para consultas atendidas
    IF @est = N'Atendido' AND @diag IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.RegistrosClinicos WHERE TurnoId = @TurnoIdActual)
        BEGIN
            INSERT INTO dbo.RegistrosClinicos (TurnoId, FechaRegistro, MotivoConsulta, Diagnostico, Observaciones)
            VALUES (@TurnoIdActual, DATEADD(minute, 30, @fHora), @mot, @diag, N'Paciente tolera evaluacion. Se indican pautas de alarma y seguimiento.');
        END;
    END;

    SET @Idx = @Idx + 1;
END;

-- ============================================================================
-- 6. CICLOS DE CAJA (APERTURA, EGRESOS Y CIERRES HISTORICOS)
-- ============================================================================
-- Fecha de Caja 1: Hace 21 dias (Lunes)
DECLARE @FechaCaja1 DATE = DATEADD(day, -21, @LunesActual);
IF NOT EXISTS (SELECT 1 FROM dbo.AperturasCaja WHERE Fecha = @FechaCaja1)
    INSERT INTO dbo.AperturasCaja (Fecha, FondoInicial, AdministrativoId, FechaHoraApertura)
    VALUES (@FechaCaja1, 20000.00, @AdminId, DATEADD(hour, 7, CONVERT(datetime2(0), @FechaCaja1)));

IF NOT EXISTS (SELECT 1 FROM dbo.Egresos WHERE Fecha = @FechaCaja1 AND Concepto = N'Compra de resmas e insumos de oficina')
    INSERT INTO dbo.Egresos (Fecha, FechaHoraRegistro, Concepto, Monto, Categoria, Comprobante, AdministrativoId)
    VALUES (@FechaCaja1, DATEADD(hour, 12, CONVERT(datetime2(0), @FechaCaja1)), N'Compra de resmas e insumos de oficina', 4500.00, N'Insumos', N'FAC-A-0001-9981', @AdminId);

IF NOT EXISTS (SELECT 1 FROM dbo.CierresCaja WHERE Fecha = @FechaCaja1)
BEGIN
    DECLARE @EfCaja1 DECIMAL(12,2) = ISNULL((SELECT SUM(MontoCobrado) FROM dbo.Cobros WHERE CONVERT(date, FechaHoraCobro) = @FechaCaja1 AND MedioPago = N'Efectivo'), 0);
    DECLARE @TjCaja1 DECIMAL(12,2) = ISNULL((SELECT SUM(MontoCobrado) FROM dbo.Cobros WHERE CONVERT(date, FechaHoraCobro) = @FechaCaja1 AND MedioPago = N'Tarjeta'), 0);
    DECLARE @TrCaja1 DECIMAL(12,2) = ISNULL((SELECT SUM(MontoCobrado) FROM dbo.Cobros WHERE CONVERT(date, FechaHoraCobro) = @FechaCaja1 AND MedioPago IN (N'Transferencia', N'MercadoPago')), 0);
    DECLARE @CantCaja1 INT = ISNULL((SELECT COUNT(*) FROM dbo.Cobros WHERE CONVERT(date, FechaHoraCobro) = @FechaCaja1), 0);
    DECLARE @EgCaja1 DECIMAL(12,2) = ISNULL((SELECT SUM(Monto) FROM dbo.Egresos WHERE Fecha = @FechaCaja1), 0);
    DECLARE @Fondo1 DECIMAL(12,2) = 20000.00;
    DECLARE @Contado1 DECIMAL(12,2) = @Fondo1 + @EfCaja1 - @EgCaja1;

    INSERT INTO dbo.CierresCaja
        (Fecha, AdministrativoId, TotalEfectivo, TotalTarjeta, TotalTransferencia, TotalGeneral, CantidadCobros, EfectivoContado, Diferencia, FechaHoraCierre, TotalEgresos, FondoInicial)
    VALUES
        (@FechaCaja1, @AdminId, @EfCaja1, @TjCaja1, @TrCaja1, @EfCaja1 + @TjCaja1 + @TrCaja1, @CantCaja1, @Contado1, 0.00, DATEADD(hour, 19, CONVERT(datetime2(0), @FechaCaja1)), @EgCaja1, @Fondo1);
END;

-- Fecha de Caja 2: Hace 14 dias (Lunes)
DECLARE @FechaCaja2 DATE = DATEADD(day, -14, @LunesActual);
IF NOT EXISTS (SELECT 1 FROM dbo.AperturasCaja WHERE Fecha = @FechaCaja2)
    INSERT INTO dbo.AperturasCaja (Fecha, FondoInicial, AdministrativoId, FechaHoraApertura)
    VALUES (@FechaCaja2, 20000.00, @AdminId, DATEADD(hour, 7, CONVERT(datetime2(0), @FechaCaja2)));

IF NOT EXISTS (SELECT 1 FROM dbo.Egresos WHERE Fecha = @FechaCaja2 AND Concepto = N'Servicio de cadeteria y envio de muestras de laboratorio')
    INSERT INTO dbo.Egresos (Fecha, FechaHoraRegistro, Concepto, Monto, Categoria, Comprobante, AdministrativoId)
    VALUES (@FechaCaja2, DATEADD(hour, 13, CONVERT(datetime2(0), @FechaCaja2)), N'Servicio de cadeteria y envio de muestras de laboratorio', 3200.00, N'Viaticos', N'REC-B-0004-1205', @AdminId);

IF NOT EXISTS (SELECT 1 FROM dbo.CierresCaja WHERE Fecha = @FechaCaja2)
BEGIN
    DECLARE @EfCaja2 DECIMAL(12,2) = ISNULL((SELECT SUM(MontoCobrado) FROM dbo.Cobros WHERE CONVERT(date, FechaHoraCobro) = @FechaCaja2 AND MedioPago = N'Efectivo'), 0);
    DECLARE @TjCaja2 DECIMAL(12,2) = ISNULL((SELECT SUM(MontoCobrado) FROM dbo.Cobros WHERE CONVERT(date, FechaHoraCobro) = @FechaCaja2 AND MedioPago = N'Tarjeta'), 0);
    DECLARE @TrCaja2 DECIMAL(12,2) = ISNULL((SELECT SUM(MontoCobrado) FROM dbo.Cobros WHERE CONVERT(date, FechaHoraCobro) = @FechaCaja2 AND MedioPago IN (N'Transferencia', N'MercadoPago')), 0);
    DECLARE @CantCaja2 INT = ISNULL((SELECT COUNT(*) FROM dbo.Cobros WHERE CONVERT(date, FechaHoraCobro) = @FechaCaja2), 0);
    DECLARE @EgCaja2 DECIMAL(12,2) = ISNULL((SELECT SUM(Monto) FROM dbo.Egresos WHERE Fecha = @FechaCaja2), 0);
    DECLARE @Fondo2 DECIMAL(12,2) = 20000.00;
    DECLARE @Contado2 DECIMAL(12,2) = @Fondo2 + @EfCaja2 - @EgCaja2;

    INSERT INTO dbo.CierresCaja
        (Fecha, AdministrativoId, TotalEfectivo, TotalTarjeta, TotalTransferencia, TotalGeneral, CantidadCobros, EfectivoContado, Diferencia, FechaHoraCierre, TotalEgresos, FondoInicial)
    VALUES
        (@FechaCaja2, @AdminId, @EfCaja2, @TjCaja2, @TrCaja2, @EfCaja2 + @TjCaja2 + @TrCaja2, @CantCaja2, @Contado2, 0.00, DATEADD(hour, 19, CONVERT(datetime2(0), @FechaCaja2)), @EgCaja2, @Fondo2);
END;

-- Fecha de Caja 3: Hace 7 dias (Lunes)
DECLARE @FechaCaja3 DATE = DATEADD(day, -7, @LunesActual);
IF NOT EXISTS (SELECT 1 FROM dbo.AperturasCaja WHERE Fecha = @FechaCaja3)
    INSERT INTO dbo.AperturasCaja (Fecha, FondoInicial, AdministrativoId, FechaHoraApertura)
    VALUES (@FechaCaja3, 22000.00, @AdminId, DATEADD(hour, 7, CONVERT(datetime2(0), @FechaCaja3)));

IF NOT EXISTS (SELECT 1 FROM dbo.Egresos WHERE Fecha = @FechaCaja3 AND Concepto = N'Insumos sanitarios descartables y alcohol')
    INSERT INTO dbo.Egresos (Fecha, FechaHoraRegistro, Concepto, Monto, Categoria, Comprobante, AdministrativoId)
    VALUES (@FechaCaja3, DATEADD(hour, 11, CONVERT(datetime2(0), @FechaCaja3)), N'Insumos sanitarios descartables y alcohol', 6800.00, N'Insumos', N'FAC-A-0002-4411', @AdminId);

IF NOT EXISTS (SELECT 1 FROM dbo.CierresCaja WHERE Fecha = @FechaCaja3)
BEGIN
    DECLARE @EfCaja3 DECIMAL(12,2) = ISNULL((SELECT SUM(MontoCobrado) FROM dbo.Cobros WHERE CONVERT(date, FechaHoraCobro) = @FechaCaja3 AND MedioPago = N'Efectivo'), 0);
    DECLARE @TjCaja3 DECIMAL(12,2) = ISNULL((SELECT SUM(MontoCobrado) FROM dbo.Cobros WHERE CONVERT(date, FechaHoraCobro) = @FechaCaja3 AND MedioPago = N'Tarjeta'), 0);
    DECLARE @TrCaja3 DECIMAL(12,2) = ISNULL((SELECT SUM(MontoCobrado) FROM dbo.Cobros WHERE CONVERT(date, FechaHoraCobro) = @FechaCaja3 AND MedioPago IN (N'Transferencia', N'MercadoPago')), 0);
    DECLARE @CantCaja3 INT = ISNULL((SELECT COUNT(*) FROM dbo.Cobros WHERE CONVERT(date, FechaHoraCobro) = @FechaCaja3), 0);
    DECLARE @EgCaja3 DECIMAL(12,2) = ISNULL((SELECT SUM(Monto) FROM dbo.Egresos WHERE Fecha = @FechaCaja3), 0);
    DECLARE @Fondo3 DECIMAL(12,2) = 22000.00;
    DECLARE @Contado3 DECIMAL(12,2) = @Fondo3 + @EfCaja3 - @EgCaja3;

    INSERT INTO dbo.CierresCaja
        (Fecha, AdministrativoId, TotalEfectivo, TotalTarjeta, TotalTransferencia, TotalGeneral, CantidadCobros, EfectivoContado, Diferencia, FechaHoraCierre, TotalEgresos, FondoInicial)
    VALUES
        (@FechaCaja3, @AdminId, @EfCaja3, @TjCaja3, @TrCaja3, @EfCaja3 + @TjCaja3 + @TrCaja3, @CantCaja3, @Contado3, 0.00, DATEADD(hour, 19, CONVERT(datetime2(0), @FechaCaja3)), @EgCaja3, @Fondo3);
END;

-- Apertura de caja operativa para el dia de hoy (si no existe)
IF NOT EXISTS (SELECT 1 FROM dbo.AperturasCaja WHERE Fecha = @Hoy)
    INSERT INTO dbo.AperturasCaja (Fecha, FondoInicial, AdministrativoId, FechaHoraApertura)
    VALUES (@Hoy, 25000.00, @AdminId, DATEADD(hour, 7, CONVERT(datetime2(0), @Hoy)));

-- ============================================================================
-- 7. REPORTE FINAL DE VERIFICACION
-- ============================================================================
SELECT
    (SELECT COUNT(*) FROM dbo.Usuarios) AS TotalUsuarios,
    (SELECT COUNT(*) FROM dbo.Profesionales) AS TotalProfesionales,
    (SELECT COUNT(*) FROM dbo.Pacientes) AS TotalPacientes,
    (SELECT COUNT(*) FROM dbo.Turnos) AS TotalTurnos,
    (SELECT COUNT(*) FROM dbo.Cobros) AS TotalCobros,
    (SELECT COUNT(*) FROM dbo.AperturasCaja) AS TotalAperturasCaja,
    (SELECT COUNT(*) FROM dbo.Egresos) AS TotalEgresos,
    (SELECT COUNT(*) FROM dbo.CierresCaja) AS TotalCierresCaja;
