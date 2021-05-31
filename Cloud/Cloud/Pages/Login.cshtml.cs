using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
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
        public async Task<ActionResult> OnPostLogin(string email, string password)
        {
            Debug.WriteLine("1");
            if (UserManager.IsValid(email, password))
            {
                var user = UserManager.GetUserFromName(email);
                if (user is null)
                {
                    Debug.WriteLine("2");

                    return Page();
                }

                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                identity.AddClaim(new Claim(ClaimTypes.Name, user.Name));
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));

                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                Debug.WriteLine("55");
                return RedirectToPage("/Index");
            }

            return Redirect("/Index?" + UrlEncoder.Default.Encode("Email oder Passwort falsch!")); ;
        }
    }
}
