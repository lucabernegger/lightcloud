using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Cloud.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cloud.Pages
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public LoginModel(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task<ActionResult> OnPostLogin(string email, string password)
        {
            if (UserManager.IsValid(email, password))
            {
                var user = UserManager.GetUserFromName(email);
                user.LastLogin = DateTime.Now;
                _db.Users.Update(user);
                await _db.SaveChangesAsync();
                if (user is null)
                {
                    return Page();
                }

                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                identity.AddClaim(new Claim(ClaimTypes.Name, user.Name));
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
                if (user.IsAdmin)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
                }

                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                return RedirectToPage("/Index");
            }

            return Redirect("/Index?" + UrlEncoder.Default.Encode("Email oder Passwort falsch!")); ;
        }
    }
}
