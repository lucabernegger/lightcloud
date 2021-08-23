using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Cloud.Extensions
{
    public static class StringExtension
    {
        public static string Encrypt(this string clearText, string passphrase)
        {
            string EncryptionKey = passphrase;
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            using Aes encryptor = Aes.Create();
            Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
            encryptor.Key = pdb.GetBytes(32);
            encryptor.IV = pdb.GetBytes(16);
            encryptor.Padding = PaddingMode.None;

            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(clearBytes, 0, clearBytes.Length);
                    cs.Close();
                }
                clearText = Convert.ToBase64String(ms.ToArray());
            }

            return clearText;
        }
        public static string Decrypt(this string cipherText, string passphrase)
        {
            string EncryptionKey = passphrase;
            if (cipherText is null)
                return String.Empty;

            cipherText = cipherText.Replace(" ", "+");
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using Aes encryptor = Aes.Create();
            Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
            encryptor.Key = pdb.GetBytes(32);
            encryptor.IV = pdb.GetBytes(16);
            encryptor.Padding = PaddingMode.None;
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(cipherBytes, 0, cipherBytes.Length);
                    cs.Close();
                }
                cipherText = Encoding.Unicode.GetString(ms.ToArray());
            }

            return cipherText;
        }

        public static string Sha512(this string str)
        {
            using SHA512 shaM = SHA512.Create();
            return Convert.ToBase64String(shaM.ComputeHash(Encoding.UTF8.GetBytes(str)));
        }
    }
}
