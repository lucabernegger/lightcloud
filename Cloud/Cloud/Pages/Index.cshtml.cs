using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Hosting;

namespace Cloud.Pages
{
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IHostEnvironment _env;

        [BindProperty(SupportsGet = true)] public string Path { get; set; } = "" ; 

        public IndexModel(ILogger<IndexModel> logger,IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        /*public async Task OnPostUpload()
        {
            var user = await User.GetUser();
            foreach (var file in UploadedFiles)
            {
                string targetFileName = $"{_env.ContentRootPath}/Data/{user.Id}/{Path}/{file.FileName}";
                using (var stream = new FileStream(targetFileName, FileMode.Create))
                {
                   file.CopyToAsync(stream);
                }
            }

        }  */
        public async Task OnPostCreateFolder(string folderName)
        {
            var user = await User.GetUser();
            string targetFolderName = $"{_env.ContentRootPath}/Data/{user.Id}/{Path}/{folderName}";
            Directory.CreateDirectory(targetFolderName);
        }
        public async Task<IActionResult> OnGetDownload(string path)
        {
            var user = await User.GetUser();
            var file = System.IO.File.Open(@$"{_env.ContentRootPath}/Data/{user.Id}/{path}", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return File(file, "application/" + System.IO.Path.GetExtension(System.IO.Path.GetExtension(path)), System.IO.Path.GetFileName(path));

        }

        public async Task<IActionResult> OnGetDeleteFile(string fileName)
        {
            var user = await User.GetUser();
            string targetFileName = $"{_env.ContentRootPath}/Data/{user.Id}/{Path}";
            System.IO.File.Delete(targetFileName);
            return Redirect($"/Index");
        }
        public async Task<IActionResult> OnGetDeleteFolder(string fileName)
        {
            var user = await User.GetUser();
            string targetFileName = $"{_env.ContentRootPath}/Data/{user.Id}/{Path}";
            System.IO.Directory.Delete(targetFileName,true);
            return Redirect($"/Index");

        }

        public PartialViewResult OnGetFilesPartial(string path)
        {
            Path = path;
            return Partial("Shared/_FilesTable", this);
        }

    }
}
