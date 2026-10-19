/*
    MediStackDB - Migracion 05: apertura de caja con fondo inicial
    Ejecutar DESPUES de 01, 02, 03 y 04, sobre una base ya creada.

    Como ejecutarlo: abrir este archivo y ejecutarlo COMPLETO, de una sola vez
      - DBeaver: Ctrl+A (seleccionar todo) y Ctrl+Enter, conectado a MediStackDB.
      - SSMS: F5.
    No usa GO. Es idempotente: se puede ejecutar mas de una vez sin errores ni duplicados.

    Que agrega (no borra ni modifica datos existentes):
    - dbo.AperturasCaja: una apertura por dia, con el fondo inicial (cambio) con el que arranca el cajon.
      Sin apertura del dia no se puede cobrar en el mostrador ni registrar egresos.
      Los pagos online de Mercado Pago no dependen de la caja.
    - dbo.CierresCaja.FondoInicial: fondo congelado al cerrar (0 para los cierres ya existentes).
    - CK_CierresCaja_Importes: se recrea con la formula
        Diferencia = EfectivoContado - (FondoInicial + TotalEfectivo - TotalEgresos)
      Para los cierres existentes FondoInicial = 0, asi que la formula anterior sigue valiendo.
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

-- 1) Tabla de aperturas (una por dia)
IF OBJECT_ID(N'dbo.AperturasCaja', N'U') IS NULL
EXEC(N'
CREATE TABLE dbo.AperturasCaja
(
    AperturaCajaId INT IDENTITY(1,1) NOT NULL,
    Fecha DATE NOT NULL,
    FondoInicial DECIMAL(12,2) NOT NULL,
    AdministrativoId UNIQUEIDENTIFIER NOT NULL,
    FechaHoraApertura DATETIME2(0) NOT NULL CONSTRAINT DF_AperturasCaja_FechaHora DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_AperturasCaja PRIMARY KEY (AperturaCajaId),
    CONSTRAINT UX_AperturasCaja_Fecha UNIQUE (Fecha),
    CONSTRAINT FK_AperturasCaja_Usuarios FOREIGN KEY (AdministrativoId) REFERENCES dbo.Usuarios (UsuarioId),
    CONSTRAINT CK_AperturasCaja_FondoInicial CHECK (FondoInicial >= 0)
)');

-- 2) Columna FondoInicial en los cierres (solo si no existe)
IF COL_LENGTH(N'dbo.CierresCaja', N'FondoInicial') IS NULL
    EXEC(N'ALTER TABLE dbo.CierresCaja ADD FondoInicial DECIMAL(12,2) NOT NULL
           CONSTRAINT DF_CierresCaja_FondoInicial DEFAULT (0)');

-- 3) CHECK de importes con la formula nueva (solo si todavia no menciona FondoInicial)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = N'CK_CierresCaja_Importes'
                 AND parent_object_id = OBJECT_ID(N'dbo.CierresCaja')
                 AND definition LIKE N'%FondoInicial%')
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = N'CK_CierresCaja_Importes' AND parent_object_id = OBJECT_ID(N'dbo.CierresCaja'))
        EXEC(N'ALTER TABLE dbo.CierresCaja DROP CONSTRAINT CK_CierresCaja_Importes');
    EXEC(N'ALTER TABLE dbo.CierresCaja ADD CONSTRAINT CK_CierresCaja_Importes CHECK
    (
        TotalEfectivo >= 0 AND TotalTarjeta >= 0 AND TotalTransferencia >= 0 AND TotalEgresos >= 0
        AND FondoInicial >= 0
        AND TotalGeneral = TotalEfectivo + TotalTarjeta + TotalTransferencia
        AND CantidadCobros >= 0 AND EfectivoContado >= 0
        AND Diferencia = EfectivoContado - (FondoInicial + TotalEfectivo - TotalEgresos)
    )');
END;

-- Verificacion: deberia devolver 1 · 1 · 1
SELECT
    CASE WHEN OBJECT_ID(N'dbo.AperturasCaja', N'U') IS NOT NULL THEN 1 ELSE 0 END AS TablaAperturas,
    CASE WHEN COL_LENGTH(N'dbo.CierresCaja', N'FondoInicial') IS NOT NULL THEN 1 ELSE 0 END AS ColumnaFondo,
    (SELECT COUNT(*) FROM sys.check_constraints
     WHERE name = N'CK_CierresCaja_Importes' AND definition LIKE N'%FondoInicial%') AS CheckNuevo;
