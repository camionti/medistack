using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using MediStack.Dominio;

namespace MediStack.Datos
{
    public sealed class CobrosDatos
    {
        private readonly string _cadenaConexion;

        public CobrosDatos()
        {
            ConnectionStringSettings configuracion = ConfigurationManager.ConnectionStrings["MediStackDB"];
            if (configuracion == null || string.IsNullOrWhiteSpace(configuracion.ConnectionString))
            {
                throw new ConfigurationErrorsException("No se encontro la cadena de conexion MediStackDB.");
            }

            _cadenaConexion = configuracion.ConnectionString;
        }

        public DataTable ObtenerCobros(
            Guid? pacienteId, Guid? profesionalId, DateTime? desde, DateTime? hasta)
        {
            return Consultar(@"
                SELECT c.CobroId, c.TurnoId, c.TipoCobro, c.MontoBase, c.MontoObraSocial,
                       c.Copago, c.SenaDescontada, c.MontoCobrado, c.MedioPago,
                       c.MontoRecibido, c.Vuelto, c.FechaHoraCobro,
                       CASE WHEN c.TipoCobro = N'Sena' THEN t.EstadoSena ELSE N'Pagado' END AS EstadoCobro,
                       paciente.Nombre + N' ' + paciente.Apellido AS Paciente,
                       profesional.Nombre + N' ' + profesional.Apellido AS Profesional,
                       e.Nombre AS Especialidad, t.FechaHora AS FechaTurno
                FROM dbo.Cobros c
                INNER JOIN dbo.Turnos t ON t.TurnoId = c.TurnoId
                INNER JOIN dbo.Usuarios paciente ON paciente.UsuarioId = t.PacienteId
                INNER JOIN dbo.Usuarios profesional ON profesional.UsuarioId = t.ProfesionalId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = t.EspecialidadId
                WHERE (@PacienteId IS NULL OR t.PacienteId = @PacienteId)
                  AND (@ProfesionalId IS NULL OR t.ProfesionalId = @ProfesionalId)
                  AND (@Desde IS NULL OR c.FechaHoraCobro >= @Desde)
                  AND (@Hasta IS NULL OR c.FechaHoraCobro < DATEADD(day, 1, @Hasta))
                ORDER BY c.FechaHoraCobro DESC, c.CobroId DESC;",
                ParametroGuidNullable("@PacienteId", pacienteId),
                ParametroGuidNullable("@ProfesionalId", profesionalId),
                new SqlParameter("@Desde", SqlDbType.Date) { Value = desde.HasValue ? (object)desde.Value.Date : DBNull.Value },
                new SqlParameter("@Hasta", SqlDbType.Date) { Value = hasta.HasValue ? (object)hasta.Value.Date : DBNull.Value });
        }

        public DataTable ObtenerTurnosCobrables(Guid? pacienteId, Guid? profesionalId)
        {
            return Consultar(@"
                SELECT t.TurnoId, t.Estado, t.EstadoSena, t.MontoSena,
                       t.FechaHora AS FechaTurno, t.PacienteId,
                       paciente.Nombre + N' ' + paciente.Apellido AS Paciente,
                       profesional.Nombre + N' ' + profesional.Apellido AS Profesional,
                       e.Nombre AS Especialidad,
                       CAST(CASE WHEN t.Estado IN (N'Solicitado', N'Confirmado', N'EnEspera')
                                 AND t.EstadoSena = N'Pendiente' AND t.MontoSena > 0
                                 AND NOT EXISTS (SELECT 1 FROM dbo.Cobros c
                                                 WHERE c.TurnoId = t.TurnoId AND c.TipoCobro = N'Sena')
                                 THEN 1 ELSE 0 END AS bit) AS PuedeCobrarSena,
                       CAST(CASE WHEN t.Estado IN (N'EnEspera', N'Atendido')
                                 AND NOT EXISTS (SELECT 1 FROM dbo.Cobros c
                                                 WHERE c.TurnoId = t.TurnoId AND c.TipoCobro = N'Consulta')
                                 THEN 1 ELSE 0 END AS bit) AS PuedeCobrarConsulta
                FROM dbo.Turnos t
                INNER JOIN dbo.Usuarios paciente ON paciente.UsuarioId = t.PacienteId
                INNER JOIN dbo.Usuarios profesional ON profesional.UsuarioId = t.ProfesionalId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = t.EspecialidadId
                WHERE (@PacienteId IS NULL OR t.PacienteId = @PacienteId)
                  AND (@ProfesionalId IS NULL OR t.ProfesionalId = @ProfesionalId)
                  AND
                  (
                      (t.Estado IN (N'Solicitado', N'Confirmado', N'EnEspera') AND t.EstadoSena = N'Pendiente'
                       AND t.MontoSena > 0 AND NOT EXISTS
                           (SELECT 1 FROM dbo.Cobros c WHERE c.TurnoId = t.TurnoId AND c.TipoCobro = N'Sena'))
                      OR
                      (t.Estado IN (N'EnEspera', N'Atendido') AND NOT EXISTS
                           (SELECT 1 FROM dbo.Cobros c WHERE c.TurnoId = t.TurnoId AND c.TipoCobro = N'Consulta'))
                  )
                ORDER BY t.FechaHora DESC, t.TurnoId DESC;",
                ParametroGuidNullable("@PacienteId", pacienteId),
                ParametroGuidNullable("@ProfesionalId", profesionalId));
        }

        public DataTable ObtenerImportesCobro(int turnoId)
        {
            return Consultar(@"
                SELECT t.MontoSena,
                       pe.ValorConsulta AS MontoBase,
                       ROUND(pe.ValorConsulta * COALESCE(convenio.PorcentajeCobertura, 0) / 100.0, 2)
                           AS MontoObraSocial,
                       pe.ValorConsulta -
                           ROUND(pe.ValorConsulta * COALESCE(convenio.PorcentajeCobertura, 0) / 100.0, 2)
                           AS Copago,
                       CASE WHEN t.EstadoSena = N'Pagada'
                            THEN CASE WHEN t.MontoSena >
                                pe.ValorConsulta -
                                    ROUND(pe.ValorConsulta * COALESCE(convenio.PorcentajeCobertura, 0) / 100.0, 2)
                                 THEN pe.ValorConsulta -
                                    ROUND(pe.ValorConsulta * COALESCE(convenio.PorcentajeCobertura, 0) / 100.0, 2)
                                 ELSE t.MontoSena END
                            ELSE 0 END AS SenaDescontada,
                       CASE WHEN t.EstadoSena = N'Pagada'
                            THEN (pe.ValorConsulta -
                                ROUND(pe.ValorConsulta * COALESCE(convenio.PorcentajeCobertura, 0) / 100.0, 2))
                                - CASE WHEN t.MontoSena >
                                    pe.ValorConsulta -
                                        ROUND(pe.ValorConsulta * COALESCE(convenio.PorcentajeCobertura, 0) / 100.0, 2)
                                    THEN pe.ValorConsulta -
                                        ROUND(pe.ValorConsulta * COALESCE(convenio.PorcentajeCobertura, 0) / 100.0, 2)
                                    ELSE t.MontoSena END
                            ELSE pe.ValorConsulta -
                                ROUND(pe.ValorConsulta * COALESCE(convenio.PorcentajeCobertura, 0) / 100.0, 2)
                            END AS SaldoConsulta
                FROM dbo.Turnos t
                INNER JOIN dbo.ProfesionalesEspecialidades pe
                    ON pe.ProfesionalId = t.ProfesionalId AND pe.EspecialidadId = t.EspecialidadId
                LEFT JOIN dbo.Pacientes p ON p.PacienteId = t.PacienteId
                OUTER APPLY
                (
                    SELECT TOP (1) ce.PorcentajeCobertura
                    FROM dbo.Convenios co
                    INNER JOIN dbo.CoberturasEspecialidades ce
                        ON ce.ObraSocialId = co.ObraSocialId
                        AND ce.EspecialidadId = co.EspecialidadId
                    WHERE co.ProfesionalId = t.ProfesionalId
                      AND co.EspecialidadId = t.EspecialidadId
                      AND co.ObraSocialId = p.ObraSocialId
                      AND co.Activo = 1
                      AND co.FechaDesde <= CONVERT(date, t.FechaHora)
                      AND (co.FechaHasta IS NULL OR co.FechaHasta >= CONVERT(date, t.FechaHora))
                    ORDER BY co.FechaDesde DESC
                ) convenio
                WHERE t.TurnoId = @TurnoId;",
                new SqlParameter("@TurnoId", SqlDbType.Int) { Value = turnoId });
        }

        public int RegistrarCobro(int turnoId, string tipoCobro, string medioPago, decimal montoRecibido, Guid cobradorId,
            string cuentaDevolucion)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                conexion.Open();
                using (SqlTransaction transaccion = conexion.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        ValidarCajaAbierta(conexion, transaccion);
                        ValidarCobrador(conexion, transaccion, cobradorId);

                        int nuevoCobroId;
                        using (SqlCommand comando = new SqlCommand(@"
                            DECLARE @PacienteId UNIQUEIDENTIFIER;
                            DECLARE @ProfesionalId UNIQUEIDENTIFIER;
                            DECLARE @EspecialidadId INT;
                            DECLARE @EstadoTurno NVARCHAR(20);
                            DECLARE @EstadoSena NVARCHAR(20);
                            DECLARE @MontoSena DECIMAL(12,2);
                            DECLARE @MontoBase DECIMAL(12,2);
                            DECLARE @PorcentajeCobertura DECIMAL(5,2) = 0;
                            DECLARE @MontoObraSocial DECIMAL(12,2);
                            DECLARE @Copago DECIMAL(12,2);
                            DECLARE @SenaDescontada DECIMAL(12,2) = 0;
                            DECLARE @MontoCobrado DECIMAL(12,2);
                            DECLARE @Vuelto DECIMAL(12,2);

                            SELECT @PacienteId = t.PacienteId, @ProfesionalId = t.ProfesionalId,
                                   @EspecialidadId = t.EspecialidadId, @EstadoTurno = t.Estado,
                                   @EstadoSena = t.EstadoSena, @MontoSena = t.MontoSena
                            FROM dbo.Turnos t WITH (UPDLOCK, HOLDLOCK)
                            WHERE t.TurnoId = @TurnoId;

                            IF @PacienteId IS NULL
                                THROW 51101, N'El turno seleccionado no existe.', 1;

                            IF @TipoCobro = N'Sena'
                            BEGIN
                                IF @EstadoTurno NOT IN (N'Solicitado', N'Confirmado')
                                   OR @EstadoSena <> N'Pendiente' OR @MontoSena <= 0
                                    THROW 51102, N'El turno no tiene una seña pendiente de cobro.', 1;

                                SET @MontoCobrado = @MontoSena;
                            END
                            ELSE IF @TipoCobro = N'Consulta'
                            BEGIN
                                IF @EstadoTurno NOT IN (N'EnEspera', N'Atendido')
                                    THROW 51103, N'Solo se puede cobrar una consulta de un turno en sala de espera o atendido.', 1;

                                SELECT @MontoBase = pe.ValorConsulta
                                FROM dbo.ProfesionalesEspecialidades pe
                                WHERE pe.ProfesionalId = @ProfesionalId
                                  AND pe.EspecialidadId = @EspecialidadId AND pe.Activa = 1;

                                IF @MontoBase IS NULL
                                    THROW 51104, N'No se encontró un valor de consulta activo para el turno.', 1;

                                SELECT TOP (1) @PorcentajeCobertura = ce.PorcentajeCobertura
                                FROM dbo.Pacientes p
                                INNER JOIN dbo.Convenios co ON co.ObraSocialId = p.ObraSocialId
                                    AND co.ProfesionalId = @ProfesionalId
                                    AND co.EspecialidadId = @EspecialidadId
                                INNER JOIN dbo.CoberturasEspecialidades ce
                                    ON ce.ObraSocialId = co.ObraSocialId
                                    AND ce.EspecialidadId = co.EspecialidadId
                                WHERE p.PacienteId = @PacienteId
                                  AND co.Activo = 1
                                  AND co.FechaDesde <= CONVERT(date, (SELECT FechaHora FROM dbo.Turnos WHERE TurnoId = @TurnoId))
                                  AND (co.FechaHasta IS NULL
                                       OR co.FechaHasta >= CONVERT(date, (SELECT FechaHora FROM dbo.Turnos WHERE TurnoId = @TurnoId)))
                                ORDER BY co.FechaDesde DESC;

                                SET @MontoObraSocial = ROUND(@MontoBase * @PorcentajeCobertura / 100.0, 2);
                                SET @Copago = @MontoBase - @MontoObraSocial;
                                IF @EstadoSena = N'Pagada'
                                    SET @SenaDescontada = CASE WHEN @MontoSena > @Copago THEN @Copago ELSE @MontoSena END;
                                SET @MontoCobrado = @Copago - @SenaDescontada;

                                IF EXISTS (SELECT 1 FROM dbo.Cobros WITH (UPDLOCK, HOLDLOCK)
                                           WHERE TurnoId = @TurnoId AND TipoCobro = N'Consulta')
                                    THROW 51105, N'La consulta de este turno ya tiene un cobro registrado.', 1;
                            END
                            ELSE
                                THROW 51106, N'Selecciona un tipo de cobro válido.', 1;

                            SET @Vuelto = @MontoRecibido - @MontoCobrado;

                            IF EXISTS (SELECT 1 FROM dbo.Cobros WITH (UPDLOCK, HOLDLOCK)
                                       WHERE TurnoId = @TurnoId AND TipoCobro = @TipoCobro)
                                THROW 51105, N'Ya existe un cobro de este tipo para el turno.', 1;

                            IF @MontoRecibido < @MontoCobrado
                                THROW 51107, N'El importe recibido no puede ser menor al importe a cobrar.', 1;

                            IF @MedioPago <> N'Efectivo' AND @MontoRecibido <> @MontoCobrado
                                THROW 51108, N'Con tarjeta o transferencia el importe recibido debe coincidir con el importe a cobrar.', 1;

                            IF @TipoCobro = N'Sena'
                                INSERT INTO dbo.Cobros
                                    (TurnoId, TipoCobro, MontoCobrado, MedioPago, MontoRecibido,
                                     Vuelto, UsuarioCobradorId, CuentaDevolucion)
                                VALUES
                                    (@TurnoId, @TipoCobro, @MontoCobrado, @MedioPago, @MontoRecibido,
                                     @Vuelto, @CobradorId, @CuentaDevolucion);
                            ELSE
                                INSERT INTO dbo.Cobros
                                    (TurnoId, TipoCobro, MontoBase, MontoObraSocial, Copago,
                                     SenaDescontada, MontoCobrado, MedioPago, MontoRecibido,
                                     Vuelto, UsuarioCobradorId)
                                VALUES
                                    (@TurnoId, @TipoCobro, @MontoBase, @MontoObraSocial, @Copago,
                                     @SenaDescontada, @MontoCobrado, @MedioPago, @MontoRecibido,
                                     @Vuelto, @CobradorId);

                            IF @TipoCobro = N'Sena'
                                UPDATE dbo.Turnos
                                SET EstadoSena = N'Pagada', FechaHoraSena = SYSDATETIME()
                                WHERE TurnoId = @TurnoId;

                            SELECT CAST(SCOPE_IDENTITY() AS int);",
                            conexion, transaccion))
                        {
                            comando.Parameters.Add("@TurnoId", SqlDbType.Int).Value = turnoId;
                            comando.Parameters.Add("@TipoCobro", SqlDbType.NVarChar, 20).Value = tipoCobro;
                            comando.Parameters.Add("@MedioPago", SqlDbType.NVarChar, 20).Value = medioPago;
                            comando.Parameters.Add("@CuentaDevolucion", SqlDbType.NVarChar, 30).Value =
                                string.IsNullOrWhiteSpace(cuentaDevolucion) ? (object)DBNull.Value : cuentaDevolucion;
                            comando.Parameters.Add("@MontoRecibido", SqlDbType.Decimal).Value = montoRecibido;
                            comando.Parameters["@MontoRecibido"].Precision = 12;
                            comando.Parameters["@MontoRecibido"].Scale = 2;
                            comando.Parameters.Add("@CobradorId", SqlDbType.UniqueIdentifier).Value = cobradorId;
                            nuevoCobroId = Convert.ToInt32(comando.ExecuteScalar());
                        }

                        transaccion.Commit();
                        return nuevoCobroId;
                    }
                    catch
                    {
                        transaccion.Rollback();
                        throw;
                    }
                }
            }
        }

        // Registra la seña de un turno pagada online por Mercado Pago, en UNA transaccion:
        //   cobro tipo 'Sena' (medio 'MercadoPago', sin cajero) + seña 'Pagada' + turno 'Confirmado'.
        // Es idempotente: si el mismo pago llega dos veces (vuelta del navegador y webhook),
        // la segunda vez no hace nada y devuelve YaAcreditada.
        // No valida la caja abierta: los pagos online no pasan por el mostrador.
        public ResultadoAcreditacionSena AcreditarSenaOnline(int turnoId, string mercadoPagoPagoId, decimal montoPagado)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                conexion.Open();
                using (SqlTransaction transaccion = conexion.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        string resultado;
                        using (SqlCommand comando = new SqlCommand(@"
                            DECLARE @Estado NVARCHAR(20);
                            DECLARE @EstadoSena NVARCHAR(20);
                            DECLARE @MontoSena DECIMAL(12,2);

                            -- 1) Idempotencia: este mismo pago ya fue registrado.
                            IF EXISTS (SELECT 1 FROM dbo.Cobros WITH (UPDLOCK, HOLDLOCK)
                                       WHERE MercadoPagoPagoId = @PagoId)
                            BEGIN
                                SELECT N'YaAcreditada';
                                RETURN;
                            END;

                            -- 2) Leer y bloquear el turno (nadie lo modifica hasta el commit).
                            SELECT @Estado = Estado, @EstadoSena = EstadoSena, @MontoSena = MontoSena
                            FROM dbo.Turnos WITH (UPDLOCK, HOLDLOCK)
                            WHERE TurnoId = @TurnoId;

                            -- 3) El turno tiene que seguir esperando este pago.
                            IF @Estado IS NULL
                               OR @EstadoSena <> N'Pendiente'
                               OR @Estado NOT IN (N'Solicitado', N'Confirmado')
                            BEGIN
                                SELECT N'TurnoNoDisponible';
                                RETURN;
                            END;

                            -- 4) Se pago exactamente la seña.
                            IF @MontoPagado <> @MontoSena
                            BEGIN
                                SELECT N'MontoIncorrecto';
                                RETURN;
                            END;

                            -- 5) Registrar el cobro online (sin cajero: UsuarioCobradorId NULL).
                            INSERT INTO dbo.Cobros
                                (TurnoId, TipoCobro, MontoCobrado, MedioPago, MontoRecibido,
                                 Vuelto, UsuarioCobradorId, MercadoPagoPagoId)
                            VALUES
                                (@TurnoId, N'Sena', @MontoSena, N'MercadoPago', @MontoSena,
                                 0, NULL, @PagoId);

                            -- 6) Seña pagada y turno confirmado.
                            UPDATE dbo.Turnos
                            SET EstadoSena = N'Pagada',
                                FechaHoraSena = SYSDATETIME(),
                                Estado = N'Confirmado'
                            WHERE TurnoId = @TurnoId;

                            SELECT N'Acreditada';",
                            conexion, transaccion))
                        {
                            comando.Parameters.Add("@TurnoId", SqlDbType.Int).Value = turnoId;
                            comando.Parameters.Add("@PagoId", SqlDbType.NVarChar, 40).Value = mercadoPagoPagoId;
                            comando.Parameters.Add("@MontoPagado", SqlDbType.Decimal).Value = montoPagado;
                            comando.Parameters["@MontoPagado"].Precision = 12;
                            comando.Parameters["@MontoPagado"].Scale = 2;
                            resultado = Convert.ToString(comando.ExecuteScalar());
                        }

                        transaccion.Commit();
                        return (ResultadoAcreditacionSena)Enum.Parse(typeof(ResultadoAcreditacionSena), resultado);
                    }
                    catch
                    {
                        transaccion.Rollback();
                        throw;
                    }
                }
            }
        }

        public DataTable ObtenerMovimientosCaja(DateTime desde, DateTime hasta)
        {
            return Consultar(@"
                SELECT c.CobroId, c.TurnoId, c.FechaHoraCobro, c.TipoCobro,
                       c.MontoCobrado, c.MedioPago, c.MontoRecibido, c.Vuelto,
                       paciente.Nombre + N' ' + paciente.Apellido AS Paciente,
                       profesional.Nombre + N' ' + profesional.Apellido AS Profesional,
                       e.Nombre AS Especialidad,
                       ISNULL(cobrador.Nombre + N' ' + cobrador.Apellido, N'Online') AS Cobrador
                FROM dbo.Cobros c
                INNER JOIN dbo.Turnos t ON t.TurnoId = c.TurnoId
                INNER JOIN dbo.Usuarios paciente ON paciente.UsuarioId = t.PacienteId
                INNER JOIN dbo.Usuarios profesional ON profesional.UsuarioId = t.ProfesionalId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = t.EspecialidadId
                LEFT JOIN dbo.Usuarios cobrador ON cobrador.UsuarioId = c.UsuarioCobradorId
                WHERE c.FechaHoraCobro >= @Desde AND c.FechaHoraCobro < DATEADD(day, 1, @Hasta)
                ORDER BY c.FechaHoraCobro DESC, c.CobroId DESC;",
                new SqlParameter("@Desde", SqlDbType.Date) { Value = desde.Date },
                new SqlParameter("@Hasta", SqlDbType.Date) { Value = hasta.Date });
        }

        public DataTable ObtenerTotalesCaja(DateTime desde, DateTime hasta)
        {
            return Consultar(@"
                SELECT COUNT(*) AS CantidadCobros,
                       COALESCE(SUM(CASE WHEN MedioPago = N'Efectivo' THEN MontoCobrado ELSE 0 END), 0) AS TotalEfectivo,
                       COALESCE(SUM(CASE WHEN MedioPago = N'Tarjeta' THEN MontoCobrado ELSE 0 END), 0) AS TotalTarjeta,
                       COALESCE(SUM(CASE WHEN MedioPago = N'Transferencia' THEN MontoCobrado ELSE 0 END), 0) AS TotalTransferencia,
                       COALESCE(SUM(MontoCobrado), 0) AS TotalGeneral
                FROM dbo.Cobros
                WHERE FechaHoraCobro >= @Desde AND FechaHoraCobro < DATEADD(day, 1, @Hasta);",
                new SqlParameter("@Desde", SqlDbType.Date) { Value = desde.Date },
                new SqlParameter("@Hasta", SqlDbType.Date) { Value = hasta.Date });
        }

        public DataTable ObtenerCierresCaja(DateTime desde, DateTime hasta)
        {
            return Consultar(@"
                SELECT c.CierreCajaId, c.Fecha, c.FechaHoraCierre,
                       c.TotalEfectivo, c.TotalTarjeta, c.TotalTransferencia,
                       c.TotalGeneral, c.CantidadCobros, c.EfectivoContado, c.Diferencia,
                       c.TotalEgresos, c.FondoInicial,
                       u.Nombre + N' ' + u.Apellido AS Administrativo
                FROM dbo.CierresCaja c
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = c.AdministrativoId
                WHERE c.Fecha >= @Desde AND c.Fecha <= @Hasta
                ORDER BY c.Fecha DESC;",
                new SqlParameter("@Desde", SqlDbType.Date) { Value = desde.Date },
                new SqlParameter("@Hasta", SqlDbType.Date) { Value = hasta.Date });
        }

        public DataTable ObtenerCierreCaja(DateTime fecha)
        {
            return Consultar(@"
                SELECT CierreCajaId, Fecha, TotalEfectivo, TotalTarjeta, TotalTransferencia,
                       TotalGeneral, CantidadCobros, EfectivoContado, Diferencia, FechaHoraCierre,
                       TotalEgresos, FondoInicial
                FROM dbo.CierresCaja
                WHERE Fecha = @Fecha;",
                new SqlParameter("@Fecha", SqlDbType.Date) { Value = fecha.Date });
        }

        // Una fila con: la fecha de hoy segun SQL Server, la caja abierta (si hay, de cualquier dia)
        // y si la caja de hoy ya esta cerrada. De aca sale el estado que muestran las pantallas.
        public DataTable ObtenerEstadoCaja()
        {
            return Consultar(@"
                DECLARE @Hoy DATE = CONVERT(date, SYSDATETIME());

                SELECT @Hoy AS Hoy,
                       a.Fecha AS FechaCajaAbierta, a.FondoInicial, a.FechaHoraApertura,
                       u.Nombre + N' ' + u.Apellido AS Administrativo,
                       CASE WHEN EXISTS (SELECT 1 FROM dbo.CierresCaja WHERE Fecha = @Hoy)
                            THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS CerradaHoy
                FROM (SELECT 1 AS Uno) base
                LEFT JOIN
                (
                    SELECT TOP (1) ap.Fecha, ap.FondoInicial, ap.FechaHoraApertura, ap.AdministrativoId
                    FROM dbo.AperturasCaja ap
                    WHERE ap.Fecha <= @Hoy
                      AND NOT EXISTS (SELECT 1 FROM dbo.CierresCaja c WHERE c.Fecha = ap.Fecha)
                    ORDER BY ap.Fecha
                ) a ON 1 = 1
                LEFT JOIN dbo.Usuarios u ON u.UsuarioId = a.AdministrativoId;");
        }

        public DataTable ObtenerAperturaCaja(DateTime fecha)
        {
            return Consultar(@"
                SELECT a.AperturaCajaId, a.Fecha, a.FondoInicial, a.FechaHoraApertura,
                       u.Nombre + N' ' + u.Apellido AS Administrativo
                FROM dbo.AperturasCaja a
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = a.AdministrativoId
                WHERE a.Fecha = @Fecha;",
                new SqlParameter("@Fecha", SqlDbType.Date) { Value = fecha.Date });
        }

        // Abre la caja de HOY con el fondo inicial (el cambio con el que arranca el cajon).
        // Una sola apertura por dia; una caja cerrada no se vuelve a abrir.
        public void AbrirCaja(Guid administrativoId, decimal fondoInicial)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                conexion.Open();
                using (SqlTransaction transaccion = conexion.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        ValidarCobrador(conexion, transaccion, administrativoId);
                        using (SqlCommand comando = new SqlCommand(@"
                            DECLARE @Fecha DATE = CONVERT(date, SYSDATETIME());

                            IF EXISTS (SELECT 1 FROM dbo.CierresCaja WITH (UPDLOCK, HOLDLOCK) WHERE Fecha = @Fecha)
                                THROW 51110, N'La caja de esta fecha ya está cerrada.', 1;

                            IF EXISTS (SELECT 1 FROM dbo.AperturasCaja WITH (UPDLOCK, HOLDLOCK) WHERE Fecha = @Fecha)
                                THROW 51114, N'La caja de hoy ya está abierta.', 1;

                            -- Regla estricta: no se arranca el dia si quedo una caja anterior sin cerrar.
                            DECLARE @Pendiente DATE;
                            SELECT TOP (1) @Pendiente = a.Fecha
                            FROM dbo.AperturasCaja a WITH (UPDLOCK, HOLDLOCK)
                            WHERE a.Fecha < @Fecha
                              AND NOT EXISTS (SELECT 1 FROM dbo.CierresCaja c WHERE c.Fecha = a.Fecha)
                            ORDER BY a.Fecha;

                            IF @Pendiente IS NOT NULL
                            BEGIN
                                DECLARE @MensajePendiente NVARCHAR(200) =
                                    N'La caja del ' + CONVERT(nvarchar(10), @Pendiente, 103)
                                    + N' quedó sin cerrar. Cerrala antes de abrir la de hoy.';
                                THROW 51115, @MensajePendiente, 1;
                            END;

                            INSERT INTO dbo.AperturasCaja (Fecha, FondoInicial, AdministrativoId)
                            VALUES (@Fecha, @FondoInicial, @AdministrativoId);",
                            conexion, transaccion))
                        {
                            SqlParameter pFondo = comando.Parameters.Add("@FondoInicial", SqlDbType.Decimal);
                            pFondo.Precision = 12;
                            pFondo.Scale = 2;
                            pFondo.Value = fondoInicial;
                            comando.Parameters.Add("@AdministrativoId", SqlDbType.UniqueIdentifier).Value = administrativoId;
                            comando.ExecuteNonQuery();
                        }

                        transaccion.Commit();
                    }
                    catch
                    {
                        transaccion.Rollback();
                        throw;
                    }
                }
            }
        }

        public DataTable ObtenerEgresos(DateTime desde, DateTime hasta)
        {
            return Consultar(@"
                SELECT e.EgresoId, e.Fecha, e.FechaHoraRegistro, e.Concepto, e.Monto, e.Categoria,
                       e.Comprobante, u.Nombre + N' ' + u.Apellido AS Administrativo
                FROM dbo.Egresos e
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = e.AdministrativoId
                WHERE e.Fecha >= @Desde AND e.Fecha <= @Hasta
                ORDER BY e.Fecha DESC, e.EgresoId DESC;",
                new SqlParameter("@Desde", SqlDbType.Date) { Value = desde.Date },
                new SqlParameter("@Hasta", SqlDbType.Date) { Value = hasta.Date });
        }

        public decimal ObtenerTotalEgresos(DateTime fecha)
        {
            DataTable tabla = Consultar(
                "SELECT COALESCE(SUM(Monto), 0) FROM dbo.Egresos WHERE Fecha = @Fecha;",
                new SqlParameter("@Fecha", SqlDbType.Date) { Value = fecha.Date });
            return Convert.ToDecimal(tabla.Rows[0][0]);
        }

        public void RegistrarEgreso(
            Guid administrativoId, string concepto, decimal monto, string categoria, string comprobante)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                conexion.Open();
                using (SqlTransaction transaccion = conexion.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        ValidarCobrador(conexion, transaccion, administrativoId);
                        ValidarCajaAbierta(conexion, transaccion);
                        using (SqlCommand comando = new SqlCommand(@"
                            INSERT INTO dbo.Egresos (Fecha, Concepto, Monto, Categoria, Comprobante, AdministrativoId)
                            VALUES (CONVERT(date, SYSDATETIME()), @Concepto, @Monto, @Categoria, @Comprobante,
                                    @AdministrativoId);",
                            conexion, transaccion))
                        {
                            comando.Parameters.Add("@Concepto", SqlDbType.NVarChar, 200).Value = concepto;
                            SqlParameter pMonto = comando.Parameters.Add("@Monto", SqlDbType.Decimal);
                            pMonto.Precision = 12;
                            pMonto.Scale = 2;
                            pMonto.Value = monto;
                            comando.Parameters.Add("@Categoria", SqlDbType.NVarChar, 30).Value = categoria;
                            comando.Parameters.Add("@Comprobante", SqlDbType.NVarChar, 100).Value =
                                string.IsNullOrWhiteSpace(comprobante) ? (object)DBNull.Value : comprobante;
                            comando.Parameters.Add("@AdministrativoId", SqlDbType.UniqueIdentifier).Value = administrativoId;
                            comando.ExecuteNonQuery();
                        }

                        transaccion.Commit();
                    }
                    catch
                    {
                        transaccion.Rollback();
                        throw;
                    }
                }
            }
        }

        // Cierra la caja que este abierta: la de hoy o, si quedo colgada, la de un dia anterior.
        // Devuelve la fecha de la caja que se cerro.
        public DateTime CerrarCaja(Guid administrativoId, decimal efectivoContado)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                conexion.Open();
                using (SqlTransaction transaccion = conexion.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        DateTime fechaCerrada;
                        ValidarCobrador(conexion, transaccion, administrativoId);
                        using (SqlCommand comando = new SqlCommand(@"
                            -- La caja a cerrar es la apertura que todavia no tiene cierre
                            -- (con la regla estricta, hay como mucho una).
                            DECLARE @Fecha DATE;
                            DECLARE @FondoInicial DECIMAL(12,2);
                            SELECT TOP (1) @Fecha = a.Fecha, @FondoInicial = a.FondoInicial
                            FROM dbo.AperturasCaja a WITH (UPDLOCK, HOLDLOCK)
                            WHERE a.Fecha <= CONVERT(date, SYSDATETIME())
                              AND NOT EXISTS (SELECT 1 FROM dbo.CierresCaja c WITH (UPDLOCK, HOLDLOCK)
                                              WHERE c.Fecha = a.Fecha)
                            ORDER BY a.Fecha;

                            IF @Fecha IS NULL
                                THROW 51113, N'No hay ninguna caja abierta para cerrar.', 1;

                            DECLARE @TotalEfectivo DECIMAL(12,2);
                            DECLARE @TotalTarjeta DECIMAL(12,2);
                            DECLARE @TotalTransferencia DECIMAL(12,2);
                            DECLARE @TotalGeneral DECIMAL(12,2);
                            DECLARE @CantidadCobros INT;
                            DECLARE @TotalEgresos DECIMAL(12,2);

                            -- Egresos del dia: salen del efectivo del cajon y se descuentan del esperado.
                            SELECT @TotalEgresos = COALESCE(SUM(Monto), 0)
                            FROM dbo.Egresos WITH (UPDLOCK, HOLDLOCK)
                            WHERE Fecha = @Fecha;

                            SELECT @CantidadCobros = COUNT(*),
                                   @TotalEfectivo = COALESCE(SUM(CASE WHEN MedioPago = N'Efectivo' THEN MontoCobrado ELSE 0 END), 0),
                                   @TotalTarjeta = COALESCE(SUM(CASE WHEN MedioPago = N'Tarjeta' THEN MontoCobrado ELSE 0 END), 0),
                                   @TotalTransferencia = COALESCE(SUM(CASE WHEN MedioPago = N'Transferencia' THEN MontoCobrado ELSE 0 END), 0),
                                   @TotalGeneral = COALESCE(SUM(MontoCobrado), 0)
                            FROM dbo.Cobros WITH (UPDLOCK, HOLDLOCK, INDEX(IX_Cobros_FechaHoraCobro_MedioPago))
                            WHERE FechaHoraCobro >= @Fecha
                              AND FechaHoraCobro < DATEADD(day, 1, @Fecha)
                              -- Los pagos online (Mercado Pago) no pasan por el mostrador: no entran en el cierre.
                              AND MedioPago <> N'MercadoPago';

                            INSERT INTO dbo.CierresCaja
                                (Fecha, AdministrativoId, TotalEfectivo, TotalTarjeta,
                                 TotalTransferencia, TotalGeneral, CantidadCobros,
                                 EfectivoContado, Diferencia, TotalEgresos, FondoInicial)
                            VALUES
                                (@Fecha, @AdministrativoId, @TotalEfectivo, @TotalTarjeta,
                                 @TotalTransferencia, @TotalGeneral, @CantidadCobros,
                                 @EfectivoContado,
                                 -- En el cajon deberia haber: fondo + efectivo cobrado - egresos.
                                 @EfectivoContado - (@FondoInicial + @TotalEfectivo - @TotalEgresos),
                                 @TotalEgresos, @FondoInicial);

                            SELECT @Fecha;",
                            conexion, transaccion))
                        {
                            comando.Parameters.Add("@AdministrativoId", SqlDbType.UniqueIdentifier).Value = administrativoId;
                            comando.Parameters.Add("@EfectivoContado", SqlDbType.Decimal).Value = efectivoContado;
                            comando.Parameters["@EfectivoContado"].Precision = 12;
                            comando.Parameters["@EfectivoContado"].Scale = 2;
                            fechaCerrada = Convert.ToDateTime(comando.ExecuteScalar());
                        }

                        transaccion.Commit();
                        return fechaCerrada;
                    }
                    catch
                    {
                        transaccion.Rollback();
                        throw;
                    }
                }
            }
        }

        // Registra la seña cobrada en el mostrador al reservar un turno presencial.
        // Se ejecuta DENTRO de la transaccion de la reserva (TurnosDatos.SolicitarTurno):
        // la caja del dia tiene que estar abierta y quien cobra tiene que ser administrativo.
        internal static void RegistrarSenaPresencialEnTransaccion(SqlConnection conexion, SqlTransaction transaccion,
            int turnoId, decimal monto, string medioPago, Guid cobradorId, string cuentaDevolucion)
        {
            ValidarCajaAbierta(conexion, transaccion);
            ValidarCobrador(conexion, transaccion, cobradorId);

            using (SqlCommand comando = new SqlCommand(@"
                INSERT INTO dbo.Cobros
                    (TurnoId, TipoCobro, MontoCobrado, MedioPago, MontoRecibido, Vuelto, UsuarioCobradorId,
                     CuentaDevolucion)
                VALUES
                    (@TurnoId, N'Sena', @Monto, @MedioPago, @Monto, 0, @CobradorId, @CuentaDevolucion);",
                conexion, transaccion))
            {
                comando.Parameters.Add("@TurnoId", SqlDbType.Int).Value = turnoId;
                comando.Parameters.Add("@Monto", SqlDbType.Decimal).Value = monto;
                comando.Parameters["@Monto"].Precision = 12;
                comando.Parameters["@Monto"].Scale = 2;
                comando.Parameters.Add("@MedioPago", SqlDbType.NVarChar, 20).Value = medioPago;
                comando.Parameters.Add("@CobradorId", SqlDbType.UniqueIdentifier).Value = cobradorId;
                comando.Parameters.Add("@CuentaDevolucion", SqlDbType.NVarChar, 30).Value =
                    string.IsNullOrWhiteSpace(cuentaDevolucion) ? (object)DBNull.Value : cuentaDevolucion;
                comando.ExecuteNonQuery();
            }
        }

        // "Hoy" lo decide siempre el reloj de SQL Server (no el del servidor web),
        // asi la validacion y las grabaciones usan exactamente la misma fecha.
        internal static void ValidarCajaAbierta(SqlConnection conexion, SqlTransaction transaccion)
        {
            using (SqlCommand comando = new SqlCommand(@"
                DECLARE @Fecha DATE = CONVERT(date, SYSDATETIME());

                IF EXISTS (SELECT 1 FROM dbo.CierresCaja WITH (UPDLOCK, HOLDLOCK) WHERE Fecha = @Fecha)
                    THROW 51111, N'La caja del día ya está cerrada y no admite nuevos cobros.', 1;

                -- Sin apertura del dia (con su fondo inicial) no se cobra en el mostrador.
                IF NOT EXISTS (SELECT 1 FROM dbo.AperturasCaja WITH (UPDLOCK, HOLDLOCK) WHERE Fecha = @Fecha)
                    THROW 51113, N'La caja del día no está abierta. Abrila desde Caja indicando el fondo inicial.', 1;",
                conexion, transaccion))
            {
                comando.ExecuteNonQuery();
            }
        }

        internal static void ValidarCobrador(
            SqlConnection conexion, SqlTransaction transaccion, Guid cobradorId)
        {
            using (SqlCommand comando = new SqlCommand(@"
                IF NOT EXISTS
                (
                    SELECT 1
                    FROM dbo.Usuarios u WITH (UPDLOCK, HOLDLOCK)
                    INNER JOIN dbo.Roles r ON r.RolId = u.RolId
                    WHERE u.UsuarioId = @CobradorId AND u.Activo = 1
                      AND r.Codigo = N'ADMINISTRATIVO'
                )
                    THROW 51112, N'El usuario no está habilitado para administrar cobros y caja.', 1;",
                conexion, transaccion))
            {
                comando.Parameters.Add("@CobradorId", SqlDbType.UniqueIdentifier).Value = cobradorId;
                comando.ExecuteNonQuery();
            }
        }

        public ComprobanteCobroModel ObtenerComprobanteCobro(int cobroId)
        {
            DataTable tabla = Consultar(@"
                SELECT c.CobroId, c.TurnoId, c.FechaHoraCobro, c.TipoCobro, c.MedioPago,
                       COALESCE(c.MontoBase, c.MontoCobrado) AS MontoBase,
                       COALESCE(c.MontoObraSocial, 0) AS MontoObraSocial,
                       COALESCE(c.Copago, c.MontoCobrado) AS Copago,
                       COALESCE(c.SenaDescontada, 0) AS SenaDescontada,
                       c.MontoCobrado, c.MontoRecibido, c.Vuelto,
                       t.PacienteId,
                       paciente.Nombre + N' ' + paciente.Apellido AS PacienteNombreCompleto,
                       paciente.DNI AS PacienteDni,
                       COALESCE(os.Nombre, N'Particular') AS ObraSocialNombre,
                       COALESCE(p.NumeroAfiliado, N'-') AS NumeroAfiliado,
                       t.FechaHora AS FechaHoraTurno,
                       profesional.Nombre + N' ' + profesional.Apellido AS ProfesionalNombreCompleto,
                       COALESCE(prof.MatriculaProfesional, N'-') AS ProfesionalMatricula,
                       e.Nombre AS EspecialidadNombre,
                       ISNULL(cajero.Nombre + N' ' + cajero.Apellido, N'Online') AS CajeroNombreCompleto
                FROM dbo.Cobros c
                INNER JOIN dbo.Turnos t ON t.TurnoId = c.TurnoId
                INNER JOIN dbo.Usuarios paciente ON paciente.UsuarioId = t.PacienteId
                LEFT JOIN dbo.Pacientes p ON p.PacienteId = t.PacienteId
                LEFT JOIN dbo.ObrasSociales os ON os.ObraSocialId = p.ObraSocialId
                INNER JOIN dbo.Usuarios profesional ON profesional.UsuarioId = t.ProfesionalId
                LEFT JOIN dbo.Profesionales prof ON prof.ProfesionalId = t.ProfesionalId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = t.EspecialidadId
                LEFT JOIN dbo.Usuarios cajero ON cajero.UsuarioId = c.UsuarioCobradorId
                WHERE c.CobroId = @CobroId;",
                new SqlParameter("@CobroId", SqlDbType.Int) { Value = cobroId });

            if (tabla.Rows.Count == 0)
            {
                return null;
            }

            DataRow fila = tabla.Rows[0];
            return new ComprobanteCobroModel
            {
                CobroId = Convert.ToInt32(fila["CobroId"]),
                TurnoId = Convert.ToInt32(fila["TurnoId"]),
                FechaHoraCobro = Convert.ToDateTime(fila["FechaHoraCobro"]),
                TipoCobro = Convert.ToString(fila["TipoCobro"]),
                MedioPago = Convert.ToString(fila["MedioPago"]),
                MontoBase = Convert.ToDecimal(fila["MontoBase"]),
                MontoObraSocial = Convert.ToDecimal(fila["MontoObraSocial"]),
                Copago = Convert.ToDecimal(fila["Copago"]),
                SenaDescontada = Convert.ToDecimal(fila["SenaDescontada"]),
                MontoCobrado = Convert.ToDecimal(fila["MontoCobrado"]),
                MontoRecibido = Convert.ToDecimal(fila["MontoRecibido"]),
                Vuelto = Convert.ToDecimal(fila["Vuelto"]),
                PacienteId = (Guid)fila["PacienteId"],
                PacienteNombreCompleto = Convert.ToString(fila["PacienteNombreCompleto"]),
                PacienteDni = Convert.ToString(fila["PacienteDni"]),
                ObraSocialNombre = Convert.ToString(fila["ObraSocialNombre"]),
                NumeroAfiliado = Convert.ToString(fila["NumeroAfiliado"]),
                FechaHoraTurno = Convert.ToDateTime(fila["FechaHoraTurno"]),
                ProfesionalNombreCompleto = Convert.ToString(fila["ProfesionalNombreCompleto"]),
                ProfesionalMatricula = Convert.ToString(fila["ProfesionalMatricula"]),
                EspecialidadNombre = Convert.ToString(fila["EspecialidadNombre"]),
                CajeroNombreCompleto = Convert.ToString(fila["CajeroNombreCompleto"])
            };
        }

        private DataTable Consultar(string consulta, params SqlParameter[] parametros)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            using (SqlCommand comando = new SqlCommand(consulta, conexion))
            using (SqlDataAdapter adaptador = new SqlDataAdapter(comando))
            {
                if (parametros != null)
                {
                    comando.Parameters.AddRange(parametros);
                }

                DataTable resultado = new DataTable();
                adaptador.Fill(resultado);
                return resultado;
            }
        }

        private static SqlParameter ParametroGuidNullable(string nombre, Guid? valor)
        {
            return new SqlParameter(nombre, SqlDbType.UniqueIdentifier)
            {
                Value = valor.HasValue ? (object)valor.Value : DBNull.Value
            };
        }
    }
}
