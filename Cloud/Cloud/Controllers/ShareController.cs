using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cloud.Extensions;
using Cloud.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using FileShare = System.IO.FileShare;

namespace Cloud.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ShareController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ShareController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }


        [HttpGet("download")]
        public async Task<IActionResult> Download(string hash, string p)
        {
            var dbFile = _db.Shares.FirstOrDefault(o => o.ShareLink == hash);
            if (dbFile is not null)
            {
                if (dbFile.ExpiryDate < DateTime.Now)
                {
                    _db.Shares.Remove(dbFile);
                    await _db.SaveChangesAsync();
                    System.IO.File.Delete(@$"{_env.ContentRootPath}/Data/{dbFile.File}.share");
                    return NotFound("File not found");
                }

                var file = System.IO.File.Open($"{_env.ContentRootPath}/Data/{dbFile.File}.share", FileMode.Open,
                    FileAccess.Read, FileShare.ReadWrite);
                var bytes = file.ReadToEnd();
                await file.DisposeAsync();
                bytes = Crypto.DecryptByteArray(Encoding.UTF8.GetBytes(p), bytes);
                return File(bytes, "application/" + Path.GetExtension(Path.GetExtension(dbFile.File)),
                    Path.GetFileName(dbFile.File));
            }

            return NotFound("File not found");
        }
    }
}