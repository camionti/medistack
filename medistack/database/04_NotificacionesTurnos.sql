/*
    MediStackDB - Migracion 04: notificaciones de turnos por email y recordatorio 48hs
    Ejecutar DESPUES de 01_MediStackDB.sql, 02_SenaOnline.sql y 03_EgresosCaja.sql.

    Idempotente: se puede ejecutar varias veces sin errores ni duplicados. No usa GO.
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

-- 1) Columna para evitar enviar recordatorios duplicados (solo si no existe)
IF COL_LENGTH(N'dbo.Turnos', N'Recordatorio48hsEnviado') IS NULL
    EXEC(N'ALTER TABLE dbo.Turnos ADD Recordatorio48hsEnviado BIT NOT NULL CONSTRAINT DF_Turnos_Recordatorio48hsEnviado DEFAULT (0)');

-- 2) Indice filtrado para consultar rapido los turnos pendientes de recordatorio en la ventana de 48 hs
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Turnos_RecordatorioPendiente' AND object_id = OBJECT_ID(N'dbo.Turnos'))
    EXEC(N'CREATE INDEX IX_Turnos_RecordatorioPendiente ON dbo.Turnos (FechaHora)
           WHERE Estado IN (N''Solicitado'', N''Confirmado'') AND Recordatorio48hsEnviado = 0');

-- 3) Verificacion
SELECT
    COL_LENGTH(N'dbo.Turnos', N'Recordatorio48hsEnviado') AS ColumnaRecordatorio_Bytes,
    (SELECT COUNT(*) FROM sys.indexes WHERE name = N'IX_Turnos_RecordatorioPendiente') AS IndiceRecordatorioCreado;
