using System;
using System.IO;
using System.Threading.Tasks;
using Cloud.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;

namespace Cloud.Pages.Admin
{
    [Authorize(Roles = "Admin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IHostEnvironment _env;

        public IndexModel(ApplicationDbContext db, IHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [BindProperty] public AddUserData Data { get; set; }

        public async Task<IActionResult> OnPostAddUser()
        {
            var pw = UserManager.GenerateRandomPassword();
            var hashed = UserManager.GenerateHashAndSalt(pw);
            var user = new User
            {
                IsAdmin = Data.Admin == 1,
                MaxFileBytes = FileMethods.GigabyteToByte(Data.MaxStorage),
                Name = Data.Name,
                Password = hashed[0],
                Salt = hashed[1],
                LastLogin = DateTime.MinValue
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            Directory.CreateDirectory(_env.ContentRootPath + "/Data/" + user.Id + "/");
            return Redirect("/Admin/Index?addUser_success=" + pw);
        }

        public async Task<IActionResult> OnGetDeleteUser(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user is not null)
            {
                _db.Users.Remove(user);
                await _db.SaveChangesAsync();
                Directory.Delete(_env.ContentRootPath + "/Data/" + user.Id + "/", true);
            }

            return Page();
        }
    }

    public class AddUserData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Admin { get; set; }
        public int MaxStorage { get; set; }
    }
}