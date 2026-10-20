using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using MediStack.Datos;
using MediStack.Dominio;

namespace MediStack.Negocio
{
    public sealed class AgendaNegocio
    {
        private readonly GestionClinicaDatos _datos = new GestionClinicaDatos();

        public DataTable ObtenerProfesionales()
        {
            return _datos.ObtenerProfesionalesAgenda();
        }

        public DataTable ObtenerEspecialidades(Guid profesionalId)
        {
            if (profesionalId == Guid.Empty)
            {
                return new DataTable();
            }

            return _datos.ObtenerEspecialidadesProfesionalAgenda(profesionalId);
        }

        public DataTable ObtenerHorarios(Guid? profesionalId)
        {
            return _datos.ObtenerHorariosAtencion(profesionalId);
        }

        public DataTable ObtenerTurnos(Guid profesionalId, DateTime fecha)
        {
            ValidarSeleccionAgenda(profesionalId, fecha);
            return _datos.ObtenerTurnosAgenda(profesionalId, fecha);
        }

        public IList<DiaAgendaSemanal> ObtenerAgendaSemanal(Guid profesionalId, DateTime fechaReferencia)
        {
            ValidarSeleccionAgenda(profesionalId, fechaReferencia);
            DateTime inicio = ObtenerInicioSemana(fechaReferencia);
            DateTime fin = inicio.AddDays(5);
            DataTable turnosTabla = _datos.ObtenerTurnosAgendaSemana(profesionalId, inicio, fin);
            DataTable horarios = _datos.ObtenerHorariosAtencion(profesionalId);
            IList<TurnoAgenda> turnos = CargarTurnos(turnosTabla);
            List<DiaAgendaSemanal> dias = new List<DiaAgendaSemanal>();
            CultureInfo es = CultureInfo.GetCultureInfo("es-AR");

            for (int i = 0; i < 5; i++)
            {
                DateTime fecha = inicio.AddDays(i);
                int diaSemana = i + 1;
                List<TurnoAgenda> delDia = turnos
                    .Where(t => t.FechaHora.Date == fecha.Date)
                    .OrderBy(t => t.FechaHora)
                    .ThenBy(t => t.TurnoId)
                    .ToList();
                bool habilitado = horarios.Rows.Cast<DataRow>()
                    .Any(h => Convert.ToBoolean(h["Activo"]) && Convert.ToInt32(h["DiaSemana"]) == diaSemana);
                bool fueraDeHorario = delDia.Any(t => !EstaDentroDeHorario(t, horarios, diaSemana));

                dias.Add(new DiaAgendaSemanal
                {
                    Fecha = fecha,
                    DiaSemana = diaSemana,
                    NombreDia = fecha.ToString("dddd", es),
                    Habilitado = habilitado || delDia.Count > 0,
                    TieneTurnosFueraDeHorario = fueraDeHorario,
                    Turnos = delDia
                });
            }

            return dias;
        }

        public bool TieneTurnosFinDeSemana(Guid profesionalId, DateTime fechaReferencia)
        {
            ValidarSeleccionAgenda(profesionalId, fechaReferencia);
            DateTime inicio = ObtenerInicioSemana(fechaReferencia);
            DataTable turnos = _datos.ObtenerTurnosAgendaSemana(profesionalId, inicio.AddDays(5), inicio.AddDays(7));
            return turnos.Rows.Count > 0;
        }

        public IList<TurnoAgenda> ObtenerTurnosJornada(Guid profesionalId, DateTime fecha)
        {
            ValidarSeleccionAgenda(profesionalId, fecha);
            return CargarTurnos(_datos.ObtenerTurnosAgenda(profesionalId, fecha));
        }

        public IList<FranjaAgenda> ObtenerDisponibilidad(Guid profesionalId, DateTime fecha)
        {
            ValidarSeleccionAgenda(profesionalId, fecha);
            int diaSemana = ((int)fecha.DayOfWeek + 6) % 7 + 1;
            DataTable horarios = _datos.ObtenerHorariosAtencionDia(profesionalId, diaSemana);
            IList<TurnoAgenda> turnos = CargarTurnos(_datos.ObtenerTurnosAgenda(profesionalId, fecha));
            List<FranjaAgenda> disponibilidad = new List<FranjaAgenda>();

            foreach (DataRow fila in horarios.Rows)
            {
                TimeSpan inicio = (TimeSpan)fila["HoraInicio"];
                TimeSpan fin = (TimeSpan)fila["HoraFin"];
                int duracion = Convert.ToInt32(fila["DuracionEstandarMinutos"]);
                DateTime hora = fecha.Date.Add(inicio);
                DateTime finHorario = fecha.Date.Add(fin);

                while (hora.AddMinutes(duracion) <= finHorario)
                {
                    DateTime finFranja = hora.AddMinutes(duracion);
                    List<TurnoAgenda> ocupantes = turnos
                        .Where(turno => !string.Equals(turno.Estado, "Cancelado", StringComparison.OrdinalIgnoreCase)
                            && turno.FechaHora < finFranja
                            && turno.FechaHora.AddMinutes(turno.DuracionMinutos) > hora)
                        .ToList();

                    FranjaAgenda franja = new FranjaAgenda
                    {
                        FechaHora = hora,
                        Fin = finFranja,
                        Especialidad = Convert.ToString(fila["Especialidad"]),
                        Disponible = ocupantes.Count == 0,
                        Estado = ocupantes.Count == 0 ? "Disponible" :
                            ocupantes.Count == 1 ? ocupantes[0].Estado : "Conflicto de turnos"
                    };

                    if (ocupantes.Count == 1)
                    {
                        franja.TurnoId = ocupantes[0].TurnoId;
                        franja.PacienteId = ocupantes[0].PacienteId;
                        franja.Paciente = ocupantes[0].Paciente;
                        franja.Motivo = ocupantes[0].Motivo;
                    }
                    else if (ocupantes.Count > 1)
                    {
                        franja.Motivo = "Hay mas de un turno que ocupa esta franja. Revisa los turnos del dia.";
                    }

                    disponibilidad.Add(franja);
                    hora = finFranja;
                }
            }

            return disponibilidad;
        }

        public void GuardarHorario(HorarioAtencion horario)
        {
            if (horario == null || horario.ProfesionalId == Guid.Empty
                || horario.EspecialidadId <= 0 || horario.HorarioAtencionId < 0)
            {
                throw new InvalidOperationException("Selecciona un profesional y una especialidad validos.");
            }

            if (horario.DiaSemana < 1 || horario.DiaSemana > 7)
            {
                throw new InvalidOperationException("Selecciona un dia de la semana valido.");
            }

            if (horario.HoraInicio < TimeSpan.Zero || horario.HoraFin > TimeSpan.FromDays(1)
                || horario.HoraInicio >= horario.HoraFin)
            {
                throw new InvalidOperationException("La hora de fin debe ser posterior a la hora de inicio.");
            }

            try
            {
                _datos.GuardarHorarioAtencion(horario);
            }
            catch (SqlException ex)
            {
                LanzarErrorHorario(ex);
            }
        }

        public void CambiarEstadoHorario(int horarioAtencionId, bool activo)
        {
            if (horarioAtencionId <= 0)
            {
                throw new InvalidOperationException("Selecciona un horario valido.");
            }

            try
            {
                _datos.CambiarEstadoHorarioAtencion(horarioAtencionId, activo);
            }
            catch (SqlException ex)
            {
                LanzarErrorHorario(ex);
            }
        }

        private static IList<TurnoAgenda> CargarTurnos(DataTable datos)
        {
            List<TurnoAgenda> items = new List<TurnoAgenda>(datos.Rows.Count);
            foreach (DataRow fila in datos.Rows)
            {
                items.Add(new TurnoAgenda
                {
                    TurnoId = Convert.ToInt32(fila["TurnoId"]),
                    PacienteId = (Guid)fila["PacienteId"],
                    Paciente = Convert.ToString(fila["Paciente"]),
                    FechaHora = Convert.ToDateTime(fila["FechaHora"]),
                    Estado = Convert.ToString(fila["Estado"]),
                    Motivo = fila.IsNull("Motivo") ? string.Empty : Convert.ToString(fila["Motivo"]),
                    Especialidad = Convert.ToString(fila["Especialidad"]),
                    DuracionMinutos = Convert.ToInt16(fila["DuracionEstandarMinutos"]),
                    EstadoSena = Convert.ToString(fila["EstadoSena"]),
                    MontoSena = Convert.ToDecimal(fila["MontoSena"]),
                    TurnoReprogramadoDesdeId = ValorEnteroNullable(fila, "TurnoReprogramadoDesdeId"),
                    TurnoReprogramadoHaciaId = ValorEnteroNullable(fila, "TurnoReprogramadoHaciaId"),
                    FechaReprogramacion = ValorFechaNullable(fila, "FechaReprogramacion")
                });
            }

            return items;
        }

        private static DateTime ObtenerInicioSemana(DateTime fecha)
        {
            int desplazamiento = ((int)fecha.DayOfWeek + 6) % 7;
            return fecha.Date.AddDays(-desplazamiento);
        }

        private static bool EstaDentroDeHorario(TurnoAgenda turno, DataTable horarios, int diaSemana)
        {
            return horarios.Rows.Cast<DataRow>().Any(h =>
                Convert.ToBoolean(h["Activo"])
                && Convert.ToInt32(h["DiaSemana"]) == diaSemana
                && turno.FechaHora.TimeOfDay >= (TimeSpan)h["HoraInicio"]
                && turno.FechaHora.TimeOfDay.Add(TimeSpan.FromMinutes(turno.DuracionMinutos)) <= (TimeSpan)h["HoraFin"]);
        }

        private static int? ValorEnteroNullable(DataRow fila, string columna)
        {
            return !fila.Table.Columns.Contains(columna) || fila.IsNull(columna)
                ? (int?)null : Convert.ToInt32(fila[columna]);
        }

        private static DateTime? ValorFechaNullable(DataRow fila, string columna)
        {
            return !fila.Table.Columns.Contains(columna) || fila.IsNull(columna)
                ? (DateTime?)null : Convert.ToDateTime(fila[columna]);
        }

        private static void ValidarSeleccionAgenda(Guid profesionalId, DateTime fecha)
        {
            if (profesionalId == Guid.Empty || fecha.Date < new DateTime(1900, 1, 1)
                || fecha.Date > new DateTime(9998, 12, 31))
            {
                throw new InvalidOperationException("Selecciona un profesional y una fecha validos.");
            }
        }

        private static void LanzarErrorHorario(SqlException ex)
        {
            if (ex.Number == 51001 || ex.Number == 51002 || ex.Number == 51003 || ex.Number == 51004)
            {
                throw new InvalidOperationException(ex.Message);
            }

            if (ex.Number == 2601 || ex.Number == 2627)
            {
                throw new InvalidOperationException("Ya existe un horario con ese profesional, especialidad, dia y hora de inicio.");
            }

            if (ex.Number == 547)
            {
                throw new InvalidOperationException("El horario requiere una relacion activa entre el profesional y la especialidad.");
            }

            throw new InvalidOperationException("No se pudo guardar el horario en MediStackDB (error SQL " + ex.Number + ").");
        }
    }
}
