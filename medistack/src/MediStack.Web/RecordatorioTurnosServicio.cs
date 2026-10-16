using System;
using System.Diagnostics;
using System.Threading;
using System.Web.Hosting;
using MediStack.Negocio;

namespace MediStack.Web
{
    public sealed class RecordatorioTurnosServicio : IRegisteredObject
    {
        private static readonly object _bloqueo = new object();
        private static RecordatorioTurnosServicio _instancia;
        private readonly Timer _timer;
        private readonly TurnosNegocio _turnosNegocio = new TurnosNegocio();
        private bool _deteniendo;

        private RecordatorioTurnosServicio()
        {
            HostingEnvironment.RegisterObject(this);
            // Primer chequeo a los 20 segundos de iniciar, luego periódicamente cada 30 minutos
            _timer = new Timer(EjecutarChequeo, null, TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(30));
        }

        public static void Iniciar()
        {
            lock (_bloqueo)
            {
                if (_instancia == null)
                {
                    _instancia = new RecordatorioTurnosServicio();
                }
            }
        }

        public static void Detener()
        {
            lock (_bloqueo)
            {
                if (_instancia != null)
                {
                    HostingEnvironment.UnregisterObject(_instancia);
                    _instancia.DisposeInterno();
                    _instancia = null;
                }
            }
        }

        private async void EjecutarChequeo(object state)
        {
            if (_deteniendo)
            {
                return;
            }

            try
            {
                string baseUrl = ConfiguracionSitio.ObtenerUrlBase();
                int enviados = await _turnosNegocio.ProcesarRecordatorios48hsAsync(baseUrl);
                if (enviados > 0)
                {
                    Trace.TraceInformation("MediStack: se enviaron " + enviados + " recordatorios de turnos a 48 hs.");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error en RecordatorioTurnosServicio: " + ex);
            }
        }

        public void Stop(bool immediate)
        {
            _deteniendo = true;
            DisposeInterno();
            HostingEnvironment.UnregisterObject(this);
        }

        private void DisposeInterno()
        {
            if (_timer != null)
            {
                _timer.Dispose();
            }
        }
    }
}
