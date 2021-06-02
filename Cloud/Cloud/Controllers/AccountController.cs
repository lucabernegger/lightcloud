using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

namespace Cloud.Controllers
{
    [ApiController]
    [ResponseCache(NoStore = true, Duration = 0, VaryByHeader = "*")]
    public class AccountController : Controller
    {
        [HttpGet("/Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToPage("/Index");
        }
    }
}
