using System;
using System.Collections.Generic;
using System.Data;
using MediStack.Datos;
using MediStack.Dominio;

namespace MediStack.Negocio
{
    public sealed class ConsultorioNegocio
    {
        private readonly TurnosDatos _turnosDatos = new TurnosDatos();
        private readonly GestionClinicaDatos _gestionClinicaDatos = new GestionClinicaDatos();

        public IList<TurnoConsultorioItem> ObtenerTurnosConsultorio(
            Guid usuarioActualId, string rolCodigo, Guid profesionalId, DateTime fecha)
        {
            ValidarAccesoProfesional(usuarioActualId, rolCodigo, profesionalId);

            // Liberación perezosa de turnos vencidos para mantener la agenda sincronizada
            try
            {
                _turnosDatos.LiberarReservasVencidas();
            }
            catch
            {
                // La consulta de consultorio no debe interrumpirse si falla la limpieza de reservas
            }

            return _turnosDatos.ObtenerTurnosConsultorio(profesionalId, fecha);
        }

        public void GuardarAtencionClinica(Guid usuarioActualId, string rolCodigo, AtencionClinicaEntrada entrada)
        {
            if (entrada == null)
            {
                throw new ArgumentNullException("entrada");
            }

            if (entrada.TurnoId <= 0)
            {
                throw new ArgumentException("El identificador del turno no es válido.", "entrada");
            }

            if (string.IsNullOrWhiteSpace(entrada.Diagnostico))
            {
                throw new InvalidOperationException("El diagnóstico es obligatorio para registrar la atención médica.");
            }

            TurnoDetalle turno = _turnosDatos.ObtenerTurno(entrada.TurnoId);
            if (turno == null)
            {
                throw new InvalidOperationException("El turno no existe.");
            }

            ValidarAccesoProfesional(usuarioActualId, rolCodigo, turno.ProfesionalId);

            if (turno.FechaHora.Date != DateTime.Today)
            {
                throw new InvalidOperationException("Solo se puede registrar la atención médica en turnos correspondientes a la jornada del día de hoy.");
            }

            if (turno.Estado == "Cancelado")
            {
                throw new InvalidOperationException("No se puede registrar atención clínica sobre un turno cancelado.");
            }

            _turnosDatos.GuardarAtencionClinica(
                entrada.TurnoId,
                entrada.MotivoConsulta,
                entrada.Diagnostico,
                entrada.Observaciones);
        }

        public ResumenClinicoRapido ObtenerResumenClinicoRapido(
            Guid usuarioActualId, string rolCodigo, Guid pacienteId, Guid profesionalId)
        {
            if (pacienteId == Guid.Empty)
            {
                throw new ArgumentException("El identificador del paciente no es válido.", "pacienteId");
            }

            ValidarAccesoProfesional(usuarioActualId, rolCodigo, profesionalId);

            ResumenClinicoRapido resumen = _gestionClinicaDatos.ObtenerResumenClinicoRapido(pacienteId);
            if (resumen == null)
            {
                throw new InvalidOperationException("No se encontraron datos del paciente.");
            }

            return resumen;
        }

        public DataTable ObtenerProfesionales()
        {
            return _turnosDatos.ObtenerProfesionalesActivos();
        }

        private static void ValidarAccesoProfesional(Guid usuarioActualId, string rolCodigo, Guid profesionalId)
        {
            bool esAdministrativo = string.Equals(rolCodigo, "ADMINISTRATIVO", StringComparison.OrdinalIgnoreCase);
            bool esProfesional = string.Equals(rolCodigo, "PROFESIONAL", StringComparison.OrdinalIgnoreCase);

            if (!esAdministrativo && !esProfesional)
            {
                throw new InvalidOperationException("No tienes permisos para acceder al consultorio médico.");
            }

            if (esProfesional && usuarioActualId != profesionalId)
            {
                throw new InvalidOperationException("No tienes autorización para acceder a los datos de otro profesional.");
            }
        }
    }
}
