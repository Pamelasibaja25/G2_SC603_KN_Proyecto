using System.Security.Cryptography;
using System.Text;

namespace G2_SC603_KN_Proyecto.Helpers
{
    /// <summary>
    /// Lógica de hashing de contraseñas centralizada. Antes vivía duplicada
    /// (y desincronizada) entre HomeController (login, ya con PBKDF2) y
    /// AccountController (Recuperar/Cambiar, que seguía en SHA-256 plano).
    /// Que ambos usen esto mismo evita que una ruta "downgradee" el hash de
    /// un usuario que ya había migrado a PBKDF2.
    /// </summary>
    public static class PasswordHasher
    {
        private const int Iteraciones = 100_000;
        private const int TamanoSalt = 16;
        private const int TamanoHash = 32;

        /// <summary>Genera un hash PBKDF2 con salt aleatorio e iteraciones.</summary>
        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(TamanoSalt);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password), salt, Iteraciones, HashAlgorithmName.SHA256, TamanoHash);

            return $"PBKDF2${Iteraciones}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// Verifica una contraseña contra el hash guardado, soportando tanto
        /// el formato viejo (SHA-256 plano, sin salt) como el nuevo
        /// (PBKDF2 con salt e iteraciones). "esHashViejo" indica si conviene
        /// migrar el hash guardado a PBKDF2 después de esta verificación.
        /// </summary>
        public static bool Verify(string password, string hashGuardado, out bool esHashViejo)
        {
            if (!string.IsNullOrEmpty(hashGuardado) && hashGuardado.StartsWith("PBKDF2$"))
            {
                esHashViejo = false;
                return VerificarPbkdf2(password, hashGuardado);
            }

            esHashViejo = true;
            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            string hashIngresado = BitConverter.ToString(bytes).Replace("-", "").ToLower();
            return hashIngresado == (hashGuardado ?? "").ToLower();
        }

        private static bool VerificarPbkdf2(string password, string hashGuardado)
        {
            string[] partes = hashGuardado.Split('$');
            if (partes.Length != 4 || !int.TryParse(partes[1], out int iteraciones))
            {
                return false;
            }

            byte[] salt = Convert.FromBase64String(partes[2]);
            byte[] hashEsperado = Convert.FromBase64String(partes[3]);

            byte[] hashCalculado = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password), salt, iteraciones, HashAlgorithmName.SHA256, hashEsperado.Length);

            return CryptographicOperations.FixedTimeEquals(hashCalculado, hashEsperado);
        }
    }
}
