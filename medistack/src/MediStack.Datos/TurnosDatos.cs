using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using MediStack.Dominio;

namespace MediStack.Datos
{
    public sealed class TurnosDatos
    {
        private readonly string _cadenaConexion;

        public TurnosDatos()
        {
            ConnectionStringSettings configuracion = ConfigurationManager.ConnectionStrings["MediStackDB"];
            if (configuracion == null || string.IsNullOrWhiteSpace(configuracion.ConnectionString))
            {
                throw new ConfigurationErrorsException("No se encontro la cadena de conexion MediStackDB.");
            }

            _cadenaConexion = configuracion.ConnectionString;
        }

        public DataTable ObtenerPacientesActivos()
        {
            return Consultar(@"
                SELECT p.PacienteId, u.Nombre + N' ' + u.Apellido + N' - ' + u.NumeroDocumento AS Nombre
                FROM dbo.Pacientes p
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = p.PacienteId
                WHERE p.Activo = 1 AND u.Activo = 1
                ORDER BY u.Apellido, u.Nombre;");
        }

        public DataTable ObtenerProfesionalesActivos()
        {
            return Consultar(@"
                SELECT p.ProfesionalId, u.Nombre + N' ' + u.Apellido AS Nombre
                FROM dbo.Profesionales p
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = p.ProfesionalId
                WHERE p.Activo = 1 AND u.Activo = 1
                ORDER BY u.Apellido, u.Nombre;");
        }

        public DataTable ObtenerEspecialidadesActivas()
        {
            return Consultar(@"
                SELECT e.EspecialidadId, e.Nombre, e.DuracionEstandarMinutos
                FROM dbo.Especialidades e
                WHERE e.Activa = 1
                ORDER BY e.Nombre;");
        }

        public DataTable ObtenerProfesionalesPorEspecialidad(int especialidadId)
        {
            return Consultar(@"
                SELECT p.ProfesionalId, u.Nombre + N' ' + u.Apellido AS Nombre
                FROM dbo.ProfesionalesEspecialidades pe
                INNER JOIN dbo.Profesionales p ON p.ProfesionalId = pe.ProfesionalId
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = p.ProfesionalId
                WHERE pe.EspecialidadId = @EspecialidadId
                  AND pe.Activa = 1 AND p.Activo = 1 AND u.Activo = 1
                ORDER BY u.Apellido, u.Nombre;",
                ParametroInt32("@EspecialidadId", especialidadId));
        }

        public DataTable ObtenerDiasAtencion(Guid profesionalId, int especialidadId)
        {
            return Consultar(@"
                SELECT DISTINCT h.DiaSemana
                FROM dbo.HorariosAtencion h
                WHERE h.ProfesionalId = @ProfesionalId AND h.EspecialidadId = @EspecialidadId
                  AND h.Activo = 1
                ORDER BY h.DiaSemana;",
                ParametroGuid("@ProfesionalId", profesionalId),
                ParametroInt32("@EspecialidadId", especialidadId));
        }

        public DataTable ObtenerEspecialidadesProfesional(Guid profesionalId)
        {
            return Consultar(@"
                SELECT e.EspecialidadId, e.Nombre, e.DuracionEstandarMinutos
                FROM dbo.ProfesionalesEspecialidades pe
                INNER JOIN dbo.Profesionales p ON p.ProfesionalId = pe.ProfesionalId
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = p.ProfesionalId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = pe.EspecialidadId
                WHERE pe.ProfesionalId = @ProfesionalId
                  AND pe.Activa = 1 AND p.Activo = 1 AND u.Activo = 1 AND e.Activa = 1
                ORDER BY e.Nombre;",
                ParametroGuid("@ProfesionalId", profesionalId));
        }

        public DataTable ObtenerHorariosDisponibilidad(Guid profesionalId, int especialidadId, DateTime fecha)
        {
            return Consultar(@"
                SELECT h.HoraInicio, h.HoraFin, e.Nombre AS Especialidad,
                       e.DuracionEstandarMinutos
                FROM dbo.HorariosAtencion h
                INNER JOIN dbo.Profesionales p ON p.ProfesionalId = h.ProfesionalId
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = p.ProfesionalId
                INNER JOIN dbo.ProfesionalesEspecialidades pe
                    ON pe.ProfesionalId = h.ProfesionalId AND pe.EspecialidadId = h.EspecialidadId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = h.EspecialidadId
                WHERE h.ProfesionalId = @ProfesionalId AND h.EspecialidadId = @EspecialidadId
                  AND h.DiaSemana = @DiaSemana AND h.Activo = 1
                  AND pe.Activa = 1 AND p.Activo = 1 AND u.Activo = 1 AND e.Activa = 1
                ORDER BY h.HoraInicio;",
                ParametroGuid("@ProfesionalId", profesionalId),
                ParametroInt32("@EspecialidadId", especialidadId),
                new SqlParameter("@DiaSemana", SqlDbType.TinyInt) { Value = DiaSemana(fecha) });
        }

        public DataTable ObtenerTurnosDelDia(
            Guid profesionalId, int especialidadId, DateTime fecha, int? turnoExcluido)
        {
            return Consultar(@"
                SELECT t.TurnoId, t.PacienteId, paciente.Nombre + N' ' + paciente.Apellido AS Paciente,
                       t.FechaHora, t.Estado, e.DuracionEstandarMinutos
                FROM dbo.Turnos t
                INNER JOIN dbo.Pacientes p ON p.PacienteId = t.PacienteId
                INNER JOIN dbo.Usuarios paciente ON paciente.UsuarioId = p.PacienteId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = t.EspecialidadId
                WHERE t.ProfesionalId = @ProfesionalId AND t.EspecialidadId = @EspecialidadId
                  AND t.FechaHora >= @Fecha AND t.FechaHora < DATEADD(day, 1, @Fecha)
                  AND t.Estado IN (N'Solicitado', N'Confirmado')
                  AND (@TurnoExcluido IS NULL OR t.TurnoId <> @TurnoExcluido)
                ORDER BY t.FechaHora, t.TurnoId;",
                ParametroGuid("@ProfesionalId", profesionalId),
                ParametroInt32("@EspecialidadId", especialidadId),
                new SqlParameter("@Fecha", SqlDbType.Date) { Value = fecha.Date },
                new SqlParameter("@TurnoExcluido", SqlDbType.Int)
                {
                    Value = turnoExcluido.HasValue ? (object)turnoExcluido.Value : DBNull.Value
                });
        }

        public DataTable ObtenerTurnos(Guid? pacienteId, Guid? profesionalId, DateTime? desde, DateTime? hasta)
        {
            return Consultar(@"
                SELECT t.TurnoId, t.PacienteId, t.ProfesionalId, t.EspecialidadId,
                       paciente.Nombre + N' ' + paciente.Apellido AS Paciente,
                       profesional.Nombre + N' ' + profesional.Apellido AS Profesional,
                       e.Nombre AS Especialidad, e.DuracionEstandarMinutos, t.FechaHora,
                       t.Estado, t.Motivo, t.EstadoSena, t.MontoSena
                FROM dbo.Turnos t
                INNER JOIN dbo.Pacientes p ON p.PacienteId = t.PacienteId
                INNER JOIN dbo.Usuarios paciente ON paciente.UsuarioId = p.PacienteId
                INNER JOIN dbo.Profesionales pr ON pr.ProfesionalId = t.ProfesionalId
                INNER JOIN dbo.Usuarios profesional ON profesional.UsuarioId = pr.ProfesionalId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = t.EspecialidadId
                WHERE (@PacienteId IS NULL OR t.PacienteId = @PacienteId)
                  AND (@ProfesionalId IS NULL OR t.ProfesionalId = @ProfesionalId)
                  AND (@Desde IS NULL OR t.FechaHora >= @Desde)
                  AND (@Hasta IS NULL OR t.FechaHora < DATEADD(day, 1, @Hasta))
                ORDER BY t.FechaHora DESC, t.TurnoId DESC;",
                ParametroGuidNullable("@PacienteId", pacienteId),
                ParametroGuidNullable("@ProfesionalId", profesionalId),
                new SqlParameter("@Desde", SqlDbType.Date) { Value = desde.HasValue ? (object)desde.Value.Date : DBNull.Value },
                new SqlParameter("@Hasta", SqlDbType.Date) { Value = hasta.HasValue ? (object)hasta.Value.Date : DBNull.Value });
        }

        public TurnoDetalle ObtenerTurno(int turnoId)
        {
            DataTable datos = Consultar(@"
                SELECT TurnoId, PacienteId, ProfesionalId, EspecialidadId,
                       FechaHora, Estado, Motivo
                FROM dbo.Turnos
                WHERE TurnoId = @TurnoId;",
                ParametroInt32("@TurnoId", turnoId));

            if (datos.Rows.Count == 0)
            {
                return null;
            }

            DataRow fila = datos.Rows[0];
            return new TurnoDetalle
            {
                TurnoId = Convert.ToInt32(fila["TurnoId"]),
                PacienteId = (Guid)fila["PacienteId"],
                ProfesionalId = (Guid)fila["ProfesionalId"],
                EspecialidadId = Convert.ToInt32(fila["EspecialidadId"]),
                FechaHora = Convert.ToDateTime(fila["FechaHora"]),
                Estado = Convert.ToString(fila["Estado"]),
                Motivo = fila.IsNull("Motivo") ? string.Empty : Convert.ToString(fila["Motivo"])
            };
        }

        public void SolicitarTurno(SolicitudTurno solicitud)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                conexion.Open();
                using (SqlTransaction transaccion = conexion.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        ValidarHorarioYDisponibilidad(conexion, transaccion, solicitud, null);
                        InsertarTurno(conexion, transaccion, solicitud);
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

        public void ReprogramarTurno(int turnoId, Guid? pacienteAutorizado, SolicitudTurno destino)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                conexion.Open();
                using (SqlTransaction transaccion = conexion.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        TurnoDetalle original = ObtenerTurnoBloqueado(conexion, transaccion, turnoId);
                        if (original == null)
                        {
                            throw new InvalidOperationException("El turno ya no existe. Actualiza la pagina.");
                        }

                        if (pacienteAutorizado.HasValue && original.PacienteId != pacienteAutorizado.Value)
                        {
                            throw new InvalidOperationException("No tienes permiso para reprogramar este turno.");
                        }

                        if (original.FechaHora <= DateTime.Now || !EsVigente(original.Estado))
                        {
                            throw new InvalidOperationException("Solo se pueden reprogramar turnos futuros solicitados o confirmados.");
                        }

                        if (original.ProfesionalId == destino.ProfesionalId
                            && original.EspecialidadId == destino.EspecialidadId
                            && original.FechaHora == destino.FechaHora)
                        {
                            throw new InvalidOperationException("Selecciona un horario diferente para reprogramar el turno.");
                        }

                        ValidarHorarioYDisponibilidad(conexion, transaccion, destino, turnoId);
                        CancelarTurnoEnTransaccion(conexion, transaccion, turnoId);
                        InsertarTurno(conexion, transaccion, destino);
                        transaccion.Commit();
                    }
                    catch (InvalidOperationException)
                    {
                        transaccion.Rollback();
                        throw;
                    }
                    catch (SqlException)
                    {
                        transaccion.Rollback();
                        throw;
                    }
                    catch
                    {
                        transaccion.Rollback();
                        throw;
                    }
                }
            }
        }

        public void CancelarTurno(int turnoId, Guid? pacienteAutorizado)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                conexion.Open();
                using (SqlTransaction transaccion = conexion.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        TurnoDetalle turno = ObtenerTurnoBloqueado(conexion, transaccion, turnoId);
                        if (turno == null)
                        {
                            throw new InvalidOperationException("El turno ya no existe. Actualiza la pagina.");
                        }

                        if (pacienteAutorizado.HasValue && turno.PacienteId != pacienteAutorizado.Value)
                        {
                            throw new InvalidOperationException("No tienes permiso para cancelar este turno.");
                        }

                        if (turno.FechaHora <= DateTime.Now || !EsVigente(turno.Estado))
                        {
                            throw new InvalidOperationException("Solo se pueden cancelar turnos futuros solicitados o confirmados.");
                        }

                        CancelarTurnoEnTransaccion(conexion, transaccion, turnoId);
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

        public void CambiarEstadoTurno(
            int turnoId, string nuevoEstado, bool esAdministrativo, Guid? profesionalAutorizado)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                conexion.Open();
                using (SqlTransaction transaccion = conexion.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        TurnoDetalle turno = ObtenerTurnoBloqueado(conexion, transaccion, turnoId);
                        if (turno == null)
                        {
                            throw new InvalidOperationException("El turno ya no existe. Actualiza la pagina.");
                        }

                        if (profesionalAutorizado.HasValue
                            && turno.ProfesionalId != profesionalAutorizado.Value)
                        {
                            throw new InvalidOperationException("No tienes permiso para cambiar el estado de este turno.");
                        }

                        bool puedeCambiar = esAdministrativo
                            ? ((turno.Estado == "Solicitado" && nuevoEstado == "Confirmado")
                                || (EsVigente(turno.Estado) && nuevoEstado == "Cancelado")
                                || (turno.Estado == "Confirmado"
                                    && (nuevoEstado == "Atendido" || nuevoEstado == "Ausente")
                                    && turno.FechaHora <= DateTime.Now))
                            : turno.Estado == "Confirmado"
                                && (nuevoEstado == "Atendido" || nuevoEstado == "Ausente")
                                && turno.FechaHora <= DateTime.Now;

                        if (!puedeCambiar)
                        {
                            throw new InvalidOperationException("El cambio de estado no está permitido para este turno.");
                        }

                        using (SqlCommand comando = new SqlCommand(@"
                            UPDATE dbo.Turnos SET Estado = @Estado WHERE TurnoId = @TurnoId;",
                            conexion, transaccion))
                        {
                            comando.Parameters.Add("@Estado", SqlDbType.NVarChar, 20).Value = nuevoEstado;
                            comando.Parameters.Add("@TurnoId", SqlDbType.Int).Value = turnoId;
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

        private static void ValidarHorarioYDisponibilidad(
            SqlConnection conexion, SqlTransaction transaccion, SolicitudTurno solicitud, int? turnoExcluido)
        {
            using (SqlCommand comando = new SqlCommand(@"
                DECLARE @DiaSemana TINYINT =
                    (DATEDIFF(day, CONVERT(date, '19000101'), CONVERT(date, @FechaHora)) % 7) + 1;
                DECLARE @Hora TIME(0) = CONVERT(time(0), @FechaHora);
                DECLARE @Duracion SMALLINT;
                DECLARE @FinHorario TIME(0);

                SELECT @Duracion = e.DuracionEstandarMinutos
                FROM dbo.ProfesionalesEspecialidades pe WITH (UPDLOCK, HOLDLOCK)
                INNER JOIN dbo.Profesionales p ON p.ProfesionalId = pe.ProfesionalId
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = p.ProfesionalId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = pe.EspecialidadId
                WHERE pe.ProfesionalId = @ProfesionalId AND pe.EspecialidadId = @EspecialidadId
                  AND pe.Activa = 1 AND p.Activo = 1 AND u.Activo = 1 AND e.Activa = 1;

                IF @Duracion IS NULL
                    THROW 51020, N'El profesional no atiende esta especialidad o no está activo.', 1;

                SELECT TOP (1) @FinHorario = h.HoraFin
                FROM dbo.HorariosAtencion h WITH (UPDLOCK, HOLDLOCK)
                WHERE h.ProfesionalId = @ProfesionalId AND h.EspecialidadId = @EspecialidadId
                  AND h.DiaSemana = @DiaSemana AND h.Activo = 1
                  AND @Hora >= h.HoraInicio
                  AND DATEADD(minute, @Duracion, @Hora) <= h.HoraFin
                  AND DATEDIFF(minute, h.HoraInicio, @Hora) % @Duracion = 0;

                IF @FinHorario IS NULL
                    THROW 51021, N'El horario elegido no pertenece a una franja activa de la agenda o no respeta la duración de la especialidad.', 1;

                IF @FechaHora <= SYSDATETIME()
                    THROW 51022, N'No se pueden solicitar turnos en una fecha y hora pasadas.', 1;

                IF NOT EXISTS
                (
                    SELECT 1 FROM dbo.Pacientes p WITH (UPDLOCK, HOLDLOCK)
                    INNER JOIN dbo.Usuarios u ON u.UsuarioId = p.PacienteId
                    WHERE p.PacienteId = @PacienteId AND p.Activo = 1 AND u.Activo = 1
                )
                    THROW 51023, N'El paciente seleccionado no está activo.', 1;

                IF EXISTS
                (
                    SELECT 1
                    FROM dbo.Turnos t WITH (UPDLOCK, HOLDLOCK, INDEX(UX_Turnos_Profesional_FechaHora_Vigente))
                    INNER JOIN dbo.Especialidades e ON e.EspecialidadId = t.EspecialidadId
                    WHERE t.ProfesionalId = @ProfesionalId
                      AND t.Estado IN (N'Solicitado', N'Confirmado')
                      AND t.FechaHora < DATEADD(minute, @Duracion, @FechaHora)
                      AND DATEADD(minute, e.DuracionEstandarMinutos, t.FechaHora) > @FechaHora
                      AND (@TurnoExcluido IS NULL OR t.TurnoId <> @TurnoExcluido)
                )
                    THROW 51024, N'El horario ya fue ocupado por otro turno.', 1;

                IF EXISTS
                (
                    SELECT 1
                    FROM dbo.Turnos t WITH (UPDLOCK, HOLDLOCK, INDEX(UX_Turnos_Paciente_Especialidad_Vigente))
                    WHERE t.PacienteId = @PacienteId AND t.EspecialidadId = @EspecialidadId
                      AND t.Estado IN (N'Solicitado', N'Confirmado')
                      AND (@TurnoExcluido IS NULL OR t.TurnoId <> @TurnoExcluido)
                )
                    THROW 51025, N'El paciente ya tiene un turno vigente para esta especialidad.', 1;",
                conexion, transaccion))
            {
                comando.Parameters.Add("@PacienteId", SqlDbType.UniqueIdentifier).Value = solicitud.PacienteId;
                comando.Parameters.Add("@ProfesionalId", SqlDbType.UniqueIdentifier).Value = solicitud.ProfesionalId;
                comando.Parameters.Add("@EspecialidadId", SqlDbType.Int).Value = solicitud.EspecialidadId;
                comando.Parameters.Add("@FechaHora", SqlDbType.DateTime2).Value = solicitud.FechaHora;
                comando.Parameters.Add("@TurnoExcluido", SqlDbType.Int).Value =
                    turnoExcluido.HasValue ? (object)turnoExcluido.Value : DBNull.Value;
                comando.ExecuteNonQuery();
            }
        }

        private static TurnoDetalle ObtenerTurnoBloqueado(
            SqlConnection conexion, SqlTransaction transaccion, int turnoId)
        {
            using (SqlCommand comando = new SqlCommand(@"
                SELECT TurnoId, PacienteId, ProfesionalId, EspecialidadId,
                       FechaHora, Estado, Motivo
                FROM dbo.Turnos WITH (UPDLOCK, HOLDLOCK)
                WHERE TurnoId = @TurnoId;",
                conexion, transaccion))
            {
                comando.Parameters.Add("@TurnoId", SqlDbType.Int).Value = turnoId;
                using (SqlDataReader lector = comando.ExecuteReader())
                {
                    if (!lector.Read())
                    {
                        return null;
                    }

                    return new TurnoDetalle
                    {
                        TurnoId = lector.GetInt32(0),
                        PacienteId = lector.GetGuid(1),
                        ProfesionalId = lector.GetGuid(2),
                        EspecialidadId = lector.GetInt32(3),
                        FechaHora = lector.GetDateTime(4),
                        Estado = lector.GetString(5),
                        Motivo = lector.IsDBNull(6) ? string.Empty : lector.GetString(6)
                    };
                }
            }
        }

        private static void CancelarTurnoEnTransaccion(
            SqlConnection conexion, SqlTransaction transaccion, int turnoId)
        {
            using (SqlCommand comando = new SqlCommand(@"
                UPDATE dbo.Turnos SET Estado = N'Cancelado' WHERE TurnoId = @TurnoId;",
                conexion, transaccion))
            {
                comando.Parameters.Add("@TurnoId", SqlDbType.Int).Value = turnoId;
                comando.ExecuteNonQuery();
            }
        }

        private static void InsertarTurno(
            SqlConnection conexion, SqlTransaction transaccion, SolicitudTurno solicitud)
        {
            using (SqlCommand comando = new SqlCommand(@"
                INSERT INTO dbo.Turnos (PacienteId, ProfesionalId, EspecialidadId, FechaHora, Estado, Motivo)
                VALUES (@PacienteId, @ProfesionalId, @EspecialidadId, @FechaHora, N'Solicitado', @Motivo);",
                conexion, transaccion))
            {
                comando.Parameters.Add("@PacienteId", SqlDbType.UniqueIdentifier).Value = solicitud.PacienteId;
                comando.Parameters.Add("@ProfesionalId", SqlDbType.UniqueIdentifier).Value = solicitud.ProfesionalId;
                comando.Parameters.Add("@EspecialidadId", SqlDbType.Int).Value = solicitud.EspecialidadId;
                comando.Parameters.Add("@FechaHora", SqlDbType.DateTime2).Value = solicitud.FechaHora;
                comando.Parameters.Add("@Motivo", SqlDbType.NVarChar, 500).Value =
                    string.IsNullOrWhiteSpace(solicitud.Motivo) ? (object)DBNull.Value : solicitud.Motivo.Trim();
                comando.ExecuteNonQuery();
            }
        }

        private DataTable Consultar(string sql, params SqlParameter[] parametros)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            using (SqlCommand comando = new SqlCommand(sql, conexion))
            using (SqlDataAdapter adaptador = new SqlDataAdapter(comando))
            {
                comando.Parameters.AddRange(parametros);
                DataTable datos = new DataTable();
                adaptador.Fill(datos);
                return datos;
            }
        }

        private static int DiaSemana(DateTime fecha)
        {
            return ((int)fecha.DayOfWeek + 6) % 7 + 1;
        }

        private static SqlParameter ParametroGuid(string nombre, Guid valor)
        {
            return new SqlParameter(nombre, SqlDbType.UniqueIdentifier) { Value = valor };
        }

        private static SqlParameter ParametroGuidNullable(string nombre, Guid? valor)
        {
            return new SqlParameter(nombre, SqlDbType.UniqueIdentifier)
            {
                Value = valor.HasValue ? (object)valor.Value : DBNull.Value
            };
        }

        private static SqlParameter ParametroInt32(string nombre, int valor)
        {
            return new SqlParameter(nombre, SqlDbType.Int) { Value = valor };
        }

        private static bool EsVigente(string estado)
        {
            return estado == "Solicitado" || estado == "Confirmado";
        }
    }
}
