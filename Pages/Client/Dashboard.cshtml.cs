using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccreditationSystem.Pages.Customer
{
    public class DashboardModel : PageModel
    {
        public string UserName { get; private set; }
        public int UserId { get; private set; }
        public string UserEmail { get; private set; }

        public IActionResult OnGet()
        {
            // Check if user is logged in
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToPage("/Auth/Login", new { returnUrl = "/Client/Home" });
            }

            // Check if user is a client
            string userRole = HttpContext.Session.GetString("UserRole")?.ToLower() ?? "";
            if (userRole != "client")
            {
                return RedirectToPage("/AccessDenied");
            }

            // Set properties from session
            UserId = userId.Value;
            UserName = HttpContext.Session.GetString("UserName") ?? "Client";
            UserEmail = HttpContext.Session.GetString("UserEmail") ?? "";

            return Page();
        }
    }
}
