using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Cloud.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;

namespace Cloud
{
    public class UserManager
    {
        private static readonly Random Random = new();

        public static bool IsValid(string email, string password)
        {
            using var db = new ApplicationDbContext();
            var user = db.Users.FirstOrDefault(u => u.Name.Equals(email));
            if (user is null) return false;

            var dbpassword = user.Password;
            var hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password,
                Convert.FromBase64String(user.Salt),
                KeyDerivationPrf.HMACSHA1,
                10000,
                256 / 8));
            return dbpassword.Equals(hashed);
        }

        public static User GetUserFromName(string email)
        {
            using ApplicationDbContext db = new();
            return db.Users.FirstOrDefault(o => o.Name == email);
        }

        public static Task<User> GetUserById(int id)
        {
            using ApplicationDbContext db = new();
            return db.Users.FirstOrDefaultAsync(o => o.Id == id);
        }

        public static string[] GenerateHashAndSalt(string password)
        {
            var salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            var hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password,
                salt,
                KeyDerivationPrf.HMACSHA1,
                10000,
                256 / 8));
            return new[] {hashed, Convert.ToBase64String(salt)};
        }

        public static string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, 5)
                .Select(s => s[Random.Next(s.Length)]).ToArray());
        }
        public static string GenerateRandomCryptoString()
        {
            using (RandomNumberGenerator rng = new RNGCryptoServiceProvider())
            {
                byte[] tokenData = new byte[32];
                rng.GetBytes(tokenData);

               return Convert.ToBase64String(tokenData);
            }
        }

        public static async Task<User> GetUser(ClaimsPrincipal claimsPrincipal)
        {
            await using var db = new ApplicationDbContext();
            var id = claimsPrincipal.Claims.ToList().FirstOrDefault(o => o.Type == ClaimTypes.NameIdentifier);
            if (id is not null) return await db.Users.FirstOrDefaultAsync(o => o.Id == int.Parse(id.Value));

            return null;
        }

        public static async Task SetNewPassword(int id, string password)
        {
            await using var db = new ApplicationDbContext();
            var user = await db.Users.FindAsync(id);
            var hashedPassword = GenerateHashAndSalt(password);
            user.Password = hashedPassword[0];
            user.Salt = hashedPassword[1];
            await db.SaveChangesAsync();
        }

        public static async Task SetAdmin(int id, bool toggle)
        {
            await using var db = new ApplicationDbContext();
            var user = await db.Users.FindAsync(id);
            user.IsAdmin = toggle;
            await db.SaveChangesAsync();
        }

        public static async Task SetNewName(int id, string name)
        {
            await using var db = new ApplicationDbContext();
            var user = await db.Users.FindAsync(id);
            user.Name = name;
            await db.SaveChangesAsync();
        }
        private const int Keysize = 256;

        // This constant determines the number of iterations for the password bytes generation function.
        private const int DerivationIterations = 1000;
        private static byte[] Generate256BitsOfRandomEntropy()
        {
            var randomBytes = new byte[32]; // 32 Bytes will give us 256 bits.
            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                // Fill the array with cryptographically secure random bytes.
                rngCsp.GetBytes(randomBytes);
            }
            return randomBytes;
        }

        private static Random random = new Random((int)DateTime.Now.Ticks);//thanks to McAden
        public static string RandomString(int size)
        {
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }

            return builder.ToString();
        }
        public static string Encrypt(string clearText, string passphrase)
        {
            string EncryptionKey = passphrase;
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            return clearText;
        }
        public static string Decrypt(string cipherText, string passphrase)
        {
            string EncryptionKey = passphrase;
            if (cipherText is null)
                return String.Empty;

            cipherText = cipherText.Replace(" ", "+");
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }

        public static string Sha512(string str)
        {
            using SHA512 shaM = new SHA512Managed();
            return Convert.ToBase64String(shaM.ComputeHash(Encoding.UTF8.GetBytes(str)));
        }
    }

    public static class UserManagerHelper
    {
        public static async Task<User> GetUser(this ClaimsPrincipal claimsPrincipal)
        {
            await using var db = new ApplicationDbContext();
            var id = claimsPrincipal.Claims.ToList().FirstOrDefault(o => o.Type == ClaimTypes.NameIdentifier);
            if (id is not null) return await db.Users.FirstOrDefaultAsync(o => o.Id == int.Parse(id.Value));

            return null;
        }
    }
}