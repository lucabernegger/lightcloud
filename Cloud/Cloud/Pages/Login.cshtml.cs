using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Cloud.Extensions;
using Cloud.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OtpNet;

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
                if (user.TotpActive)
                {
                    
                    this.HttpContext.Session.SetString("loginValid","true");
                    this.HttpContext.Session.SetInt32("userId",user.Id);
                    var hash1 = password.Sha512();
                    var serverKeyComponent1 = UserManager.GenerateRandomCryptoString();

                    HttpContext.Session.SetString("ServerFileKeyComponent", serverKeyComponent1);
                    HttpContext.Response.Cookies.Append("ClientFileKeyComponent", hash1.Encrypt(serverKeyComponent1));
                    password = null;
                    hash1 = null;
                    GC.Collect();
                    return Redirect("/Login?totp_req");

                }
                
                user.LastLogin = DateTime.Now;
                _db.Users.Update(user);
                await _db.SaveChangesAsync();

                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                identity.AddClaim(new Claim(ClaimTypes.Name, user.Name));
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
                if (user.IsAdmin) identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));

                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                var hash = password.Sha512();
                var serverKeyComponent = UserManager.GenerateRandomCryptoString();
                
                HttpContext.Session.SetString("ServerFileKeyComponent", serverKeyComponent);
                HttpContext.Response.Cookies.Append("ClientFileKeyComponent", hash.Encrypt(serverKeyComponent));
                password = null;
                hash = null;
                GC.Collect();
                return RedirectToPage("/Index");
            }

            return Redirect("/Login?error=" + UrlEncoder.Default.Encode("Password or username wrong"));
        }

        public async Task<IActionResult> OnPostCheckTotp(string totp)
        {
           
            var user =await UserManager.GetUserById(HttpContext.Session.GetInt32("userId").Value);
            
            var lastLoginAttemtp = DateTime.FromBinary(long.Parse(HttpContext.Session.GetString("lastLoginAttempt") ?? "0"));
            if (lastLoginAttemtp.AddSeconds(30) > DateTime.Now)
            {
                return Redirect("/Login?error=" + UrlEncoder.Default.Encode("Try again in 30s"));

            }
            var validLogin = HttpContext.Session.GetString("loginValid") == "true";
            var totpObj = new Totp(Base32Encoding.ToBytes(user.TotpSecret));
            var valid = totpObj.VerifyTotp(totp, out long timeStepMatched,
                new VerificationWindow(2,2));
            this.HttpContext.Session.SetString("lastLoginAttempt", DateTime.Now.ToBinary().ToString());

            if (valid && validLogin)
            {
                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                identity.AddClaim(new Claim(ClaimTypes.Name, user.Name));
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
                if (user.IsAdmin) identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                return RedirectToPage("/Index");
            }
            return Redirect("/Login?error=" + UrlEncoder.Default.Encode("Code Invalid"));
        }
    }
}