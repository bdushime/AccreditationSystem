using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccreditationSystem.Pages
{

    public class DebugSessionModel : PageModel
    {
        public string UserId { get; set; }
        public string UserRole { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string CurrentPath { get; set; }
        public string RoleLowercase { get; set; }

        public void OnGet()
        {
            UserId = HttpContext.Session.GetInt32("UserId")?.ToString() ?? "Not set";
            UserRole = HttpContext.Session.GetString("UserRole") ?? "Not set";
            UserName = HttpContext.Session.GetString("UserName") ?? "Not set";
            UserEmail = HttpContext.Session.GetString("UserEmail") ?? "Not set";
            CurrentPath = HttpContext.Request.Path.Value;
            RoleLowercase = UserRole.ToLower();
        }
    }
}
