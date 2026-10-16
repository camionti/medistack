/*
    MediStackDB - Migracion 03: egresos de caja
    Ejecutar DESPUES de 01_MediStackDB.sql y 02_SenaOnline.sql, sobre una base ya creada.

    Como ejecutarlo: abrir este archivo y ejecutarlo COMPLETO, de una sola vez
      - DBeaver: Ctrl+A (seleccionar todo) y Ctrl+Enter, conectado a MediStackDB.
      - SSMS: F5.
    No usa GO. Es idempotente: se puede ejecutar mas de una vez sin errores ni duplicados.

    Que agrega (no borra ni modifica datos existentes):
    - dbo.Egresos: gastos menores pagados en efectivo desde el cajon (insumos, viaticos, etc.).
    - dbo.CierresCaja.TotalEgresos: egresos del dia congelados al cerrar la caja (0 para los
      cierres ya existentes).
    - CK_CierresCaja_Importes: se recrea con la formula
        Diferencia = EfectivoContado - (TotalEfectivo - TotalEgresos)
      Para los cierres existentes TotalEgresos = 0, asi que la formula vieja sigue valiendo.
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

-- 1) Tabla de egresos
IF OBJECT_ID(N'dbo.Egresos', N'U') IS NULL
EXEC(N'
CREATE TABLE dbo.Egresos
(
    EgresoId INT IDENTITY(1,1) NOT NULL,
    Fecha DATE NOT NULL CONSTRAINT DF_Egresos_Fecha DEFAULT (CONVERT(date, SYSDATETIME())),
    FechaHoraRegistro DATETIME2(0) NOT NULL CONSTRAINT DF_Egresos_FechaHora DEFAULT (SYSDATETIME()),
    Concepto NVARCHAR(200) NOT NULL,
    Monto DECIMAL(12,2) NOT NULL,
    Categoria NVARCHAR(30) NOT NULL,
    Comprobante NVARCHAR(100) NULL,
    AdministrativoId UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_Egresos PRIMARY KEY (EgresoId),
    CONSTRAINT FK_Egresos_Usuarios FOREIGN KEY (AdministrativoId) REFERENCES dbo.Usuarios (UsuarioId),
    CONSTRAINT CK_Egresos_Monto CHECK (Monto > 0),
    CONSTRAINT CK_Egresos_Categoria CHECK (Categoria IN (N''Insumos'', N''Viaticos'', N''Mantenimiento'', N''Otros''))
)');

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Egresos_Fecha' AND object_id = OBJECT_ID(N'dbo.Egresos'))
    EXEC(N'CREATE INDEX IX_Egresos_Fecha ON dbo.Egresos (Fecha) INCLUDE (Monto)');

-- 2) Columna TotalEgresos en los cierres (solo si no existe)
IF COL_LENGTH(N'dbo.CierresCaja', N'TotalEgresos') IS NULL
    EXEC(N'ALTER TABLE dbo.CierresCaja ADD TotalEgresos DECIMAL(12,2) NOT NULL
           CONSTRAINT DF_CierresCaja_TotalEgresos DEFAULT (0)');

-- 3) CHECK de importes con la formula nueva (solo si todavia no menciona TotalEgresos)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = N'CK_CierresCaja_Importes'
                 AND parent_object_id = OBJECT_ID(N'dbo.CierresCaja')
                 AND definition LIKE N'%TotalEgresos%')
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = N'CK_CierresCaja_Importes' AND parent_object_id = OBJECT_ID(N'dbo.CierresCaja'))
        EXEC(N'ALTER TABLE dbo.CierresCaja DROP CONSTRAINT CK_CierresCaja_Importes');
    EXEC(N'ALTER TABLE dbo.CierresCaja ADD CONSTRAINT CK_CierresCaja_Importes CHECK
    (
        TotalEfectivo >= 0 AND TotalTarjeta >= 0 AND TotalTransferencia >= 0 AND TotalEgresos >= 0
        AND TotalGeneral = TotalEfectivo + TotalTarjeta + TotalTransferencia
        AND CantidadCobros >= 0 AND EfectivoContado >= 0
        AND Diferencia = EfectivoContado - (TotalEfectivo - TotalEgresos)
    )');
END;
