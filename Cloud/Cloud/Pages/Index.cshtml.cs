﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Cloud.Pages
{
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IHostEnvironment _env;

            [BindProperty(SupportsGet = true)] public string Path { get; set; } = "" ; //TODO: Hardcoded path

        [BindProperty]
        public IFormFile UploadedFile { get; set; }

        public IndexModel(ILogger<IndexModel> logger,IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public async Task OnPostUpload()
        {
            var user = await User.GetUser();
            string targetFileName = $"{_env.ContentRootPath}/Data/{user.Id}/{Path}/{UploadedFile.FileName}"; 

            using (var stream = new FileStream(targetFileName, FileMode.Create))
            {
                await UploadedFile.CopyToAsync(stream);
            }
        }

        public async Task<IActionResult> OnGetDownload(string path)
        {
            var user = await User.GetUser();
            var file = System.IO.File.Open(@$"{_env.ContentRootPath}/Data/{user.Id}/{path}", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return File(file, "application/" + System.IO.Path.GetExtension(System.IO.Path.GetExtension(path)), System.IO.Path.GetFileName(path));

        }
    }
}
