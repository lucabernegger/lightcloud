using System;
using System.Diagnostics;
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
        public static string GenerateRandomCryptoString(int size = 32)
        {
            using (RandomNumberGenerator rng = new RNGCryptoServiceProvider())
            {
                byte[] tokenData = new byte[size];
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