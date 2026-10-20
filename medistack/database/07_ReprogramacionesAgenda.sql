/*
    Relacion entre un turno nuevo y el turno original cancelado por reprogramacion.
    La migracion es idempotente y conserva los turnos existentes.
*/
USE MediStackDB;

IF COL_LENGTH(N'dbo.Turnos', N'TurnoReprogramadoDesdeId') IS NULL
BEGIN
    ALTER TABLE dbo.Turnos ADD TurnoReprogramadoDesdeId INT NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_Turnos_ReprogramadoDesde'
      AND parent_object_id = OBJECT_ID(N'dbo.Turnos')
)
BEGIN
    ALTER TABLE dbo.Turnos
        ADD CONSTRAINT FK_Turnos_ReprogramadoDesde
        FOREIGN KEY (TurnoReprogramadoDesdeId) REFERENCES dbo.Turnos (TurnoId);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Turnos_ReprogramadoDesde'
      AND object_id = OBJECT_ID(N'dbo.Turnos')
)
BEGIN
    CREATE INDEX IX_Turnos_ReprogramadoDesde
        ON dbo.Turnos (TurnoReprogramadoDesdeId)
        WHERE TurnoReprogramadoDesdeId IS NOT NULL;
END;
GO
