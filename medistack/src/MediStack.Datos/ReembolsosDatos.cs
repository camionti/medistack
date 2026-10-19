using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using MediStack.Dominio;

namespace MediStack.Datos
{
    // Reembolsos de señas. Un reembolso nace Pendiente al cancelar el turno (dentro de la misma
    // transaccion de la cancelacion) y pasa a Completado cuando el administrativo registra que
    // la devolucion se hizo. Todas son manuales:
    //   MercadoPago            -> se devuelve desde la cuenta de la clinica y se carga el ID de Mercado Pago.
    //   Efectivo               -> se entrega con la caja abierta; se registra un egreso.
    //   Tarjeta/Transferencia  -> se transfiere a la cuenta informada al cobrar y se carga el comprobante.
    public sealed class ReembolsosDatos
    {
        private readonly string _cadenaConexion;

        public ReembolsosDatos()
        {
            ConnectionStringSettings configuracion = ConfigurationManager.ConnectionStrings["MediStackDB"];
            if (configuracion == null || string.IsNullOrWhiteSpace(configuracion.ConnectionString))
            {
                throw new ConfigurationErrorsException("No se encontro la cadena de conexion MediStackDB.");
            }

            _cadenaConexion = configuracion.ConnectionString;
        }

        // Se llama DENTRO de la transaccion de la cancelacion, con el turno ya marcado como Cancelado.
        // Si la seña estaba pagada:
        //   devolver = true  -> seña ReembolsoPendiente + reembolso Pendiente (con el medio del cobro original).
        //   devolver = false -> seña Retenida (la clinica se la queda).
        internal static ResultadoCancelacion ResolverSenaAlCancelar(SqlConnection conexion, SqlTransaction transaccion,
            int turnoId, bool devolver, MotivoCancelacion motivo, Guid? solicitadoPorId)
        {
            using (SqlCommand comando = new SqlCommand(@"
                DECLARE @EstadoSena NVARCHAR(20);
                DECLARE @MontoSena DECIMAL(12,2);
                SELECT @EstadoSena = EstadoSena, @MontoSena = MontoSena
                FROM dbo.Turnos WITH (UPDLOCK, HOLDLOCK)
                WHERE TurnoId = @TurnoId;

                -- Sin seña pagada no hay nada que devolver ni retener.
                IF @EstadoSena IS NULL OR @EstadoSena <> N'Pagada'
                BEGIN
                    SELECT N'SinSenaPagada' AS Destino, CAST(NULL AS INT) AS ReembolsoId,
                           CAST(NULL AS NVARCHAR(20)) AS MedioPago, CAST(0 AS DECIMAL(12,2)) AS Monto;
                    RETURN;
                END;

                IF @Devolver = 0
                BEGIN
                    UPDATE dbo.Turnos SET EstadoSena = N'Retenida' WHERE TurnoId = @TurnoId;
                    SELECT N'Retenida' AS Destino, CAST(NULL AS INT) AS ReembolsoId,
                           CAST(NULL AS NVARCHAR(20)) AS MedioPago, @MontoSena AS Monto;
                    RETURN;
                END;

                -- Se devuelve lo que realmente se cobro y por el mismo medio.
                DECLARE @CobroId INT;
                DECLARE @Medio NVARCHAR(20);
                DECLARE @Monto DECIMAL(12,2);
                SELECT TOP (1) @CobroId = CobroId, @Medio = MedioPago, @Monto = MontoCobrado
                FROM dbo.Cobros WITH (UPDLOCK, HOLDLOCK)
                WHERE TurnoId = @TurnoId AND TipoCobro = N'Sena';

                -- Datos viejos sin cobro registrado: se asume pago en el mostrador en efectivo.
                IF @CobroId IS NULL
                BEGIN
                    SET @Medio = N'Efectivo';
                    SET @Monto = @MontoSena;
                END;

                IF @Monto IS NULL OR @Monto <= 0
                BEGIN
                    SELECT N'SinSenaPagada' AS Destino, CAST(NULL AS INT) AS ReembolsoId,
                           CAST(NULL AS NVARCHAR(20)) AS MedioPago, CAST(0 AS DECIMAL(12,2)) AS Monto;
                    RETURN;
                END;

                UPDATE dbo.Turnos SET EstadoSena = N'ReembolsoPendiente' WHERE TurnoId = @TurnoId;

                INSERT INTO dbo.Reembolsos (TurnoId, CobroId, Monto, MedioPago, Motivo, SolicitadoPorId)
                VALUES (@TurnoId, @CobroId, @Monto, @Medio, @Motivo, @SolicitadoPorId);

                SELECT N'Reembolso' AS Destino, CAST(SCOPE_IDENTITY() AS INT) AS ReembolsoId,
                       @Medio AS MedioPago, @Monto AS Monto;",
                conexion, transaccion))
            {
                comando.Parameters.Add("@TurnoId", SqlDbType.Int).Value = turnoId;
                comando.Parameters.Add("@Devolver", SqlDbType.Bit).Value = devolver;
                comando.Parameters.Add("@Motivo", SqlDbType.NVarChar, 30).Value = motivo == MotivoCancelacion.Clinica
                    ? ReglasReembolso.MotivoCancelacionClinica
                    : ReglasReembolso.MotivoCancelacionPaciente;
                comando.Parameters.Add("@SolicitadoPorId", SqlDbType.UniqueIdentifier).Value =
                    solicitadoPorId.HasValue ? (object)solicitadoPorId.Value : DBNull.Value;

                using (SqlDataReader lector = comando.ExecuteReader())
                {
                    if (!lector.Read())
                    {
                        return new ResultadoCancelacion { DestinoSena = DestinoSenaCancelacion.SinSenaPagada };
                    }

                    string destino = lector.GetString(0);
                    return new ResultadoCancelacion
                    {
                        DestinoSena = destino == "Reembolso"
                            ? DestinoSenaCancelacion.Reembolso
                            : destino == "Retenida"
                                ? DestinoSenaCancelacion.Retenida
                                : DestinoSenaCancelacion.SinSenaPagada,
                        ReembolsoId = lector.IsDBNull(1) ? (int?)null : lector.GetInt32(1),
                        MedioPago = lector.IsDBNull(2) ? null : lector.GetString(2),
                        Monto = lector.IsDBNull(3) ? 0m : lector.GetDecimal(3)
                    };
                }
            }
        }

        public DataTable ObtenerReembolsosPendientes()
        {
            return Consultar(SqlListado + @"
                WHERE r.Estado = N'Pendiente'
                ORDER BY r.FechaHoraSolicitud;");
        }

        public DataTable ObtenerHistorialReembolsos(DateTime desde, DateTime hasta)
        {
            return Consultar(SqlListado + @"
                WHERE r.FechaHoraSolicitud >= @Desde AND r.FechaHoraSolicitud < DATEADD(day, 1, @Hasta)
                ORDER BY r.FechaHoraSolicitud DESC;",
                new SqlParameter("@Desde", SqlDbType.Date) { Value = desde.Date },
                new SqlParameter("@Hasta", SqlDbType.Date) { Value = hasta.Date });
        }

        private const string SqlListado = @"
                SELECT r.ReembolsoId, r.TurnoId, r.Monto, r.MedioPago, r.Estado, r.Motivo,
                       r.FechaHoraSolicitud, r.FechaHoraCompletado,
                       t.FechaHora AS FechaTurno,
                       -- A donde devolver: la cuenta informada al cobrar o, si fue online,
                       -- la operacion de Mercado Pago (para encontrar el pago y devolverlo).
                       c.CuentaDevolucion, c.MercadoPagoPagoId,
                       paciente.Nombre + N' ' + paciente.Apellido AS Paciente,
                       profesional.Nombre + N' ' + profesional.Apellido AS Profesional,
                       e.Nombre AS Especialidad,
                       completo.Nombre + N' ' + completo.Apellido AS CompletadoPor,
                       COALESCE(r.Comprobante,
                                CASE WHEN r.EgresoId IS NOT NULL
                                     THEN N'Egreso #' + CAST(r.EgresoId AS NVARCHAR(10)) END) AS Constancia
                FROM dbo.Reembolsos r
                INNER JOIN dbo.Turnos t ON t.TurnoId = r.TurnoId
                INNER JOIN dbo.Usuarios paciente ON paciente.UsuarioId = t.PacienteId
                INNER JOIN dbo.Usuarios profesional ON profesional.UsuarioId = t.ProfesionalId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = t.EspecialidadId
                LEFT JOIN dbo.Cobros c ON c.CobroId = r.CobroId
                LEFT JOIN dbo.Usuarios completo ON completo.UsuarioId = r.CompletadoPorId";

        // El paciente retira la seña en el mostrador: sale plata del cajon, asi que la caja tiene
        // que estar abierta, y se registra un egreso que el cierre de caja descuenta solo.
        public void CompletarReembolsoEfectivo(int reembolsoId, Guid administrativoId)
        {
            Completar(reembolsoId, @"
                IF @Medio <> N'Efectivo'
                    THROW 51122, N'Este reembolso no se devuelve en efectivo.', 1;

                INSERT INTO dbo.Egresos (Fecha, Concepto, Monto, Categoria, Comprobante, AdministrativoId)
                VALUES (CONVERT(date, SYSDATETIME()),
                        N'Devolución de seña del turno #' + CAST(@TurnoId AS NVARCHAR(10)),
                        @Monto, N'Reembolso', N'Reembolso #' + CAST(@ReembolsoId AS NVARCHAR(10)),
                        @AdministrativoId);

                UPDATE dbo.Reembolsos
                SET Estado = N'Completado', FechaHoraCompletado = SYSDATETIME(),
                    CompletadoPorId = @AdministrativoId, EgresoId = CAST(SCOPE_IDENTITY() AS INT)
                WHERE ReembolsoId = @ReembolsoId;",
                administrativoId, null, true);
        }

        // Devolucion hecha por fuera del sistema y registrada con su comprobante:
        //   Tarjeta o transferencia: transferencia a la cuenta informada al cobrar.
        //   Mercado Pago: "Devolver dinero" desde la cuenta de la clinica; se carga el ID que muestra.
        public void CompletarReembolsoManual(int reembolsoId, Guid administrativoId, string comprobante)
        {
            Completar(reembolsoId, @"
                IF @Medio NOT IN (N'Tarjeta', N'Transferencia', N'MercadoPago')
                    THROW 51122, N'Este reembolso no se registra como devolución manual.', 1;

                UPDATE dbo.Reembolsos
                SET Estado = N'Completado', FechaHoraCompletado = SYSDATETIME(),
                    CompletadoPorId = @AdministrativoId, Comprobante = @Constancia,
                    MercadoPagoReembolsoId = CASE WHEN @Medio = N'MercadoPago'
                                                  THEN LEFT(@Constancia, 40)
                                                  ELSE MercadoPagoReembolsoId END
                WHERE ReembolsoId = @ReembolsoId;",
                administrativoId, comprobante, false);
        }

        // Esqueleto comun: bloquea el reembolso, valida que siga pendiente, aplica el paso propio
        // del medio y pasa la seña del turno a Reembolsada. Todo en una transaccion.
        private void Completar(int reembolsoId, string sqlPropio, Guid administrativoId,
            string constancia, bool requiereCajaAbierta)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                conexion.Open();
                using (SqlTransaction transaccion = conexion.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        CobrosDatos.ValidarCobrador(conexion, transaccion, administrativoId);

                        if (requiereCajaAbierta)
                        {
                            CobrosDatos.ValidarCajaAbierta(conexion, transaccion);
                        }

                        using (SqlCommand comando = new SqlCommand(@"
                            DECLARE @Estado NVARCHAR(20);
                            DECLARE @Medio NVARCHAR(20);
                            DECLARE @Monto DECIMAL(12,2);
                            DECLARE @TurnoId INT;
                            SELECT @Estado = Estado, @Medio = MedioPago, @Monto = Monto, @TurnoId = TurnoId
                            FROM dbo.Reembolsos WITH (UPDLOCK, HOLDLOCK)
                            WHERE ReembolsoId = @ReembolsoId;

                            IF @Estado IS NULL
                                THROW 51120, N'El reembolso no existe. Actualizá la página.', 1;

                            IF @Estado = N'Completado'
                                THROW 51121, N'Este reembolso ya fue completado.', 1;
" + sqlPropio + @"

                            UPDATE dbo.Turnos SET EstadoSena = N'Reembolsada'
                            WHERE TurnoId = @TurnoId AND EstadoSena = N'ReembolsoPendiente';",
                            conexion, transaccion))
                        {
                            comando.Parameters.Add("@ReembolsoId", SqlDbType.Int).Value = reembolsoId;
                            comando.Parameters.Add("@AdministrativoId", SqlDbType.UniqueIdentifier).Value = administrativoId;
                            comando.Parameters.Add("@Constancia", SqlDbType.NVarChar, 100).Value =
                                string.IsNullOrWhiteSpace(constancia) ? (object)DBNull.Value : constancia;

                            comando.ExecuteNonQuery();
                            transaccion.Commit();
                        }
                    }
                    catch
                    {
                        transaccion.Rollback();
                        throw;
                    }
                }
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
    }
}
