using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cloud.Migrations;
using Cloud.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cloud.Pages
{
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class UserSettingsModel : PageModel
    {
        [BindProperty]
        public SetPasswordData SetPasswordData { get; set; }

        private readonly ApplicationDbContext _db;
        public UserSettingsModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> OnPostSetPassword()
        {
            var user = await User.GetUser();
            if (UserManager.IsValid(user.Name, SetPasswordData.OldPassword) && SetPasswordData.NewPassword.Equals(SetPasswordData.NewPasswordRepeat))
            {
                await UserManager.SetNewPassword(user.Id,SetPasswordData.NewPassword);
            }

            return Page();
        }
    }

    public class SetPasswordData
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        public string NewPasswordRepeat { get; set; }
    }
}
