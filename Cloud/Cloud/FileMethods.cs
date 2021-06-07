using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Cloud.Models;

namespace Cloud
{
    public static class FileMethods
    {
        public static string BytesToString(long byteCount)
        {
            string[] suf = {"B", "KB", "MB", "GB", "TB", "PB", "EB"}; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            var bytes = Math.Abs(byteCount);
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1000)));
            var num = Math.Round(bytes / Math.Pow(1000, place), 1);
            return Math.Sign(byteCount) * num + suf[place];
        }

        public static double ByteToGigabyte(long bytes)
        {
            return Math.Round((double) bytes / 1000000000, 2);
        }

        public static long GigabyteToByte(long bytes)
        {
            return bytes * 1000000000;
        }

        public static long GetSizeOfDirectory(this DirectoryInfo d)
        {
            long size = 0;
            // Add file sizes.
            var fis = d.GetFiles();
            foreach (var fi in fis) size += fi.Length;
            // Add subdirectory sizes.
            var dis = d.GetDirectories();
            foreach (var di in dis) size += GetSizeOfDirectory(di);
            return size;
        }
        
        public static string Sha256(string text)
        {
            var crypt = new SHA256Managed();
            var hash = string.Empty;
            var crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(text));
            foreach (var theByte in crypto) hash += theByte.ToString("x2");
            return hash;
        }

        public static bool IsFileShared(string path, int userid)
        {
            using var db = new ApplicationDbContext();
            var p = @$"{userid}/{path}";
            return db.Shares.Any(o => o.File == p);
        }

        public static string GetSharedLink(string path, int userid)
        {
            using var db = new ApplicationDbContext();
            var p = @$"{userid}/{path}";
            return Startup.Settings.BaseDomain + "Share/" + db.Shares.FirstOrDefault(o => o.File == p)?.ShareLink;
        }
    }
}