using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using MediStack.Dominio;

namespace MediStack.Datos
{
    public class GestionClinicaDatos
    {
        private readonly string _cadenaConexion;

        public GestionClinicaDatos()
        {
            ConnectionStringSettings configuracion = ConfigurationManager.ConnectionStrings["MediStackDB"];
            if (configuracion == null || string.IsNullOrWhiteSpace(configuracion.ConnectionString))
            {
                throw new ConfigurationErrorsException("No se encontro la cadena de conexion MediStackDB.");
            }

            _cadenaConexion = configuracion.ConnectionString;
        }

        public DataTable BuscarPacientes(string busqueda)
        {
            return Consultar(@"
                SELECT p.PacienteId, u.NombreUsuario, u.Nombre, u.Apellido, u.NumeroDocumento,
                       u.Email, u.Telefono, u.FechaNacimiento, p.NumeroAfiliado,
                       p.ContactoEmergenciaNombre, p.ContactoEmergenciaTelefono,
                       p.ObraSocialId, o.Nombre AS ObraSocial, p.Activo
                FROM dbo.Pacientes p
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = p.PacienteId
                LEFT JOIN dbo.ObrasSociales o ON o.ObraSocialId = p.ObraSocialId
                WHERE @Busqueda = N''
                   OR u.NombreUsuario LIKE N'%' + @Busqueda + N'%'
                   OR u.Nombre LIKE N'%' + @Busqueda + N'%'
                   OR u.Apellido LIKE N'%' + @Busqueda + N'%'
                   OR u.NumeroDocumento LIKE N'%' + @Busqueda + N'%'
                   OR u.Email LIKE N'%' + @Busqueda + N'%'
                ORDER BY u.Apellido, u.Nombre;",
                ParametroTexto("@Busqueda", busqueda, 100));
        }

        public DataTable ObtenerFichaPaciente(Guid pacienteId)
        {
            return Consultar(@"
                SELECT p.PacienteId, u.Nombre, u.Apellido, u.NumeroDocumento,
                       u.FechaNacimiento, u.Email, u.Telefono, o.Nombre AS ObraSocial,
                       p.NumeroAfiliado, p.ContactoEmergenciaNombre,
                       p.ContactoEmergenciaTelefono, p.Activo, p.FechaAlta
                FROM dbo.Pacientes p
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = p.PacienteId
                LEFT JOIN dbo.ObrasSociales o ON o.ObraSocialId = p.ObraSocialId
                WHERE p.PacienteId = @PacienteId;",
                ParametroGuid("@PacienteId", pacienteId));
        }

        public Paciente ObtenerPacientePorAfiliado(string numeroAfiliado)
        {
            DataTable tabla = Consultar(@"
        SELECT TOP 1 p.PacienteId, p.ObraSocialId, p.NumeroAfiliado
        FROM dbo.Pacientes p
        WHERE p.NumeroAfiliado = @NumeroAfiliado;",
                ParametroTexto("@NumeroAfiliado", numeroAfiliado, 50));

            if (tabla.Rows.Count == 0) return null;

            DataRow fila = tabla.Rows[0];
            return new Paciente
            {
                UsuarioId = (Guid)fila["PacienteId"],
                ObraSocialId = fila["ObraSocialId"] == DBNull.Value ? (int?)null : (int)fila["ObraSocialId"],
                NumeroAfiliado = fila["NumeroAfiliado"] == DBNull.Value ? null : fila["NumeroAfiliado"].ToString()
            };
        }

        public Paciente ObtenerPacientePorDocumento(string numeroDocumento)
        {
            DataTable tabla = Consultar(@"
        SELECT TOP 1 p.PacienteId, u.NumeroDocumento
        FROM dbo.Pacientes p
        INNER JOIN dbo.Usuarios u ON u.UsuarioId = p.PacienteId
        WHERE u.NumeroDocumento = @NumeroDocumento;",
                ParametroTexto("@NumeroDocumento", numeroDocumento, 20));

            if (tabla.Rows.Count == 0) return null;

            DataRow fila = tabla.Rows[0];
            return new Paciente
            {
                UsuarioId = (Guid)fila["PacienteId"]
            };
        }

        public DataTable ObtenerHistorialPaciente(Guid pacienteId)
        {
            return Consultar(@"
                SELECT t.TurnoId, t.FechaHora, profesional.Nombre + N' ' + profesional.Apellido AS Profesional,
                       e.Nombre AS Especialidad, t.Estado, t.Motivo,
                       rc.FechaRegistro, rc.MotivoConsulta, rc.Diagnostico, rc.Observaciones,
                       t.EstadoSena, t.MontoSena
                FROM dbo.Turnos t
                INNER JOIN dbo.Profesionales p ON p.ProfesionalId = t.ProfesionalId
                INNER JOIN dbo.Usuarios profesional ON profesional.UsuarioId = p.ProfesionalId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = t.EspecialidadId
                LEFT JOIN dbo.RegistrosClinicos rc ON rc.TurnoId = t.TurnoId
                WHERE t.PacienteId = @PacienteId
                ORDER BY t.FechaHora DESC, t.TurnoId DESC;",
                ParametroGuid("@PacienteId", pacienteId));
        }

        public DataTable ObtenerCobrosPaciente(Guid pacienteId)
        {
            return Consultar(@"
                SELECT c.CobroId, c.TurnoId, c.FechaHoraCobro, c.TipoCobro,
                       c.MedioPago, c.MontoCobrado
                FROM dbo.Cobros c
                INNER JOIN dbo.Turnos t ON t.TurnoId = c.TurnoId
                WHERE t.PacienteId = @PacienteId
                ORDER BY c.FechaHoraCobro DESC, c.CobroId DESC;",
                ParametroGuid("@PacienteId", pacienteId));
        }

        public DataTable ObtenerCoberturasPaciente(Guid pacienteId)
        {
            return Consultar(@"
                SELECT e.Nombre AS Especialidad, c.PorcentajeCobertura
                FROM dbo.Pacientes p
                INNER JOIN dbo.CoberturasEspecialidades c ON c.ObraSocialId = p.ObraSocialId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = c.EspecialidadId
                WHERE p.PacienteId = @PacienteId
                ORDER BY e.Nombre;",
                ParametroGuid("@PacienteId", pacienteId));
        }

        public bool PuedeProfesionalVerFichaPaciente(Guid pacienteId, Guid profesionalId)
        {
            DataTable resultado = Consultar(@"
                SELECT TOP (1) 1 AS PuedeVer
                FROM dbo.Turnos
                WHERE PacienteId = @PacienteId
                  AND ProfesionalId = @ProfesionalId
                  AND Estado <> N'Cancelado';",
                ParametroGuid("@PacienteId", pacienteId),
                ParametroGuid("@ProfesionalId", profesionalId));
            return resultado.Rows.Count > 0;
        }

        public DataTable ObtenerProfesionalesAgenda()
        {
            return Consultar(@"
                SELECT p.ProfesionalId, u.Nombre + N' ' + u.Apellido AS Nombre,
                       p.Activo
                FROM dbo.Profesionales p
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = p.ProfesionalId
                ORDER BY p.Activo DESC, u.Apellido, u.Nombre;");
        }

        public DataTable ObtenerEspecialidadesProfesionalAgenda(Guid profesionalId)
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

        public DataTable ObtenerHorariosAtencion(Guid? profesionalId)
        {
            return Consultar(@"
                SELECT h.HorarioAtencionId, h.ProfesionalId,
                       u.Nombre + N' ' + u.Apellido AS Profesional,
                       h.EspecialidadId, e.Nombre AS Especialidad, e.DuracionEstandarMinutos,
                       h.DiaSemana, h.HoraInicio, h.HoraFin, h.Activo
                FROM dbo.HorariosAtencion h
                INNER JOIN dbo.Profesionales p ON p.ProfesionalId = h.ProfesionalId
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = p.ProfesionalId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = h.EspecialidadId
                WHERE @ProfesionalId IS NULL OR h.ProfesionalId = @ProfesionalId
                ORDER BY h.Activo DESC, u.Apellido, u.Nombre, h.DiaSemana, h.HoraInicio, e.Nombre;",
                new SqlParameter("@ProfesionalId", SqlDbType.UniqueIdentifier)
                {
                    Value = profesionalId.HasValue ? (object)profesionalId.Value : DBNull.Value
                });
        }

        public DataTable ObtenerHorariosAtencionDia(Guid profesionalId, int diaSemana)
        {
            return Consultar(@"
                SELECT h.HorarioAtencionId, h.ProfesionalId, h.EspecialidadId,
                       e.Nombre AS Especialidad, e.DuracionEstandarMinutos,
                       h.DiaSemana, h.HoraInicio, h.HoraFin
                FROM dbo.HorariosAtencion h
                INNER JOIN dbo.Profesionales p ON p.ProfesionalId = h.ProfesionalId
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = p.ProfesionalId
                INNER JOIN dbo.ProfesionalesEspecialidades pe
                    ON pe.ProfesionalId = h.ProfesionalId AND pe.EspecialidadId = h.EspecialidadId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = h.EspecialidadId
                WHERE h.ProfesionalId = @ProfesionalId AND h.DiaSemana = @DiaSemana
                  AND h.Activo = 1 AND p.Activo = 1 AND u.Activo = 1
                  AND pe.Activa = 1 AND e.Activa = 1
                ORDER BY h.HoraInicio, e.Nombre;",
                ParametroGuid("@ProfesionalId", profesionalId),
                new SqlParameter("@DiaSemana", SqlDbType.TinyInt) { Value = diaSemana });
        }

        public DataTable ObtenerTurnosAgenda(Guid profesionalId, DateTime fecha)
        {
            return Consultar(@"
                SELECT t.TurnoId, t.PacienteId,
                       paciente.Nombre + N' ' + paciente.Apellido AS Paciente,
                       t.FechaHora, t.Estado, t.Motivo, t.EstadoSena, t.MontoSena,
                       e.Nombre AS Especialidad, e.DuracionEstandarMinutos
                FROM dbo.Turnos t
                INNER JOIN dbo.Pacientes p ON p.PacienteId = t.PacienteId
                INNER JOIN dbo.Usuarios paciente ON paciente.UsuarioId = p.PacienteId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = t.EspecialidadId
                WHERE t.ProfesionalId = @ProfesionalId
                  AND t.FechaHora >= @Fecha
                  AND t.FechaHora < DATEADD(day, 1, @Fecha)
                ORDER BY t.FechaHora, t.TurnoId;",
                ParametroGuid("@ProfesionalId", profesionalId),
                new SqlParameter("@Fecha", SqlDbType.Date) { Value = fecha.Date });
        }

        public void GuardarHorarioAtencion(HorarioAtencion horario)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                conexion.Open();
                using (SqlTransaction transaccion = conexion.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        using (SqlCommand comando = new SqlCommand(@"
                            DECLARE @DiaSemanaAnterior TINYINT;
                            DECLARE @HoraInicioAnterior TIME(0);
                            DECLARE @HoraFinAnterior TIME(0);

                            IF NOT EXISTS
                            (
                                SELECT 1
                                FROM dbo.ProfesionalesEspecialidades pe
                                INNER JOIN dbo.Profesionales p ON p.ProfesionalId = pe.ProfesionalId
                                INNER JOIN dbo.Usuarios u ON u.UsuarioId = p.ProfesionalId
                                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = pe.EspecialidadId
                                WHERE pe.ProfesionalId = @ProfesionalId
                                  AND pe.EspecialidadId = @EspecialidadId
                                  AND pe.Activa = 1 AND p.Activo = 1 AND u.Activo = 1 AND e.Activa = 1
                            )
                                THROW 51002, N'El profesional no tiene asignada una especialidad activa.', 1;

                            IF @HorarioAtencionId > 0 AND NOT EXISTS
                            (
                                SELECT 1
                                FROM dbo.HorariosAtencion
                                WHERE HorarioAtencionId = @HorarioAtencionId
                                  AND ProfesionalId = @ProfesionalId
                                  AND EspecialidadId = @EspecialidadId
                            )
                                THROW 51001, N'El horario ya no existe. Actualiza la pagina.', 1;

                            IF @HorarioAtencionId > 0
                            BEGIN
                                SELECT @DiaSemanaAnterior = DiaSemana, @HoraInicioAnterior = HoraInicio,
                                       @HoraFinAnterior = HoraFin
                                FROM dbo.HorariosAtencion WITH (UPDLOCK, HOLDLOCK)
                                WHERE HorarioAtencionId = @HorarioAtencionId;
                            END;

                            IF EXISTS
                            (
                                SELECT 1
                                FROM dbo.HorariosAtencion h WITH (UPDLOCK, HOLDLOCK)
                                WHERE h.ProfesionalId = @ProfesionalId
                                  AND h.DiaSemana = @DiaSemana
                                  AND h.Activo = 1
                                  AND h.HorarioAtencionId <> @HorarioAtencionId
                                  AND h.HoraInicio < @HoraFin
                                  AND h.HoraFin > @HoraInicio
                            )
                                THROW 51003, N'El horario se superpone con otra franja activa del profesional.', 1;

                            IF @HorarioAtencionId > 0 AND EXISTS
                            (
                                SELECT 1
                                FROM dbo.Turnos t
                                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = t.EspecialidadId
                                WHERE t.ProfesionalId = @ProfesionalId
                                  AND t.EspecialidadId = @EspecialidadId
                                  AND t.Estado IN (N'Solicitado', N'Confirmado')
                                  AND t.FechaHora >= CONVERT(date, SYSDATETIME())
                                  AND (DATEDIFF(day, CONVERT(date, '19000101'), CONVERT(date, t.FechaHora)) % 7) + 1 = @DiaSemanaAnterior
                                  AND CONVERT(time, t.FechaHora) >= @HoraInicioAnterior
                                  AND DATEADD(minute, e.DuracionEstandarMinutos, t.FechaHora) <=
                                      DATEADD(minute, DATEDIFF(minute, CONVERT(time, '00:00'), @HoraFinAnterior),
                                          CONVERT(datetime2(0), CONVERT(date, t.FechaHora)))
                                  AND
                                  (
                                      @DiaSemana <> @DiaSemanaAnterior
                                      OR CONVERT(time, t.FechaHora) < @HoraInicio
                                      OR DATEADD(minute, e.DuracionEstandarMinutos, t.FechaHora) >
                                          DATEADD(minute, DATEDIFF(minute, CONVERT(time, '00:00'), @HoraFin),
                                              CONVERT(datetime2(0), CONVERT(date, t.FechaHora)))
                                  )
                            )
                                THROW 51004, N'No se puede modificar el horario porque dejaría turnos solicitados o confirmados fuera de la franja.', 1;

                            IF @HorarioAtencionId = 0
                            BEGIN
                                INSERT INTO dbo.HorariosAtencion
                                    (ProfesionalId, EspecialidadId, DiaSemana, HoraInicio, HoraFin)
                                VALUES
                                    (@ProfesionalId, @EspecialidadId, @DiaSemana, @HoraInicio, @HoraFin);
                            END
                            ELSE
                            BEGIN
                                UPDATE dbo.HorariosAtencion
                                SET DiaSemana = @DiaSemana, HoraInicio = @HoraInicio, HoraFin = @HoraFin
                                WHERE HorarioAtencionId = @HorarioAtencionId;
                            END;",
                            conexion, transaccion))
                        {
                            AgregarHorarioParametros(comando, horario);
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

        public void CambiarEstadoHorarioAtencion(int horarioAtencionId, bool activo)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                conexion.Open();
                using (SqlTransaction transaccion = conexion.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        using (SqlCommand comando = new SqlCommand(@"
                            DECLARE @ProfesionalId UNIQUEIDENTIFIER;
                            DECLARE @EspecialidadId INT;
                            DECLARE @DiaSemana TINYINT;
                            DECLARE @HoraInicio TIME(0);
                            DECLARE @HoraFin TIME(0);

                            SELECT @ProfesionalId = ProfesionalId, @EspecialidadId = EspecialidadId,
                                   @DiaSemana = DiaSemana, @HoraInicio = HoraInicio, @HoraFin = HoraFin
                            FROM dbo.HorariosAtencion WITH (UPDLOCK, HOLDLOCK)
                            WHERE HorarioAtencionId = @HorarioAtencionId;

                            IF @ProfesionalId IS NULL
                                THROW 51001, N'El horario ya no existe. Actualiza la pagina.', 1;

                            IF @Activo = 1
                            BEGIN
                                IF NOT EXISTS
                                (
                                    SELECT 1 FROM dbo.ProfesionalesEspecialidades pe
                                    INNER JOIN dbo.Profesionales p ON p.ProfesionalId = pe.ProfesionalId
                                    INNER JOIN dbo.Usuarios u ON u.UsuarioId = p.ProfesionalId
                                    INNER JOIN dbo.Especialidades e ON e.EspecialidadId = pe.EspecialidadId
                                    WHERE pe.ProfesionalId = @ProfesionalId
                                      AND pe.EspecialidadId = @EspecialidadId
                                      AND pe.Activa = 1 AND p.Activo = 1 AND u.Activo = 1 AND e.Activa = 1
                                )
                                    THROW 51002, N'El profesional no tiene asignada una especialidad activa.', 1;

                                IF EXISTS
                                (
                                    SELECT 1
                                    FROM dbo.HorariosAtencion h WITH (UPDLOCK, HOLDLOCK)
                                    WHERE h.ProfesionalId = @ProfesionalId AND h.DiaSemana = @DiaSemana
                                      AND h.Activo = 1 AND h.HorarioAtencionId <> @HorarioAtencionId
                                      AND h.HoraInicio < @HoraFin AND h.HoraFin > @HoraInicio
                                )
                                    THROW 51003, N'El horario se superpone con otra franja activa del profesional.', 1;
                            END
                            ELSE IF EXISTS
                            (
                                SELECT 1
                                FROM dbo.Turnos t
                                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = t.EspecialidadId
                                WHERE t.ProfesionalId = @ProfesionalId AND t.EspecialidadId = @EspecialidadId
                                  AND t.Estado IN (N'Solicitado', N'Confirmado')
                                  AND t.FechaHora >= CONVERT(date, SYSDATETIME())
                                  AND (DATEDIFF(day, CONVERT(date, '19000101'), CONVERT(date, t.FechaHora)) % 7) + 1 = @DiaSemana
                                  AND CONVERT(time, t.FechaHora) < @HoraFin
                                  AND DATEADD(minute, e.DuracionEstandarMinutos, t.FechaHora) >
                                      DATEADD(minute, DATEDIFF(minute, CONVERT(time, '00:00'), @HoraInicio),
                                          CONVERT(datetime2(0), CONVERT(date, t.FechaHora)))
                            )
                                THROW 51004, N'No se puede desactivar el horario porque contiene turnos solicitados o confirmados.', 1;

                            UPDATE dbo.HorariosAtencion
                            SET Activo = @Activo
                            WHERE HorarioAtencionId = @HorarioAtencionId;",
                            conexion, transaccion))
                        {
                            comando.Parameters.Add("@HorarioAtencionId", SqlDbType.Int).Value = horarioAtencionId;
                            comando.Parameters.Add("@Activo", SqlDbType.Bit).Value = activo;
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

        public DataTable BuscarProfesionales(string busqueda)
        {
            return Consultar(@"
                SELECT p.ProfesionalId, u.NombreUsuario, u.Nombre, u.Apellido,
                       u.NumeroDocumento, u.Email, u.Telefono, u.FechaNacimiento,
                       p.MatriculaProfesional, p.Activo
                FROM dbo.Profesionales p
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = p.ProfesionalId
                WHERE @Busqueda = N''
                   OR u.NombreUsuario LIKE N'%' + @Busqueda + N'%'
                   OR u.Nombre LIKE N'%' + @Busqueda + N'%'
                   OR u.Apellido LIKE N'%' + @Busqueda + N'%'
                   OR u.NumeroDocumento LIKE N'%' + @Busqueda + N'%'
                   OR p.MatriculaProfesional LIKE N'%' + @Busqueda + N'%'
                ORDER BY u.Apellido, u.Nombre;",
                ParametroTexto("@Busqueda", busqueda, 100));
        }

        public DataTable ObtenerObrasSociales(string busqueda)
        {
            return Consultar(@"
                SELECT ObraSocialId, Nombre, CodigoCUIT, Activa
                FROM dbo.ObrasSociales
                WHERE @Busqueda = N''
                   OR Nombre LIKE N'%' + @Busqueda + N'%'
                   OR CodigoCUIT LIKE N'%' + @Busqueda + N'%'
                ORDER BY Activa DESC, Nombre;",
                ParametroTexto("@Busqueda", busqueda, 100));
        }

        public DataTable ObtenerObrasSocialesActivas()
        {
            return Consultar(@"
                SELECT ObraSocialId, Nombre
                FROM dbo.ObrasSociales
                WHERE Activa = 1
                ORDER BY Nombre;");
        }

        public DataTable ObtenerEspecialidades(string busqueda)
        {
            return Consultar(@"
                SELECT EspecialidadId, Codigo, Nombre, Descripcion, DuracionEstandarMinutos, Activa
                FROM dbo.Especialidades
                WHERE @Busqueda = N''
                   OR Codigo LIKE N'%' + @Busqueda + N'%'
                   OR Nombre LIKE N'%' + @Busqueda + N'%'
                ORDER BY Activa DESC, Nombre;",
                ParametroTexto("@Busqueda", busqueda, 100));
        }

        public DataTable ObtenerEspecialidadesActivas()
        {
            return Consultar(@"
                SELECT EspecialidadId, Nombre
                FROM dbo.Especialidades
                WHERE Activa = 1
                ORDER BY Nombre;");
        }

        public DataTable ObtenerOpcionesObraSocialPaciente()
        {
            return Consultar(@"
                SELECT ObraSocialId, Nombre
                FROM dbo.ObrasSociales
                WHERE Activa = 1
                ORDER BY Activa DESC, Nombre;");
        }

        public void GuardarPaciente(Paciente paciente, string passwordHash)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                conexion.Open();
                using (SqlTransaction transaccion = conexion.BeginTransaction())
                {
                    try
                    {
                        if (paciente.UsuarioId == Guid.Empty)
                        {
                            paciente.UsuarioId = Guid.NewGuid();
                            using (SqlCommand comando = new SqlCommand(@"
                                INSERT INTO dbo.Usuarios
                                    (UsuarioId, RolId, NombreUsuario, PasswordHash, Nombre, Apellido,
                                     NumeroDocumento, FechaNacimiento, Email, Telefono)
                                VALUES
                                    (@UsuarioId, (SELECT RolId FROM dbo.Roles WHERE Codigo = N'PACIENTE'),
                                     @NombreUsuario, @PasswordHash, @Nombre, @Apellido,
                                     @NumeroDocumento, @FechaNacimiento, @Email, @Telefono);",
                                conexion, transaccion))
                            {
                                AgregarUsuario(comando, paciente.UsuarioId, paciente.NombreUsuario, passwordHash,
                                    paciente.Nombre, paciente.Apellido, paciente.NumeroDocumento,
                                    paciente.FechaNacimiento, paciente.Email, paciente.Telefono);
                                comando.ExecuteNonQuery();
                            }

                            using (SqlCommand comando = new SqlCommand(@"
                                INSERT INTO dbo.Pacientes
                                    (PacienteId, ObraSocialId, NumeroAfiliado,
                                     ContactoEmergenciaNombre, ContactoEmergenciaTelefono)
                                VALUES
                                    (@UsuarioId, @ObraSocialId, @NumeroAfiliado,
                                     @ContactoEmergenciaNombre, @ContactoEmergenciaTelefono);",
                                conexion, transaccion))
                            {
                                AgregarPaciente(comando, paciente);
                                comando.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            using (SqlCommand comando = new SqlCommand(@"
                                UPDATE dbo.Usuarios
                                SET Nombre = @Nombre, Apellido = @Apellido,
                                    NumeroDocumento = @NumeroDocumento, FechaNacimiento = @FechaNacimiento,
                                    Email = @Email, Telefono = @Telefono
                                WHERE UsuarioId = @UsuarioId AND RolId =
                                    (SELECT RolId FROM dbo.Roles WHERE Codigo = N'PACIENTE');
                                IF @@ROWCOUNT = 0 THROW 51001, N'El paciente ya no existe.', 1;
                                UPDATE dbo.Pacientes
                                SET ObraSocialId = @ObraSocialId, NumeroAfiliado = @NumeroAfiliado,
                                    ContactoEmergenciaNombre = @ContactoEmergenciaNombre,
                                    ContactoEmergenciaTelefono = @ContactoEmergenciaTelefono
                                WHERE PacienteId = @UsuarioId;",
                                conexion, transaccion))
                            {
                                AgregarUsuario(comando, paciente.UsuarioId, paciente.NombreUsuario, null,
                                    paciente.Nombre, paciente.Apellido, paciente.NumeroDocumento,
                                    paciente.FechaNacimiento, paciente.Email, paciente.Telefono);
                                AgregarPaciente(comando, paciente);
                                comando.ExecuteNonQuery();
                            }
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

        public void CambiarEstadoPaciente(Guid pacienteId, bool activo)
        {
            EjecutarEnTransaccion((conexion, transaccion) =>
            {
                using (SqlCommand comando = new SqlCommand(@"
                    UPDATE dbo.Pacientes SET Activo = @Activo WHERE PacienteId = @UsuarioId;
                    IF @@ROWCOUNT = 0 THROW 51001, N'El paciente ya no existe.', 1;
                    UPDATE dbo.Usuarios SET Activo = @Activo WHERE UsuarioId = @UsuarioId;",
                    conexion, transaccion))
                {
                    comando.Parameters.Add("@UsuarioId", SqlDbType.UniqueIdentifier).Value = pacienteId;
                    comando.Parameters.Add("@Activo", SqlDbType.Bit).Value = activo;
                    comando.ExecuteNonQuery();
                }
            });
        }

        public void GuardarProfesional(Profesional profesional, string passwordHash)
        {
            EjecutarEnTransaccion((conexion, transaccion) =>
            {
                if (profesional.UsuarioId == Guid.Empty)
                {
                    profesional.UsuarioId = Guid.NewGuid();
                    using (SqlCommand comando = new SqlCommand(@"
                        INSERT INTO dbo.Usuarios
                            (UsuarioId, RolId, NombreUsuario, PasswordHash, Nombre, Apellido,
                             NumeroDocumento, FechaNacimiento, Email, Telefono)
                        VALUES
                            (@UsuarioId, (SELECT RolId FROM dbo.Roles WHERE Codigo = N'PROFESIONAL'),
                             @NombreUsuario, @PasswordHash, @Nombre, @Apellido,
                             @NumeroDocumento, @FechaNacimiento, @Email, @Telefono);",
                        conexion, transaccion))
                    {
                        AgregarUsuario(comando, profesional.UsuarioId, profesional.NombreUsuario, passwordHash,
                            profesional.Nombre, profesional.Apellido, profesional.NumeroDocumento,
                            profesional.FechaNacimiento, profesional.Email, profesional.Telefono);
                        comando.ExecuteNonQuery();
                    }

                    using (SqlCommand comando = new SqlCommand(@"
                        INSERT INTO dbo.Profesionales (ProfesionalId, MatriculaProfesional)
                        VALUES (@UsuarioId, @MatriculaProfesional);",
                        conexion, transaccion))
                    {
                        AgregarProfesional(comando, profesional);
                        comando.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (SqlCommand comando = new SqlCommand(@"
                        UPDATE dbo.Usuarios
                        SET Nombre = @Nombre, Apellido = @Apellido,
                            NumeroDocumento = @NumeroDocumento, FechaNacimiento = @FechaNacimiento,
                            Email = @Email, Telefono = @Telefono
                        WHERE UsuarioId = @UsuarioId AND RolId =
                            (SELECT RolId FROM dbo.Roles WHERE Codigo = N'PROFESIONAL');
                        IF @@ROWCOUNT = 0 THROW 51001, N'El profesional ya no existe.', 1;
                        UPDATE dbo.Profesionales
                        SET MatriculaProfesional = @MatriculaProfesional
                        WHERE ProfesionalId = @UsuarioId;",
                        conexion, transaccion))
                    {
                        AgregarUsuario(comando, profesional.UsuarioId, profesional.NombreUsuario, null,
                            profesional.Nombre, profesional.Apellido, profesional.NumeroDocumento,
                            profesional.FechaNacimiento, profesional.Email, profesional.Telefono);
                        AgregarProfesional(comando, profesional);
                        comando.ExecuteNonQuery();
                    }
                }
            });
        }

        public void CambiarEstadoProfesional(Guid profesionalId, bool activo)
        {
            EjecutarEnTransaccion((conexion, transaccion) =>
            {
                using (SqlCommand comando = new SqlCommand(@"
                    UPDATE dbo.Profesionales SET Activo = @Activo WHERE ProfesionalId = @UsuarioId;
                    IF @@ROWCOUNT = 0 THROW 51001, N'El profesional ya no existe.', 1;
                    UPDATE dbo.Usuarios SET Activo = @Activo WHERE UsuarioId = @UsuarioId;",
                    conexion, transaccion))
                {
                    comando.Parameters.Add("@UsuarioId", SqlDbType.UniqueIdentifier).Value = profesionalId;
                    comando.Parameters.Add("@Activo", SqlDbType.Bit).Value = activo;
                    comando.ExecuteNonQuery();
                }
            });
        }

        public void GuardarEspecialidad(Especialidad especialidad)
        {
            if (especialidad.EspecialidadId == 0)
            {
                Ejecutar(@"
                    INSERT INTO dbo.Especialidades
                        (Codigo, Nombre, Descripcion, DuracionEstandarMinutos)
                    VALUES (@Codigo, @Nombre, @Descripcion, @Duracion);",
                    ParametroTexto("@Codigo", especialidad.Codigo, 30),
                    ParametroTexto("@Nombre", especialidad.Nombre, 100),
                    ParametroTextoNullable("@Descripcion", especialidad.Descripcion, 255),
                    ParametroInt16("@Duracion", especialidad.DuracionEstandarMinutos));
            }
            else
            {
                Ejecutar(@"
                    UPDATE dbo.Especialidades
                    SET Codigo = @Codigo, Nombre = @Nombre, Descripcion = @Descripcion,
                        DuracionEstandarMinutos = @Duracion
                    WHERE EspecialidadId = @EspecialidadId;
                    IF @@ROWCOUNT = 0 THROW 51001, N'La especialidad ya no existe.', 1;",
                    ParametroTexto("@Codigo", especialidad.Codigo, 30),
                    ParametroTexto("@Nombre", especialidad.Nombre, 100),
                    ParametroTextoNullable("@Descripcion", especialidad.Descripcion, 255),
                    ParametroInt16("@Duracion", especialidad.DuracionEstandarMinutos),
                    ParametroInt32("@EspecialidadId", especialidad.EspecialidadId));
            }
        }

        public void CambiarEstadoEspecialidad(int especialidadId, bool activa)
        {
            Ejecutar(@"UPDATE dbo.Especialidades SET Activa = @Activa WHERE EspecialidadId = @EspecialidadId;
                IF @@ROWCOUNT = 0 THROW 51001, N'La especialidad ya no existe.', 1;",
                ParametroBooleano("@Activa", activa),
                ParametroInt32("@EspecialidadId", especialidadId));
        }

        public void GuardarObraSocial(ObraSocial obraSocial)
        {
            if (obraSocial.ObraSocialId == 0)
            {
                Ejecutar(@"
                    INSERT INTO dbo.ObrasSociales (Nombre, CodigoCUIT)
                    VALUES (@Nombre, @CodigoCUIT);",
                    ParametroTexto("@Nombre", obraSocial.Nombre, 100),
                    ParametroTexto("@CodigoCUIT", obraSocial.CodigoCUIT, 20));
            }
            else
            {
                Ejecutar(@"
                    UPDATE dbo.ObrasSociales
                    SET Nombre = @Nombre, CodigoCUIT = @CodigoCUIT
                    WHERE ObraSocialId = @ObraSocialId;
                    IF @@ROWCOUNT = 0 THROW 51001, N'La obra social ya no existe.', 1;",
                    ParametroTexto("@Nombre", obraSocial.Nombre, 100),
                    ParametroTexto("@CodigoCUIT", obraSocial.CodigoCUIT, 20),
                    ParametroInt32("@ObraSocialId", obraSocial.ObraSocialId));
            }
        }

        public void CambiarEstadoObraSocial(int obraSocialId, bool activa)
        {
            Ejecutar(@"UPDATE dbo.ObrasSociales SET Activa = @Activa WHERE ObraSocialId = @ObraSocialId;
                IF @@ROWCOUNT = 0 THROW 51001, N'La obra social ya no existe.', 1;",
                ParametroBooleano("@Activa", activa),
                ParametroInt32("@ObraSocialId", obraSocialId));
        }

        public DataTable BuscarCoberturas(string busqueda)
        {
            return Consultar(@"
                SELECT c.ObraSocialId, c.EspecialidadId, o.Nombre AS ObraSocial,
                       e.Nombre AS Especialidad, c.PorcentajeCobertura
                FROM dbo.CoberturasEspecialidades c
                INNER JOIN dbo.ObrasSociales o ON o.ObraSocialId = c.ObraSocialId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = c.EspecialidadId
                WHERE @Busqueda = N''
                   OR o.Nombre LIKE N'%' + @Busqueda + N'%'
                   OR e.Nombre LIKE N'%' + @Busqueda + N'%'
                ORDER BY o.Nombre, e.Nombre;",
                ParametroTexto("@Busqueda", busqueda, 100));
        }

        public DataTable ObtenerCoberturas()
        {
            return Consultar(@"
                SELECT c.ObraSocialId, c.EspecialidadId, o.Nombre AS ObraSocial,
                       e.Nombre AS Especialidad, c.PorcentajeCobertura
                FROM dbo.CoberturasEspecialidades c
                INNER JOIN dbo.ObrasSociales o ON o.ObraSocialId = c.ObraSocialId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = c.EspecialidadId
                ORDER BY o.Nombre, e.Nombre;");
        }

        public void GuardarCobertura(CoberturaEspecialidad cobertura, bool esNueva)
        {
            if (esNueva)
            {
                Ejecutar(@"
                    INSERT INTO dbo.CoberturasEspecialidades
                        (ObraSocialId, EspecialidadId, PorcentajeCobertura)
                    VALUES (@ObraSocialId, @EspecialidadId, @Porcentaje);",
                    ParametroInt32("@ObraSocialId", cobertura.ObraSocialId),
                    ParametroInt32("@EspecialidadId", cobertura.EspecialidadId),
                    ParametroDecimal("@Porcentaje", cobertura.PorcentajeCobertura, 5, 2));
            }
            else
            {
                Ejecutar(@"
                    UPDATE dbo.CoberturasEspecialidades
                    SET PorcentajeCobertura = @Porcentaje
                    WHERE ObraSocialId = @ObraSocialId AND EspecialidadId = @EspecialidadId;
                    IF @@ROWCOUNT = 0 THROW 51001, N'La cobertura ya no existe.', 1;",
                    ParametroDecimal("@Porcentaje", cobertura.PorcentajeCobertura, 5, 2),
                    ParametroInt32("@ObraSocialId", cobertura.ObraSocialId),
                    ParametroInt32("@EspecialidadId", cobertura.EspecialidadId));
            }
        }

        public void EliminarCobertura(int obraSocialId, int especialidadId)
        {
            Ejecutar(@"
                DELETE FROM dbo.CoberturasEspecialidades
                WHERE ObraSocialId = @ObraSocialId AND EspecialidadId = @EspecialidadId;
                IF @@ROWCOUNT = 0 THROW 51001, N'La cobertura ya no existe.', 1;",
                ParametroInt32("@ObraSocialId", obraSocialId),
                ParametroInt32("@EspecialidadId", especialidadId));
        }

        public DataTable BuscarConvenios(string busqueda)
        {
            return Consultar(@"
                SELECT c.ConvenioId, c.ProfesionalId, c.EspecialidadId, c.ObraSocialId,
                       u.Nombre + N' ' + u.Apellido AS Profesional, e.Nombre AS Especialidad,
                       o.Nombre AS ObraSocial, c.FechaDesde, c.FechaHasta, c.Activo,
                       pe.ValorConsulta, pe.EsquemaTipo, pe.EsquemaValor, pe.EsEspecialidadPrincipal
                FROM dbo.Convenios c
                INNER JOIN dbo.ProfesionalesEspecialidades pe
                    ON pe.ProfesionalId = c.ProfesionalId AND pe.EspecialidadId = c.EspecialidadId
                INNER JOIN dbo.Profesionales p ON p.ProfesionalId = c.ProfesionalId
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = p.ProfesionalId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = c.EspecialidadId
                INNER JOIN dbo.ObrasSociales o ON o.ObraSocialId = c.ObraSocialId
                WHERE @Busqueda = N''
                   OR u.Nombre LIKE N'%' + @Busqueda + N'%'
                   OR u.Apellido LIKE N'%' + @Busqueda + N'%'
                   OR e.Nombre LIKE N'%' + @Busqueda + N'%'
                   OR o.Nombre LIKE N'%' + @Busqueda + N'%'
                ORDER BY c.Activo DESC, u.Apellido, e.Nombre, o.Nombre, c.FechaDesde DESC;",
                ParametroTexto("@Busqueda", busqueda, 100));
        }

        public DataTable ObtenerProfesionalesParaConvenio()
        {
            return Consultar(@"
                SELECT p.ProfesionalId, u.Nombre + N' ' + u.Apellido AS Nombre
                FROM dbo.Profesionales p
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = p.ProfesionalId
                WHERE p.Activo = 1 AND u.Activo = 1
                ORDER BY u.Apellido, u.Nombre;");
        }

        public DataTable ObtenerCoberturasParaConvenio()
        {
            return Consultar(@"
                SELECT c.ObraSocialId, c.EspecialidadId,
                       CONVERT(NVARCHAR(12), c.ObraSocialId) + N'|' +
                           CONVERT(NVARCHAR(12), c.EspecialidadId) AS Clave,
                       o.Nombre + N' - ' + e.Nombre AS Nombre
                FROM dbo.CoberturasEspecialidades c
                INNER JOIN dbo.ObrasSociales o ON o.ObraSocialId = c.ObraSocialId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = c.EspecialidadId
                WHERE o.Activa = 1 AND e.Activa = 1
                ORDER BY o.Nombre, e.Nombre;");
        }

        public DataTable ObtenerConvenio(int convenioId)
        {
            return Consultar(@"
                SELECT c.ConvenioId, c.ProfesionalId, c.EspecialidadId, c.ObraSocialId,
                       c.FechaDesde, c.FechaHasta, c.Activo, pe.ValorConsulta,
                       pe.EsquemaTipo, pe.EsquemaValor, pe.EsEspecialidadPrincipal
                FROM dbo.Convenios c
                INNER JOIN dbo.ProfesionalesEspecialidades pe
                    ON pe.ProfesionalId = c.ProfesionalId AND pe.EspecialidadId = c.EspecialidadId
                WHERE c.ConvenioId = @ConvenioId;",
                ParametroInt32("@ConvenioId", convenioId));
        }

        public void GuardarConvenio(Convenio convenio, bool esNuevo)
        {
            EjecutarEnTransaccion((conexion, transaccion) =>
            {
                using (SqlCommand comando = new SqlCommand(@"
                    IF NOT EXISTS
                    (
                        SELECT 1 FROM dbo.Profesionales p
                        INNER JOIN dbo.Usuarios u ON u.UsuarioId = p.ProfesionalId
                        INNER JOIN dbo.Especialidades e ON e.EspecialidadId = @EspecialidadId
                        WHERE p.ProfesionalId = @ProfesionalId AND p.Activo = 1
                          AND u.Activo = 1 AND e.Activa = 1
                    )
                        THROW 51002, N'Selecciona un profesional y una especialidad activos.', 1;

                    IF NOT EXISTS
                    (
                        SELECT 1 FROM dbo.CoberturasEspecialidades c
                        INNER JOIN dbo.ObrasSociales o ON o.ObraSocialId = c.ObraSocialId
                        WHERE c.ObraSocialId = @ObraSocialId
                          AND c.EspecialidadId = @EspecialidadId AND o.Activa = 1
                    )
                        THROW 51003, N'La obra social no tiene una cobertura activa para esta especialidad.', 1;

                    IF NOT EXISTS
                    (
                        SELECT 1 FROM dbo.ProfesionalesEspecialidades
                        WHERE ProfesionalId = @ProfesionalId AND EspecialidadId = @EspecialidadId
                    )
                    BEGIN
                        INSERT INTO dbo.ProfesionalesEspecialidades
                            (ProfesionalId, EspecialidadId, EsEspecialidadPrincipal,
                             ValorConsulta, EsquemaTipo, EsquemaValor)
                        VALUES
                            (@ProfesionalId, @EspecialidadId, @EsEspecialidadPrincipal,
                             @ValorConsulta, @EsquemaTipo, @EsquemaValor);
                    END
                    ELSE
                    BEGIN
                        UPDATE dbo.ProfesionalesEspecialidades
                        SET EsEspecialidadPrincipal = @EsEspecialidadPrincipal,
                            ValorConsulta = @ValorConsulta,
                            EsquemaTipo = @EsquemaTipo,
                            EsquemaValor = @EsquemaValor,
                            Activa = 1
                        WHERE ProfesionalId = @ProfesionalId AND EspecialidadId = @EspecialidadId;
                    END;",
                    conexion, transaccion))
                {
                    AgregarConvenio(comando, convenio);
                    comando.ExecuteNonQuery();
                }

                if (esNuevo)
                {
                    using (SqlCommand comando = new SqlCommand(@"
                        INSERT INTO dbo.Convenios
                            (ProfesionalId, EspecialidadId, ObraSocialId, FechaDesde, FechaHasta, Activo)
                        VALUES
                            (@ProfesionalId, @EspecialidadId, @ObraSocialId, @FechaDesde, @FechaHasta, @Activo);",
                        conexion, transaccion))
                    {
                        AgregarConvenio(comando, convenio);
                        comando.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (SqlCommand comando = new SqlCommand(@"
                        UPDATE dbo.Convenios
                        SET ProfesionalId = @ProfesionalId, EspecialidadId = @EspecialidadId,
                            ObraSocialId = @ObraSocialId, FechaDesde = @FechaDesde,
                            FechaHasta = @FechaHasta, Activo = @Activo
                        WHERE ConvenioId = @ConvenioId;
                        IF @@ROWCOUNT = 0 THROW 51001, N'El convenio ya no existe.', 1;",
                        conexion, transaccion))
                    {
                        AgregarConvenio(comando, convenio);
                        comando.ExecuteNonQuery();
                    }
                }
            });
        }

        public void CambiarEstadoConvenio(int convenioId, bool activo)
        {
            Ejecutar(@"UPDATE dbo.Convenios SET Activo = @Activo WHERE ConvenioId = @ConvenioId;
                IF @@ROWCOUNT = 0 THROW 51001, N'El convenio ya no existe.', 1;",
                ParametroBooleano("@Activo", activo),
                ParametroInt32("@ConvenioId", convenioId));
        }

        private void AgregarUsuario(
            SqlCommand comando, Guid usuarioId, string nombreUsuario, string passwordHash,
            string nombre, string apellido, string documento, DateTime fechaNacimiento,
            string email, string telefono)
        {
            comando.Parameters.Add("@UsuarioId", SqlDbType.UniqueIdentifier).Value = usuarioId;
            comando.Parameters.Add("@NombreUsuario", SqlDbType.NVarChar, 50).Value =
                nombreUsuario == null ? (object)DBNull.Value : nombreUsuario;
            SqlParameter hash = comando.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 256);
            hash.Value = passwordHash == null ? (object)DBNull.Value : passwordHash;
            comando.Parameters.Add("@Nombre", SqlDbType.NVarChar, 100).Value = nombre;
            comando.Parameters.Add("@Apellido", SqlDbType.NVarChar, 100).Value = apellido;
            comando.Parameters.Add("@NumeroDocumento", SqlDbType.NVarChar, 20).Value = documento;
            comando.Parameters.Add("@FechaNacimiento", SqlDbType.Date).Value = fechaNacimiento.Date;
            comando.Parameters.Add("@Email", SqlDbType.NVarChar, 256).Value = email;
            comando.Parameters.Add("@Telefono", SqlDbType.NVarChar, 30).Value =
                string.IsNullOrWhiteSpace(telefono) ? (object)DBNull.Value : telefono;
        }

        private static void AgregarPaciente(SqlCommand comando, Paciente paciente)
        {
            AgregarUsuarioIdSiFalta(comando, paciente.UsuarioId);
            comando.Parameters.Add("@ObraSocialId", SqlDbType.Int).Value =
                paciente.ObraSocialId.HasValue ? (object)paciente.ObraSocialId.Value : DBNull.Value;
            comando.Parameters.Add("@NumeroAfiliado", SqlDbType.NVarChar, 50).Value = ValorNullable(paciente.NumeroAfiliado);
            comando.Parameters.Add("@ContactoEmergenciaNombre", SqlDbType.NVarChar, 100).Value =
                ValorNullable(paciente.ContactoEmergenciaNombre);
            comando.Parameters.Add("@ContactoEmergenciaTelefono", SqlDbType.NVarChar, 30).Value =
                ValorNullable(paciente.ContactoEmergenciaTelefono);
        }

        private static void AgregarProfesional(SqlCommand comando, Profesional profesional)
        {
            AgregarUsuarioIdSiFalta(comando, profesional.UsuarioId);
            comando.Parameters.Add("@MatriculaProfesional", SqlDbType.NVarChar, 50).Value =
                profesional.MatriculaProfesional;
        }

        private static void AgregarUsuarioIdSiFalta(SqlCommand comando, Guid usuarioId)
        {
            if (!comando.Parameters.Contains("@UsuarioId"))
            {
                comando.Parameters.Add("@UsuarioId", SqlDbType.UniqueIdentifier).Value = usuarioId;
            }
        }

        private static void AgregarConvenio(SqlCommand comando, Convenio convenio)
        {
            comando.Parameters.Add("@ConvenioId", SqlDbType.Int).Value = convenio.ConvenioId;
            comando.Parameters.Add("@ProfesionalId", SqlDbType.UniqueIdentifier).Value = convenio.ProfesionalId;
            comando.Parameters.Add("@EspecialidadId", SqlDbType.Int).Value = convenio.EspecialidadId;
            comando.Parameters.Add("@ObraSocialId", SqlDbType.Int).Value = convenio.ObraSocialId;
            comando.Parameters.Add("@FechaDesde", SqlDbType.Date).Value = convenio.FechaDesde.Date;
            comando.Parameters.Add("@FechaHasta", SqlDbType.Date).Value =
                convenio.FechaHasta.HasValue ? (object)convenio.FechaHasta.Value.Date : DBNull.Value;
            comando.Parameters.Add("@Activo", SqlDbType.Bit).Value = convenio.Activo;
            comando.Parameters.Add("@ValorConsulta", SqlDbType.Decimal).Value = convenio.ValorConsulta;
            comando.Parameters["@ValorConsulta"].Precision = 12;
            comando.Parameters["@ValorConsulta"].Scale = 2;
            comando.Parameters.Add("@EsquemaTipo", SqlDbType.NVarChar, 20).Value = convenio.EsquemaTipo;
            comando.Parameters.Add("@EsquemaValor", SqlDbType.Decimal).Value = convenio.EsquemaValor;
            comando.Parameters["@EsquemaValor"].Precision = 7;
            comando.Parameters["@EsquemaValor"].Scale = 2;
            comando.Parameters.Add("@EsEspecialidadPrincipal", SqlDbType.Bit).Value = convenio.EsEspecialidadPrincipal;
        }

        private static void AgregarHorarioParametros(SqlCommand comando, HorarioAtencion horario)
        {
            comando.Parameters.Add("@HorarioAtencionId", SqlDbType.Int).Value = horario.HorarioAtencionId;
            comando.Parameters.Add("@ProfesionalId", SqlDbType.UniqueIdentifier).Value = horario.ProfesionalId;
            comando.Parameters.Add("@EspecialidadId", SqlDbType.Int).Value = horario.EspecialidadId;
            comando.Parameters.Add("@DiaSemana", SqlDbType.TinyInt).Value = horario.DiaSemana;
            comando.Parameters.Add("@HoraInicio", SqlDbType.Time).Value = horario.HoraInicio;
            comando.Parameters.Add("@HoraFin", SqlDbType.Time).Value = horario.HoraFin;
        }

        private DataTable Consultar(string consulta, params SqlParameter[] parametros)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            using (SqlCommand comando = new SqlCommand(consulta, conexion))
            using (SqlDataAdapter adaptador = new SqlDataAdapter(comando))
            {
                comando.Parameters.AddRange(parametros);
                DataTable tabla = new DataTable();
                adaptador.Fill(tabla);
                return tabla;
            }
        }

        private void Ejecutar(string consulta, params SqlParameter[] parametros)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            using (SqlCommand comando = new SqlCommand(consulta, conexion))
            {
                comando.Parameters.AddRange(parametros);
                conexion.Open();
                comando.ExecuteNonQuery();
            }
        }

        private void EjecutarEnTransaccion(Action<SqlConnection, SqlTransaction> accion)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                conexion.Open();
                using (SqlTransaction transaccion = conexion.BeginTransaction())
                {
                    try
                    {
                        accion(conexion, transaccion);
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

        private static object ValorNullable(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? (object)DBNull.Value : valor.Trim();
        }

        private static SqlParameter ParametroTexto(string nombre, string valor, int longitud)
        {
            return new SqlParameter(nombre, SqlDbType.NVarChar, longitud) { Value = valor ?? string.Empty };
        }

        private static SqlParameter ParametroTextoNullable(string nombre, string valor, int longitud)
        {
            return new SqlParameter(nombre, SqlDbType.NVarChar, longitud) { Value = ValorNullable(valor) };
        }

        private static SqlParameter ParametroInt32(string nombre, int valor)
        {
            return new SqlParameter(nombre, SqlDbType.Int) { Value = valor };
        }

        private static SqlParameter ParametroGuid(string nombre, Guid valor)
        {
            return new SqlParameter(nombre, SqlDbType.UniqueIdentifier) { Value = valor };
        }

        private static SqlParameter ParametroInt16(string nombre, short valor)
        {
            return new SqlParameter(nombre, SqlDbType.SmallInt) { Value = valor };
        }

        private static SqlParameter ParametroBooleano(string nombre, bool valor)
        {
            return new SqlParameter(nombre, SqlDbType.Bit) { Value = valor };
        }

        private static SqlParameter ParametroDecimal(string nombre, decimal valor, byte precision, byte escala)
        {
            return new SqlParameter(nombre, SqlDbType.Decimal)
            {
                Precision = precision,
                Scale = escala,
                Value = valor
            };
        }
    }
}
