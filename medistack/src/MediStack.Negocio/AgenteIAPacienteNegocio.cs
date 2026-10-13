using System;
using MediStack.Datos;
using MediStack.Dominio;

namespace MediStack.Negocio
{
    public sealed class AgenteIAPacienteNegocio
    {
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
                Mensajes = new System.Collections.Generic.List<MensajeIA>
                {
                    new MensajeIA { Rol = "user", Contenido = consulta }
                },
                Herramientas = new System.Collections.Generic.List<DefinicionHerramientaIA>()
            });

            return new RespuestaAgenteIA
            {
                Texto = respuestaProveedor.Contenido,
                FuncionEjecutada = respuestaProveedor.Herramienta == null
                    ? null
                    : respuestaProveedor.Herramienta.Nombre,
                RequiereConfirmacion = respuestaProveedor.Herramienta != null
            };
        }
    }
}
