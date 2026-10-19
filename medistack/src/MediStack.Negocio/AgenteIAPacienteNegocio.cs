using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Web.Script.Serialization;
using MediStack.Datos;
using MediStack.Dominio;

namespace MediStack.Negocio
{
    // Asistente conversacional (Requerimiento 11). Orquesta al proveedor de IA con "function calling":
    // el modelo solo puede pedir funciones que este agente expone segun el rol del usuario autenticado,
    // y cada funcion reutiliza la logica existente (TurnosNegocio, CobrosNegocio) sin agregar reglas nuevas.
    // Las operaciones que modifican datos nunca se ejecutan solas: quedan como accion pendiente que el
    // usuario debe confirmar explicitamente.
    public sealed class AgenteIAPacienteNegocio
    {
        private const int MaximoPasosHerramientas = 4;
        private const int MaximoMensajesHistorial = 12;

        private readonly InteraccionesIADatos _interacciones = new InteraccionesIADatos();
        private readonly TurnosNegocio _turnos = new TurnosNegocio();
        private readonly CobrosNegocio _cobros = new CobrosNegocio();
        private readonly IProveedorIA _proveedor;

        public AgenteIAPacienteNegocio()
            : this(ProveedorIAFactory.Crear())
        {
        }

        public AgenteIAPacienteNegocio(IProveedorIA proveedor)
        {
            _proveedor = proveedor ?? throw new ArgumentNullException("proveedor");
        }

        // Compatibilidad: el asistente nacio para pacientes.
        public RespuestaAgenteIA ProcesarMensaje(Guid pacienteId, string mensaje, string canal)
        {
            return ProcesarMensaje(pacienteId, "PACIENTE", mensaje, canal, null);
        }

        public RespuestaAgenteIA ProcesarMensaje(
            Guid usuarioId, string rolCodigo, string mensaje, string canal, IList<MensajeIA> historial)
        {
            if (usuarioId == Guid.Empty)
            {
                throw new InvalidOperationException("No se pudo identificar al usuario autenticado.");
            }

            string rol = (rolCodigo ?? string.Empty).Trim().ToUpperInvariant();
            if (rol != "PACIENTE" && rol != "PROFESIONAL" && rol != "ADMINISTRATIVO")
            {
                throw new InvalidOperationException("El rol de la cuenta no tiene acceso al asistente.");
            }

            string consulta = (mensaje ?? string.Empty).Trim();
            if (consulta.Length == 0 || consulta.Length > 2000)
            {
                throw new InvalidOperationException("Escribí una consulta de entre 1 y 2000 caracteres.");
            }

            RespuestaAgenteIA respuesta = CrearRespuesta(usuarioId, rol, consulta, historial);
            _interacciones.Registrar(new InteraccionIA
            {
                UsuarioId = usuarioId,
                Canal = string.Equals(canal, "Voz", StringComparison.OrdinalIgnoreCase) ? "Voz" : "Texto",
                Consulta = consulta,
                FuncionEjecutada = respuesta.FuncionEjecutada,
                Respuesta = respuesta.Texto,
                GastoTokens = respuesta.GastoTokens
            });
            return respuesta;
        }

        // Ejecuta una accion previamente propuesta, tras la confirmacion explicita del usuario.
        public string ConfirmarCancelacion(Guid pacienteId, int turnoId)
        {
            if (pacienteId == Guid.Empty || turnoId <= 0)
            {
                throw new InvalidOperationException("No se pudo identificar el turno a cancelar.");
            }

            ResultadoCancelacion cancelacion = _turnos.CancelarTurno(
                turnoId, pacienteId, MotivoCancelacion.Paciente, pacienteId,
                System.Configuration.ConfigurationManager.AppSettings["App.UrlBase"]);
            string texto = "El turno #" + turnoId + " fue cancelado.";
            string textoSena = TurnosNegocio.DescribirSena(cancelacion);
            if (textoSena.Length > 0)
            {
                texto += " " + textoSena;
            }
            _interacciones.Registrar(new InteraccionIA
            {
                UsuarioId = pacienteId,
                Canal = "Texto",
                Consulta = "Confirmo la cancelación del turno #" + turnoId,
                FuncionEjecutada = "cancelar_turno_confirmado",
                Respuesta = texto
            });
            return texto;
        }

        private RespuestaAgenteIA CrearRespuesta(
            Guid usuarioId, string rol, string consulta, IList<MensajeIA> historial)
        {
            List<MensajeIA> mensajes = new List<MensajeIA>();
            if (historial != null)
            {
                int inicio = Math.Max(0, historial.Count - MaximoMensajesHistorial);
                for (int i = inicio; i < historial.Count; i++)
                {
                    mensajes.Add(historial[i]);
                }
            }

            mensajes.Add(new MensajeIA { Rol = "user", Contenido = consulta });
            IList<DefinicionHerramientaIA> herramientas = DefinirHerramientas(rol);
            RespuestaAgenteIA resultado = new RespuestaAgenteIA();
            string funcionesEjecutadas = null;

            for (int paso = 0; paso <= MaximoPasosHerramientas; paso++)
            {
                RespuestaIA respuestaProveedor = _proveedor.Completar(new SolicitudIA
                {
                    UsuarioId = usuarioId,
                    Instrucciones = CrearInstrucciones(rol),
                    Mensajes = mensajes,
                    Herramientas = paso < MaximoPasosHerramientas
                        ? herramientas
                        : new List<DefinicionHerramientaIA>()
                });
                resultado.GastoTokens += respuestaProveedor.GastoTokens;

                if (respuestaProveedor.Herramienta == null)
                {
                    resultado.Texto = string.IsNullOrWhiteSpace(respuestaProveedor.Contenido)
                        ? "No pude generar una respuesta. Probá reformular la consulta."
                        : respuestaProveedor.Contenido;
                    break;
                }

                LlamadaHerramientaIA llamada = respuestaProveedor.Herramienta;
                funcionesEjecutadas = funcionesEjecutadas == null
                    ? llamada.Nombre
                    : funcionesEjecutadas + "," + llamada.Nombre;
                string resultadoJson = EjecutarHerramienta(usuarioId, rol, llamada, resultado);
                mensajes.Add(new MensajeIA { Rol = "assistant", Llamada = llamada });
                mensajes.Add(new MensajeIA
                {
                    Rol = "tool",
                    NombreResultado = llamada.Nombre,
                    ResultadoJson = resultadoJson
                });
            }

            if (string.IsNullOrWhiteSpace(resultado.Texto))
            {
                resultado.Texto = "No pude completar la consulta. Probá con una pregunta más puntual.";
            }

            resultado.FuncionEjecutada = funcionesEjecutadas != null && funcionesEjecutadas.Length > 100
                ? funcionesEjecutadas.Substring(0, 100)
                : funcionesEjecutadas;
            resultado.RequiereConfirmacion = resultado.AccionPendienteTurnoId.HasValue;
            return resultado;
        }

        private static string CrearInstrucciones(string rol)
        {
            string quien = rol == "PACIENTE" ? "un paciente"
                : rol == "PROFESIONAL" ? "un profesional de la salud"
                : "una persona del personal administrativo";
            return "Sos el asistente de MediStack, un sistema de gestión de una clínica. Hablás en español rioplatense, "
                + "de forma breve y clara, con " + quien + ". Hoy es " + DateTime.Today.ToString("dddd dd/MM/yyyy",
                    CultureInfo.GetCultureInfo("es-AR")) + ". "
                + "Usá siempre las funciones disponibles para obtener datos reales; nunca inventes turnos, importes ni horarios. "
                + "Solo podés ayudar con temas de la clínica. No des consejos médicos. "
                + "Las fechas para las funciones van en formato yyyy-MM-dd. "
                + "Si el usuario quiere cancelar un turno, usá la función de cancelación: eso solo prepara la acción "
                + "y el usuario debe confirmarla con un botón; nunca digas que ya quedó cancelado.";
        }

        private static IList<DefinicionHerramientaIA> DefinirHerramientas(string rol)
        {
            List<DefinicionHerramientaIA> lista = new List<DefinicionHerramientaIA>();
            if (rol == "PACIENTE")
            {
                lista.Add(Herramienta("mis_turnos", "Lista los turnos vigentes y próximos del paciente.", null));
                lista.Add(Herramienta("mis_cobros", "Lista los últimos cobros y señas del paciente.", null));
                lista.Add(Herramienta("especialidades_disponibles", "Lista las especialidades que se pueden reservar.", null));
                lista.Add(Herramienta("horarios_disponibles",
                    "Busca horarios libres para una especialidad en una fecha, indicando si el profesional tiene cobertura y la seña.",
                    "{\"type\":\"OBJECT\",\"properties\":{"
                    + "\"especialidad\":{\"type\":\"STRING\",\"description\":\"Nombre de la especialidad\"},"
                    + "\"fecha\":{\"type\":\"STRING\",\"description\":\"Fecha yyyy-MM-dd\"}},"
                    + "\"required\":[\"especialidad\",\"fecha\"]}"));
                lista.Add(Herramienta("cancelar_turno",
                    "Prepara la cancelación de un turno del paciente. Requiere confirmación del usuario.",
                    "{\"type\":\"OBJECT\",\"properties\":{"
                    + "\"turno_id\":{\"type\":\"INTEGER\",\"description\":\"Id del turno a cancelar\"}},"
                    + "\"required\":[\"turno_id\"]}"));
            }
            else if (rol == "PROFESIONAL")
            {
                lista.Add(Herramienta("mi_agenda", "Lista los turnos del profesional en una fecha.",
                    "{\"type\":\"OBJECT\",\"properties\":{"
                    + "\"fecha\":{\"type\":\"STRING\",\"description\":\"Fecha yyyy-MM-dd (por defecto hoy)\"}}}"));
                lista.Add(Herramienta("turnos_proximos", "Lista los próximos turnos vigentes del profesional.", null));
            }
            else
            {
                lista.Add(Herramienta("turnos_del_dia", "Lista los turnos de la clínica en una fecha.",
                    "{\"type\":\"OBJECT\",\"properties\":{"
                    + "\"fecha\":{\"type\":\"STRING\",\"description\":\"Fecha yyyy-MM-dd (por defecto hoy)\"}}}"));
                lista.Add(Herramienta("turnos_sin_resolver", "Lista los turnos pasados sin marcar como atendidos, ausentes o cancelados.", null));
                lista.Add(Herramienta("resumen_caja", "Totales de cobros por medio de pago en un período.",
                    "{\"type\":\"OBJECT\",\"properties\":{"
                    + "\"desde\":{\"type\":\"STRING\",\"description\":\"Fecha inicial yyyy-MM-dd\"},"
                    + "\"hasta\":{\"type\":\"STRING\",\"description\":\"Fecha final yyyy-MM-dd\"}}}"));
            }

            return lista;
        }

        private static DefinicionHerramientaIA Herramienta(string nombre, string descripcion, string esquema)
        {
            return new DefinicionHerramientaIA
            {
                Nombre = nombre,
                Descripcion = descripcion,
                EsquemaEntradaJson = esquema
            };
        }

        // Cada funcion se vuelve a validar contra el rol: el modelo no puede invocar funciones ajenas.
        private string EjecutarHerramienta(
            Guid usuarioId, string rol, LlamadaHerramientaIA llamada, RespuestaAgenteIA respuesta)
        {
            JavaScriptSerializer serializador = new JavaScriptSerializer();
            Dictionary<string, object> argumentos = new Dictionary<string, object>();
            try
            {
                if (!string.IsNullOrWhiteSpace(llamada.ArgumentosJson))
                {
                    argumentos = serializador.DeserializeObject(llamada.ArgumentosJson)
                        as Dictionary<string, object> ?? argumentos;
                }

                object datos = Despachar(usuarioId, rol, llamada.Nombre, argumentos, respuesta);
                return serializador.Serialize(datos);
            }
            catch (InvalidOperationException ex)
            {
                return serializador.Serialize(new Dictionary<string, object> { { "error", ex.Message } });
            }
            catch (System.Data.SqlClient.SqlException)
            {
                return serializador.Serialize(new Dictionary<string, object>
                {
                    { "error", "No se pudo consultar la información en este momento." }
                });
            }
        }

        private object Despachar(
            Guid usuarioId, string rol, string nombre, Dictionary<string, object> args, RespuestaAgenteIA respuesta)
        {
            switch (rol + ":" + nombre)
            {
                case "PACIENTE:mis_turnos":
                    return FilasTurnos(_turnos.ObtenerTurnos(usuarioId, null, DateTime.Today, null), false, 15);
                case "PACIENTE:mis_cobros":
                    return FilasCobros(_cobros.ObtenerCobros(usuarioId, null, null, null), 10);
                case "PACIENTE:especialidades_disponibles":
                    return Filas(_turnos.ObtenerEspecialidadesActivas(), "Nombre");
                case "PACIENTE:horarios_disponibles":
                    return HorariosDisponibles(usuarioId, TextoArg(args, "especialidad"), FechaArg(args, "fecha", null));
                case "PACIENTE:cancelar_turno":
                    return PrepararCancelacion(usuarioId, EnteroArg(args, "turno_id"), respuesta);
                case "PROFESIONAL:mi_agenda":
                {
                    DateTime fecha = FechaArg(args, "fecha", DateTime.Today).Value;
                    return FilasTurnos(_turnos.ObtenerTurnos(null, usuarioId, fecha, fecha), true, 30);
                }
                case "PROFESIONAL:turnos_proximos":
                    return FilasTurnos(_turnos.ObtenerTurnos(null, usuarioId, DateTime.Today, DateTime.Today.AddDays(14)), true, 30);
                case "ADMINISTRATIVO:turnos_del_dia":
                {
                    DateTime fecha = FechaArg(args, "fecha", DateTime.Today).Value;
                    return FilasTurnos(_turnos.ObtenerTurnos(null, null, fecha, fecha), true, 40);
                }
                case "ADMINISTRATIVO:turnos_sin_resolver":
                    return FilasTurnos(_turnos.ObtenerTurnosSinResolver(), true, 40);
                case "ADMINISTRATIVO:resumen_caja":
                {
                    DateTime desde = FechaArg(args, "desde", DateTime.Today).Value;
                    DateTime hasta = FechaArg(args, "hasta", DateTime.Today).Value;
                    DataTable totales = _cobros.ObtenerTotalesCaja(desde, hasta);
                    DataRow fila = totales.Rows[0];
                    return new Dictionary<string, object>
                    {
                        { "desde", desde.ToString("yyyy-MM-dd") },
                        { "hasta", hasta.ToString("yyyy-MM-dd") },
                        { "cantidad_cobros", Convert.ToInt32(fila["CantidadCobros"]) },
                        { "efectivo", Convert.ToDecimal(fila["TotalEfectivo"]) },
                        { "tarjeta", Convert.ToDecimal(fila["TotalTarjeta"]) },
                        { "transferencia", Convert.ToDecimal(fila["TotalTransferencia"]) },
                        { "total", Convert.ToDecimal(fila["TotalGeneral"]) }
                    };
                }
                default:
                    throw new InvalidOperationException("La función solicitada no está disponible para este usuario.");
            }
        }

        private object HorariosDisponibles(Guid pacienteId, string especialidad, DateTime? fecha)
        {
            if (string.IsNullOrWhiteSpace(especialidad) || !fecha.HasValue)
            {
                throw new InvalidOperationException("Indicá la especialidad y la fecha.");
            }

            DataRow elegida = null;
            foreach (DataRow fila in _turnos.ObtenerEspecialidadesActivas().Rows)
            {
                string nombreEspecialidad = Convert.ToString(fila["Nombre"]);
                if (nombreEspecialidad.IndexOf(especialidad.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    elegida = fila;
                    break;
                }
            }

            if (elegida == null)
            {
                throw new InvalidOperationException("No encontré esa especialidad.");
            }

            int especialidadId = Convert.ToInt32(elegida["EspecialidadId"]);
            List<object> resultado = new List<object>();
            foreach (ProfesionalParaPaciente profesional in
                _turnos.ObtenerProfesionalesParaPaciente(especialidadId, pacienteId))
            {
                List<string> horas = new List<string>();
                foreach (TurnoDisponible franja in _turnos.ObtenerDisponibilidad(
                    profesional.ProfesionalId, especialidadId, fecha.Value))
                {
                    if (franja.Disponible && franja.FechaHora > DateTime.Now && horas.Count < 8)
                    {
                        horas.Add(franja.FechaHora.ToString("HH:mm"));
                    }
                }

                resultado.Add(new Dictionary<string, object>
                {
                    { "profesional", profesional.Nombre },
                    { "con_cobertura", profesional.TieneCobertura },
                    { "sena_a_pagar", profesional.MontoSena },
                    { "horarios_libres", horas }
                });
            }

            return new Dictionary<string, object>
            {
                { "especialidad", Convert.ToString(elegida["Nombre"]) },
                { "fecha", fecha.Value.ToString("yyyy-MM-dd") },
                { "profesionales", resultado },
                { "nota", "Para reservar, el paciente debe usar la pantalla de turnos." }
            };
        }

        private object PrepararCancelacion(Guid pacienteId, int turnoId, RespuestaAgenteIA respuesta)
        {
            TurnoDetalle turno = _turnos.ObtenerTurno(turnoId);
            if (turno == null || turno.PacienteId != pacienteId)
            {
                throw new InvalidOperationException("No encontré ese turno entre tus turnos.");
            }

            if (turno.Estado != "Solicitado" && turno.Estado != "Confirmado")
            {
                throw new InvalidOperationException("Ese turno ya no está vigente.");
            }

            if (turno.FechaHora <= DateTime.Now)
            {
                throw new InvalidOperationException("Ese turno ya pasó y no se puede cancelar.");
            }

            if (!ReglasTurno.PacienteDentroDelPlazo(turno.FechaHora))
            {
                throw new InvalidOperationException("Los turnos solo se pueden cancelar con al menos "
                    + ReglasTurno.HorasMinimasCancelacionPaciente + " horas de anticipación. Comunicate con la clínica.");
            }

            respuesta.AccionPendienteTurnoId = turnoId;
            respuesta.AccionPendienteDescripcion = "Cancelar el turno #" + turnoId + " del "
                + turno.FechaHora.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("es-AR"));
            return new Dictionary<string, object>
            {
                { "estado", "pendiente_de_confirmacion" },
                { "turno_id", turnoId },
                { "fecha", turno.FechaHora.ToString("yyyy-MM-dd HH:mm") },
                { "mensaje", "El usuario debe confirmar con el botón para que se cancele." }
            };
        }

        private static List<object> FilasTurnos(DataTable tabla, bool incluirPaciente, int maximo)
        {
            List<object> filas = new List<object>();
            DataView vista = new DataView(tabla) { Sort = "FechaHora ASC" };
            foreach (DataRowView filaVista in vista)
            {
                DataRow fila = filaVista.Row;
                if (filas.Count >= maximo)
                {
                    break;
                }

                string estado = Convert.ToString(fila["Estado"]);
                if (estado == "Cancelado" && !incluirPaciente)
                {
                    continue;
                }

                Dictionary<string, object> item = new Dictionary<string, object>
                {
                    { "turno_id", Convert.ToInt32(fila["TurnoId"]) },
                    { "fecha", Convert.ToDateTime(fila["FechaHora"]).ToString("yyyy-MM-dd HH:mm") },
                    { "profesional", Convert.ToString(fila["Profesional"]) },
                    { "especialidad", Convert.ToString(fila["Especialidad"]) },
                    { "estado", estado }
                };
                if (incluirPaciente)
                {
                    item["paciente"] = Convert.ToString(fila["Paciente"]);
                }

                filas.Add(item);
            }

            return filas;
        }

        private static List<object> FilasCobros(DataTable tabla, int maximo)
        {
            List<object> filas = new List<object>();
            foreach (DataRow fila in tabla.Rows)
            {
                if (filas.Count >= maximo)
                {
                    break;
                }

                filas.Add(new Dictionary<string, object>
                {
                    { "fecha", Convert.ToDateTime(fila["FechaHoraCobro"]).ToString("yyyy-MM-dd HH:mm") },
                    { "concepto", Convert.ToString(fila["TipoCobro"]) },
                    { "monto", Convert.ToDecimal(fila["MontoCobrado"]) },
                    { "medio", Convert.ToString(fila["MedioPago"]) },
                    { "especialidad", Convert.ToString(fila["Especialidad"]) }
                });
            }

            return filas;
        }

        private static List<string> Filas(DataTable tabla, string columna)
        {
            List<string> valores = new List<string>();
            foreach (DataRow fila in tabla.Rows)
            {
                valores.Add(Convert.ToString(fila[columna]));
            }

            return valores;
        }

        private static string TextoArg(Dictionary<string, object> args, string clave)
        {
            object valor;
            return args.TryGetValue(clave, out valor) ? Convert.ToString(valor) : null;
        }

        private static int EnteroArg(Dictionary<string, object> args, string clave)
        {
            object valor;
            int numero;
            if (args.TryGetValue(clave, out valor)
                && int.TryParse(Convert.ToString(valor, CultureInfo.InvariantCulture), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out numero))
            {
                return numero;
            }

            throw new InvalidOperationException("Falta indicar el número de turno.");
        }

        private static DateTime? FechaArg(Dictionary<string, object> args, string clave, DateTime? porDefecto)
        {
            string texto = TextoArg(args, clave);
            if (string.IsNullOrWhiteSpace(texto))
            {
                return porDefecto;
            }

            DateTime fecha;
            if (!DateTime.TryParseExact(texto.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out fecha))
            {
                throw new InvalidOperationException("La fecha debe tener el formato yyyy-MM-dd.");
            }

            return fecha;
        }
    }
}
