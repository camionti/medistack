using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Security;

namespace MediStack.Negocio
{
    public enum EstadoToken
    {
        Valido,
        Invalido,
        Expirado,
        YaUtilizado
    }

    public class ResultadoToken
    {
        public EstadoToken Estado { get; set; }
        public string Email { get; set; }
        public string Huella { get; set; }

        public string Mensaje
        {
            get
            {
                switch (Estado)
                {
                    case EstadoToken.Expirado:
                        return "El enlace caducó (dura 2 horas). Pedí uno nuevo desde «Recuperar cuenta».";
                    case EstadoToken.YaUtilizado:
                        return "Este enlace ya se utilizó y no se puede usar de nuevo. Si lo necesitás, pedí uno nuevo desde «Recuperar cuenta».";
                    case EstadoToken.Invalido:
                        return "El enlace no es válido. Pedí uno nuevo desde «Recuperar cuenta».";
                    default:
                        return string.Empty;
                }
            }
        }

        public static ResultadoToken Con(EstadoToken estado)
        {
            return new ResultadoToken { Estado = estado };
        }
    }

    public static class TokenRecuperacion
    {
        private const string Proposito = "MediStack.RecuperacionPassword.v1";

        public static readonly TimeSpan Vigencia = TimeSpan.FromHours(2);

        public static string Generar(string email, string passwordHashActual)
        {
            long vencimiento = DateTime.UtcNow.Add(Vigencia).Ticks;
            string texto = email + "|"
                         + vencimiento.ToString(CultureInfo.InvariantCulture) + "|"
                         + CalcularHuella(passwordHashActual);

            byte[] cifrado = MachineKey.Protect(Encoding.UTF8.GetBytes(texto), Proposito);
            return HttpServerUtility.UrlTokenEncode(cifrado);
        }

        public static ResultadoToken Descifrar(string token)
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length > 2000)
            {
                return ResultadoToken.Con(EstadoToken.Invalido);
            }

            try
            {
                byte[] cifrado = HttpServerUtility.UrlTokenDecode(token);
                byte[] plano = cifrado == null ? null : MachineKey.Unprotect(cifrado, Proposito);
                if (plano == null)
                {
                    return ResultadoToken.Con(EstadoToken.Invalido);
                }

                string[] partes = Encoding.UTF8.GetString(plano).Split('|');
                long vencimiento;
                if (partes.Length != 3
                    || string.IsNullOrWhiteSpace(partes[0])
                    || !long.TryParse(partes[1], NumberStyles.None, CultureInfo.InvariantCulture, out vencimiento))
                {
                    return ResultadoToken.Con(EstadoToken.Invalido);
                }

                if (DateTime.UtcNow.Ticks > vencimiento)
                {
                    return ResultadoToken.Con(EstadoToken.Expirado);
                }

                return new ResultadoToken { Estado = EstadoToken.Valido, Email = partes[0], Huella = partes[2] };
            }
            catch (Exception)
            {
                return ResultadoToken.Con(EstadoToken.Invalido);
            }
        }

        public static string CalcularHuella(string passwordHash)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] resumen = sha.ComputeHash(Encoding.UTF8.GetBytes(passwordHash ?? string.Empty));
                return Convert.ToBase64String(resumen, 0, 16);
            }
        }
    }
}