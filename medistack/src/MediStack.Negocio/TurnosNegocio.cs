using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Hosting;
using MediStack.Datos;
using MediStack.Dominio;

namespace MediStack.Negocio
{
    public sealed class TurnosNegocio
    {
        private readonly TurnosDatos _datos = new TurnosDatos();
        private readonly EmailNegocio _emailNegocio = new EmailNegocio();

        public NotificacionTurno ObtenerDatosNotificacion(int turnoId)
        {
            if (turnoId <= 0)
            {
                return null;
            }

            return _datos.ObtenerDatosNotificacion(turnoId);
        }

        public bool EnviarConfirmacionTurno(int turnoId, string baseUrl)
        {
            return EnviarConfirmacionTurnoAsyncCore(turnoId, baseUrl).GetAwaiter().GetResult();
        }

        public async Task<bool> EnviarConfirmacionTurnoAsyncCore(int turnoId, string baseUrl)
        {
            if (turnoId <= 0)
            {
                return false;
            }

            NotificacionTurno datos = _datos.ObtenerDatosNotificacion(turnoId);
            if (datos == null || string.IsNullOrWhiteSpace(datos.PacienteEmail))
            {
                return false;
            }

            ArmarUrlsNotificacion(datos, baseUrl);
            return await _emailNegocio.EnviarConfirmacionTurnoAsync(datos);
        }

        public void EnviarConfirmacionTurnoAsync(int turnoId, string baseUrl)
        {
            if (HostingEnvironment.IsHosted)
            {
                HostingEnvironment.QueueBackgroundWorkItem(async ct =>
                {
                    try
                    {
                        await EnviarConfirmacionTurnoAsyncCore(turnoId, baseUrl);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.TraceError("Error al enviar confirmación de turno por email: " + ex);
                    }
                });
            }
            else
            {
                System.Threading.ThreadPool.QueueUserWorkItem(async state =>
                {
                    try
                    {
                        await EnviarConfirmacionTurnoAsyncCore(turnoId, baseUrl);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.TraceError("Error al enviar confirmación de turno por email: " + ex);
                    }
                });
            }
        }

        public int ProcesarRecordatorios48hs(string baseUrl)
        {
            return ProcesarRecordatorios48hsAsync(baseUrl).GetAwaiter().GetResult();
        }

        public async Task<int> ProcesarRecordatorios48hsAsync(string baseUrl)
        {
            DateTime limite48hs = DateTime.Now.AddHours(48);
            IList<NotificacionTurno> pendientes = _datos.ObtenerTurnosParaRecordatorio48hs(limite48hs);
            int enviados = 0;

            foreach (NotificacionTurno turno in pendientes)
            {
                try
                {
                    ArmarUrlsNotificacion(turno, baseUrl);
                    bool ok = await _emailNegocio.EnviarRecordatorioTurnoAsync(turno);
                    _datos.MarcarRecordatorio48hsEnviado(turno.TurnoId);
                    if (ok)
                    {
                        enviados++;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError(
                        "Error enviando recordatorio para turno #" + turno.TurnoId + ": " + ex);
                }
            }

            return enviados;
        }

        private static void ArmarUrlsNotificacion(NotificacionTurno datos, string baseUrl)
        {
            string baseLimpia = (baseUrl ?? string.Empty).TrimEnd('/');
            if (string.IsNullOrEmpty(baseLimpia))
            {
                baseLimpia = "http://localhost:59999";
            }

            datos.UrlVerTurno = baseLimpia + "/Turnos.aspx?turnoId=" + datos.TurnoId;
            datos.UrlCancelarTurno = baseLimpia + "/Turnos.aspx?accion=cancelar&turnoId=" + datos.TurnoId;
        }

        public DataTable ObtenerPacientesActivos()
        {
            return _datos.ObtenerPacientesActivos();
        }

        public DataTable ObtenerProfesionalesActivos()
        {
            return _datos.ObtenerProfesionalesActivos();
        }

        public DataTable ObtenerEspecialidades(Guid profesionalId)
        {
            if (profesionalId == Guid.Empty)
            {
                return new DataTable();
            }

            return _datos.ObtenerEspecialidadesProfesional(profesionalId);
        }

        public DataTable ObtenerEspecialidadesActivas()
        {
            return _datos.ObtenerEspecialidadesActivas();
        }

        public DataTable ObtenerProfesionalesPorEspecialidad(int especialidadId)
        {
            if (especialidadId <= 0)
            {
                return new DataTable();
            }

            return _datos.ObtenerProfesionalesPorEspecialidad(especialidadId);
        }

        // Profesionales de la especialidad para el paciente que reserva online: cada uno indica si
        // su obra social lo cubre y, si no, la seña que pagaria. Vienen ordenados: cubiertos primero.
        public IList<ProfesionalParaPaciente> ObtenerProfesionalesParaPaciente(int especialidadId, Guid pacienteId)
        {
            List<ProfesionalParaPaciente> profesionales = new List<ProfesionalParaPaciente>();
            if (especialidadId <= 0 || pacienteId == Guid.Empty)
            {
                return profesionales;
            }

            DataTable datos = _datos.ObtenerProfesionalesPorEspecialidadParaPaciente(especialidadId, pacienteId);
            foreach (DataRow fila in datos.Rows)
            {
                decimal valorConsulta = Convert.ToDecimal(fila["ValorConsulta"]);
                bool tieneCobertura = Convert.ToInt32(fila["TieneCobertura"]) == 1;
                profesionales.Add(new ProfesionalParaPaciente
                {
                    ProfesionalId = (Guid)fila["ProfesionalId"],
                    Nombre = Convert.ToString(fila["Nombre"]),
                    ValorConsulta = valorConsulta,
                    TieneCobertura = tieneCobertura,
                    MontoSena = tieneCobertura ? 0m : ReglasTurno.CalcularSena(valorConsulta)
                });
            }

            return profesionales;
        }

        // Días de la semana (1 = lunes ... 7 = domingo) en los que el profesional atiende la especialidad.
        public IList<int> ObtenerDiasAtencion(Guid profesionalId, int especialidadId)
        {
            List<int> dias = new List<int>();
            if (profesionalId == Guid.Empty || especialidadId <= 0)
            {
                return dias;
            }

            foreach (DataRow fila in _datos.ObtenerDiasAtencion(profesionalId, especialidadId).Rows)
            {
                dias.Add(Convert.ToInt32(fila["DiaSemana"]));
            }

            return dias;
        }

            public IList<ResumenDisponibilidadProfesional> ObtenerResumenDisponibilidad(
                int especialidadId, Guid pacienteId, DateTime desde, DateTime hasta, int? turnoExcluido,
                bool calcularCobertura)
            {
                List<ResumenDisponibilidadProfesional> resultados = new List<ResumenDisponibilidadProfesional>();
                if (especialidadId <= 0 || pacienteId == Guid.Empty)
                {
                    return resultados;
                }

                DateTime inicio = desde.Date < DateTime.Today ? DateTime.Today : desde.Date;
                DateTime fin = hasta.Date < inicio ? inicio.AddDays(-1) : hasta.Date;
                DataTable profesionales = calcularCobertura
                    ? _datos.ObtenerProfesionalesPorEspecialidadParaPaciente(especialidadId, pacienteId)
                    : _datos.ObtenerProfesionalesPorEspecialidad(especialidadId);

                foreach (DataRow fila in profesionales.Rows)
                {
                    Guid profesionalId = (Guid)fila["ProfesionalId"];
                    IList<FechaDisponible> fechas = ObtenerFechasDisponibles(
                        profesionalId, especialidadId, inicio, fin, turnoExcluido);
                    bool tieneCobertura = !calcularCobertura
                        || Convert.ToInt32(fila["TieneCobertura"]) == 1;
                    decimal valorConsulta = calcularCobertura
                        ? Convert.ToDecimal(fila["ValorConsulta"])
                        : 0m;

                    resultados.Add(new ResumenDisponibilidadProfesional
                    {
                        ProfesionalId = profesionalId,
                        Nombre = Convert.ToString(fila["Nombre"]),
                        ProximaFecha = fechas.Count == 0 ? (DateTime?)null : fechas[0].Fecha,
                        CantidadFechas = fechas.Count,
                        TieneDisponibilidad = fechas.Count > 0,
                        TieneCobertura = tieneCobertura,
                        MontoSena = calcularCobertura && !tieneCobertura
                            ? ReglasTurno.CalcularSena(valorConsulta) : 0m,
                        MotivoSinDisponibilidad = fechas.Count == 0
                            ? "Sin horarios libres en los próximos 60 días." : string.Empty
                    });
                }

                return resultados
                    .OrderByDescending(r => r.TieneDisponibilidad)
                    .ThenBy(r => r.ProximaFecha ?? DateTime.MaxValue)
                    .ThenByDescending(r => r.TieneCobertura)
                    .ThenBy(r => r.Nombre)
                    .ToList();
            }

            public IList<FechaDisponible> ObtenerFechasDisponibles(
                Guid profesionalId, int especialidadId, DateTime desde, DateTime hasta, int? turnoExcluido)
            {
                List<FechaDisponible> fechas = new List<FechaDisponible>();
                if (profesionalId == Guid.Empty || especialidadId <= 0 || hasta.Date < desde.Date)
                {
                    return fechas;
                }

                _datos.LiberarReservasVencidas();
                DateTime inicio = desde.Date < DateTime.Today ? DateTime.Today : desde.Date;
                DateTime fin = hasta.Date;
                DataTable horarios = _datos.ObtenerHorariosDisponibilidadVentana(
                    profesionalId, especialidadId, inicio, fin);
                DataTable turnos = _datos.ObtenerTurnosEnVentana(
                    profesionalId, especialidadId, inicio, fin, turnoExcluido);

                for (DateTime fecha = inicio; fecha <= fin; fecha = fecha.AddDays(1))
                {
                    int diaSemana = ((int)fecha.DayOfWeek + 6) % 7 + 1;
                    int cantidad = 0;
                    foreach (DataRow horario in horarios.Rows)
                    {
                        if (Convert.ToInt32(horario["DiaSemana"]) != diaSemana)
                        {
                            continue;
                        }

                        TimeSpan hora = (TimeSpan)horario["HoraInicio"];
                        TimeSpan finHorario = (TimeSpan)horario["HoraFin"];
                        short duracion = Convert.ToInt16(horario["DuracionEstandarMinutos"]);
                        while (hora.Add(TimeSpan.FromMinutes(duracion)) <= finHorario)
                        {
                            DateTime inicioTurno = fecha.Date.Add(hora);
                            DateTime finTurno = inicioTurno.AddMinutes(duracion);
                            bool ocupado = false;
                            foreach (DataRow turno in turnos.Rows)
                            {
                                DateTime ocupadoDesde = Convert.ToDateTime(turno["FechaHora"]);
                                DateTime ocupadoHasta = ocupadoDesde.AddMinutes(
                                    Convert.ToInt32(turno["DuracionEstandarMinutos"]));
                                if (ocupadoDesde < finTurno && ocupadoHasta > inicioTurno)
                                {
                                    ocupado = true;
                                    break;
                                }
                            }

                            if (!ocupado && inicioTurno > DateTime.Now)
                            {
                                cantidad++;
                            }

                            hora = hora.Add(TimeSpan.FromMinutes(duracion));
                        }
                    }

                    if (cantidad > 0)
                    {
                        fechas.Add(new FechaDisponible
                        {
                            ProfesionalId = profesionalId,
                            EspecialidadId = especialidadId,
                            Fecha = fecha,
                            CantidadHorarios = cantidad
                        });
                    }
                }

                return fechas;
            }

        // Primera fecha, desde "desde" y dentro de los próximos 60 días, con al menos un horario libre.
        public DateTime? BuscarProximaFechaDisponible(
            Guid profesionalId, int especialidadId, DateTime desde, int? turnoExcluido = null)
        {
            IList<int> dias = ObtenerDiasAtencion(profesionalId, especialidadId);
            if (dias.Count == 0)
            {
                return null;
            }

            // Limpieza perezosa una sola vez, antes de recorrer hasta 60 dias.
            _datos.LiberarReservasVencidas();

            DateTime inicio = desde.Date < DateTime.Today ? DateTime.Today : desde.Date;
            for (int i = 0; i < 60; i++)
            {
                DateTime fecha = inicio.AddDays(i);
                int dia = ((int)fecha.DayOfWeek + 6) % 7 + 1;
                if (!dias.Contains(dia))
                {
                    continue;
                }

                if (CalcularDisponibilidad(profesionalId, especialidadId, fecha, turnoExcluido)
                    .Any(franja => franja.Disponible))
                {
                    return fecha;
                }
            }

            return null;
        }

        public IList<TurnoDisponible> ObtenerDisponibilidad(
            Guid profesionalId, int especialidadId, DateTime fecha, int? turnoExcluido = null)
        {
            // Limpieza perezosa: las reservas vencidas no deben aparecer ocupando horarios.
            _datos.LiberarReservasVencidas();
            return CalcularDisponibilidad(profesionalId, especialidadId, fecha, turnoExcluido);
        }

        // Calculo de franjas libres/ocupadas, sin limpieza (la hace quien llama).
        private IList<TurnoDisponible> CalcularDisponibilidad(
            Guid profesionalId, int especialidadId, DateTime fecha, int? turnoExcluido)
        {
            ValidarSeleccion(profesionalId, especialidadId, fecha);
            List<TurnoDisponible> franjas = new List<TurnoDisponible>();
            if (fecha.Date < DateTime.Today)
            {
                return franjas;
            }

            DataTable horarios = _datos.ObtenerHorariosDisponibilidad(profesionalId, especialidadId, fecha);
            DataTable turnos = _datos.ObtenerTurnosDelDia(
                profesionalId, especialidadId, fecha, turnoExcluido);
            foreach (DataRow horario in horarios.Rows)
            {
                TimeSpan horaInicio = (TimeSpan)horario["HoraInicio"];
                TimeSpan horaFin = (TimeSpan)horario["HoraFin"];
                short duracion = Convert.ToInt16(horario["DuracionEstandarMinutos"]);
                DateTime hora = fecha.Date.Add(horaInicio);
                DateTime finHorario = fecha.Date.Add(horaFin);

                while (hora.AddMinutes(duracion) <= finHorario)
                {
                    DateTime fin = hora.AddMinutes(duracion);
                    List<DataRow> ocupantes = new List<DataRow>();
                    foreach (DataRow turno in turnos.Rows)
                    {
                        DateTime inicioTurno = Convert.ToDateTime(turno["FechaHora"]);
                        DateTime finTurno = inicioTurno.AddMinutes(
                            Convert.ToInt32(turno["DuracionEstandarMinutos"]));
                        if (inicioTurno < fin && finTurno > hora)
                        {
                            ocupantes.Add(turno);
                        }
                    }

                    bool disponible = ocupantes.Count == 0 && hora > DateTime.Now;
                    TurnoDisponible franja = new TurnoDisponible
                    {
                        ProfesionalId = profesionalId,
                        EspecialidadId = especialidadId,
                        Especialidad = Convert.ToString(horario["Especialidad"]),
                        FechaHora = hora,
                        Fin = fin,
                        DuracionMinutos = duracion,
                        Disponible = disponible,
                        Estado = ocupantes.Count == 0 ? "Disponible" :
                            ocupantes.Count == 1 ? Convert.ToString(ocupantes[0]["Estado"]) : "Conflicto de turnos"
                    };

                    if (ocupantes.Count == 1)
                    {
                        franja.TurnoId = Convert.ToInt32(ocupantes[0]["TurnoId"]);
                        franja.Paciente = Convert.ToString(ocupantes[0]["Paciente"]);
                    }
                    else if (ocupantes.Count > 1)
                    {
                        franja.Paciente = "Más de un turno ocupa este intervalo";
                    }
                    else if (!disponible)
                    {
                        franja.Estado = "Horario pasado";
                    }

                    franjas.Add(franja);
                    hora = fin;
                }
            }

            return franjas;
        }

        public DataTable ObtenerTurnos(Guid? pacienteId, Guid? profesionalId, DateTime? desde, DateTime? hasta)
        {
            if (desde.HasValue && hasta.HasValue && desde.Value.Date > hasta.Value.Date)
            {
                throw new InvalidOperationException("La fecha inicial no puede ser posterior a la fecha final.");
            }

            // Limpieza perezosa: si una reserva vencio, en "Mis turnos" ya se ve como cancelada.
            _datos.LiberarReservasVencidas();
            return _datos.ObtenerTurnos(pacienteId, profesionalId, desde, hasta);
        }

        public DataTable ObtenerTurnosSinResolver()
        {
            _datos.LiberarReservasVencidas();
            return _datos.ObtenerTurnosSinResolver();
        }

        public TurnoDetalle ObtenerTurno(int turnoId)
        {
            if (turnoId <= 0)
            {
                throw new InvalidOperationException("Selecciona un turno valido.");
            }

            return _datos.ObtenerTurno(turnoId);
        }

        public bool TieneTurnoVigenteEspecialidad(Guid pacienteId, int especialidadId, int? turnoExcluido = null)
        {
            if (pacienteId == Guid.Empty || especialidadId <= 0)
            {
                return false;
            }

            return _datos.TieneTurnoVigenteEspecialidad(pacienteId, especialidadId, turnoExcluido);
        }

        // Devuelve el Id del turno creado y la seña pendiente (0 si el turno no requiere seña).
        // Seña que corresponde al turno (0 si tiene cobertura). La usa la pantalla para avisar,
        // antes de reservar, que hay que cobrarla en el mostrador.
        public decimal CalcularSenaRequerida(SolicitudTurno solicitud)
        {
            if (solicitud == null || solicitud.PacienteId == Guid.Empty
                || solicitud.ProfesionalId == Guid.Empty || solicitud.EspecialidadId <= 0)
            {
                return 0m;
            }

            return _datos.ObtenerSenaRequerida(solicitud);
        }

        public ReservaTurno SolicitarTurno(SolicitudTurno solicitud)
        {
            ValidarSolicitud(solicitud, null);
            if (solicitud != null && !solicitud.EsReservaOnline && !string.IsNullOrEmpty(solicitud.MedioPagoSena)
                && solicitud.MedioPagoSena != "Efectivo" && solicitud.MedioPagoSena != "Tarjeta"
                && solicitud.MedioPagoSena != "Transferencia")
            {
                throw new InvalidOperationException("Seleccioná un medio de pago válido para la seña.");
            }

            ReservaTurno reserva = null;
            EjecutarSeguro(() => reserva = _datos.SolicitarTurno(solicitud));
            return reserva;
        }

        public void ReprogramarTurno(int turnoId, Guid? pacienteAutorizado, SolicitudTurno destino)
        {
            if (turnoId <= 0)
            {
                throw new InvalidOperationException("Selecciona un turno valido para reprogramar.");
            }

            ValidarSolicitud(destino, turnoId);
            EjecutarSeguro(() => _datos.ReprogramarTurno(turnoId, pacienteAutorizado, destino));
        }

        public void CancelarTurno(int turnoId, Guid? pacienteAutorizado)
        {
            if (turnoId <= 0)
            {
                throw new InvalidOperationException("Selecciona un turno valido para cancelar.");
            }

            EjecutarSeguro(() => _datos.CancelarTurno(turnoId, pacienteAutorizado));
        }

        public void CambiarEstado(int turnoId, string estado, bool esAdministrativo, Guid? profesionalAutorizado)
        {
            string[] estados = { "Confirmado", "Atendido", "Ausente", "Cancelado" };
            if (turnoId <= 0 || !estados.Contains(estado, StringComparer.Ordinal))
            {
                throw new InvalidOperationException("Selecciona un estado valido para el turno.");
            }

            EjecutarSeguro(() => _datos.CambiarEstadoTurno(
                turnoId, estado, esAdministrativo, profesionalAutorizado));
        }

        private static void ValidarSeleccion(Guid profesionalId, int especialidadId, DateTime fecha)
        {
            if (profesionalId == Guid.Empty || especialidadId <= 0
                || fecha.Date < new DateTime(1900, 1, 1) || fecha.Date > new DateTime(9998, 12, 31))
            {
                throw new InvalidOperationException("Selecciona un profesional, una especialidad y una fecha validos.");
            }
        }

        private void ValidarSolicitud(SolicitudTurno solicitud, int? turnoExcluido)
        {
            if (solicitud == null || solicitud.PacienteId == Guid.Empty
                || solicitud.ProfesionalId == Guid.Empty || solicitud.EspecialidadId <= 0)
            {
                throw new InvalidOperationException("Selecciona un paciente, profesional y especialidad validos.");
            }

            if (solicitud.FechaHora <= DateTime.Now || solicitud.FechaHora.Second != 0)
            {
                throw new InvalidOperationException("Selecciona un horario futuro valido.");
            }

            solicitud.Motivo = (solicitud.Motivo ?? string.Empty).Trim();
            if (solicitud.Motivo.Length > 500)
            {
                throw new InvalidOperationException("El motivo no puede superar los 500 caracteres.");
            }

            if (_datos.TieneTurnoVigenteEspecialidad(solicitud.PacienteId, solicitud.EspecialidadId, turnoExcluido))
            {
                string mensaje = solicitud.EsReservaOnline
                    ? "Ya tenés un turno vigente para esta especialidad. Recordá que solo podés tener un turno por especialidad a la vez; podés reprogramar o cancelar tu turno actual desde la sección 'Mis turnos'."
                    : "El paciente ya cuenta con un turno vigente para esta especialidad.";
                throw new InvalidOperationException(mensaje);
            }
        }

        private static void EjecutarSeguro(Action accion)
        {
            try
            {
                accion();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (SqlException ex)
            {
                // Caja cerrada o usuario no habilitado para cobrar (cobro de seña en el mostrador).
                if (ex.Number >= 51110 && ex.Number <= 51112)
                {
                    throw new InvalidOperationException(ex.Message);
                }

                if (ex.Number >= 51020 && ex.Number <= 51025)
                {
                    throw new InvalidOperationException(ex.Message);
                }

                if (ex.Number == 2601 || ex.Number == 2627)
                {
                    throw new InvalidOperationException("El paciente ya tiene un turno vigente para esta especialidad o el horario acaba de ocuparse.");
                }

                if (ex.Number == 547)
                {
                    throw new InvalidOperationException("La solicitud ya no coincide con las relaciones activas del paciente, profesional y especialidad.");
                }

                if (ex.Number == 1205)
                {
                    throw new InvalidOperationException("El turno tuvo un conflicto con otra operación. Actualiza la disponibilidad e intenta nuevamente.");
                }

                throw new InvalidOperationException("No se pudo completar la operación en MediStackDB (error SQL " + ex.Number + ").");
            }
        }
    }
}
