using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Cloud.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                identity.AddClaim(new Claim(ClaimTypes.Name, user.Name));
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
                if (user.IsAdmin) identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));

                var principal = new ClaimsPrincipal(identity);
                var authenticationProperties = new AuthenticationProperties
                {
                    AllowRefresh = true,
                    ExpiresUtc = DateTimeOffset.Now.AddMinutes(1),
                    IsPersistent = false,
                    IssuedUtc = DateTimeOffset.Now
                };
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,authenticationProperties);
                var hash = UserManager.Sha512(password);
                var serverKeyComponent = UserManager.GenerateRandomCryptoString();
                Debug.WriteLine("Client: " + hash);
                Debug.WriteLine("Server: " + serverKeyComponent);
                HttpContext.Session.SetString("ServerFileKeyComponent", serverKeyComponent);
                HttpContext.Response.Cookies.Append("ClientFileKeyComponent", UserManager.Encrypt(hash,serverKeyComponent));
                return RedirectToPage("/Index");
            }

            return Redirect("/Index?" + UrlEncoder.Default.Encode("Email oder Passwort falsch!"));
        }
    }
}