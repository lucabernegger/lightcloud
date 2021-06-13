using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cloud.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Cloud.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class FilesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public FilesController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("GetFiles")]
        public async Task<IEnumerable<DbFile>> GetFiles(string path)
        {
            var user = await User.GetUser();
            return _db.Files.Where(o => o.UserId == user.Id && o.Path == path).AsEnumerable();
        }

    }
}
