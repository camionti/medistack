using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using MediStack.Dominio;

namespace MediStack.Datos
{
    public sealed class TurnosDatos
    {
        private const string SqlTieneCobertura = @"
            EXISTS
            (
                SELECT 1
                FROM dbo.Pacientes pac
                INNER JOIN dbo.Convenios co ON co.ObraSocialId = pac.ObraSocialId
                    AND co.ProfesionalId = pe.ProfesionalId
                    AND co.EspecialidadId = pe.EspecialidadId
                INNER JOIN dbo.CoberturasEspecialidades ce
                    ON ce.ObraSocialId = co.ObraSocialId AND ce.EspecialidadId = co.EspecialidadId
                WHERE pac.PacienteId = @PacienteId
                  AND co.Activo = 1
                  AND ce.PorcentajeCobertura > 0
                  AND co.FechaDesde <= @FechaCobertura
                  AND (co.FechaHasta IS NULL OR co.FechaHasta >= @FechaCobertura)
            )";

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

        public DataTable ObtenerProfesionalesPorEspecialidadParaPaciente(int especialidadId, Guid pacienteId)
        {
            return Consultar(@"
                SELECT p.ProfesionalId, u.Nombre + N' ' + u.Apellido AS Nombre, pe.ValorConsulta,
                       CASE WHEN " + SqlTieneCobertura + @" THEN 1 ELSE 0 END AS TieneCobertura
                FROM dbo.ProfesionalesEspecialidades pe
                INNER JOIN dbo.Profesionales p ON p.ProfesionalId = pe.ProfesionalId
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = p.ProfesionalId
                WHERE pe.EspecialidadId = @EspecialidadId
                  AND pe.Activa = 1 AND p.Activo = 1 AND u.Activo = 1
                ORDER BY TieneCobertura DESC, u.Apellido, u.Nombre;",
                ParametroInt32("@EspecialidadId", especialidadId),
                ParametroGuid("@PacienteId", pacienteId),
                new SqlParameter("@FechaCobertura", SqlDbType.Date) { Value = DateTime.Today });
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
                  AND t.Estado IN (N'Solicitado', N'Confirmado', N'EnEspera')
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

        public DataTable ObtenerHorariosDisponibilidadVentana(
            Guid profesionalId, int especialidadId, DateTime desde, DateTime hasta)
        {
            return Consultar(@"
                SELECT h.DiaSemana, h.HoraInicio, h.HoraFin,
                       e.DuracionEstandarMinutos, e.Nombre AS Especialidad
                FROM dbo.HorariosAtencion h
                INNER JOIN dbo.Profesionales p ON p.ProfesionalId = h.ProfesionalId
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = p.ProfesionalId
                INNER JOIN dbo.ProfesionalesEspecialidades pe
                    ON pe.ProfesionalId = h.ProfesionalId AND pe.EspecialidadId = h.EspecialidadId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = h.EspecialidadId
                WHERE h.ProfesionalId = @ProfesionalId AND h.EspecialidadId = @EspecialidadId
                  AND h.Activo = 1 AND pe.Activa = 1
                  AND p.Activo = 1 AND u.Activo = 1 AND e.Activa = 1
                  AND @Desde <= @Hasta
                ORDER BY h.DiaSemana, h.HoraInicio;",
                ParametroGuid("@ProfesionalId", profesionalId),
                ParametroInt32("@EspecialidadId", especialidadId),
                new SqlParameter("@Desde", SqlDbType.Date) { Value = desde.Date },
                new SqlParameter("@Hasta", SqlDbType.Date) { Value = hasta.Date });
        }

        public DataTable ObtenerTurnosEnVentana(
            Guid profesionalId, int especialidadId, DateTime desde, DateTime hasta, int? turnoExcluido)
        {
            return Consultar(@"
                SELECT t.TurnoId, t.FechaHora, t.Estado, e.DuracionEstandarMinutos
                FROM dbo.Turnos t
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = t.EspecialidadId
                WHERE t.ProfesionalId = @ProfesionalId AND t.EspecialidadId = @EspecialidadId
                  AND t.FechaHora >= @Desde AND t.FechaHora < DATEADD(day, 1, @Hasta)
                  AND t.Estado IN (N'Solicitado', N'Confirmado', N'EnEspera')
                  AND (@TurnoExcluido IS NULL OR t.TurnoId <> @TurnoExcluido)
                ORDER BY t.FechaHora, t.TurnoId;",
                ParametroGuid("@ProfesionalId", profesionalId),
                ParametroInt32("@EspecialidadId", especialidadId),
                new SqlParameter("@Desde", SqlDbType.Date) { Value = desde.Date },
                new SqlParameter("@Hasta", SqlDbType.Date) { Value = hasta.Date },
                new SqlParameter("@TurnoExcluido", SqlDbType.Int)
                {
                    Value = turnoExcluido.HasValue ? (object)turnoExcluido.Value : DBNull.Value
                });
        }

        public DataTable ObtenerTurnosSinResolver()
        {
            return Consultar(@"
                SELECT t.TurnoId, t.FechaHora, t.Estado, t.Motivo,
                       paciente.Nombre + N' ' + paciente.Apellido AS Paciente,
                       profesional.Nombre + N' ' + profesional.Apellido AS Profesional,
                       e.Nombre AS Especialidad
                FROM dbo.Turnos t
                INNER JOIN dbo.Usuarios paciente ON paciente.UsuarioId = t.PacienteId
                INNER JOIN dbo.Usuarios profesional ON profesional.UsuarioId = t.ProfesionalId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = t.EspecialidadId
                WHERE t.Estado IN (N'Solicitado', N'Confirmado', N'EnEspera')
                  AND t.FechaHora <= SYSDATETIME()
                ORDER BY t.FechaHora, t.TurnoId;");
        }

        public DataTable ObtenerTurnos(Guid? pacienteId, Guid? profesionalId, DateTime? desde, DateTime? hasta)
        {
            return Consultar(@"
                SELECT t.TurnoId, t.PacienteId, t.ProfesionalId, t.EspecialidadId,
                       paciente.Nombre + N' ' + paciente.Apellido AS Paciente,
                       profesional.Nombre + N' ' + profesional.Apellido AS Profesional,
                       e.Nombre AS Especialidad, e.DuracionEstandarMinutos, t.FechaHora,
                       t.Estado, t.Motivo, t.EstadoSena, t.MontoSena, t.VencimientoSena,
                       t.FechaHoraLlegada
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

        public DataTable ObtenerTurnosRecepcion(
            DateTime fecha, string filtro, int? especialidadId, Guid? profesionalId, string estadoFiltro)
        {
            DateTime fechaInicio = fecha.Date;
            DateTime fechaFin = fechaInicio.AddDays(1);
            string filtroLimpio = string.IsNullOrWhiteSpace(filtro) ? null : filtro.Trim();
            string filtroLike = filtroLimpio != null ? "%" + filtroLimpio + "%" : null;
            string estadoLimpio = string.IsNullOrWhiteSpace(estadoFiltro) || estadoFiltro == "Todos" ? null : estadoFiltro.Trim();

            return Consultar(@"
                SELECT t.TurnoId, t.PacienteId,
                       paciente.Nombre + N' ' + paciente.Apellido AS Paciente,
                       paciente.NumeroDocumento AS PacienteDni,
                       t.ProfesionalId,
                       profesional.Nombre + N' ' + profesional.Apellido AS Profesional,
                       t.EspecialidadId,
                       e.Nombre AS Especialidad,
                       e.DuracionEstandarMinutos,
                       t.FechaHora,
                       t.Estado,
                       t.Motivo,
                       t.EstadoSena,
                       t.MontoSena,
                       t.VencimientoSena,
                       t.FechaHoraLlegada
                FROM dbo.Turnos t
                INNER JOIN dbo.Pacientes p ON p.PacienteId = t.PacienteId
                INNER JOIN dbo.Usuarios paciente ON paciente.UsuarioId = p.PacienteId
                INNER JOIN dbo.Profesionales pr ON pr.ProfesionalId = t.ProfesionalId
                INNER JOIN dbo.Usuarios profesional ON profesional.UsuarioId = pr.ProfesionalId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = t.EspecialidadId
                WHERE t.FechaHora >= @FechaInicio AND t.FechaHora < @FechaFin
                  AND (@EspecialidadId IS NULL OR t.EspecialidadId = @EspecialidadId)
                  AND (@ProfesionalId IS NULL OR t.ProfesionalId = @ProfesionalId)
                  AND (@EstadoFiltro IS NULL OR t.Estado = @EstadoFiltro)
                  AND (@Filtro IS NULL OR paciente.NumeroDocumento LIKE @FiltroLike 
                                       OR paciente.Apellido LIKE @FiltroLike 
                                       OR paciente.Nombre LIKE @FiltroLike)
                ORDER BY t.FechaHora ASC, t.TurnoId ASC;",
                new SqlParameter("@FechaInicio", SqlDbType.DateTime2) { Value = fechaInicio },
                new SqlParameter("@FechaFin", SqlDbType.DateTime2) { Value = fechaFin },
                ParametroInt32Nullable("@EspecialidadId", especialidadId),
                ParametroGuidNullable("@ProfesionalId", profesionalId),
                new SqlParameter("@EstadoFiltro", SqlDbType.NVarChar, 20) { Value = (object)estadoLimpio ?? DBNull.Value },
                new SqlParameter("@Filtro", SqlDbType.NVarChar, 100) { Value = (object)filtroLimpio ?? DBNull.Value },
                new SqlParameter("@FiltroLike", SqlDbType.NVarChar, 105) { Value = (object)filtroLike ?? DBNull.Value });
        }

        public TurnoDetalle ObtenerTurno(int turnoId)
        {
            DataTable datos = Consultar(@"
                SELECT TurnoId, PacienteId, ProfesionalId, EspecialidadId,
                       FechaHora, Estado, Motivo, FechaHoraLlegada
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
                Motivo = fila.IsNull("Motivo") ? string.Empty : Convert.ToString(fila["Motivo"]),
                FechaHoraLlegada = fila.IsNull("FechaHoraLlegada") ? (DateTime?)null : Convert.ToDateTime(fila["FechaHoraLlegada"])
            };
        }

        public SenaParaPagar ObtenerSenaDeTurno(int turnoId)
        {
            DataTable datos = Consultar(@"
                SELECT t.TurnoId, t.PacienteId, t.FechaHora, t.Estado, t.EstadoSena,
                       t.MontoSena, t.VencimientoSena, e.Nombre AS Especialidad,
                       profesional.Nombre + N' ' + profesional.Apellido AS Profesional
                FROM dbo.Turnos t
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = t.EspecialidadId
                INNER JOIN dbo.Usuarios profesional ON profesional.UsuarioId = t.ProfesionalId
                WHERE t.TurnoId = @TurnoId;",
                ParametroInt32("@TurnoId", turnoId));

            if (datos.Rows.Count == 0)
            {
                return null;
            }

            DataRow fila = datos.Rows[0];
            return new SenaParaPagar
            {
                TurnoId = Convert.ToInt32(fila["TurnoId"]),
                PacienteId = (Guid)fila["PacienteId"],
                FechaHora = Convert.ToDateTime(fila["FechaHora"]),
                Estado = Convert.ToString(fila["Estado"]),
                EstadoSena = Convert.ToString(fila["EstadoSena"]),
                MontoSena = Convert.ToDecimal(fila["MontoSena"]),
                VencimientoSena = fila.IsNull("VencimientoSena") ? (DateTime?)null : Convert.ToDateTime(fila["VencimientoSena"]),
                Especialidad = Convert.ToString(fila["Especialidad"]),
                Profesional = Convert.ToString(fila["Profesional"])
            };
        }

        public ReservaTurno SolicitarTurno(SolicitudTurno solicitud)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                conexion.Open();
                using (SqlTransaction transaccion = conexion.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        LiberarReservasVencidas(conexion, transaccion);
                        ValidarHorarioYDisponibilidad(conexion, transaccion, solicitud, null);

                        decimal montoSena = CalcularSenaRequerida(conexion, transaccion, solicitud);
                        string estadoSena = "NoRequerida";
                        string estadoTurno = "Solicitado";
                        DateTime? vencimientoSena = null;
                        DateTime? fechaHoraSena = null;
                        bool cobrarEnMostrador = false;

                        if (montoSena > 0 && solicitud.EsReservaOnline)
                        {
                            // Online: la seña queda pendiente y el paciente tiene unos minutos para pagarla.
                            estadoSena = "Pendiente";
                            vencimientoSena = ReglasTurno.CalcularVencimientoSena(DateTime.Now);
                        }
                        else if (montoSena > 0)
                        {
                            // Presencial: sin cobertura, la seña es obligatoria y se cobra ahora en el mostrador.
                            if (string.IsNullOrWhiteSpace(solicitud.MedioPagoSena) || !solicitud.CobradorId.HasValue)
                            {
                                throw new InvalidOperationException(
                                    "El paciente no tiene cobertura para este turno: para reservarlo hay que cobrar la seña de "
                                    + montoSena.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("es-AR")) + ".");
                            }

                            estadoSena = "Pagada";
                            estadoTurno = "Confirmado";
                            fechaHoraSena = DateTime.Now;
                            cobrarEnMostrador = true;
                        }
                        else
                        {
                            montoSena = 0m;
                        }

                        int turnoId = InsertarTurno(conexion, transaccion, solicitud, montoSena, estadoSena,
                            vencimientoSena, estadoTurno, fechaHoraSena);

                        if (cobrarEnMostrador)
                        {
                            // En la MISMA transaccion: si el cobro falla (por ejemplo, caja cerrada),
                            // tampoco se crea el turno.
                            CobrosDatos.RegistrarSenaPresencialEnTransaccion(conexion, transaccion, turnoId,
                                montoSena, solicitud.MedioPagoSena, solicitud.CobradorId.Value,
                                solicitud.CuentaDevolucionSena);
                        }

                        transaccion.Commit();
                        return new ReservaTurno
                        {
                            TurnoId = turnoId,
                            MontoSena = montoSena,
                            SenaCobradaEnMostrador = cobrarEnMostrador,
                            MedioPagoSena = cobrarEnMostrador ? solicitud.MedioPagoSena : null
                        };
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
                        LiberarReservasVencidas(conexion, transaccion);

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

                        if (pacienteAutorizado.HasValue && !ReglasTurno.PacienteDentroDelPlazo(original.FechaHora))
                        {
                            throw new InvalidOperationException("Los turnos solo se pueden reprogramar con al menos "
                                + ReglasTurno.HorasMinimasCancelacionPaciente + " horas de anticipacion. Comunicate con la clinica.");
                        }

                        if (original.ProfesionalId == destino.ProfesionalId
                            && original.EspecialidadId == destino.EspecialidadId
                            && original.FechaHora == destino.FechaHora)
                        {
                            throw new InvalidOperationException("Selecciona un horario diferente para reprogramar el turno.");
                        }

                        decimal montoSenaOriginal;
                        string estadoSenaOriginal;
                        DateTime? vencimientoSenaOriginal;
                        DateTime? fechaHoraSenaOriginal;
                        ObtenerDatosSenaTurno(
                            conexion, transaccion, turnoId,
                            out montoSenaOriginal, out estadoSenaOriginal,
                            out vencimientoSenaOriginal, out fechaHoraSenaOriginal);

                        ValidarHorarioYDisponibilidad(conexion, transaccion, destino, turnoId);

                        decimal nuevoMontoSena = 0m;
                        string nuevoEstadoSena = "NoRequerida";
                        DateTime? nuevoVencimientoSena = null;
                        DateTime? nuevaFechaHoraSena = null;
                        string nuevoEstadoTurno = "Solicitado";

                        if (estadoSenaOriginal == "Pagada")
                        {
                            nuevoMontoSena = montoSenaOriginal;
                            nuevoEstadoSena = "Pagada";
                            nuevaFechaHoraSena = fechaHoraSenaOriginal;
                            nuevoEstadoTurno = "Confirmado";
                        }
                        else if (estadoSenaOriginal == "Pendiente" || destino.EsReservaOnline)
                        {
                            nuevoMontoSena = CalcularSenaRequerida(conexion, transaccion, destino);
                            if (nuevoMontoSena > 0)
                            {
                                nuevoEstadoSena = "Pendiente";
                                nuevoVencimientoSena = vencimientoSenaOriginal ?? ReglasTurno.CalcularVencimientoSena(DateTime.Now);
                            }
                        }

                        using (SqlCommand cmdCancelar = new SqlCommand(@"
                            UPDATE dbo.Turnos
                            SET Estado = N'Cancelado',
                                EstadoSena = N'NoRequerida',
                                VencimientoSena = NULL
                            WHERE TurnoId = @TurnoId;",
                            conexion, transaccion))
                        {
                            cmdCancelar.Parameters.Add("@TurnoId", SqlDbType.Int).Value = turnoId;
                            cmdCancelar.ExecuteNonQuery();
                        }

                        int nuevoTurnoId = InsertarTurno(
                            conexion, transaccion, destino,
                            nuevoMontoSena, nuevoEstadoSena, nuevoVencimientoSena,
                            nuevoEstadoTurno, nuevaFechaHoraSena, turnoId);

                        if (estadoSenaOriginal == "Pagada")
                        {
                            using (SqlCommand cmdCobro = new SqlCommand(@"
                                UPDATE dbo.Cobros
                                SET TurnoId = @NuevoTurnoId
                                WHERE TurnoId = @TurnoId AND TipoCobro = N'Sena';",
                                conexion, transaccion))
                            {
                                cmdCobro.Parameters.Add("@NuevoTurnoId", SqlDbType.Int).Value = nuevoTurnoId;
                                cmdCobro.Parameters.Add("@TurnoId", SqlDbType.Int).Value = turnoId;
                                cmdCobro.ExecuteNonQuery();
                            }
                        }

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

        // Cancela el turno y resuelve la seña pagada en la misma transaccion:
        //   se devuelve si cancela la clinica, o si cancela el paciente con al menos 24 hs de anticipacion;
        //   si cancela el paciente con menos tiempo (solo puede hacerlo el administrativo), se retiene.
        public ResultadoCancelacion CancelarTurno(int turnoId, Guid? pacienteAutorizado,
            MotivoCancelacion motivo, Guid? usuarioId)
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

                        if (pacienteAutorizado.HasValue && !ReglasTurno.PacienteDentroDelPlazo(turno.FechaHora))
                        {
                            throw new InvalidOperationException("Los turnos solo se pueden cancelar con al menos "
                                + ReglasTurno.HorasMinimasCancelacionPaciente + " horas de anticipacion. Comunicate con la clinica.");
                        }

                        bool devolverSena = motivo == MotivoCancelacion.Clinica
                            || ReglasTurno.PacienteDentroDelPlazo(turno.FechaHora);

                        CancelarTurnoEnTransaccion(conexion, transaccion, turnoId);
                        ResultadoCancelacion resultado = ReembolsosDatos.ResolverSenaAlCancelar(
                            conexion, transaccion, turnoId, devolverSena, motivo, usuarioId);
                        transaccion.Commit();
                        return resultado;
                    }
                    catch
                    {
                        transaccion.Rollback();
                        throw;
                    }
                }
            }
        }

        // Devuelve el resultado de la seña solo cuando el nuevo estado es Cancelado (si no, null).
        public ResultadoCancelacion CambiarEstadoTurno(
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
                                || (turno.Estado == "Confirmado" && nuevoEstado == "EnEspera" && turno.FechaHora.Date == DateTime.Today)
                                || (turno.Estado == "EnEspera" && nuevoEstado == "Confirmado")
                                || (EsVigente(turno.Estado) && nuevoEstado == "Cancelado")
                                || ((turno.Estado == "Confirmado" || turno.Estado == "EnEspera")
                                    && (nuevoEstado == "Atendido" || nuevoEstado == "Ausente")
                                    && (nuevoEstado == "Ausente" || turno.Estado == "EnEspera" || turno.FechaHora <= DateTime.Now)))
                            : (turno.Estado == "Confirmado" || turno.Estado == "EnEspera")
                                && (nuevoEstado == "Atendido" || nuevoEstado == "Ausente")
                                && (nuevoEstado == "Ausente" || turno.Estado == "EnEspera" || turno.FechaHora <= DateTime.Now);

                        if (!puedeCambiar)
                        {
                            throw new InvalidOperationException("El cambio de estado no está permitido para este turno.");
                        }

                        using (SqlCommand comando = new SqlCommand(@"
                            UPDATE dbo.Turnos
                            SET Estado = @Estado,
                                FechaHoraLlegada = CASE WHEN @Estado = N'EnEspera' AND FechaHoraLlegada IS NULL THEN SYSDATETIME()
                                                        WHEN @Estado = N'Confirmado' THEN NULL
                                                        ELSE FechaHoraLlegada END,
                                -- Ausente: el paciente no vino, la clinica se queda con la seña.
                                -- Cancelado: la seña sin pagar deja de ser necesaria; la pagada
                                -- la resuelve ResolverSenaAlCancelar (mas abajo).
                                EstadoSena = CASE WHEN @Estado = N'Ausente' AND EstadoSena = N'Pagada'
                                                  THEN N'Retenida'
                                                  WHEN @Estado = N'Cancelado' AND EstadoSena = N'Pendiente'
                                                  THEN N'NoRequerida'
                                                  ELSE EstadoSena END
                            WHERE TurnoId = @TurnoId;",
                            conexion, transaccion))
                        {
                            comando.Parameters.Add("@Estado", SqlDbType.NVarChar, 20).Value = nuevoEstado;
                            comando.Parameters.Add("@TurnoId", SqlDbType.Int).Value = turnoId;
                            comando.ExecuteNonQuery();
                        }

                        // Cancelacion desde Revision del dia (turno pasado sin resolver): cuenta como
                        // cancelacion de la clinica, asi que la seña pagada se devuelve.
                        // (Si el paciente no vino, corresponde marcarlo Ausente y la seña se retiene.)
                        ResultadoCancelacion resultado = null;
                        if (nuevoEstado == "Cancelado")
                        {
                            resultado = ReembolsosDatos.ResolverSenaAlCancelar(
                                conexion, transaccion, turnoId, true, MotivoCancelacion.Clinica, null);
                        }

                        transaccion.Commit();
                        return resultado;
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
                      AND t.Estado IN (N'Solicitado', N'Confirmado', N'EnEspera')
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
                      AND t.Estado IN (N'Solicitado', N'Confirmado', N'EnEspera')
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
                       FechaHora, Estado, Motivo, FechaHoraLlegada
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
                        Motivo = lector.IsDBNull(6) ? string.Empty : lector.GetString(6),
                        FechaHoraLlegada = lector.IsDBNull(7) ? (DateTime?)null : lector.GetDateTime(7)
                    };
                }
            }
        }

        private static void ObtenerDatosSenaTurno(
            SqlConnection conexion, SqlTransaction transaccion, int turnoId,
            out decimal montoSena, out string estadoSena,
            out DateTime? vencimientoSena, out DateTime? fechaHoraSena)
        {
            montoSena = 0m;
            estadoSena = "NoRequerida";
            vencimientoSena = null;
            fechaHoraSena = null;

            using (SqlCommand comando = new SqlCommand(@"
                SELECT MontoSena, EstadoSena, VencimientoSena, FechaHoraSena
                FROM dbo.Turnos
                WHERE TurnoId = @TurnoId;",
                conexion, transaccion))
            {
                comando.Parameters.Add("@TurnoId", SqlDbType.Int).Value = turnoId;
                using (SqlDataReader lector = comando.ExecuteReader())
                {
                    if (lector.Read())
                    {
                        montoSena = lector.GetDecimal(0);
                        estadoSena = lector.GetString(1);
                        vencimientoSena = lector.IsDBNull(2) ? (DateTime?)null : lector.GetDateTime(2);
                        fechaHoraSena = lector.IsDBNull(3) ? (DateTime?)null : lector.GetDateTime(3);
                    }
                }
            }
        }

        private static void CancelarTurnoEnTransaccion(
            SqlConnection conexion, SqlTransaction transaccion, int turnoId)
        {
            using (SqlCommand comando = new SqlCommand(@"
                UPDATE dbo.Turnos
                SET Estado = N'Cancelado',
                    -- Seña sin pagar: deja de ser necesaria. Seña pagada: la resuelve
                    -- ReembolsosDatos.ResolverSenaAlCancelar (devolver o retener).
                    EstadoSena = CASE WHEN EstadoSena = N'Pendiente' THEN N'NoRequerida'
                                      ELSE EstadoSena END,
                    VencimientoSena = NULL
                WHERE TurnoId = @TurnoId;",
                conexion, transaccion))
            {
                comando.Parameters.Add("@TurnoId", SqlDbType.Int).Value = turnoId;
                comando.ExecuteNonQuery();
            }
        }

        public void LiberarReservasVencidas()
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                conexion.Open();
                LiberarReservasVencidas(conexion, null);
            }
        }

        private static void LiberarReservasVencidas(SqlConnection conexion, SqlTransaction transaccion)
        {
            using (SqlCommand comando = new SqlCommand(@"
                UPDATE dbo.Turnos
                SET Estado = N'Cancelado',
                    EstadoSena = N'Vencida'
                WHERE EstadoSena = N'Pendiente'
                  AND VencimientoSena IS NOT NULL
                  AND VencimientoSena <= DATEADD(minute, -@MargenMinutos, SYSDATETIME())
                  AND Estado = N'Solicitado';",
                conexion, transaccion))
            {
                comando.Parameters.Add("@MargenMinutos", SqlDbType.Int).Value = ReglasTurno.MinutosMargenPagoSena;
                comando.ExecuteNonQuery();
            }
        }

        // Seña que corresponde a un turno (0 si el paciente tiene cobertura). Para mostrarla antes de reservar.
        public decimal ObtenerSenaRequerida(SolicitudTurno solicitud)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                conexion.Open();
                return CalcularSenaRequerida(conexion, null, solicitud);
            }
        }

        private static decimal CalcularSenaRequerida(
            SqlConnection conexion, SqlTransaction transaccion, SolicitudTurno solicitud)
        {
            using (SqlCommand comando = new SqlCommand(@"
                SELECT pe.ValorConsulta,
                       CASE WHEN " + SqlTieneCobertura + @" THEN 1 ELSE 0 END AS TieneCobertura
                FROM dbo.ProfesionalesEspecialidades pe
                WHERE pe.ProfesionalId = @ProfesionalId
                  AND pe.EspecialidadId = @EspecialidadId
                  AND pe.Activa = 1;",
                conexion, transaccion))
            {
                comando.Parameters.Add("@PacienteId", SqlDbType.UniqueIdentifier).Value = solicitud.PacienteId;
                comando.Parameters.Add("@ProfesionalId", SqlDbType.UniqueIdentifier).Value = solicitud.ProfesionalId;
                comando.Parameters.Add("@EspecialidadId", SqlDbType.Int).Value = solicitud.EspecialidadId;
                comando.Parameters.Add("@FechaCobertura", SqlDbType.Date).Value = solicitud.FechaHora.Date;

                using (SqlDataReader lector = comando.ExecuteReader())
                {
                    if (!lector.Read())
                    {
                        return 0m;
                    }

                    decimal valorConsulta = lector.GetDecimal(0);
                    bool tieneCobertura = lector.GetInt32(1) == 1;
                    return tieneCobertura ? 0m : ReglasTurno.CalcularSena(valorConsulta);
                }
            }
        }

        private static int InsertarTurno(
            SqlConnection conexion, SqlTransaction transaccion, SolicitudTurno solicitud,
            decimal montoSena = 0m, string estadoSena = "NoRequerida", DateTime? vencimientoSena = null,
            string estado = "Solicitado", DateTime? fechaHoraSena = null,
            int? turnoReprogramadoDesdeId = null)
        {
            using (SqlCommand comando = new SqlCommand(@"
                INSERT INTO dbo.Turnos
                    (PacienteId, ProfesionalId, EspecialidadId, FechaHora, Estado, Motivo,
                     MontoSena, EstadoSena, VencimientoSena, FechaHoraSena, TurnoReprogramadoDesdeId)
                VALUES
                    (@PacienteId, @ProfesionalId, @EspecialidadId, @FechaHora, @Estado, @Motivo,
                     @MontoSena, @EstadoSena, @VencimientoSena, @FechaHoraSena, @TurnoReprogramadoDesdeId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                conexion, transaccion))
            {
                comando.Parameters.Add("@Estado", SqlDbType.NVarChar, 20).Value = estado;
                comando.Parameters.Add("@MontoSena", SqlDbType.Decimal).Value = montoSena;
                comando.Parameters["@MontoSena"].Precision = 12;
                comando.Parameters["@MontoSena"].Scale = 2;
                comando.Parameters.Add("@EstadoSena", SqlDbType.NVarChar, 20).Value = estadoSena;
                comando.Parameters.Add("@VencimientoSena", SqlDbType.DateTime2).Value =
                    vencimientoSena.HasValue ? (object)vencimientoSena.Value : DBNull.Value;
                comando.Parameters.Add("@FechaHoraSena", SqlDbType.DateTime2).Value =
                    fechaHoraSena.HasValue ? (object)fechaHoraSena.Value : DBNull.Value;
                comando.Parameters.Add("@TurnoReprogramadoDesdeId", SqlDbType.Int).Value =
                    turnoReprogramadoDesdeId.HasValue ? (object)turnoReprogramadoDesdeId.Value : DBNull.Value;
                comando.Parameters.Add("@PacienteId", SqlDbType.UniqueIdentifier).Value = solicitud.PacienteId;
                comando.Parameters.Add("@ProfesionalId", SqlDbType.UniqueIdentifier).Value = solicitud.ProfesionalId;
                comando.Parameters.Add("@EspecialidadId", SqlDbType.Int).Value = solicitud.EspecialidadId;
                comando.Parameters.Add("@FechaHora", SqlDbType.DateTime2).Value = solicitud.FechaHora;
                comando.Parameters.Add("@Motivo", SqlDbType.NVarChar, 500).Value =
                    string.IsNullOrWhiteSpace(solicitud.Motivo) ? (object)DBNull.Value : solicitud.Motivo.Trim();
                return Convert.ToInt32(comando.ExecuteScalar());
            }
        }

        public NotificacionTurno ObtenerDatosNotificacion(int turnoId)
        {
            DataTable tabla = Consultar(@"
                SELECT t.TurnoId, t.PacienteId,
                       up.Nombre + N' ' + up.Apellido AS PacienteNombre,
                       up.Email AS PacienteEmail,
                       ud.Nombre + N' ' + ud.Apellido AS ProfesionalNombre,
                       e.Nombre AS Especialidad,
                       t.FechaHora,
                       t.Estado,
                       t.MontoSena,
                       t.EstadoSena
                FROM dbo.Turnos t
                INNER JOIN dbo.Pacientes p ON p.PacienteId = t.PacienteId
                INNER JOIN dbo.Usuarios up ON up.UsuarioId = p.PacienteId
                INNER JOIN dbo.Profesionales d ON d.ProfesionalId = t.ProfesionalId
                INNER JOIN dbo.Usuarios ud ON ud.UsuarioId = d.ProfesionalId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = t.EspecialidadId
                WHERE t.TurnoId = @TurnoId;",
                ParametroInt32("@TurnoId", turnoId));

            if (tabla.Rows.Count == 0)
            {
                return null;
            }

            DataRow fila = tabla.Rows[0];
            return new NotificacionTurno
            {
                TurnoId = Convert.ToInt32(fila["TurnoId"]),
                PacienteId = (Guid)fila["PacienteId"],
                PacienteNombre = Convert.ToString(fila["PacienteNombre"]),
                PacienteEmail = Convert.ToString(fila["PacienteEmail"]),
                ProfesionalNombre = Convert.ToString(fila["ProfesionalNombre"]),
                Especialidad = Convert.ToString(fila["Especialidad"]),
                FechaHora = Convert.ToDateTime(fila["FechaHora"]),
                Estado = Convert.ToString(fila["Estado"]),
                MontoSena = Convert.ToDecimal(fila["MontoSena"]),
                EstadoSena = Convert.ToString(fila["EstadoSena"])
            };
        }

        public IList<NotificacionTurno> ObtenerTurnosParaRecordatorio48hs(DateTime fechaLimite)
        {
            DataTable tabla = Consultar(@"
                SELECT t.TurnoId, t.PacienteId,
                       up.Nombre + N' ' + up.Apellido AS PacienteNombre,
                       up.Email AS PacienteEmail,
                       ud.Nombre + N' ' + ud.Apellido AS ProfesionalNombre,
                       e.Nombre AS Especialidad,
                       t.FechaHora,
                       t.Estado,
                       t.MontoSena,
                       t.EstadoSena
                FROM dbo.Turnos t
                INNER JOIN dbo.Pacientes p ON p.PacienteId = t.PacienteId
                INNER JOIN dbo.Usuarios up ON up.UsuarioId = p.PacienteId
                INNER JOIN dbo.Profesionales d ON d.ProfesionalId = t.ProfesionalId
                INNER JOIN dbo.Usuarios ud ON ud.UsuarioId = d.ProfesionalId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = t.EspecialidadId
                WHERE t.Estado IN (N'Solicitado', N'Confirmado')
                  AND t.Recordatorio48hsEnviado = 0
                  AND t.FechaHora > SYSDATETIME()
                  AND t.FechaHora <= @FechaLimite
                ORDER BY t.FechaHora ASC;",
                new SqlParameter("@FechaLimite", SqlDbType.DateTime2) { Value = fechaLimite });

            List<NotificacionTurno> lista = new List<NotificacionTurno>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(new NotificacionTurno
                {
                    TurnoId = Convert.ToInt32(fila["TurnoId"]),
                    PacienteId = (Guid)fila["PacienteId"],
                    PacienteNombre = Convert.ToString(fila["PacienteNombre"]),
                    PacienteEmail = Convert.ToString(fila["PacienteEmail"]),
                    ProfesionalNombre = Convert.ToString(fila["ProfesionalNombre"]),
                    Especialidad = Convert.ToString(fila["Especialidad"]),
                    FechaHora = Convert.ToDateTime(fila["FechaHora"]),
                    Estado = Convert.ToString(fila["Estado"]),
                    MontoSena = Convert.ToDecimal(fila["MontoSena"]),
                    EstadoSena = Convert.ToString(fila["EstadoSena"])
                });
            }

            return lista;
        }

        public void MarcarRecordatorio48hsEnviado(int turnoId)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            using (SqlCommand comando = new SqlCommand(@"
                UPDATE dbo.Turnos
                SET Recordatorio48hsEnviado = 1
                WHERE TurnoId = @TurnoId;", conexion))
            {
                comando.Parameters.Add(ParametroInt32("@TurnoId", turnoId));
                conexion.Open();
                comando.ExecuteNonQuery();
            }
        }

        public bool TieneTurnoVigenteEspecialidad(Guid pacienteId, int especialidadId, int? turnoExcluido)
        {
            DataTable tabla = Consultar(@"
                SELECT TOP (1) 1
                FROM dbo.Turnos
                WHERE PacienteId = @PacienteId
                  AND EspecialidadId = @EspecialidadId
                  AND Estado IN (N'Solicitado', N'Confirmado', N'EnEspera')
                  AND (@TurnoExcluido IS NULL OR TurnoId <> @TurnoExcluido);",
                ParametroGuid("@PacienteId", pacienteId),
                ParametroInt32("@EspecialidadId", especialidadId),
                new SqlParameter("@TurnoExcluido", SqlDbType.Int)
                {
                    Value = turnoExcluido.HasValue ? (object)turnoExcluido.Value : DBNull.Value
                });

            return tabla.Rows.Count > 0;
        }

        public IList<TurnoConsultorioItem> ObtenerTurnosConsultorio(Guid profesionalId, DateTime fecha)
        {
            DateTime fechaInicio = fecha.Date;
            DateTime fechaFin = fechaInicio.AddDays(1);

            DataTable tabla = Consultar(@"
                SELECT t.TurnoId,
                       t.FechaHora,
                       t.PacienteId,
                       paciente.Nombre + N' ' + paciente.Apellido AS NombrePaciente,
                       paciente.NumeroDocumento,
                       ISNULL(os.Nombre, N'Particular') AS ObraSocial,
                       p.NumeroAfiliado,
                       e.Nombre AS Especialidad,
                       t.Motivo AS MotivoTurno,
                       t.Estado,
                       t.FechaHoraLlegada,
                       CASE WHEN rc.TurnoId IS NOT NULL THEN 1 ELSE 0 END AS TieneEvolucion,
                       rc.Diagnostico,
                       rc.Observaciones,
                       rc.FechaRegistro AS FechaRegistroEvolucion
                FROM dbo.Turnos t
                INNER JOIN dbo.Usuarios paciente ON paciente.UsuarioId = t.PacienteId
                INNER JOIN dbo.Pacientes p ON p.PacienteId = t.PacienteId
                LEFT JOIN dbo.ObrasSociales os ON os.ObraSocialId = p.ObraSocialId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = t.EspecialidadId
                LEFT JOIN dbo.RegistrosClinicos rc ON rc.TurnoId = t.TurnoId
                WHERE t.ProfesionalId = @ProfesionalId
                  AND t.FechaHora >= @FechaInicio AND t.FechaHora < @FechaFin
                  AND t.Estado <> N'Cancelado'
                ORDER BY
                    CASE WHEN t.Estado = N'EnEspera' THEN 0
                         WHEN t.Estado = N'Confirmado' THEN 1
                         WHEN t.Estado = N'Atendido' AND rc.TurnoId IS NULL THEN 2
                         WHEN t.Estado = N'Atendido' THEN 3
                         ELSE 4 END,
                    t.FechaHora ASC;",
                ParametroGuid("@ProfesionalId", profesionalId),
                new SqlParameter("@FechaInicio", SqlDbType.DateTime2) { Value = fechaInicio },
                new SqlParameter("@FechaFin", SqlDbType.DateTime2) { Value = fechaFin });

            List<TurnoConsultorioItem> turnos = new List<TurnoConsultorioItem>(tabla.Rows.Count);
            foreach (DataRow fila in tabla.Rows)
            {
                turnos.Add(new TurnoConsultorioItem
                {
                    TurnoId = Convert.ToInt32(fila["TurnoId"]),
                    FechaHora = Convert.ToDateTime(fila["FechaHora"]),
                    PacienteId = (Guid)fila["PacienteId"],
                    NombrePaciente = Convert.ToString(fila["NombrePaciente"]),
                    NumeroDocumento = Convert.ToString(fila["NumeroDocumento"]),
                    ObraSocial = fila.IsNull("ObraSocial") ? null : Convert.ToString(fila["ObraSocial"]),
                    NumeroAfiliado = fila.IsNull("NumeroAfiliado") ? null : Convert.ToString(fila["NumeroAfiliado"]),
                    Especialidad = Convert.ToString(fila["Especialidad"]),
                    MotivoTurno = fila.IsNull("MotivoTurno") ? null : Convert.ToString(fila["MotivoTurno"]),
                    Estado = Convert.ToString(fila["Estado"]),
                    FechaHoraLlegada = fila.IsNull("FechaHoraLlegada") ? (DateTime?)null : Convert.ToDateTime(fila["FechaHoraLlegada"]),
                    TieneEvolucion = Convert.ToBoolean(fila["TieneEvolucion"]),
                    Diagnostico = fila.IsNull("Diagnostico") ? null : Convert.ToString(fila["Diagnostico"]),
                    Observaciones = fila.IsNull("Observaciones") ? null : Convert.ToString(fila["Observaciones"]),
                    FechaRegistroEvolucion = fila.IsNull("FechaRegistroEvolucion") ? (DateTime?)null : Convert.ToDateTime(fila["FechaRegistroEvolucion"])
                });
            }

            return turnos;
        }

        public void GuardarAtencionClinica(int turnoId, string motivoConsulta, string diagnostico, string observaciones)
        {
            if (turnoId <= 0)
            {
                throw new ArgumentException("El identificador del turno no es válido.", "turnoId");
            }

            if (string.IsNullOrWhiteSpace(diagnostico))
            {
                throw new InvalidOperationException("El diagnóstico es obligatorio para registrar la atención médica.");
            }

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                conexion.Open();
                using (SqlTransaction transaccion = conexion.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        using (SqlCommand cmdTurno = new SqlCommand(@"
                            SELECT Estado
                            FROM dbo.Turnos WITH (UPDLOCK, ROWLOCK)
                            WHERE TurnoId = @TurnoId;", conexion, transaccion))
                        {
                            cmdTurno.Parameters.Add(ParametroInt32("@TurnoId", turnoId));
                            object estadoObj = cmdTurno.ExecuteScalar();
                            if (estadoObj == null || estadoObj == DBNull.Value)
                            {
                                throw new InvalidOperationException("El turno no existe.");
                            }

                            string estado = Convert.ToString(estadoObj);
                            if (estado == "Cancelado")
                            {
                                throw new InvalidOperationException("No se puede registrar atención sobre un turno cancelado.");
                            }

                            if (estado != "Atendido")
                            {
                                using (SqlCommand cmdUpdate = new SqlCommand(@"
                                    UPDATE dbo.Turnos
                                    SET Estado = N'Atendido'
                                    WHERE TurnoId = @TurnoId;", conexion, transaccion))
                                {
                                    cmdUpdate.Parameters.Add(ParametroInt32("@TurnoId", turnoId));
                                    cmdUpdate.ExecuteNonQuery();
                                }
                            }
                        }

                        using (SqlCommand cmdRegistro = new SqlCommand(@"
                            IF EXISTS (SELECT 1 FROM dbo.RegistrosClinicos WITH (UPDLOCK, ROWLOCK) WHERE TurnoId = @TurnoId)
                            BEGIN
                                UPDATE dbo.RegistrosClinicos
                                SET Diagnostico = @Diagnostico,
                                    Observaciones = @Observaciones,
                                    MotivoConsulta = ISNULL(@MotivoConsulta, MotivoConsulta),
                                    FechaRegistro = SYSDATETIME()
                                WHERE TurnoId = @TurnoId;
                            END
                            ELSE
                            BEGIN
                                INSERT INTO dbo.RegistrosClinicos (TurnoId, FechaRegistro, MotivoConsulta, Diagnostico, Observaciones)
                                VALUES (@TurnoId, SYSDATETIME(), @MotivoConsulta, @Diagnostico, @Observaciones);
                            END", conexion, transaccion))
                        {
                            cmdRegistro.Parameters.Add(ParametroInt32("@TurnoId", turnoId));
                            cmdRegistro.Parameters.Add(new SqlParameter("@MotivoConsulta", SqlDbType.NVarChar, 500)
                            {
                                Value = string.IsNullOrWhiteSpace(motivoConsulta) ? (object)DBNull.Value : motivoConsulta.Trim()
                            });
                            cmdRegistro.Parameters.Add(new SqlParameter("@Diagnostico", SqlDbType.NVarChar, 1000)
                            {
                                Value = diagnostico.Trim()
                            });
                            cmdRegistro.Parameters.Add(new SqlParameter("@Observaciones", SqlDbType.NVarChar, -1)
                            {
                                Value = string.IsNullOrWhiteSpace(observaciones) ? (object)DBNull.Value : observaciones.Trim()
                            });

                            cmdRegistro.ExecuteNonQuery();
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

        private static SqlParameter ParametroInt32Nullable(string nombre, int? valor)
        {
            return new SqlParameter(nombre, SqlDbType.Int)
            {
                Value = valor.HasValue ? (object)valor.Value : DBNull.Value
            };
        }

        private static bool EsVigente(string estado)
        {
            return estado == "Solicitado" || estado == "Confirmado" || estado == "EnEspera";
        }
    }
}
