using System;
using System.Data;
using System.Data.SqlClient;
using MediStack.Datos;
using MediStack.Dominio;

namespace MediStack.Negocio
{
    public enum EstadoCajaDia
    {
        SinAbrir,
        Abierta,
        Cerrada,
        // Quedo abierta una caja de un dia anterior: hay que cerrarla antes de abrir la de hoy.
        PendienteDiaAnterior
    }

    public sealed class EstadoCaja
    {
        public EstadoCajaDia Estado { get; set; }
        public DateTime Hoy { get; set; }
        public DateTime? FechaCaja { get; set; }
        public decimal FondoInicial { get; set; }
        public DateTime? FechaHoraApertura { get; set; }
        public string Administrativo { get; set; }
    }

    public sealed class CobrosNegocio
    {
        private readonly CobrosDatos _datos = new CobrosDatos();

        public DataTable ObtenerCobros(
            Guid? pacienteId, Guid? profesionalId, DateTime? desde, DateTime? hasta)
        {
            if (desde.HasValue && hasta.HasValue)
            {
                ValidarPeriodo(desde.Value, hasta.Value);
            }

            return _datos.ObtenerCobros(pacienteId, profesionalId, desde, hasta);
        }

        public DataTable ObtenerTurnosCobrables(Guid? pacienteId, Guid? profesionalId)
        {
            return _datos.ObtenerTurnosCobrables(pacienteId, profesionalId);
        }

        public DataTable ObtenerImportesCobro(int turnoId)
        {
            if (turnoId <= 0)
            {
                throw new InvalidOperationException("Selecciona un turno válido.");
            }

            return _datos.ObtenerImportesCobro(turnoId);
        }

        // cuentaDevolucion: solo para señas con tarjeta o transferencia (CBU, CVU o alias del paciente
        // para poder devolverla si cancela). En otros casos se ignora.
        public void RegistrarCobro(
            int turnoId, string tipoCobro, string medioPago, decimal montoRecibido, Guid cobradorId,
            string cuentaDevolucion)
        {
            if (turnoId <= 0)
            {
                throw new InvalidOperationException("Selecciona un turno válido.");
            }

            if (tipoCobro != "Sena" && tipoCobro != "Consulta")
            {
                throw new InvalidOperationException("Selecciona un tipo de cobro válido.");
            }

            if (medioPago != "Efectivo" && medioPago != "Tarjeta" && medioPago != "Transferencia")
            {
                throw new InvalidOperationException("Selecciona un medio de pago válido.");
            }

            if (montoRecibido < 0 || montoRecibido > 9999999999.99m
                || decimal.Round(montoRecibido, 2) != montoRecibido)
            {
                throw new InvalidOperationException("El importe recibido debe ser válido y tener como máximo dos decimales.");
            }

            if (cobradorId == Guid.Empty)
            {
                throw new InvalidOperationException("No se pudo validar al usuario administrativo.");
            }

            if (tipoCobro == "Sena" && ReglasReembolso.RequiereCuentaDevolucion(medioPago))
            {
                cuentaDevolucion = ReglasReembolso.NormalizarCuenta(cuentaDevolucion);
                if (!ReglasReembolso.CuentaDevolucionValida(cuentaDevolucion))
                {
                    throw new InvalidOperationException(
                        "Para cobrar la seña con tarjeta o transferencia ingresá el CBU/CVU (22 números) o el alias "
                        + "de la cuenta del paciente, para poder devolverla si cancela.");
                }
            }
            else
            {
                cuentaDevolucion = null;
            }

            EjecutarSeguro(() => _datos.RegistrarCobro(
                turnoId, tipoCobro, medioPago, montoRecibido, cobradorId, cuentaDevolucion));
        }

        // Acredita una seña pagada online. Lo llaman la vuelta de Mercado Pago y el webhook,
        // SIEMPRE despues de haber verificado el pago contra la API de Mercado Pago.
        public ResultadoAcreditacionSena AcreditarSenaOnline(int turnoId, string mercadoPagoPagoId, decimal montoPagado)
        {
            if (turnoId <= 0)
            {
                throw new InvalidOperationException("El pago no corresponde a un turno valido.");
            }

            mercadoPagoPagoId = (mercadoPagoPagoId ?? string.Empty).Trim();
            if (mercadoPagoPagoId.Length == 0 || mercadoPagoPagoId.Length > 40)
            {
                throw new InvalidOperationException("El identificador del pago no es valido.");
            }

            if (montoPagado <= 0)
            {
                throw new InvalidOperationException("El importe del pago no es valido.");
            }

            try
            {
                return _datos.AcreditarSenaOnline(turnoId, mercadoPagoPagoId, montoPagado);
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                // Dos avisos del mismo pago al mismo tiempo: el indice unico rechazo el segundo.
                return ResultadoAcreditacionSena.YaAcreditada;
            }
            catch (SqlException ex)
            {
                if (ex.Number == 1205)
                {
                    throw new InvalidOperationException("El pago tuvo un conflicto con otra operacion. Intenta nuevamente.");
                }

                throw new InvalidOperationException("No se pudo registrar el pago en MediStackDB (error SQL " + ex.Number + ").");
            }
        }

        public DataTable ObtenerMovimientosCaja(DateTime desde, DateTime hasta)
        {
            ValidarPeriodo(desde, hasta);
            return _datos.ObtenerMovimientosCaja(desde, hasta);
        }

        public DataTable ObtenerTotalesCaja(DateTime desde, DateTime hasta)
        {
            ValidarPeriodo(desde, hasta);
            return _datos.ObtenerTotalesCaja(desde, hasta);
        }

        public DataTable ObtenerCierresCaja(DateTime desde, DateTime hasta)
        {
            ValidarPeriodo(desde, hasta);
            return _datos.ObtenerCierresCaja(desde, hasta);
        }

        public DataTable ObtenerCierreCaja(DateTime fecha)
        {
            return _datos.ObtenerCierreCaja(fecha.Date);
        }

        public DataTable ObtenerEgresos(DateTime desde, DateTime hasta)
        {
            ValidarPeriodo(desde, hasta);
            return _datos.ObtenerEgresos(desde, hasta);
        }

        public decimal ObtenerTotalEgresos(DateTime fecha)
        {
            return _datos.ObtenerTotalEgresos(fecha);
        }

        public void RegistrarEgreso(
            Guid administrativoId, string concepto, decimal monto, string categoria, string comprobante)
        {
            string[] categorias = { "Insumos", "Viaticos", "Mantenimiento", "Otros" };
            concepto = (concepto ?? string.Empty).Trim();
            comprobante = (comprobante ?? string.Empty).Trim();
            if (administrativoId == Guid.Empty)
            {
                throw new InvalidOperationException("No se pudo validar al usuario administrativo.");
            }

            if (concepto.Length == 0 || concepto.Length > 200)
            {
                throw new InvalidOperationException("Informa el concepto del egreso (hasta 200 caracteres).");
            }

            if (monto <= 0 || monto > 9999999999.99m || decimal.Round(monto, 2) != monto)
            {
                throw new InvalidOperationException("El monto del egreso debe ser mayor a cero y tener como máximo dos decimales.");
            }

            if (Array.IndexOf(categorias, categoria) < 0)
            {
                throw new InvalidOperationException("Selecciona una categoría de egreso válida.");
            }

            if (comprobante.Length > 100)
            {
                throw new InvalidOperationException("El comprobante no puede superar 100 caracteres.");
            }

            EjecutarSeguro(() => _datos.RegistrarEgreso(administrativoId, concepto, monto, categoria, comprobante));
        }

        // Estado de la caja segun la fecha de SQL Server (unico reloj del sistema para la caja).
        public EstadoCaja ObtenerEstadoCaja()
        {
            DataRow fila = _datos.ObtenerEstadoCaja().Rows[0];
            EstadoCaja estado = new EstadoCaja { Hoy = Convert.ToDateTime(fila["Hoy"]).Date };

            if (fila["FechaCajaAbierta"] != DBNull.Value)
            {
                estado.FechaCaja = Convert.ToDateTime(fila["FechaCajaAbierta"]).Date;
                estado.FondoInicial = Convert.ToDecimal(fila["FondoInicial"]);
                estado.FechaHoraApertura = Convert.ToDateTime(fila["FechaHoraApertura"]);
                estado.Administrativo = Convert.ToString(fila["Administrativo"]);
                estado.Estado = estado.FechaCaja < estado.Hoy
                    ? EstadoCajaDia.PendienteDiaAnterior
                    : EstadoCajaDia.Abierta;
            }
            else
            {
                estado.Estado = Convert.ToBoolean(fila["CerradaHoy"])
                    ? EstadoCajaDia.Cerrada
                    : EstadoCajaDia.SinAbrir;
            }

            return estado;
        }

        public EstadoCajaDia ObtenerEstadoCajaHoy()
        {
            return ObtenerEstadoCaja().Estado;
        }

        public DataTable ObtenerAperturaCaja(DateTime fecha)
        {
            return _datos.ObtenerAperturaCaja(fecha.Date);
        }

        public void AbrirCaja(Guid administrativoId, decimal fondoInicial)
        {
            if (administrativoId == Guid.Empty)
            {
                throw new InvalidOperationException("No se pudo validar al usuario administrativo.");
            }

            if (fondoInicial < 0 || fondoInicial > 9999999999.99m
                || decimal.Round(fondoInicial, 2) != fondoInicial)
            {
                throw new InvalidOperationException("El fondo inicial debe ser cero o mayor y tener como máximo dos decimales.");
            }

            EjecutarSeguro(() => _datos.AbrirCaja(administrativoId, fondoInicial));
        }

        // Cierra la caja abierta (la de hoy o una anterior que quedo sin cerrar) y devuelve su fecha.
        public DateTime CerrarCaja(Guid administrativoId, decimal efectivoContado)
        {
            if (administrativoId == Guid.Empty)
            {
                throw new InvalidOperationException("No se pudo validar al usuario administrativo.");
            }

            if (efectivoContado < 0 || efectivoContado > 9999999999.99m
                || decimal.Round(efectivoContado, 2) != efectivoContado)
            {
                throw new InvalidOperationException("El efectivo contado debe ser válido y tener como máximo dos decimales.");
            }

            DateTime fechaCerrada = DateTime.MinValue;
            EjecutarSeguro(() => fechaCerrada = _datos.CerrarCaja(administrativoId, efectivoContado));
            return fechaCerrada;
        }

        private static void ValidarPeriodo(DateTime desde, DateTime hasta)
        {
            if (desde.Date > hasta.Date)
            {
                throw new InvalidOperationException("La fecha inicial no puede ser posterior a la fecha final.");
            }

            if (hasta.Date > DateTime.Today.AddYears(5))
            {
                throw new InvalidOperationException("El rango de fechas no puede superar cinco años.");
            }
        }

        private static void EjecutarSeguro(Action accion)
        {
            try
            {
                accion();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (SqlException ex)
            {
                if (ex.Number >= 51101 && ex.Number <= 51115)
                {
                    throw new InvalidOperationException(ex.Message);
                }

                if (ex.Number == 2601 || ex.Number == 2627)
                {
                    throw new InvalidOperationException("El turno ya tiene registrado ese tipo de cobro, o la caja de la fecha ya fue cerrada.");
                }

                if (ex.Number == 1205)
                {
                    throw new InvalidOperationException("La operación tuvo un conflicto con otro cobro o cierre. Actualiza la información e intenta nuevamente.");
                }

                throw new InvalidOperationException("No se pudo completar la operación en MediStackDB (error SQL " + ex.Number + ").");
            }
        }
    }
}
