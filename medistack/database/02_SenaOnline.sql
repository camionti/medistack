/*
    MediStackDB - Migracion 02: seña online con vencimiento
    Ejecutar DESPUES de 01_MediStackDB.sql, sobre una base ya creada.

    Como ejecutarlo: abrir este archivo y ejecutarlo COMPLETO, de una sola vez
      - DBeaver: Ctrl+A (seleccionar todo) y Ctrl+Enter, conectado a MediStackDB.
      - SSMS: F5.
    No usa GO, asi funciona igual en DBeaver y en SSMS.
    Es idempotente: se puede ejecutar mas de una vez sin errores ni cambios duplicados.

    Que agrega:
    - Turnos.VencimientoSena: hasta cuando el paciente puede pagar la seña de una reserva online.
        NULL  -> el turno no vence (reservas del administrativo y turnos sin seña).
        fecha -> reserva online provisional: si a esa hora la seña sigue 'Pendiente',
                 el turno se cancela y el horario se libera ("limpieza perezosa").
    - IX_Turnos_SenaPendienteVencimiento: indice filtrado para que la limpieza encuentre
      rapido solo las reservas provisionales, sin recorrer toda la tabla.
    - CK_Turnos_EstadoSena: suma el estado 'Vencida' (la reserva no se pago a tiempo y la
      limpieza la cancelo). Un CHECK no se puede editar: se borra y se vuelve a crear con la
      lista nueva, solo si todavia no incluye 'Vencida'.
      Si ya habias ejecutado una version anterior de este script, volve a ejecutarlo completo.

    Por que EXEC(N'...'): el indice usa la columna nueva. Si estuvieran en el mismo lote
    "normal", SQL Server analizaria el CREATE INDEX antes de que exista la columna.
    Con EXEC cada sentencia se analiza recien cuando se ejecuta, en orden.
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

-- 1) Columna nueva (solo si no existe)
IF COL_LENGTH(N'dbo.Turnos', N'VencimientoSena') IS NULL EXEC(N'ALTER TABLE dbo.Turnos ADD VencimientoSena DATETIME2(0) NULL');

-- 2) Indice filtrado para la limpieza de reservas vencidas (solo si no existe)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Turnos_SenaPendienteVencimiento' AND object_id = OBJECT_ID(N'dbo.Turnos')) EXEC(N'CREATE INDEX IX_Turnos_SenaPendienteVencimiento ON dbo.Turnos (VencimientoSena) WHERE EstadoSena = N''Pendiente'' AND VencimientoSena IS NOT NULL');

-- 3) Estado de seña 'Vencida' (solo si el CHECK todavia no lo admite)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Turnos_EstadoSena' AND parent_object_id = OBJECT_ID(N'dbo.Turnos') AND definition LIKE N'%Vencida%')
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Turnos_EstadoSena' AND parent_object_id = OBJECT_ID(N'dbo.Turnos'))
        EXEC(N'ALTER TABLE dbo.Turnos DROP CONSTRAINT CK_Turnos_EstadoSena');
    EXEC(N'ALTER TABLE dbo.Turnos ADD CONSTRAINT CK_Turnos_EstadoSena CHECK (EstadoSena IN (N''NoRequerida'', N''Pendiente'', N''Pagada'', N''Reembolsada'', N''Vencida''))');
END;

-- 4) Verificacion: debe devolver ColumnaCreada_Bytes = 6 (tamano de un DATETIME2(0)), IndiceCreado = 1 y AdmiteVencida = 1
SELECT
    COL_LENGTH(N'dbo.Turnos', N'VencimientoSena') AS ColumnaCreada_Bytes,
    (SELECT COUNT(*) FROM sys.indexes WHERE name = N'IX_Turnos_SenaPendienteVencimiento') AS IndiceCreado,
    (SELECT COUNT(*) FROM sys.check_constraints WHERE name = N'CK_Turnos_EstadoSena' AND definition LIKE N'%Vencida%') AS AdmiteVencida;
