using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Cloud.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;

namespace Cloud
{
    public class UserManager
    {
        public static bool IsValid(string email, string password)
        {
            using var db = new ApplicationDbContext();
            var user = db.Users.FirstOrDefault(u => u.Name == email);
            Debug.WriteLine("11");
            if (user is null)
            {
                return false;
            }
            Debug.WriteLine("22");

            string dbpassword = user.Password;
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: Convert.FromBase64String(user.Salt),
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));
            Debug.WriteLine(dbpassword.Equals(hashed));
            return dbpassword.Equals(hashed);
        }

        public static User GetUserFromName(string email)
        {
            using ApplicationDbContext db = new ApplicationDbContext();
            return db.Users.FirstOrDefault(o => o.Name == email);
        }
        public static Task<User> GetUserById(int id)
        {
            using ApplicationDbContext db = new ApplicationDbContext();
            return db.Users.FirstOrDefaultAsync(o => o.Id == id);
        }
        public static string[] GenerateHashAndSalt(string password)
        {
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));
            return new string[] { hashed, Convert.ToBase64String(salt) };
        }

        private static readonly Random random = new Random();

        public static string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, 5)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static async Task<User> GetUser(ClaimsPrincipal claimsPrincipal)
        {
            await using var db = new ApplicationDbContext();
            var id = claimsPrincipal.Claims.ToList().FirstOrDefault(o => o.Type == ClaimTypes.NameIdentifier);
            if (id is not null)
            {
                return await db.Users.FirstOrDefaultAsync(o => o.Id == int.Parse(id.Value));
            }

            return null;
        }

        public static async Task SetNewPassword(int id, string password)
        {
            using var db = new ApplicationDbContext();
            var user = await db.Users.FindAsync(id);
            var hashedPassword = UserManager.GenerateHashAndSalt(password);
            user.Password = hashedPassword[0];
            user.Salt = hashedPassword[1];
            await db.SaveChangesAsync();
        }
        public static async Task SetAdmin(int id,bool toggle)
        {
            using var db = new ApplicationDbContext();
            var user = await db.Users.FindAsync(id);
            user.IsAdmin = toggle;
            await db.SaveChangesAsync();
        }

        public static async Task SetNewName(int id, string name)
        {
            using var db = new ApplicationDbContext();
            var user = await db.Users.FindAsync(id);
            user.Name = name;
            await db.SaveChangesAsync();
        }
    }

    public static class UserManagerHelper
    {
        public static async Task<User> GetUser(this ClaimsPrincipal claimsPrincipal)
        {
            await using var db = new ApplicationDbContext();
            var id = claimsPrincipal.Claims.ToList().FirstOrDefault(o => o.Type == ClaimTypes.NameIdentifier);
            if (id is not null)
            {
                return await db.Users.FirstOrDefaultAsync(o => o.Id == int.Parse(id.Value));
            }

            return null;
        }
    }
}
