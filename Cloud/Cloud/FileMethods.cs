using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Cloud.Extensions;
using Cloud.Migrations;
using Cloud.Models;
using Microsoft.AspNetCore.Http;

namespace Cloud
{
    public static class FileMethods
    {
        public static string PrettyJson(string unPrettyJson)
        {
            var options = new JsonSerializerOptions()
            {
                WriteIndented = true
            };

            var jsonElement = JsonSerializer.Deserialize<JsonElement>(unPrettyJson);

            return JsonSerializer.Serialize(jsonElement, options);
        }
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

        public static string GetSharedLink(string path, User user,HttpContext ctx)
        {
            using var db = new ApplicationDbContext();
            var p = @$"{user.Id}/{path}";
            var clientComponent = ctx.Request.Cookies["ClientFileKeyComponent"];
            var serverComponent = ctx.Session.GetString("ServerFileKeyComponent");
            var key = user.FilePassword.Decrypt(clientComponent.Decrypt(serverComponent));
            var dbshare = db.Shares.FirstOrDefault(o => o.File == p);
            return Startup.Settings.BaseDomain + "Share/download?hash=" + UrlEncoder.Default.Encode(dbshare?.ShareLink)+"&p="+ UrlEncoder.Default.Encode(dbshare.Key.Decrypt(key));
        }
        public static string GetRelativePath(string fromPath, string toPath)
        {
            if (string.IsNullOrEmpty(fromPath))
            {
                throw new ArgumentNullException("fromPath");
            }

            if (string.IsNullOrEmpty(toPath))
            {
                throw new ArgumentNullException("toPath");
            }

            Uri fromUri = new Uri(AppendDirectorySeparatorChar(fromPath));
            Uri toUri = new Uri(AppendDirectorySeparatorChar(toPath));

            if (fromUri.Scheme != toUri.Scheme)
            {
                return toPath;
            }

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (string.Equals(toUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
            {
                //relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }

        private static string AppendDirectorySeparatorChar(string path)
        {
            // Append a slash only if the path is a directory and does not have a slash.
            if (!Path.HasExtension(path) &&
                !path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                return path + Path.DirectorySeparatorChar;
            }

            return path;
        }
        public static Byte[] BitmapToBytesCode(Bitmap image)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }
    }
}