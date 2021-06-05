using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Cloud.Models;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Cloud
{
    public static class FileMethods
    {
        public static String BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1000)));
            double num = Math.Round(bytes / Math.Pow(1000, place), 1);
            return (Math.Sign(byteCount) * num) + suf[place];
        }
        public static double ByteToGigabyte(long bytes)
        {
            return Math.Round((double)bytes / 1000000000,2);
        }
        public static long GigabyteToByte(long bytes)
        {
            return bytes * 1000000000;
        }
        public static long GetSizeOfDirectory(this DirectoryInfo d)
        {
            long size = 0;
            // Add file sizes.
            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis)
            {
                size += fi.Length;
            }
            // Add subdirectory sizes.
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                size += GetSizeOfDirectory(di);
            }
            return size;
        }
        public static List<FileInfo> DirSearch(string sDir)
        {
            var list = new List<FileInfo>();
            try
            {
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    foreach (string f in Directory.GetFiles(d))
                    {
                       list.Add(new(d));
                    }
                    DirSearch(d);
                }
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }

            return list;
        }

        public static string Sha256(string text)
        {
            var crypt = new SHA256Managed();
            string hash = String.Empty;
            byte[] crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(text));
            foreach (byte theByte in crypto)
            {
                hash += theByte.ToString("x2");
            }
            return hash;
        }

        public static bool IsFileShared(string path,int userid)
        {
            using var db = new ApplicationDbContext();
            string p = @$"{userid}/{path}";
            return db.Shares.Any(o => o.File == p);
        }
    }
}
