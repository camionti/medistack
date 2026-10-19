using System;
using System.Text.RegularExpressions;

namespace MediStack.Dominio
{
    // Modelos de reembolsos de señas.

    // Quien origina la cancelacion. Define si la seña pagada se devuelve o se retiene.
    public enum MotivoCancelacion
    {
        // El paciente cancela (desde su cuenta o pidiendolo en el mostrador).
        Paciente,
        // Cancela la clinica o el profesional: la seña se devuelve siempre.
        Clinica
    }

    // Que paso con la seña al cancelar el turno.
    public enum DestinoSenaCancelacion
    {
        // No habia seña pagada (turno cubierto, seña no pagada a tiempo, etc.): no hay nada que devolver.
        SinSenaPagada,
        // Corresponde devolverla: se creo un reembolso pendiente.
        Reembolso,
        // Cancelacion del paciente fuera de plazo: la clinica se queda con la seña.
        Retenida
    }

    public sealed class ResultadoCancelacion
    {
        public DestinoSenaCancelacion DestinoSena { get; set; }

        public int? ReembolsoId { get; set; }

        // Medio con el que se devuelve (el mismo con el que se pago la seña).
        public string MedioPago { get; set; }

        public decimal Monto { get; set; }
    }

    public static class ReglasReembolso
    {
        public const string MotivoCancelacionPaciente = "CancelacionPaciente";
        public const string MotivoCancelacionClinica = "CancelacionClinica";

        public static string TextoMedio(string medioPago)
        {
            switch (medioPago)
            {
                case "MercadoPago": return "Mercado Pago";
                case "Tarjeta": return "tarjeta";
                case "Transferencia": return "transferencia";
                default: return "efectivo";
            }
        }

        // Frase para el paciente / el administrativo sobre como se devuelve la seña.
        // Todas las devoluciones las hace la clinica a mano.
        public static string ComoSeDevuelve(string medioPago)
        {
            switch (medioPago)
            {
                case "MercadoPago":
                    return "se devuelve a la cuenta de Mercado Pago con la que se pagó";
                case "Efectivo":
                    return "se devuelve en efectivo en el mostrador de la clínica";
                default:
                    return "se devuelve por transferencia a la cuenta informada al pagarla";
            }
        }

        // Medios que exigen informar una cuenta (CBU, CVU o alias) al cobrar la seña en el mostrador.
        public static bool RequiereCuentaDevolucion(string medioPago)
        {
            return medioPago == "Tarjeta" || medioPago == "Transferencia";
        }

        // CBU o CVU: 22 digitos. Alias: 6 a 20 caracteres (letras, numeros, punto o guion).
        public static bool CuentaDevolucionValida(string cuenta)
        {
            string valor = NormalizarCuenta(cuenta);
            return Regex.IsMatch(valor, "^[0-9]{22}$")
                || (Regex.IsMatch(valor, "^[A-Za-z0-9.-]{6,20}$") && !Regex.IsMatch(valor, "^[0-9]+$"));
        }

        public static string NormalizarCuenta(string cuenta)
        {
            return (cuenta ?? string.Empty).Replace(" ", string.Empty).Trim();
        }
    }
}
