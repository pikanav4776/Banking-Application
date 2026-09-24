using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BankingData
{
    // Stores the SSN as base64(nonce | tag | ciphertext) using AES-GCM; the key comes from the BANKING_SSN_KEY env var.
    public class SsnEncryptionConverter : ValueConverter<long, string>
    {
        public SsnEncryptionConverter() : base(ssn => Encrypt(ssn), stored => Decrypt(stored)) { }

        private static byte[] GetKey()
        {
            string? secret = Environment.GetEnvironmentVariable("BANKING_SSN_KEY");
            if(string.IsNullOrEmpty(secret))
            {
                throw new InvalidOperationException("Set the BANKING_SSN_KEY environment variable before saving or loading SSNs.");
            }
            return SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        }

        private static string Encrypt(long ssn)
        {
            byte[] plaintext = Encoding.UTF8.GetBytes(ssn.ToString());
            byte[] nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
            byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];
            byte[] ciphertext = new byte[plaintext.Length];

            using var aes = new AesGcm(GetKey(), tag.Length);
            aes.Encrypt(nonce, plaintext, ciphertext, tag);

            return Convert.ToBase64String(nonce.Concat(tag).Concat(ciphertext).ToArray());
        }

        private static long Decrypt(string stored)
        {
            byte[] blob = Convert.FromBase64String(stored);
            int nonceSize = AesGcm.NonceByteSizes.MaxSize;
            int tagSize = AesGcm.TagByteSizes.MaxSize;

            byte[] nonce = blob[..nonceSize];
            byte[] tag = blob[nonceSize..(nonceSize + tagSize)];
            byte[] ciphertext = blob[(nonceSize + tagSize)..];
            byte[] plaintext = new byte[ciphertext.Length];

            using var aes = new AesGcm(GetKey(), tagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            return long.Parse(Encoding.UTF8.GetString(plaintext));
        }
    }
}
