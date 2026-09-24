using System.Security.Cryptography;
using System.Text;

namespace AccountManagement
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16; // 128 bits
        private const int KeySize = 32;  // 256 bits
        private const int Iterations = 210000; // OWASP guidance for PBKDF2-HMAC-SHA512
        private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA512;
        private const char Delimiter = '.';

        // Hashes a plaintext password with a unique, cryptographically secure salt.
        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                HashAlgorithm,
                KeySize
            );

            // salt and hash in one string so a single database column holds both
            return string.Join(Delimiter, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
        }

        // Verifies a plaintext password against a stored "salt.hash" value.
        public static bool VerifyPassword(string password, string storedHashWithSalt)
        {
            var segments = storedHashWithSalt.Split(Delimiter);
            if (segments.Length != 2)
            {
                return false;
            }

            byte[] salt;
            byte[] storedHash;
            try
            {
                salt = Convert.FromBase64String(segments[0]);
                storedHash = Convert.FromBase64String(segments[1]);
            }
            catch (FormatException)
            {
                return false; // not a hash produced by HashPassword (e.g. a legacy plain-text value)
            }

            if (salt.Length != SaltSize || storedHash.Length != KeySize)
            {
                return false;
            }

            byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                HashAlgorithm,
                KeySize
            );

            // fixed-time comparison to avoid timing attacks
            return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
        }
    }
}
