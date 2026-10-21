using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using MediStack.Dominio;

namespace MediStack.Datos
{
    public class UsuarioDatos
    {
        private readonly string _cadenaConexion;

        public UsuarioDatos()
        {
            ConnectionStringSettings configuracion = ConfigurationManager.ConnectionStrings["MediStackDB"];
            if (configuracion == null || string.IsNullOrWhiteSpace(configuracion.ConnectionString))
            {
                throw new ConfigurationErrorsException("No se encontro la cadena de conexion MediStackDB.");
            }

            _cadenaConexion = configuracion.ConnectionString;
        }

        public Usuario ObtenerPorNombreUsuario(string nombreUsuario)
        {
            const string consulta = @"
                SELECT u.UsuarioId, u.NombreUsuario, u.PasswordHash, u.Nombre, u.Apellido,
                       u.Activo, u.IntentosFallidos, u.BloqueadoHasta, r.Codigo AS RolCodigo, r.Nombre AS RolNombre
                FROM dbo.Usuarios AS u
                INNER JOIN dbo.Roles AS r ON r.RolId = u.RolId
                WHERE u.NombreUsuario = @NombreUsuario;";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            using (SqlCommand comando = new SqlCommand(consulta, conexion))
            {
                comando.Parameters.Add("@NombreUsuario", SqlDbType.NVarChar, 50).Value = nombreUsuario;
                conexion.Open();

                using (SqlDataReader lector = comando.ExecuteReader(CommandBehavior.SingleRow))
                {
                    if (!lector.Read())
                    {
                        return null;
                    }

                    return new Usuario
                    {
                        UsuarioId = lector.GetGuid(0),
                        NombreUsuario = lector.GetString(1),
                        PasswordHash = lector.GetString(2),
                        Nombre = lector.GetString(3),
                        Apellido = lector.GetString(4),
                        Activo = lector.GetBoolean(5),
                        IntentosFallidos = lector.GetInt32(6),
                        BloqueadoHasta = lector.IsDBNull(7) ? (DateTime?)null : lector.GetDateTime(7),
                        RolCodigo = lector.GetString(8),
                        RolNombre = lector.GetString(9)
                    };
                }
            }
        }

        public void RegistrarIntentoFallido(Guid usuarioId)
        {
            const string consulta = @"
                UPDATE dbo.Usuarios
                SET IntentosFallidos =
                        CASE WHEN BloqueadoHasta IS NOT NULL AND BloqueadoHasta <= SYSDATETIME()
                             THEN 1
                             WHEN IntentosFallidos >= 4 THEN 5
                             ELSE IntentosFallidos + 1
                        END,
                    BloqueadoHasta =
                        CASE WHEN (BloqueadoHasta IS NOT NULL AND BloqueadoHasta <= SYSDATETIME())
                                   OR IntentosFallidos >= 4
                             THEN DATEADD(MINUTE, 15, SYSDATETIME())
                             ELSE BloqueadoHasta
                        END
                WHERE UsuarioId = @UsuarioId
                  AND Activo = 1
                  AND (BloqueadoHasta IS NULL OR BloqueadoHasta <= SYSDATETIME());";

            EjecutarActualizacion(consulta, usuarioId);
        }

        public void RegistrarAccesoCorrecto(Guid usuarioId)
        {
            const string consulta = @"
                UPDATE dbo.Usuarios
                SET IntentosFallidos = 0,
                    BloqueadoHasta = NULL,
                    UltimoAcceso = SYSDATETIME()
                WHERE UsuarioId = @UsuarioId AND Activo = 1;";

            EjecutarActualizacion(consulta, usuarioId);
        }

        public DataTable ListarBloqueados()
        {
            const string consulta = @"
        SELECT u.UsuarioId, u.NombreUsuario,
               u.Nombre + N' ' + u.Apellido AS NombreCompleto,
               r.Nombre AS Rol, u.IntentosFallidos, u.BloqueadoHasta,
               DATEDIFF(MINUTE, SYSDATETIME(), u.BloqueadoHasta) AS MinutosRestantes
        FROM dbo.Usuarios AS u
        INNER JOIN dbo.Roles AS r ON r.RolId = u.RolId
        WHERE u.Activo = 1
          AND u.BloqueadoHasta IS NOT NULL
          AND u.BloqueadoHasta > SYSDATETIME()
        ORDER BY u.BloqueadoHasta DESC;"; 

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            using (SqlCommand comando = new SqlCommand(consulta, conexion))
            {
                conexion.Open();
                using (SqlDataReader lector = comando.ExecuteReader())
                {
                    DataTable tabla = new DataTable(); 
                    tabla.Load(lector); 
                    return tabla;
                }
            }
        }

        public int Desbloquear(Guid usuarioId)
        {
            const string consulta = @"
        UPDATE dbo.Usuarios
        SET IntentosFallidos = 0, BloqueadoHasta = NULL
        WHERE UsuarioId = @UsuarioId
          AND Activo = 1
          AND BloqueadoHasta IS NOT NULL
          AND BloqueadoHasta > SYSDATETIME();"; 

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            using (SqlCommand comando = new SqlCommand(consulta, conexion))
            {
                comando.Parameters.Add("@UsuarioId", SqlDbType.UniqueIdentifier).Value = usuarioId;
                conexion.Open();
                return comando.ExecuteNonQuery(); 
            }
        }

        public Usuario ObtenerPorId(Guid usuarioId)
        {
            const string consulta = @"
        SELECT u.UsuarioId, u.NombreUsuario, u.PasswordHash, u.Nombre, u.Apellido, u.Email,
               u.Activo, u.IntentosFallidos, u.BloqueadoHasta,
               r.Codigo AS RolCodigo, r.Nombre AS RolNombre
        FROM dbo.Usuarios AS u
        INNER JOIN dbo.Roles AS r ON r.RolId = u.RolId
        WHERE u.UsuarioId = @UsuarioId;";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            using (SqlCommand comando = new SqlCommand(consulta, conexion))
            {
                comando.Parameters.Add("@UsuarioId", SqlDbType.UniqueIdentifier).Value = usuarioId;
                conexion.Open();
                using (SqlDataReader lector = comando.ExecuteReader(CommandBehavior.SingleRow))
                {
                    if (!lector.Read()) { return null; }
                    return new Usuario
                    {
                        UsuarioId = lector.GetGuid(0),
                        NombreUsuario = lector.GetString(1),
                        PasswordHash = lector.GetString(2),
                        Nombre = lector.GetString(3),
                        Apellido = lector.GetString(4),
                        Email = lector.GetString(5),
                        Activo = lector.GetBoolean(6),
                        IntentosFallidos = lector.GetInt32(7),
                        BloqueadoHasta = lector.IsDBNull(8) ? (DateTime?)null : lector.GetDateTime(8),
                        RolCodigo = lector.GetString(9),
                        RolNombre = lector.GetString(10)
                    };
                }
            }
        }
        private void EjecutarActualizacion(string consulta, Guid usuarioId)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            using (SqlCommand comando = new SqlCommand(consulta, conexion))
            {
                comando.Parameters.Add("@UsuarioId", SqlDbType.UniqueIdentifier).Value = usuarioId;
                conexion.Open();
                comando.ExecuteNonQuery();
            }
        }
    }
}
