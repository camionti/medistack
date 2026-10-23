/*
    Migración: Recepción del Día, Sala de Espera y Registro de Llegada (Check-in).
    - Agrega la columna FechaHoraLlegada a dbo.Turnos.
    - Amplía CK_Turnos_Estado con el estado 'EnEspera'.
    - Actualiza los índices únicos filtrados de turnos vigentes para incluir 'EnEspera'.
    - Agrega índice de soporte para consultas operativas del mostrador del día.
    Migración estrictamente idempotente que preserva todos los datos existentes.
*/
USE [MediStackDB];
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- 1) Columna FechaHoraLlegada
IF COL_LENGTH(N'dbo.Turnos', N'FechaHoraLlegada') IS NULL
BEGIN
    ALTER TABLE dbo.Turnos ADD FechaHoraLlegada DATETIME2(0) NULL;
END;
GO

-- 2) Ampliación de CHECK constraint CK_Turnos_Estado con 'EnEspera'
IF NOT EXISTS (
    SELECT 1 
    FROM sys.check_constraints 
    WHERE name = N'CK_Turnos_Estado' 
      AND parent_object_id = OBJECT_ID(N'dbo.Turnos') 
      AND definition LIKE N'%EnEspera%'
)
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM sys.check_constraints 
        WHERE name = N'CK_Turnos_Estado' 
          AND parent_object_id = OBJECT_ID(N'dbo.Turnos')
    )
    BEGIN
        EXEC(N'ALTER TABLE dbo.Turnos DROP CONSTRAINT CK_Turnos_Estado');
    END;

    EXEC(N'ALTER TABLE dbo.Turnos ADD CONSTRAINT CK_Turnos_Estado CHECK (Estado IN (N''Solicitado'', N''Confirmado'', N''EnEspera'', N''Atendido'', N''Ausente'', N''Cancelado''))');
END;
GO

-- 3) Actualización de índice único filtrado por Paciente y Especialidad (vigentes)
IF EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = N'UX_Turnos_Paciente_Especialidad_Vigente' 
      AND object_id = OBJECT_ID(N'dbo.Turnos')
      AND (filter_definition IS NULL OR filter_definition NOT LIKE N'%EnEspera%')
)
BEGIN
    EXEC(N'DROP INDEX UX_Turnos_Paciente_Especialidad_Vigente ON dbo.Turnos');
    EXEC(N'CREATE UNIQUE INDEX UX_Turnos_Paciente_Especialidad_Vigente ON dbo.Turnos (PacienteId, EspecialidadId) WHERE Estado IN (N''Solicitado'', N''Confirmado'', N''EnEspera'')');
END;
GO

-- 4) Actualización de índice único filtrado por Profesional y FechaHora (vigentes)
IF EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = N'UX_Turnos_Profesional_FechaHora_Vigente' 
      AND object_id = OBJECT_ID(N'dbo.Turnos')
      AND (filter_definition IS NULL OR filter_definition NOT LIKE N'%EnEspera%')
)
BEGIN
    EXEC(N'DROP INDEX UX_Turnos_Profesional_FechaHora_Vigente ON dbo.Turnos');
    EXEC(N'CREATE UNIQUE INDEX UX_Turnos_Profesional_FechaHora_Vigente ON dbo.Turnos (ProfesionalId, FechaHora) WHERE Estado IN (N''Solicitado'', N''Confirmado'', N''EnEspera'')');
END;
GO

-- 5) Índice para consultas optimizadas de recepción del día
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = N'IX_Turnos_RecepcionDia' 
      AND object_id = OBJECT_ID(N'dbo.Turnos')
)
BEGIN
    EXEC(N'CREATE INDEX IX_Turnos_RecepcionDia ON dbo.Turnos (FechaHora, Estado) INCLUDE (PacienteId, ProfesionalId, EspecialidadId, EstadoSena, MontoSena, FechaHoraLlegada)');
END;
GO
