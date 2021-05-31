using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Cloud.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        [BindProperty(SupportsGet = true)] public string Path { get; set; } = "Data"; //TODO: Hardcoded path

        [BindProperty]
        public IFormFile UploadedFile { get; set; }
        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public async Task OnPostUpload()
        {
            string targetFileName = $"{Path}/{UploadedFile.FileName}"; //TODO: Hardcoded path

            using (var stream = new FileStream(targetFileName, FileMode.Create))
            {
                await UploadedFile.CopyToAsync(stream);
            }
        }
    }
}
