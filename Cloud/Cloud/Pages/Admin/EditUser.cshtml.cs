using System.Collections.Generic;
using System.Threading.Tasks;
using Cloud.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Cloud.Pages.Admin
{
    public class EditUserModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public EditUserModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)] public int Id { get; set; }
        [BindProperty] public EditUserData Data { get; set; }

        public SelectList Options { get; set; }

        public async Task<IActionResult> OnPostUpdateUser()
        {
            var user = await UserManager.GetUserById(Data.Id);
            if (user is not null)
            {
                user.Name = Data.Name;
                user.IsAdmin = Data.Admin == 1;
                user.MaxFileBytes = FileMethods.GigabyteToByte(Data.MaxStorage);
                _db.Users.Update(user);
                await _db.SaveChangesAsync();
            }

            

            return Redirect("/Admin/Index");
        }

        public async Task OnGetAsync()
        {
            var user = await UserManager.GetUserById(Id);
            var dict = new Dictionary<int, string> {{0, "User"}, {1, "Admin"}};
            Options = new SelectList(dict, "Key", "Value", user.IsAdmin ? 1 : 0);
        }
    }

    public class EditUserData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Admin { get; set; }
        public int MaxStorage { get; set; }
    }
}