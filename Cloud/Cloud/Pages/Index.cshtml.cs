using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Cloud.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;
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
            var fileExtensionToOpenText = new[] {".txt", ".json", ".lua", ".cs", ".yml"};
            var user = await User.GetUser();
            var file = System.IO.File.Open(@$"{_env.ContentRootPath}/Data/{user.Id}/{path}", FileMode.Open,
                FileAccess.Read, FileShare.ReadWrite);
            if (fileExtensionToOpenText.Contains(System.IO.Path.GetExtension(path)))
                return Content(await System.IO.File.ReadAllTextAsync(@$"{_env.ContentRootPath}/Data/{user.Id}/{path}"));
            return File(file, "application/" + System.IO.Path.GetExtension(System.IO.Path.GetExtension(path)),
                System.IO.Path.GetFileName(path));
        }

        public async Task<IActionResult> OnGetDeleteFile()
        {
            var user = await User.GetUser();
            var targetFileName = $"{_env.ContentRootPath}/Data/{user.Id}/{Path}";
            Debug.WriteLine(targetFileName);
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
    }
}