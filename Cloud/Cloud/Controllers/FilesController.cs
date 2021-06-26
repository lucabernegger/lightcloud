using System;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Cloud.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Cloud.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class FilesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public FilesController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("GetFiles")]
        public async Task<List<DbFile>> GetFiles(string path)
        {
            var user = await User.GetUser();
            Debug.WriteLine("UID: " + user.Id);
            return _db.Files.Where(o => o.UserId == user.Id && o.Path == path).ToList();
        }
        [HttpGet("Preview")]
        public string GetPreview(int id)
        {
            if (Startup.PreviewCache.ContainsKey(id))
            {
                var prev = Startup.PreviewCache[id];
                Startup.PreviewCache.Remove(id);
                return prev;
            }

            return String.Empty;

        }
    }
}
