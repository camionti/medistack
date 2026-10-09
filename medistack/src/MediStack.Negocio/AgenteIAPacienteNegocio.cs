using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using MediStack.Datos;
using MediStack.Dominio;

namespace MediStack.Negocio
{
    public sealed class AgenteIAPacienteNegocio
    {
        private readonly TurnosNegocio _turnos = new TurnosNegocio();
        private readonly InteraccionesIADatos _interacciones = new InteraccionesIADatos();
        private readonly IProveedorIA _proveedor;

        public AgenteIAPacienteNegocio()
            : this(ProveedorIAFactory.Crear())
        {
        }

        public AgenteIAPacienteNegocio(IProveedorIA proveedor)
        {
            _proveedor = proveedor ?? throw new ArgumentNullException("proveedor");
        }

        public RespuestaAgenteIA ProcesarMensaje(Guid pacienteId, string mensaje, string canal)
        {
            if (pacienteId == Guid.Empty)
            {
                throw new InvalidOperationException("No se pudo identificar al paciente autenticado.");
            }

            string consulta = (mensaje ?? string.Empty).Trim();
            if (consulta.Length == 0 || consulta.Length > 2000)
            {
                throw new InvalidOperationException("Escribí una consulta de entre 1 y 2000 caracteres.");
            }

            RespuestaAgenteIA respuesta = CrearRespuesta(pacienteId, consulta);
            _interacciones.Registrar(new InteraccionIA
            {
                UsuarioId = pacienteId,
                Canal = string.Equals(canal, "Voz", StringComparison.OrdinalIgnoreCase) ? "Voz" : "Texto",
                Consulta = consulta,
                FuncionEjecutada = respuesta.FuncionEjecutada,
                Respuesta = respuesta.Texto
            });
            return respuesta;
        }

        private RespuestaAgenteIA CrearRespuesta(Guid pacienteId, string consulta)
        {
            RespuestaIA respuestaProveedor = _proveedor.Completar(new SolicitudIA
            {
                UsuarioId = pacienteId,
                Mensajes = new List<MensajeIA>
                {
                    new MensajeIA { Rol = "user", Contenido = consulta }
                },
                Herramientas = new List<DefinicionHerramientaIA>()
            });

            if (respuestaProveedor.Herramienta != null
                && string.Equals(respuestaProveedor.Herramienta.Nombre, "ConsultarMisTurnos", StringComparison.Ordinal))
            {
                DataTable propios = _turnos.ObtenerTurnos(pacienteId, null, DateTime.Today, null);
                return new RespuestaAgenteIA
                {
                    FuncionEjecutada = "ConsultarMisTurnos",
                    Texto = FormatearTurnos(propios)
                };
            }

            if (respuestaProveedor.Herramienta != null
                && string.Equals(respuestaProveedor.Herramienta.Nombre, "ConsultarDisponibilidad", StringComparison.Ordinal))
            {
                DataTable especialidades = _turnos.ObtenerEspecialidadesActivas();
                StringBuilder nombres = new StringBuilder();
                foreach (DataRow fila in especialidades.Rows)
                {
                    if (nombres.Length > 0)
                    {
                        nombres.Append(", ");
                    }

                    nombres.Append(Convert.ToString(fila["Nombre"], CultureInfo.CurrentCulture));
                }
                return new RespuestaAgenteIA
                {
                    FuncionEjecutada = "ConsultarDisponibilidad",
                    Texto = "Puedo ayudarte a buscar un turno. Indicame la especialidad y la fecha que preferís. " +
                        "Especialidades disponibles: " + nombres + "."
                };
            }

            return new RespuestaAgenteIA
            {
                Texto = respuestaProveedor.Contenido ?? "No pude interpretar la consulta."
            };
        }

        private static string FormatearTurnos(DataTable turnos)
        {
            if (turnos == null || turnos.Rows.Count == 0)
            {
                return "No tenés turnos desde hoy en adelante.";
            }

            StringBuilder resultado = new StringBuilder("Tus próximos turnos son:");
            foreach (DataRow fila in turnos.Rows)
            {
                resultado.Append(" ");
                resultado.Append(Convert.ToDateTime(fila["FechaHora"]).ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture));
                resultado.Append(" - ");
                resultado.Append(Convert.ToString(fila["Especialidad"], CultureInfo.CurrentCulture));
                resultado.Append(" (");
                resultado.Append(Convert.ToString(fila["Estado"], CultureInfo.CurrentCulture));
                resultado.Append(").");
            }

            return resultado.ToString();
        }
    }
}
