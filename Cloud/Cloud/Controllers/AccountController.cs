using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Cloud.Controllers
{
    [ApiController]
    [ResponseCache(NoStore = true, Duration = 0, VaryByHeader = "*")]
    public class AccountController : Controller
    {
        [HttpGet("/Logout")]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Remove("ServerFileKeyComponent");
            await HttpContext.SignOutAsync();
            return RedirectToPage("/Index");
        }

    }
}