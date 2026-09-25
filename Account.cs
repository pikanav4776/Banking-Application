using System.Security.Cryptography;
using System.Text;

namespace AccountManagement
{
    public abstract class Account
    {
        private const int SaltSize = 16;       // 128 bits
        private const int KeySize = 32;        // 256 bits
        private const int Iterations = 210000; // OWASP guidance for PBKDF2-HMAC-SHA512
        private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA512;
        private const char Delimiter = '.';

        public int accountId { get; set; }
        public required string username { get; set; }
        protected string _password = string.Empty;

        public void setPassword(string newPassword)
        {
            _password = Hash(newPassword);
        }

        public bool verifyPassword(string password)
        {
            return Verify(password, _password);
        }

        // The one hashing routine for the whole app (passwords and SSNs): PBKDF2 with a unique random salt,
        // stored as "salt.hash" so a single column holds both. Hashing is one-way, so values can only be verified.
        protected static string Hash(string secret)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(secret), salt, Iterations, HashAlgorithm, KeySize);
            return string.Join(Delimiter, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
        }

        protected static bool Verify(string secret, string stored)
        {
            string[] segments = stored.Split(Delimiter);
            if(segments.Length != 2)
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
            catch(FormatException)
            {
                return false; // not a value produced by Hash
            }

            if(salt.Length != SaltSize || storedHash.Length != KeySize)
            {
                return false;
            }

            byte[] computed = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(secret), salt, Iterations, HashAlgorithm, KeySize);
            return CryptographicOperations.FixedTimeEquals(storedHash, computed);
        }
    }
}
