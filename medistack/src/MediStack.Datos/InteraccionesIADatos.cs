using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using MediStack.Dominio;

namespace MediStack.Datos
{
    public sealed class InteraccionesIADatos
    {
        private readonly string _cadenaConexion;

        public InteraccionesIADatos()
        {
            ConnectionStringSettings configuracion = ConfigurationManager.ConnectionStrings["MediStackDB"];
            if (configuracion == null || string.IsNullOrWhiteSpace(configuracion.ConnectionString))
            {
                throw new ConfigurationErrorsException("No se encontro la cadena de conexion MediStackDB.");
            }

            _cadenaConexion = configuracion.ConnectionString;
        }

        public void Registrar(InteraccionIA interaccion)
        {
            if (interaccion == null || interaccion.UsuarioId == Guid.Empty)
            {
                throw new ArgumentException("La interaccion de IA no es valida.", "interaccion");
            }

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            using (SqlCommand comando = new SqlCommand(@"
                INSERT INTO dbo.InteraccionesIA
                    (UsuarioId, Canal, Consulta, FuncionEjecutada, Respuesta, GastoTokens)
                VALUES
                    (@UsuarioId, @Canal, @Consulta, @FuncionEjecutada, @Respuesta, @GastoTokens);", conexion))
            {
                comando.Parameters.Add("@UsuarioId", SqlDbType.UniqueIdentifier).Value = interaccion.UsuarioId;
                comando.Parameters.Add("@Canal", SqlDbType.NVarChar, 10).Value = (interaccion.Canal ?? "Texto").Trim();
                comando.Parameters.Add("@Consulta", SqlDbType.NVarChar, -1).Value = (interaccion.Consulta ?? string.Empty).Trim();
                comando.Parameters.Add("@FuncionEjecutada", SqlDbType.NVarChar, 100).Value =
                    string.IsNullOrWhiteSpace(interaccion.FuncionEjecutada)
                        ? (object)DBNull.Value
                        : interaccion.FuncionEjecutada.Trim();
                comando.Parameters.Add("@Respuesta", SqlDbType.NVarChar, -1).Value =
                    string.IsNullOrWhiteSpace(interaccion.Respuesta)
                        ? (object)DBNull.Value
                        : interaccion.Respuesta.Trim();
                comando.Parameters.Add("@GastoTokens", SqlDbType.Int).Value = Math.Max(0, interaccion.GastoTokens);
                conexion.Open();
                comando.ExecuteNonQuery();
            }
        }
    }
}
