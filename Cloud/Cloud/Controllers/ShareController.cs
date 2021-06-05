using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cloud.Models;
using Microsoft.AspNetCore.Hosting;

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

       

        [HttpGet("{hash}")]
        public IActionResult Download(string hash)
        {
            var dbFile = _db.Shares.FirstOrDefault(o => o.ShareLink == hash);
            if (dbFile is not null)
            {
                if (dbFile.ExpiryDate < DateTime.Now)
                {
                    _db.Shares.Remove(dbFile);
                    _db.SaveChanges();
                    return NotFound("File not found");
                }
                var file = System.IO.File.Open($"{_env.ContentRootPath}/Data/{dbFile.File}", FileMode.Open, FileAccess.Read, System.IO.FileShare.ReadWrite);

                return File(file, "application/" + System.IO.Path.GetExtension(System.IO.Path.GetExtension(dbFile.File)), System.IO.Path.GetFileName(dbFile.File));

            }

            return NotFound("File not found");

        }
    }
}
