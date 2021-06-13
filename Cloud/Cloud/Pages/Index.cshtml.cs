using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Cloud.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using FileShare = System.IO.FileShare;

namespace Cloud.Pages
{
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IHostEnvironment _env;

        public IndexModel(IHostEnvironment env, ApplicationDbContext db)
        {
            _env = env;
            _db = db;
        }

        [BindProperty(SupportsGet = true)] public string Path { get; set; } = "";


        public async Task OnPostCreateFolder(string folderName)
        {
            var user = await User.GetUser();
            var targetFolderName = $"{_env.ContentRootPath}/Data/{user.Id}/{Path}{folderName}";
            Directory.CreateDirectory(targetFolderName);
            foreach (var dbFile in _db.Files.Where(o=>o.Path == targetFolderName && o.UserId == user.Id))
            {
                _db.Remove(dbFile);
            }

            await _db.SaveChangesAsync();
        }

        public async Task<IActionResult> OnGetDownload(string path)
        {
            var user = await User.GetUser();
            var clientComponent = this.HttpContext.Request.Cookies["ClientFileKeyComponent"];
            var serverComponent = this.HttpContext.Session.GetString("ServerFileKeyComponent");
            var key = UserManager.Decrypt(user.FilePassword, UserManager.Decrypt(clientComponent, serverComponent));
            var file = System.IO.File.Open(@$"{_env.ContentRootPath}/Data/{user.Id}/{path}", FileMode.Open,
                FileAccess.Read);
            var bytesToDec = Startup.ReadToEnd(file);

            return File(Crypto.DecryptByteArray(Encoding.UTF8.GetBytes(key),bytesToDec), "application/" + System.IO.Path.GetExtension(System.IO.Path.GetExtension(path)),
                System.IO.Path.GetFileName(path)); 
        }

        public async Task<IActionResult> OnGetDeleteFile()
        {
            var user = await User.GetUser();
            var targetFileName = $"{_env.ContentRootPath}/Data/{user.Id}/{Path}";
            foreach (var file in _db.Files.Where(o=>o.UserId == user.Id))
            {
                var f = file.Path + file.Filename;
                if (f == targetFileName)
                {
                    _db.Remove(file);
                }
          
            }
            _db.SaveChanges();

            System.IO.File.Delete(targetFileName);
            return Redirect("/Index");
        }

        public async Task<IActionResult> OnGetDeleteFolder()
        {
            var user = await User.GetUser();
            var targetFileName = $"{_env.ContentRootPath}/Data/{user.Id}/{Path}";
            Directory.Delete(targetFileName, true);
            return Redirect("/Index");
        }

        public async Task<IActionResult> OnPostShareFile(string path, DateTime expiryDate)
        {
            var user = await User.GetUser();
            if (!FileMethods.IsFileShared(path, user.Id))
            {
                var share = new Models.FileShare
                {
                    ExpiryDate = expiryDate,
                    File = @$"{user.Id}/" + path,
                    ShareLink = FileMethods.Sha256(path)
                };
                _db.Shares.Add(share);
                await _db.SaveChangesAsync();
                return Redirect(
                    $"/Index?download_link={UrlEncoder.Default.Encode(Startup.Settings.BaseDomain + "Share/" + share.ShareLink)}");
            }

            return Page();
        }

        public async Task<IActionResult> OnGetStopShare()
        {
            var user = await User.GetUser();
            var p = @$"{user.Id}/{Path}";
            var dbfile = _db.Shares.FirstOrDefault(o => o.File == p);
            if (dbfile is not null)
            {
                _db.Shares.Remove(dbfile);
                await _db.SaveChangesAsync();
            }

            return Redirect("/Index");
        }

        public PartialViewResult OnGetFilesPartial(string path)
        {
            Path = path;
            return Partial("Shared/_FilesTable", this);
        }

        public async Task<IActionResult> OnPostDeleteMarked(string markedJson)
        {
            var user = await User.GetUser();
            var ids = JsonConvert.DeserializeObject<int[]>(markedJson);
            foreach (var id in ids)
            {
                var dbfile = _db.Files.FirstOrDefault(o => o.Id == id && o.UserId == user.Id);
                if(dbfile is null)
                    continue;

                var targetFileName = $"{dbfile.Path}{dbfile.Filename}";
                _db.Remove(dbfile);
                System.IO.File.Delete(targetFileName);
            }
            _db.SaveChanges();
            return Page();
        }

        public async Task<IActionResult> OnPostDownloadMarked(string markedJson)
        {
            Debug.WriteLine(markedJson);
            var user = await User.GetUser();
            var ids = JsonConvert.DeserializeObject<int[]>(markedJson);
            await using var outStream = new MemoryStream();
            using (var archive = new ZipArchive(outStream, ZipArchiveMode.Create, true))
            {
                foreach (var id in ids)
                {
                    var dbfile = _db.Files.FirstOrDefault(o => o.Id == id && o.UserId == user.Id);
                    if (dbfile is null)
                        continue;

                    var targetFileName = $"{dbfile.Path}{dbfile.Filename}";
                    var fileInArchive = archive.CreateEntry(dbfile.Filename, CompressionLevel.Optimal);
                    await using var entryStream = fileInArchive.Open();
                    await using var fileToCompressStream = new MemoryStream(System.IO.File.ReadAllBytes(targetFileName));
                    await fileToCompressStream.CopyToAsync(entryStream);
                }

            }

            var compressedBytes = outStream.ToArray();
            return File(compressedBytes, "application/zip", $"{DateTime.Now.ToShortDateString()}.zip");
        }
    }
}