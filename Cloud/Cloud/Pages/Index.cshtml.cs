using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Cloud.Extensions;
using Cloud.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

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
            foreach (var dbFile in _db.Files.Where(o => o.Path == targetFolderName && o.UserId == user.Id))
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
            var key = user.FilePassword.Decrypt(clientComponent.Decrypt(serverComponent));
            var file = System.IO.File.Open(@$"{_env.ContentRootPath}/Data/{user.Id}/{path}", FileMode.Open,
                FileAccess.Read);
            var bytesToDec = file.ReadToEnd();
            var bytes = Crypto.DecryptByteArray(Encoding.UTF8.GetBytes(key), bytesToDec);
            key = null;
            clientComponent = null;
            serverComponent = null;
            await file.DisposeAsync();
            GC.Collect();
            return File(bytes, "application/" + System.IO.Path.GetExtension(System.IO.Path.GetExtension(path)),
                System.IO.Path.GetFileName(path));
        }

        public async Task<IActionResult> OnGetDeleteFile()
        {
            var user = await User.GetUser();
            var targetFileName = $"{_env.ContentRootPath}/Data/{user.Id}/{Path}";
            foreach (var file in _db.Files.Where(o => o.UserId == user.Id))
            {
                var f = file.Path + file.Filename;
                if (f == targetFileName)
                {
                    _db.Remove(file);
                    var dbfile = _db.Shares.FirstOrDefault(o => o.File == @$"{user.Id}/" + file.Path);
                    if (dbfile is not null)
                    {
                        _db.Shares.Remove(dbfile);
                        System.IO.File.Delete(@$"{_env.ContentRootPath}/Data/{dbfile.File}.share");
                        await _db.SaveChangesAsync();
                    }
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

        public async Task<IActionResult> OnPostShareFile(string path, DateTime expiryDate, CancellationToken cs)
        {
            var user = await User.GetUser();
            if (!FileMethods.IsFileShared(path, user.Id))
            {
                var clientComponent = this.HttpContext.Request.Cookies["ClientFileKeyComponent"];
                var serverComponent = this.HttpContext.Session.GetString("ServerFileKeyComponent");
                var key = user.FilePassword.Decrypt(clientComponent.Decrypt(serverComponent));
                var file = System.IO.File.Open(@$"{_env.ContentRootPath}/Data/{user.Id}/{path}", FileMode.Open,
                    FileAccess.Read);
                var bytesToDec = file.ReadToEnd();
                var bytes = Crypto.DecryptByteArray(Encoding.UTF8.GetBytes(key), bytesToDec);
                var sKey = UserManager.GenerateRandomCryptoString(16);
                bytes = Crypto.EncryptByteArray(Encoding.UTF8.GetBytes(sKey), bytes);
                await System.IO.File.WriteAllBytesAsync(@$"{_env.ContentRootPath}/Data/{user.Id}/{path}.share", bytes, cs);
                
                var share = new Models.FileShare
                {
                    ExpiryDate = expiryDate,
                    File = @$"{user.Id}/" + path,
                    ShareLink = FileMethods.Sha256(path),
                    Key = sKey.Encrypt(key)
                };
                Debug.WriteLine("KEYDEC: " + sKey);
                Debug.WriteLine("KEYENC: " + share.Key);
                _db.Shares.Add(share);
                await _db.SaveChangesAsync();
                key = null;
                clientComponent = null;
                serverComponent = null;
                await file.DisposeAsync();
                GC.Collect();
                string link = (Startup.Settings.BaseDomain + "Share/download?hash=" + UrlEncoder.Default.Encode(share.ShareLink)+"&p="+UrlEncoder.Default.Encode(sKey));
                return Redirect($"/Index?download_link={link}");
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
                System.IO.File.Delete(@$"{_env.ContentRootPath}/Data/{dbfile.File}.share");
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
                if (dbfile is null)
                    continue;

                var targetFileName = $"{dbfile.Path}{dbfile.Filename}";
                var dbshare = _db.Shares.FirstOrDefault(o => o.File == @$"{user.Id}/" + dbfile.Path);
                if (dbshare is not null)
                {
                    _db.Shares.Remove(dbshare);
                    System.IO.File.Delete(@$"{_env.ContentRootPath}/Data/{dbshare.File}.share");
                    await _db.SaveChangesAsync();
                }
                _db.Remove(dbfile);
                System.IO.File.Delete(targetFileName);
            }
            await _db.SaveChangesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDownloadMarked(string markedJson)
        {
            var user = await User.GetUser();
            var ids = JsonConvert.DeserializeObject<int[]>(markedJson);
            var clientComponent = this.HttpContext.Request.Cookies["ClientFileKeyComponent"];
            var serverComponent = this.HttpContext.Session.GetString("ServerFileKeyComponent");
            var key = user.FilePassword.Decrypt(clientComponent.Decrypt(serverComponent));
            await using var outStream = new MemoryStream();
            using (var archive = new ZipArchive(outStream, ZipArchiveMode.Create, true))
            {
                if (ids != null)
                    foreach (var id in ids)
                    {
                        var dbfile = _db.Files.FirstOrDefault(o => o.Id == id && o.UserId == user.Id);
                        if (dbfile is null)
                            continue;

                        var targetFileName = $"{dbfile.Path}{dbfile.Filename}";
                        var fileInArchive = archive.CreateEntry(dbfile.Filename, CompressionLevel.Optimal);
                        await using var entryStream = fileInArchive.Open();
                        var bytesToDec = await System.IO.File.ReadAllBytesAsync(targetFileName);
                        var bytes = Crypto.DecryptByteArray(Encoding.UTF8.GetBytes(key), bytesToDec);
                        await using var stream = new MemoryStream(bytes);
                        await using var fileToCompressStream = stream;
                        await fileToCompressStream.CopyToAsync(entryStream);
                        await stream.DisposeAsync();
                    }
            }
            var compressedBytes = outStream.ToArray();

            key = null;
            clientComponent = null;
            serverComponent = null;
            GC.Collect();
            return File(compressedBytes, "application/zip", $"{DateTime.Now.ToShortDateString()}.zip");
        }
    }
}