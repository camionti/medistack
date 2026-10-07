using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

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
                       CAST(CASE WHEN t.Estado IN (N'Solicitado', N'Confirmado')
                                 AND t.EstadoSena = N'Pendiente' AND t.MontoSena > 0
                                 AND NOT EXISTS (SELECT 1 FROM dbo.Cobros c
                                                 WHERE c.TurnoId = t.TurnoId AND c.TipoCobro = N'Sena')
                                 THEN 1 ELSE 0 END AS bit) AS PuedeCobrarSena,
                       CAST(CASE WHEN t.Estado = N'Atendido'
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
                      (t.Estado IN (N'Solicitado', N'Confirmado') AND t.EstadoSena = N'Pendiente'
                       AND t.MontoSena > 0 AND NOT EXISTS
                           (SELECT 1 FROM dbo.Cobros c WHERE c.TurnoId = t.TurnoId AND c.TipoCobro = N'Sena'))
                      OR
                      (t.Estado = N'Atendido' AND NOT EXISTS
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

        public void RegistrarCobro(int turnoId, string tipoCobro, string medioPago, decimal montoRecibido, Guid cobradorId)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                conexion.Open();
                using (SqlTransaction transaccion = conexion.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        ValidarCajaAbierta(conexion, transaccion, DateTime.Today);
                        ValidarCobrador(conexion, transaccion, cobradorId);

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
                                IF @EstadoTurno <> N'Atendido'
                                    THROW 51103, N'Solo se puede cobrar una consulta de un turno atendido.', 1;

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
                                     Vuelto, UsuarioCobradorId)
                                VALUES
                                    (@TurnoId, @TipoCobro, @MontoCobrado, @MedioPago, @MontoRecibido,
                                     @Vuelto, @CobradorId);
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
                                WHERE TurnoId = @TurnoId;",
                            conexion, transaccion))
                        {
                            comando.Parameters.Add("@TurnoId", SqlDbType.Int).Value = turnoId;
                            comando.Parameters.Add("@TipoCobro", SqlDbType.NVarChar, 20).Value = tipoCobro;
                            comando.Parameters.Add("@MedioPago", SqlDbType.NVarChar, 20).Value = medioPago;
                            comando.Parameters.Add("@MontoRecibido", SqlDbType.Decimal).Value = montoRecibido;
                            comando.Parameters["@MontoRecibido"].Precision = 12;
                            comando.Parameters["@MontoRecibido"].Scale = 2;
                            comando.Parameters.Add("@CobradorId", SqlDbType.UniqueIdentifier).Value = cobradorId;
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

        public DataTable ObtenerMovimientosCaja(DateTime desde, DateTime hasta)
        {
            return Consultar(@"
                SELECT c.CobroId, c.TurnoId, c.FechaHoraCobro, c.TipoCobro,
                       c.MontoCobrado, c.MedioPago, c.MontoRecibido, c.Vuelto,
                       paciente.Nombre + N' ' + paciente.Apellido AS Paciente,
                       profesional.Nombre + N' ' + profesional.Apellido AS Profesional,
                       e.Nombre AS Especialidad,
                       ISNULL(cobrador.Nombre + N' ' + cobrador.Apellido, N'No informado') AS Cobrador
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
                       TotalGeneral, CantidadCobros, EfectivoContado, Diferencia, FechaHoraCierre
                FROM dbo.CierresCaja
                WHERE Fecha = @Fecha;",
                new SqlParameter("@Fecha", SqlDbType.Date) { Value = fecha.Date });
        }

        public void CerrarCaja(DateTime fecha, Guid administrativoId, decimal efectivoContado)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                conexion.Open();
                using (SqlTransaction transaccion = conexion.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        if (fecha.Date != DateTime.Today)
                        {
                            throw new InvalidOperationException("La caja solo se puede cerrar para la fecha de hoy.");
                        }

                        ValidarCobrador(conexion, transaccion, administrativoId);
                        using (SqlCommand comando = new SqlCommand(@"
                            IF EXISTS (SELECT 1 FROM dbo.CierresCaja WITH (UPDLOCK, HOLDLOCK)
                                       WHERE Fecha = @Fecha)
                                THROW 51110, N'La caja de esta fecha ya está cerrada.', 1;

                            DECLARE @TotalEfectivo DECIMAL(12,2);
                            DECLARE @TotalTarjeta DECIMAL(12,2);
                            DECLARE @TotalTransferencia DECIMAL(12,2);
                            DECLARE @TotalGeneral DECIMAL(12,2);
                            DECLARE @CantidadCobros INT;

                            SELECT @CantidadCobros = COUNT(*),
                                   @TotalEfectivo = COALESCE(SUM(CASE WHEN MedioPago = N'Efectivo' THEN MontoCobrado ELSE 0 END), 0),
                                   @TotalTarjeta = COALESCE(SUM(CASE WHEN MedioPago = N'Tarjeta' THEN MontoCobrado ELSE 0 END), 0),
                                   @TotalTransferencia = COALESCE(SUM(CASE WHEN MedioPago = N'Transferencia' THEN MontoCobrado ELSE 0 END), 0),
                                   @TotalGeneral = COALESCE(SUM(MontoCobrado), 0)
                            FROM dbo.Cobros WITH (UPDLOCK, HOLDLOCK, INDEX(IX_Cobros_FechaHoraCobro_MedioPago))
                            WHERE FechaHoraCobro >= @Fecha
                              AND FechaHoraCobro < DATEADD(day, 1, @Fecha);

                            INSERT INTO dbo.CierresCaja
                                (Fecha, AdministrativoId, TotalEfectivo, TotalTarjeta,
                                 TotalTransferencia, TotalGeneral, CantidadCobros,
                                 EfectivoContado, Diferencia)
                            VALUES
                                (@Fecha, @AdministrativoId, @TotalEfectivo, @TotalTarjeta,
                                 @TotalTransferencia, @TotalGeneral, @CantidadCobros,
                                 @EfectivoContado, @EfectivoContado - @TotalEfectivo);",
                            conexion, transaccion))
                        {
                            comando.Parameters.Add("@Fecha", SqlDbType.Date).Value = fecha.Date;
                            comando.Parameters.Add("@AdministrativoId", SqlDbType.UniqueIdentifier).Value = administrativoId;
                            comando.Parameters.Add("@EfectivoContado", SqlDbType.Decimal).Value = efectivoContado;
                            comando.Parameters["@EfectivoContado"].Precision = 12;
                            comando.Parameters["@EfectivoContado"].Scale = 2;
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

        private static void ValidarCajaAbierta(
            SqlConnection conexion, SqlTransaction transaccion, DateTime fecha)
        {
            using (SqlCommand comando = new SqlCommand(@"
                IF EXISTS (SELECT 1 FROM dbo.CierresCaja WITH (UPDLOCK, HOLDLOCK) WHERE Fecha = @Fecha)
                    THROW 51111, N'La caja del día ya está cerrada y no admite nuevos cobros.', 1;",
                conexion, transaccion))
            {
                comando.Parameters.Add("@Fecha", SqlDbType.Date).Value = fecha.Date;
                comando.ExecuteNonQuery();
            }
        }

        private static void ValidarCobrador(
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
