using System;
using System.Threading.Tasks;
using Cloud.Extensions;
using Cloud.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OtpNet;
using QRCoder;

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
        [BindProperty] public SettingsData SettingsData { get; set; }
        [BindProperty(SupportsGet = true)] public string TotpSecret { get; set; }

        public void OnGet()
        {
            if (TotpSecret is not null)
            {
                var qrGen = new QRCodeGenerator();
                var qrCodeData = qrGen.CreateQrCode("otpauth://totp/Lightcloud?secret=" + TotpSecret,
                    QRCodeGenerator.ECCLevel.Q);
                var qrCode = new QRCode(qrCodeData);
                var qrCodeImage = qrCode.GetGraphic(20);
                ViewData.Add("qr", Convert.ToBase64String(FileMethods.BitmapToBytesCode(qrCodeImage)));
            }
        }

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

        public async Task<IActionResult> OnPostSetSettings()
        {
            var user = await User.GetUser();
            user.ShowKnownFileExtensions = SettingsData.ShowFileExtensions;
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostActivateTotp()
        {
            var user = await User.GetUser();
            var secret = KeyGeneration.GenerateRandomKey(20);
            var base32Secret = Base32Encoding.ToString(secret);
            user.TotpActive = true;
            user.TotpSecret = base32Secret;
            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            return Redirect("/UserSettings?TotpSecret=" + base32Secret);
        }

        public async Task<IActionResult> OnPostDisableTotp()
        {
            var user = await User.GetUser();
            user.TotpActive = false;
            user.TotpSecret = string.Empty;
            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            return Page();
        }
    }

    public class SetPasswordData
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        public string NewPasswordRepeat { get; set; }
    }

    public class SettingsData
    {
        public bool ShowFileExtensions { get; set; }
    }
}