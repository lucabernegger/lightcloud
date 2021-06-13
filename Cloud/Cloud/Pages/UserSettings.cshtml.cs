using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Cloud.Extensions;
using Cloud.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cloud.Pages
{
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class UserSettingsModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public UserSettingsModel(ApplicationDbContext db)
        {
            _db = db;
        }
        [BindProperty] public SetPasswordData SetPasswordData { get; set; }

        public async Task<IActionResult> OnPostSetPassword()
        {
            var user = await User.GetUser();
            if (UserManager.IsValid(user.Name, SetPasswordData.OldPassword) &&
                SetPasswordData.NewPassword.Equals(SetPasswordData.NewPasswordRepeat))
            {
              
                var hashedPassword = UserManager.GenerateHashAndSalt(SetPasswordData.NewPassword);
                user.Password = hashedPassword[0];
                user.Salt = hashedPassword[1];
                var text = user.FilePassword.Decrypt(SetPasswordData.OldPassword.Sha512());
                user.FilePassword = text.Encrypt(SetPasswordData.NewPassword.Sha512());
                _db.Users.Update(user);
                await _db.SaveChangesAsync();
            }

            await HttpContext.SignOutAsync();
            return Redirect("/Login");
        }
    }

    public class SetPasswordData
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        public string NewPasswordRepeat { get; set; }
    }
}