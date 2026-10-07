using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using MediStack.Datos;
using MediStack.Dominio;

namespace MediStack.Negocio
{
    public sealed class TurnosNegocio
    {
        private readonly TurnosDatos _datos = new TurnosDatos();

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

        // Primera fecha, desde "desde" y dentro de los próximos 60 días, con al menos un horario libre.
        public DateTime? BuscarProximaFechaDisponible(
            Guid profesionalId, int especialidadId, DateTime desde, int? turnoExcluido = null)
        {
            IList<int> dias = ObtenerDiasAtencion(profesionalId, especialidadId);
            if (dias.Count == 0)
            {
                return null;
            }

            DateTime inicio = desde.Date < DateTime.Today ? DateTime.Today : desde.Date;
            for (int i = 0; i < 60; i++)
            {
                DateTime fecha = inicio.AddDays(i);
                int dia = ((int)fecha.DayOfWeek + 6) % 7 + 1;
                if (!dias.Contains(dia))
                {
                    continue;
                }

                if (ObtenerDisponibilidad(profesionalId, especialidadId, fecha, turnoExcluido)
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

            return _datos.ObtenerTurnos(pacienteId, profesionalId, desde, hasta);
        }

        public TurnoDetalle ObtenerTurno(int turnoId)
        {
            if (turnoId <= 0)
            {
                throw new InvalidOperationException("Selecciona un turno valido.");
            }

            return _datos.ObtenerTurno(turnoId);
        }

        public void SolicitarTurno(SolicitudTurno solicitud)
        {
            ValidarSolicitud(solicitud);
            EjecutarSeguro(() => _datos.SolicitarTurno(solicitud));
        }

        public void ReprogramarTurno(int turnoId, Guid? pacienteAutorizado, SolicitudTurno destino)
        {
            if (turnoId <= 0)
            {
                throw new InvalidOperationException("Selecciona un turno valido para reprogramar.");
            }

            ValidarSolicitud(destino);
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

        private static void ValidarSolicitud(SolicitudTurno solicitud)
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
