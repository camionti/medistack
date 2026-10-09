using System;

namespace MediStack.Dominio
{
    public sealed class InteraccionIA
    {
        public Guid UsuarioId { get; set; }
        public string Canal { get; set; }
        public string Consulta { get; set; }
        public string FuncionEjecutada { get; set; }
        public string Respuesta { get; set; }
        public int GastoTokens { get; set; }
    }

    public sealed class RespuestaAgenteIA
    {
        public string Texto { get; set; }
        public string FuncionEjecutada { get; set; }
        public bool RequiereConfirmacion { get; set; }
    }
}
