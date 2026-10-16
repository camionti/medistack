using System;
using System.Collections.Generic;

namespace MediStack.Dominio
{
    public sealed class MensajeIA
    {
        public string Rol { get; set; }
        public string Contenido { get; set; }

        // Turno del asistente que pidio ejecutar una funcion (rol "assistant").
        public LlamadaHerramientaIA Llamada { get; set; }

        // Resultado de una funcion ejecutada por el sistema, devuelto al modelo (rol "tool").
        public string NombreResultado { get; set; }
        public string ResultadoJson { get; set; }
    }

    public sealed class SolicitudIA
    {
        public Guid UsuarioId { get; set; }
        public string Instrucciones { get; set; }
        public IList<MensajeIA> Mensajes { get; set; }
        public IList<DefinicionHerramientaIA> Herramientas { get; set; }
    }

    public sealed class RespuestaIA
    {
        public string Contenido { get; set; }
        public LlamadaHerramientaIA Herramienta { get; set; }
        public int GastoTokens { get; set; }
    }

    public sealed class DefinicionHerramientaIA
    {
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string EsquemaEntradaJson { get; set; }
    }

    public sealed class LlamadaHerramientaIA
    {
        public string Nombre { get; set; }
        public string ArgumentosJson { get; set; }
    }
}
