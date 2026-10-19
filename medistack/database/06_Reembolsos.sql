/*
    MediStackDB - Migracion 06: reembolsos de señas
    Ejecutar DESPUES de 01 a 05, sobre una base ya creada.

    Como ejecutarlo: abrir este archivo y ejecutarlo COMPLETO, de una sola vez
      - DBeaver: Ctrl+A (seleccionar todo) y Ctrl+Enter, conectado a MediStackDB.
      - SSMS: F5.
    No usa GO. Es idempotente: se puede ejecutar mas de una vez sin errores ni duplicados.

    Que agrega (no borra ni modifica datos existentes):
    - CK_Turnos_EstadoSena: suma 'ReembolsoPendiente' (se cancelo y hay que devolver la seña)
      y 'Retenida' (cancelacion fuera de plazo o ausente: la clinica se queda con la seña).
      'Reembolsada' ahora significa que la devolucion se hizo de verdad.
    - CK_Egresos_Categoria: suma 'Reembolso' (seña devuelta en efectivo desde el cajon).
    - dbo.Cobros.CuentaDevolucion: CBU, CVU o alias que se pide al cobrar una seña en el mostrador
      con tarjeta o transferencia, para poder devolverla si el turno se cancela.
    - dbo.Reembolsos: una devolucion por turno, con su medio (el mismo con el que se pago la seña).
      Todas se hacen a mano y se registran con su constancia:
        MercadoPago            -> se devuelve desde la cuenta de la clinica ("Devolver dinero") y se
                                  carga el ID que muestra Mercado Pago.
        Efectivo               -> se entrega en el mostrador con la caja abierta; genera un egreso.
        Tarjeta/Transferencia  -> se transfiere a la cuenta informada al cobrar y se carga el comprobante.
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

-- 1) Estados de seña nuevos (solo si el CHECK todavia no los admite)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Turnos_EstadoSena' AND parent_object_id = OBJECT_ID(N'dbo.Turnos') AND definition LIKE N'%Retenida%')
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Turnos_EstadoSena' AND parent_object_id = OBJECT_ID(N'dbo.Turnos'))
        EXEC(N'ALTER TABLE dbo.Turnos DROP CONSTRAINT CK_Turnos_EstadoSena');
    EXEC(N'ALTER TABLE dbo.Turnos ADD CONSTRAINT CK_Turnos_EstadoSena CHECK (EstadoSena IN (N''NoRequerida'', N''Pendiente'', N''Pagada'', N''Reembolsada'', N''Vencida'', N''ReembolsoPendiente'', N''Retenida''))');
END;

-- 2) Categoria de egreso 'Reembolso' (solo si el CHECK todavia no la admite)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Egresos_Categoria' AND parent_object_id = OBJECT_ID(N'dbo.Egresos') AND definition LIKE N'%Reembolso%')
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Egresos_Categoria' AND parent_object_id = OBJECT_ID(N'dbo.Egresos'))
        EXEC(N'ALTER TABLE dbo.Egresos DROP CONSTRAINT CK_Egresos_Categoria');
    EXEC(N'ALTER TABLE dbo.Egresos ADD CONSTRAINT CK_Egresos_Categoria CHECK (Categoria IN (N''Insumos'', N''Viaticos'', N''Mantenimiento'', N''Otros'', N''Reembolso''))');
END;

-- 3) Tabla de reembolsos
IF OBJECT_ID(N'dbo.Reembolsos', N'U') IS NULL
EXEC(N'
CREATE TABLE dbo.Reembolsos
(
    ReembolsoId INT IDENTITY(1,1) NOT NULL,
    TurnoId INT NOT NULL,
    CobroId INT NULL,
    Monto DECIMAL(12,2) NOT NULL,
    MedioPago NVARCHAR(20) NOT NULL,
    Motivo NVARCHAR(30) NOT NULL,
    Estado NVARCHAR(20) NOT NULL CONSTRAINT DF_Reembolsos_Estado DEFAULT (N''Pendiente''),
    FechaHoraSolicitud DATETIME2(0) NOT NULL CONSTRAINT DF_Reembolsos_FechaHoraSolicitud DEFAULT (SYSDATETIME()),
    SolicitadoPorId UNIQUEIDENTIFIER NULL,
    FechaHoraCompletado DATETIME2(0) NULL,
    CompletadoPorId UNIQUEIDENTIFIER NULL,
    MercadoPagoReembolsoId NVARCHAR(40) NULL,
    EgresoId INT NULL,
    Comprobante NVARCHAR(100) NULL,
    CONSTRAINT PK_Reembolsos PRIMARY KEY (ReembolsoId),
    CONSTRAINT UX_Reembolsos_Turno UNIQUE (TurnoId),
    CONSTRAINT FK_Reembolsos_Turnos FOREIGN KEY (TurnoId) REFERENCES dbo.Turnos (TurnoId),
    CONSTRAINT FK_Reembolsos_Cobros FOREIGN KEY (CobroId) REFERENCES dbo.Cobros (CobroId),
    CONSTRAINT FK_Reembolsos_SolicitadoPor FOREIGN KEY (SolicitadoPorId) REFERENCES dbo.Usuarios (UsuarioId),
    CONSTRAINT FK_Reembolsos_CompletadoPor FOREIGN KEY (CompletadoPorId) REFERENCES dbo.Usuarios (UsuarioId),
    CONSTRAINT FK_Reembolsos_Egresos FOREIGN KEY (EgresoId) REFERENCES dbo.Egresos (EgresoId),
    CONSTRAINT CK_Reembolsos_Monto CHECK (Monto > 0),
    CONSTRAINT CK_Reembolsos_MedioPago CHECK (MedioPago IN (N''Efectivo'', N''Tarjeta'', N''Transferencia'', N''MercadoPago'')),
    CONSTRAINT CK_Reembolsos_Motivo CHECK (Motivo IN (N''CancelacionPaciente'', N''CancelacionClinica'')),
    CONSTRAINT CK_Reembolsos_Estado CHECK (Estado IN (N''Pendiente'', N''Completado'')),
    -- Un reembolso completado tiene que tener la constancia que corresponde a su medio.
    CONSTRAINT CK_Reembolsos_Completado CHECK
    (
        (Estado = N''Pendiente'' AND FechaHoraCompletado IS NULL)
        OR
        (Estado = N''Completado'' AND FechaHoraCompletado IS NOT NULL AND
            (   (MedioPago = N''MercadoPago'' AND MercadoPagoReembolsoId IS NOT NULL)
             OR (MedioPago = N''Efectivo'' AND EgresoId IS NOT NULL)
             OR (MedioPago IN (N''Tarjeta'', N''Transferencia'') AND Comprobante IS NOT NULL)))
    )
)');

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Reembolsos_Estado' AND object_id = OBJECT_ID(N'dbo.Reembolsos'))
    EXEC(N'CREATE INDEX IX_Reembolsos_Estado ON dbo.Reembolsos (Estado, FechaHoraSolicitud)');

-- Una version anterior de este script tenia la columna UltimoError (devolucion automatica por
-- Mercado Pago, que se quito): si existe, se elimina.
IF COL_LENGTH(N'dbo.Reembolsos', N'UltimoError') IS NOT NULL
    EXEC(N'ALTER TABLE dbo.Reembolsos DROP COLUMN UltimoError');

-- 4) Cuenta para devoluciones en los cobros (seña con tarjeta o transferencia en el mostrador)
IF COL_LENGTH(N'dbo.Cobros', N'CuentaDevolucion') IS NULL
    EXEC(N'ALTER TABLE dbo.Cobros ADD CuentaDevolucion NVARCHAR(30) NULL');

-- Verificacion: deberia devolver 1 · 1 · 1 · 1
SELECT
    (SELECT COUNT(*) FROM sys.check_constraints WHERE name = N'CK_Turnos_EstadoSena' AND definition LIKE N'%Retenida%') AS EstadosSenaNuevos,
    (SELECT COUNT(*) FROM sys.check_constraints WHERE name = N'CK_Egresos_Categoria' AND definition LIKE N'%Reembolso%') AS CategoriaReembolso,
    CASE WHEN OBJECT_ID(N'dbo.Reembolsos', N'U') IS NOT NULL THEN 1 ELSE 0 END AS TablaReembolsos,
    CASE WHEN COL_LENGTH(N'dbo.Cobros', N'CuentaDevolucion') IS NOT NULL THEN 1 ELSE 0 END AS CuentaDevolucion;
