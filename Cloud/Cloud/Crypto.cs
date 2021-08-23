using System.IO;
using System.Security.Cryptography;

namespace Cloud
{
    public class Crypto
    {
        public static byte[] EncryptByteArray(byte[] key, byte[] secret)
        {
            using Aes aes = Aes.Create();
            aes.Key = key;
            return aes.EncryptCbc(secret,aes.IV);
        }

        /// <summary>
        /// Decrypt a byte array using AES 128
        /// </summary>
        /// <param name="key">key in bytes</param>
        /// <param name="secret">the encrypted bytes</param>
        /// <returns>decrypted bytes</returns>
        public static byte[] DecryptByteArray(byte[] key, byte[] secret)
        {
            using Aes aes = Aes.Create();
            aes.Key = key;

            return aes.DecryptCbc(secret,aes.IV);
        }
    }
}
