using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace MediStack.Datos
{
    // Reportes administrativos (Requerimiento 10). Solo lectura: cada consulta agrega datos que ya existen.
    // Los reportes de turnos filtran por la fecha del turno; los de ingresos, por la fecha del cobro.
    public sealed class ReportesDatos
    {
        private readonly string _cadenaConexion;

        public ReportesDatos()
        {
            ConnectionStringSettings configuracion = ConfigurationManager.ConnectionStrings["MediStackDB"];
            if (configuracion == null || string.IsNullOrWhiteSpace(configuracion.ConnectionString))
            {
                throw new ConfigurationErrorsException("No se encontro la cadena de conexion MediStackDB.");
            }

            _cadenaConexion = configuracion.ConnectionString;
        }

        public DataTable TurnosPorEstado(DateTime desde, DateTime hasta)
        {
            return Consultar(@"
                SELECT t.Estado, COUNT(*) AS Cantidad
                FROM dbo.Turnos t
                WHERE t.FechaHora >= @Desde AND t.FechaHora < DATEADD(day, 1, @Hasta)
                GROUP BY t.Estado
                ORDER BY COUNT(*) DESC, t.Estado;", desde, hasta);
        }

        public DataTable TurnosPorProfesional(DateTime desde, DateTime hasta)
        {
            return Consultar(@"
                SELECT u.Apellido + N', ' + u.Nombre AS Profesional,
                       COUNT(*) AS Total,
                       SUM(CASE WHEN t.Estado = N'Atendido' THEN 1 ELSE 0 END) AS Atendidos,
                       SUM(CASE WHEN t.Estado = N'Ausente' THEN 1 ELSE 0 END) AS Ausentes,
                       SUM(CASE WHEN t.Estado = N'Cancelado' THEN 1 ELSE 0 END) AS Cancelados,
                       SUM(CASE WHEN t.Estado IN (N'Solicitado', N'Confirmado') THEN 1 ELSE 0 END) AS Pendientes,
                       CAST(CASE WHEN SUM(CASE WHEN t.Estado IN (N'Atendido', N'Ausente') THEN 1 ELSE 0 END) = 0
                                 THEN 0
                                 ELSE 100.0 * SUM(CASE WHEN t.Estado = N'Ausente' THEN 1 ELSE 0 END)
                                      / SUM(CASE WHEN t.Estado IN (N'Atendido', N'Ausente') THEN 1 ELSE 0 END)
                            END AS DECIMAL(5,1)) AS PorcentajeAusentismo
                FROM dbo.Turnos t
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = t.ProfesionalId
                WHERE t.FechaHora >= @Desde AND t.FechaHora < DATEADD(day, 1, @Hasta)
                GROUP BY u.Apellido, u.Nombre
                ORDER BY COUNT(*) DESC, u.Apellido, u.Nombre;", desde, hasta);
        }

        public DataTable TurnosPorEspecialidad(DateTime desde, DateTime hasta)
        {
            return Consultar(@"
                SELECT e.Nombre AS Especialidad,
                       COUNT(*) AS Total,
                       SUM(CASE WHEN t.Estado = N'Atendido' THEN 1 ELSE 0 END) AS Atendidos,
                       SUM(CASE WHEN t.Estado = N'Ausente' THEN 1 ELSE 0 END) AS Ausentes,
                       SUM(CASE WHEN t.Estado = N'Cancelado' THEN 1 ELSE 0 END) AS Cancelados
                FROM dbo.Turnos t
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = t.EspecialidadId
                WHERE t.FechaHora >= @Desde AND t.FechaHora < DATEADD(day, 1, @Hasta)
                GROUP BY e.Nombre
                ORDER BY COUNT(*) DESC, e.Nombre;", desde, hasta);
        }

        public DataTable IngresosPorMedioPago(DateTime desde, DateTime hasta)
        {
            return Consultar(@"
                SELECT c.MedioPago, COUNT(*) AS Cantidad, SUM(c.MontoCobrado) AS Total
                FROM dbo.Cobros c
                WHERE c.FechaHoraCobro >= @Desde AND c.FechaHoraCobro < DATEADD(day, 1, @Hasta)
                GROUP BY c.MedioPago
                ORDER BY SUM(c.MontoCobrado) DESC;", desde, hasta);
        }

        public DataTable IngresosPorEspecialidad(DateTime desde, DateTime hasta)
        {
            return Consultar(@"
                SELECT e.Nombre AS Especialidad,
                       COUNT(*) AS Cobros,
                       SUM(c.MontoCobrado) AS Total,
                       SUM(c.MontoObraSocial) AS CubiertoObraSocial
                FROM dbo.Cobros c
                INNER JOIN dbo.Turnos t ON t.TurnoId = c.TurnoId
                INNER JOIN dbo.Especialidades e ON e.EspecialidadId = t.EspecialidadId
                WHERE c.FechaHoraCobro >= @Desde AND c.FechaHoraCobro < DATEADD(day, 1, @Hasta)
                GROUP BY e.Nombre
                ORDER BY SUM(c.MontoCobrado) DESC, e.Nombre;", desde, hasta);
        }

        public DataTable ConsultasPorObraSocial(DateTime desde, DateTime hasta)
        {
            return Consultar(@"
                SELECT COALESCE(os.Nombre, N'Particular (sin obra social)') AS ObraSocial,
                       COUNT(*) AS Consultas,
                       SUM(c.MontoBase) AS MontoBase,
                       SUM(c.MontoObraSocial) AS CubiertoObraSocial,
                       SUM(c.Copago) AS CopagoPacientes
                FROM dbo.Cobros c
                INNER JOIN dbo.Turnos t ON t.TurnoId = c.TurnoId
                INNER JOIN dbo.Pacientes p ON p.PacienteId = t.PacienteId
                LEFT JOIN dbo.ObrasSociales os ON os.ObraSocialId = p.ObraSocialId
                WHERE c.TipoCobro = N'Consulta'
                  AND c.FechaHoraCobro >= @Desde AND c.FechaHoraCobro < DATEADD(day, 1, @Hasta)
                GROUP BY os.Nombre
                ORDER BY COUNT(*) DESC, os.Nombre;", desde, hasta);
        }

        public DataTable HonorariosLiquidados(DateTime desde, DateTime hasta)
        {
            return Consultar(@"
                SELECT u.Apellido + N', ' + u.Nombre AS Profesional,
                       COUNT(*) AS Liquidaciones,
                       SUM(l.TotalLiquidado) AS Total
                FROM dbo.LiquidacionesHonorarios l
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = l.ProfesionalId
                WHERE l.PeriodoHasta >= @Desde AND l.PeriodoDesde <= @Hasta
                GROUP BY u.Apellido, u.Nombre
                ORDER BY SUM(l.TotalLiquidado) DESC, u.Apellido;", desde, hasta);
        }

        public DataTable EgresosPorCategoria(DateTime desde, DateTime hasta)
        {
            return Consultar(@"
                SELECT e.Categoria, COUNT(*) AS Cantidad, SUM(e.Monto) AS Total
                FROM dbo.Egresos e
                WHERE e.Fecha >= @Desde AND e.Fecha <= @Hasta
                GROUP BY e.Categoria
                ORDER BY SUM(e.Monto) DESC;", desde, hasta);
        }

        // Auditoria del asistente (Requerimiento 11): quien consulto, por que canal y que funcion se ejecuto.
        public DataTable InteraccionesIA(DateTime desde, DateTime hasta)
        {
            return Consultar(@"
                SELECT TOP (200) i.Fecha, u.Nombre + N' ' + u.Apellido AS Usuario, r.Nombre AS Rol,
                       i.Canal, i.Consulta, i.FuncionEjecutada, i.GastoTokens
                FROM dbo.InteraccionesIA i
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = i.UsuarioId
                INNER JOIN dbo.Roles r ON r.RolId = u.RolId
                WHERE i.Fecha >= @Desde AND i.Fecha < DATEADD(day, 1, @Hasta)
                ORDER BY i.Fecha DESC, i.InteraccionIAId DESC;", desde, hasta);
        }

        private DataTable Consultar(string consulta, DateTime desde, DateTime hasta)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            using (SqlCommand comando = new SqlCommand(consulta, conexion))
            using (SqlDataAdapter adaptador = new SqlDataAdapter(comando))
            {
                comando.Parameters.Add("@Desde", SqlDbType.Date).Value = desde.Date;
                comando.Parameters.Add("@Hasta", SqlDbType.Date).Value = hasta.Date;
                DataTable resultado = new DataTable();
                adaptador.Fill(resultado);
                return resultado;
            }
        }
    }
}
