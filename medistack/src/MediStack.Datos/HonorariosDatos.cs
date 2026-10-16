using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace MediStack.Datos
{
    // Liquidacion de honorarios (Requerimiento 9). El honorario de cada cobro de consulta se calcula
    // con el esquema de la combinacion profesional + especialidad del turno (ProfesionalesEspecialidades).
    public sealed class HonorariosDatos
    {
        // Cobros de consulta del profesional en el periodo, con el honorario que le corresponde a cada uno.
        // Base de calculo: Particular (sin cobertura) = MontoBase; Convenio = Copago (lo que cobra la clinica
        // directamente al paciente). Porcentaje aplica sobre la base; Fijo es un monto por consulta.
        private const string ConsultaDetalle = @"
            SELECT c.CobroId, c.TurnoId, c.FechaHoraCobro,
                   paciente.Nombre + N' ' + paciente.Apellido AS Paciente,
                   e.Nombre AS Especialidad,
                   CASE WHEN c.MontoObraSocial > 0 THEN N'Convenio' ELSE N'Particular' END AS Modalidad,
                   CASE WHEN c.MontoObraSocial > 0 THEN c.Copago ELSE c.MontoBase END AS BaseCalculo,
                   pe.EsquemaTipo, pe.EsquemaValor,
                   CAST(CASE WHEN pe.EsquemaTipo = N'Porcentaje'
                             THEN ROUND((CASE WHEN c.MontoObraSocial > 0 THEN c.Copago ELSE c.MontoBase END)
                                        * pe.EsquemaValor / 100.0, 2)
                             ELSE pe.EsquemaValor END AS DECIMAL(12,2)) AS Honorario
            FROM dbo.Cobros c
            INNER JOIN dbo.Turnos t ON t.TurnoId = c.TurnoId
            INNER JOIN dbo.ProfesionalesEspecialidades pe
                ON pe.ProfesionalId = t.ProfesionalId AND pe.EspecialidadId = t.EspecialidadId
            INNER JOIN dbo.Usuarios paciente ON paciente.UsuarioId = t.PacienteId
            INNER JOIN dbo.Especialidades e ON e.EspecialidadId = t.EspecialidadId
            WHERE c.TipoCobro = N'Consulta'
              AND t.ProfesionalId = @ProfesionalId
              AND c.FechaHoraCobro >= @Desde
              AND c.FechaHoraCobro < DATEADD(day, 1, @Hasta)";

        private readonly string _cadenaConexion;

        public HonorariosDatos()
        {
            ConnectionStringSettings configuracion = ConfigurationManager.ConnectionStrings["MediStackDB"];
            if (configuracion == null || string.IsNullOrWhiteSpace(configuracion.ConnectionString))
            {
                throw new ConfigurationErrorsException("No se encontro la cadena de conexion MediStackDB.");
            }

            _cadenaConexion = configuracion.ConnectionString;
        }

        public DataTable ObtenerProfesionales()
        {
            return Consultar(@"
                SELECT p.ProfesionalId, u.Apellido + N', ' + u.Nombre AS Nombre
                FROM dbo.Profesionales p
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = p.ProfesionalId
                ORDER BY u.Apellido, u.Nombre;");
        }

        public DataTable CalcularDetalle(Guid profesionalId, DateTime desde, DateTime hasta)
        {
            return Consultar(ConsultaDetalle + " ORDER BY c.FechaHoraCobro, c.CobroId;",
                new SqlParameter("@ProfesionalId", SqlDbType.UniqueIdentifier) { Value = profesionalId },
                new SqlParameter("@Desde", SqlDbType.Date) { Value = desde.Date },
                new SqlParameter("@Hasta", SqlDbType.Date) { Value = hasta.Date });
        }

        public DataTable ObtenerLiquidaciones(Guid? profesionalId, DateTime? desde, DateTime? hasta)
        {
            return Consultar(@"
                SELECT l.LiquidacionHonorariosId, l.ProfesionalId,
                       u.Apellido + N', ' + u.Nombre AS Profesional,
                       l.PeriodoDesde, l.PeriodoHasta, l.TotalLiquidado, l.FechaLiquidacion
                FROM dbo.LiquidacionesHonorarios l
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = l.ProfesionalId
                WHERE (@ProfesionalId IS NULL OR l.ProfesionalId = @ProfesionalId)
                  AND (@Desde IS NULL OR l.PeriodoHasta >= @Desde)
                  AND (@Hasta IS NULL OR l.PeriodoDesde <= @Hasta)
                ORDER BY l.FechaLiquidacion DESC, l.LiquidacionHonorariosId DESC;",
                new SqlParameter("@ProfesionalId", SqlDbType.UniqueIdentifier)
                {
                    Value = profesionalId.HasValue ? (object)profesionalId.Value : DBNull.Value
                },
                new SqlParameter("@Desde", SqlDbType.Date) { Value = desde.HasValue ? (object)desde.Value.Date : DBNull.Value },
                new SqlParameter("@Hasta", SqlDbType.Date) { Value = hasta.HasValue ? (object)hasta.Value.Date : DBNull.Value });
        }

        // Congela la liquidacion: calcula el total dentro de la transaccion (no confia en lo que mostro la
        // pantalla) y rechaza periodos que se superponen con una liquidacion previa del mismo profesional.
        public decimal Liquidar(Guid profesionalId, DateTime desde, DateTime hasta)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                conexion.Open();
                using (SqlTransaction transaccion = conexion.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        decimal total;
                        using (SqlCommand comando = new SqlCommand(@"
                            IF NOT EXISTS (SELECT 1 FROM dbo.Profesionales WHERE ProfesionalId = @ProfesionalId)
                                THROW 51120, N'El profesional seleccionado no existe.', 1;

                            IF EXISTS (SELECT 1 FROM dbo.LiquidacionesHonorarios WITH (UPDLOCK, HOLDLOCK)
                                       WHERE ProfesionalId = @ProfesionalId
                                         AND PeriodoDesde <= @Hasta AND PeriodoHasta >= @Desde)
                                THROW 51121, N'Ya existe una liquidación de este profesional que se superpone con el período elegido.', 1;

                            DECLARE @Total DECIMAL(12,2);
                            DECLARE @Cantidad INT;
                            SELECT @Total = COALESCE(SUM(d.Honorario), 0), @Cantidad = COUNT(*)
                            FROM (" + ConsultaDetalle + @") d;

                            IF @Cantidad = 0
                                THROW 51122, N'El profesional no tiene cobros de consulta en el período elegido.', 1;

                            INSERT INTO dbo.LiquidacionesHonorarios (ProfesionalId, PeriodoDesde, PeriodoHasta, TotalLiquidado)
                            VALUES (@ProfesionalId, @Desde, @Hasta, @Total);

                            SELECT @Total;",
                            conexion, transaccion))
                        {
                            comando.Parameters.Add("@ProfesionalId", SqlDbType.UniqueIdentifier).Value = profesionalId;
                            comando.Parameters.Add("@Desde", SqlDbType.Date).Value = desde.Date;
                            comando.Parameters.Add("@Hasta", SqlDbType.Date).Value = hasta.Date;
                            total = Convert.ToDecimal(comando.ExecuteScalar());
                        }

                        transaccion.Commit();
                        return total;
                    }
                    catch
                    {
                        transaccion.Rollback();
                        throw;
                    }
                }
            }
        }

        private DataTable Consultar(string consulta, params SqlParameter[] parametros)
        {
            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            using (SqlCommand comando = new SqlCommand(consulta, conexion))
            using (SqlDataAdapter adaptador = new SqlDataAdapter(comando))
            {
                if (parametros != null)
                {
                    comando.Parameters.AddRange(parametros);
                }

                DataTable resultado = new DataTable();
                adaptador.Fill(resultado);
                return resultado;
            }
        }
    }
}
