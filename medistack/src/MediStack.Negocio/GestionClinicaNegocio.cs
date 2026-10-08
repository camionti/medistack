using System;
using System.Data;
using System.Data.SqlClient;
using System.Net.Mail;
using MediStack.Datos;
using MediStack.Dominio;

namespace MediStack.Negocio
{
    public class GestionClinicaNegocio
    {
        private readonly GestionClinicaDatos _datos;

        public GestionClinicaNegocio()
        {
            _datos = new GestionClinicaDatos();
        }

        public DataTable BuscarPacientes(string busqueda)
        {
            return EjecutarSeguro(() => _datos.BuscarPacientes(LimpiarBusqueda(busqueda)));
        }

        public DataTable BuscarProfesionales(string busqueda)
        {
            return EjecutarSeguro(() => _datos.BuscarProfesionales(LimpiarBusqueda(busqueda)));
        }

        public DataTable ObtenerObrasSociales(string busqueda)
        {
            return EjecutarSeguro(() => _datos.ObtenerObrasSociales(LimpiarBusqueda(busqueda)));
        }

        public DataTable ObtenerObrasSocialesActivas()
        {
            return EjecutarSeguro(() => _datos.ObtenerObrasSocialesActivas());
        }

        public DataTable ObtenerOpcionesObraSocialPaciente()
        {
            return EjecutarSeguro(() => _datos.ObtenerOpcionesObraSocialPaciente());
        }

        public DataTable ObtenerEspecialidades(string busqueda)
        {
            return EjecutarSeguro(() => _datos.ObtenerEspecialidades(LimpiarBusqueda(busqueda)));
        }

        public DataTable ObtenerEspecialidadesActivas()
        {
            return EjecutarSeguro(() => _datos.ObtenerEspecialidadesActivas());
        }

        public DataTable ObtenerCoberturas()
        {
            return EjecutarSeguro(() => _datos.ObtenerCoberturas());
        }

        public DataTable BuscarCoberturas(string busqueda)
        {
            return EjecutarSeguro(() => _datos.BuscarCoberturas(LimpiarBusqueda(busqueda)));
        }

        public DataTable BuscarConvenios(string busqueda)
        {
            return EjecutarSeguro(() => _datos.BuscarConvenios(LimpiarBusqueda(busqueda)));
        }

        public DataTable ObtenerProfesionalesParaConvenio()
        {
            return EjecutarSeguro(() => _datos.ObtenerProfesionalesParaConvenio());
        }

        public DataTable ObtenerCoberturasParaConvenio()
        {
            return EjecutarSeguro(() => _datos.ObtenerCoberturasParaConvenio());
        }

        public DataTable ObtenerConvenio(int convenioId)
        {
            if (convenioId <= 0)
            {
                throw new InvalidOperationException("El convenio seleccionado no es valido.");
            }

            return EjecutarSeguro(() => _datos.ObtenerConvenio(convenioId));
        }

        public void GuardarPaciente(Paciente paciente, string password)
        {
            ValidarPaciente(paciente);
            ValidarUnicidadAfiliado(paciente);
            ValidarUnicidadDocumento(paciente);
            ValidarUnicidadEmail(paciente);           
            ValidarUnicidadNombreUsuario(paciente);   
            string hash = null;
            if (paciente.UsuarioId == Guid.Empty)
            {
                ValidarContrasenaNueva(password);
                hash = VerificadorPassword.CrearHash(password);
            }

            EjecutarSeguro(() => _datos.GuardarPaciente(paciente, hash));
        }

        public void CambiarEstadoPaciente(Guid pacienteId, bool activo)
        {
            if (pacienteId == Guid.Empty)
            {
                throw new InvalidOperationException("Selecciona un paciente valido.");
            }

            EjecutarSeguro(() => _datos.CambiarEstadoPaciente(pacienteId, activo));
        }

        public void GuardarProfesional(Profesional profesional, string password)
        {
            ValidarProfesional(profesional);
            string hash = null;
            if (profesional.UsuarioId == Guid.Empty)
            {
                ValidarContrasenaNueva(password);
                hash = VerificadorPassword.CrearHash(password);
            }

            EjecutarSeguro(() => _datos.GuardarProfesional(profesional, hash));
        }

        public void CambiarEstadoProfesional(Guid profesionalId, bool activo)
        {
            if (profesionalId == Guid.Empty)
            {
                throw new InvalidOperationException("Selecciona un profesional valido.");
            }

            EjecutarSeguro(() => _datos.CambiarEstadoProfesional(profesionalId, activo));
        }

        public void GuardarEspecialidad(Especialidad especialidad)
        {
            if (especialidad == null)
            {
                throw new InvalidOperationException("Completa los datos de la especialidad.");
            }

            especialidad.Codigo = TextoObligatorio(especialidad.Codigo, "El codigo", 30);
            especialidad.Nombre = TextoObligatorio(especialidad.Nombre, "El nombre", 100);
            especialidad.Descripcion = TextoOpcional(especialidad.Descripcion, 255, "La descripcion");
            if (especialidad.DuracionEstandarMinutos < 1 || especialidad.DuracionEstandarMinutos > 1440)
            {
                throw new InvalidOperationException("La duracion debe ser de entre 1 y 1440 minutos.");
            }

            EjecutarSeguro(() => _datos.GuardarEspecialidad(especialidad));
        }

        public void CambiarEstadoEspecialidad(int especialidadId, bool activa)
        {
            if (especialidadId <= 0)
            {
                throw new InvalidOperationException("Selecciona una especialidad valida.");
            }

            EjecutarSeguro(() => _datos.CambiarEstadoEspecialidad(especialidadId, activa));
        }

        public void GuardarObraSocial(ObraSocial obraSocial)
        {
            if (obraSocial == null)
            {
                throw new InvalidOperationException("Completa los datos de la obra social.");
            }

            obraSocial.Nombre = TextoObligatorio(obraSocial.Nombre, "El nombre", 100);
            obraSocial.CodigoCUIT = TextoObligatorio(obraSocial.CodigoCUIT, "El CUIT o codigo", 20);
            EjecutarSeguro(() => _datos.GuardarObraSocial(obraSocial));
        }

        public void CambiarEstadoObraSocial(int obraSocialId, bool activa)
        {
            if (obraSocialId <= 0)
            {
                throw new InvalidOperationException("Selecciona una obra social valida.");
            }

            EjecutarSeguro(() => _datos.CambiarEstadoObraSocial(obraSocialId, activa));
        }

        public void GuardarCobertura(CoberturaEspecialidad cobertura, bool esNueva)
        {
            if (cobertura == null || cobertura.ObraSocialId <= 0 || cobertura.EspecialidadId <= 0)
            {
                throw new InvalidOperationException("Selecciona una obra social y una especialidad.");
            }

            if (cobertura.PorcentajeCobertura < 0 || cobertura.PorcentajeCobertura > 100)
            {
                throw new InvalidOperationException("La cobertura debe estar entre 0 y 100 por ciento.");
            }

            EjecutarSeguro(() => _datos.GuardarCobertura(cobertura, esNueva));
        }

        public void EliminarCobertura(int obraSocialId, int especialidadId)
        {
            if (obraSocialId <= 0 || especialidadId <= 0)
            {
                throw new InvalidOperationException("Selecciona una cobertura valida.");
            }

            EjecutarSeguro(() => _datos.EliminarCobertura(obraSocialId, especialidadId));
        }

        public void GuardarConvenio(Convenio convenio, bool esNuevo)
        {
            if (convenio == null || convenio.ProfesionalId == Guid.Empty
                || convenio.EspecialidadId <= 0 || convenio.ObraSocialId <= 0)
            {
                throw new InvalidOperationException("Completa el profesional, la especialidad y la cobertura.");
            }

            convenio.EsquemaTipo = (convenio.EsquemaTipo ?? string.Empty).Trim();
            if (convenio.EsquemaTipo != "Porcentaje" && convenio.EsquemaTipo != "Fijo")
            {
                throw new InvalidOperationException("Selecciona un esquema de honorarios valido.");
            }

            if (convenio.ValorConsulta < 0 || convenio.ValorConsulta > 9999999999.99m)
            {
                throw new InvalidOperationException("El valor de consulta debe ser un importe valido.");
            }

            if (convenio.EsquemaValor < 0
                || (convenio.EsquemaTipo == "Porcentaje" && convenio.EsquemaValor > 100)
                || convenio.EsquemaValor > 99999.99m)
            {
                throw new InvalidOperationException(
                    convenio.EsquemaTipo == "Porcentaje"
                        ? "El porcentaje de honorarios debe estar entre 0 y 100."
                        : "El importe de honorarios debe ser un valor valido.");
            }

            convenio.FechaDesde = convenio.FechaDesde.Date;
            if (convenio.FechaHasta.HasValue)
            {
                convenio.FechaHasta = convenio.FechaHasta.Value.Date;
                if (convenio.FechaHasta.Value < convenio.FechaDesde)
                {
                    throw new InvalidOperationException("La fecha de fin no puede ser anterior a la fecha de inicio.");
                }
            }

            EjecutarSeguro(() => _datos.GuardarConvenio(convenio, esNuevo));
        }

        public void CambiarEstadoConvenio(int convenioId, bool activo)
        {
            if (convenioId <= 0)
            {
                throw new InvalidOperationException("Selecciona un convenio valido.");
            }

            EjecutarSeguro(() => _datos.CambiarEstadoConvenio(convenioId, activo));
        }

        private static void ValidarPaciente(Paciente paciente)
        {
            if (paciente == null)
            {
                throw new InvalidOperationException("Completa los datos del paciente.");
            }

            paciente.NombreUsuario = TextoObligatorio(paciente.NombreUsuario, "El usuario", 50);
            paciente.Nombre = TextoObligatorio(paciente.Nombre, "El nombre", 100);
            paciente.Apellido = TextoObligatorio(paciente.Apellido, "El apellido", 100);
            paciente.NumeroDocumento = TextoObligatorio(paciente.NumeroDocumento, "El documento", 20);
            paciente.Email = ValidarEmail(paciente.Email);
            paciente.Telefono = TextoOpcional(paciente.Telefono, 30, "El telefono");
            paciente.NumeroAfiliado = TextoOpcional(paciente.NumeroAfiliado, 50, "El numero de afiliado");
            paciente.ContactoEmergenciaNombre = TextoOpcional(
                paciente.ContactoEmergenciaNombre, 100, "El contacto de emergencia");
            paciente.ContactoEmergenciaTelefono = TextoOpcional(
                paciente.ContactoEmergenciaTelefono, 30, "El telefono del contacto de emergencia");
            ValidarFechaNacimiento(paciente.FechaNacimiento);
        }

        private void ValidarUnicidadAfiliado(Paciente paciente)
        {
            if (string.IsNullOrWhiteSpace(paciente.NumeroAfiliado))
                return;

            var pacienteExistente = _datos.ObtenerPacientePorAfiliado(paciente.NumeroAfiliado);

            if (pacienteExistente != null && pacienteExistente.UsuarioId != paciente.UsuarioId)
            {
                throw new InvalidOperationException("El número de afiliado ya se encuentra registrado para otro paciente.");
            }
        }

        private void ValidarUnicidadDocumento(Paciente paciente)
        {
            if (string.IsNullOrWhiteSpace(paciente.NumeroDocumento))
                return;

            var pacienteExistente = _datos.ObtenerPacientePorDocumento(paciente.NumeroDocumento);

            if (pacienteExistente != null && pacienteExistente.UsuarioId != paciente.UsuarioId)
            {
                throw new InvalidOperationException("El número de documento ya se encuentra registrado para otro paciente.");
            }
        }
        private void ValidarUnicidadEmail(Paciente paciente)
        {
            if (_datos.ExisteEmail(paciente.Email))
            {
                throw new InvalidOperationException("El correo electrónico ingresado ya se encuentra registrado.");
            }
        }

        private void ValidarUnicidadNombreUsuario(Paciente paciente)
        {
            if (_datos.ExisteNombreUsuario(paciente.NombreUsuario))
            {
                throw new InvalidOperationException("Ese nombre de usuario ya está en uso. Por favor, elegí otro.");
            }
        }
        private static void ValidarProfesional(Profesional profesional)
        {
            if (profesional == null)
            {
                throw new InvalidOperationException("Completa los datos del profesional.");
            }

            profesional.NombreUsuario = TextoObligatorio(profesional.NombreUsuario, "El usuario", 50);
            profesional.Nombre = TextoObligatorio(profesional.Nombre, "El nombre", 100);
            profesional.Apellido = TextoObligatorio(profesional.Apellido, "El apellido", 100);
            profesional.NumeroDocumento = TextoObligatorio(profesional.NumeroDocumento, "El documento", 20);
            profesional.Email = ValidarEmail(profesional.Email);
            profesional.Telefono = TextoOpcional(profesional.Telefono, 30, "El telefono");
            profesional.MatriculaProfesional = TextoObligatorio(
                profesional.MatriculaProfesional, "La matricula profesional", 50);
            ValidarFechaNacimiento(profesional.FechaNacimiento);
        }

        private static void ValidarFechaNacimiento(DateTime fecha)
        {
            if (fecha.Date > DateTime.Today || fecha.Date < new DateTime(1900, 1, 1))
            {
                throw new InvalidOperationException("Ingresa una fecha de nacimiento valida.");
            }
        }

        private static void ValidarContrasenaNueva(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 10 || password.Length > 128)
            {
                throw new InvalidOperationException("La contrasena inicial debe tener entre 10 y 128 caracteres.");
            }
        }

        private static string ValidarEmail(string email)
        {
            string valor = TextoObligatorio(email, "El correo electronico", 256);
            try
            {
                MailAddress direccion = new MailAddress(valor);
                if (!string.Equals(direccion.Address, valor, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Ingresa un correo electronico valido.");
                }
            }
            catch (FormatException)
            {
                throw new InvalidOperationException("Ingresa un correo electronico valido.");
            }

            return valor;
        }

        private static string TextoObligatorio(string valor, string nombre, int longitudMaxima)
        {
            string limpio = (valor ?? string.Empty).Trim();
            if (limpio.Length == 0)
            {
                throw new InvalidOperationException(nombre + " es obligatorio.");
            }

            if (limpio.Length > longitudMaxima)
            {
                throw new InvalidOperationException(nombre + " supera el maximo de " + longitudMaxima + " caracteres.");
            }

            return limpio;
        }

        private static string TextoOpcional(string valor, int longitudMaxima, string nombre)
        {
            string limpio = (valor ?? string.Empty).Trim();
            if (limpio.Length > longitudMaxima)
            {
                throw new InvalidOperationException(nombre + " supera el maximo de " + longitudMaxima + " caracteres.");
            }

            return limpio;
        }

        private static string LimpiarBusqueda(string busqueda)
        {
            string valor = (busqueda ?? string.Empty).Trim();
            if (valor.Length > 100)
            {
                throw new InvalidOperationException("La busqueda no puede superar los 100 caracteres.");
            }

            return valor;
        }

        private static void EjecutarSeguro(Action accion)
        {
            EjecutarSeguro(() =>
            {
                accion();
                return true;
            });
        }

        private static T EjecutarSeguro<T>(Func<T> accion)
        {
            try
            {
                return accion();
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2601 || ex.Number == 2627)
                {
                    throw new InvalidOperationException("Ya existe un registro con alguno de esos datos unicos.");
                }

                if (ex.Number == 547)
                {
                    throw new InvalidOperationException(
                        "No se puede completar la operacion porque hay datos relacionados. Revisa las coberturas, convenios y turnos vinculados.");
                }

                if (ex.Number == 2628 || ex.Number == 8152)
                {
                    throw new InvalidOperationException("Uno de los datos ingresados supera la longitud permitida.");
                }

                if (ex.Number == 51001 || ex.Number == 51002 || ex.Number == 51003)
                {
                    throw new InvalidOperationException(ex.Message);
                }

                throw new InvalidOperationException(
                    "No se pudo completar la operacion en MediStackDB (error SQL " + ex.Number + ").");
            }
        }
    }
}
