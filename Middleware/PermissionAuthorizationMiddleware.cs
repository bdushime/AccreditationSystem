using AccreditationSystem.Pages.Services;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace AccreditationSystem.Middleware
{
    public class PermissionAuthorizationMiddleware
    {
        private readonly RequestDelegate _next;

        public PermissionAuthorizationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IPermissionService permissionService)
        {
            // Skip permission check for login, register, and public pages
            string path = context.Request.Path.Value.ToLower();
            if (path.StartsWith("/auth/") ||
                path.StartsWith("/public/") ||
                path.Equals("/") ||
                path.StartsWith("/lib/") ||
                path.StartsWith("/css/") ||
                path.StartsWith("/js/") ||
                path.StartsWith("/images/") ||
                path.StartsWith("/api/") ||  // API endpoints will be checked separately
                path.Equals("/accessdenied"))
            {
                await _next(context);
                return;
            }

            // Get the current user's ID
            int? userId = context.Session.GetInt32("UserId");
            if (userId == null)
            {
                // Auth middleware will handle redirecting to login
                await _next(context);
                return;
            }

            // Admin role has access to everything
            string userRole = context.Session.GetString("UserRole");
            if (userRole?.ToLower() == "admin")
            {
                await _next(context);
                return;
            }

            // Determine required permission based on path
            string requiredPermission = GetRequiredPermission(path);
            if (string.IsNullOrEmpty(requiredPermission))
            {
                // No specific permission required for this path
                await _next(context);
                return;
            }

            // Check if user has the required permission
            bool hasPermission = await permissionService.UserHasPermissionAsync(userId.Value, requiredPermission);
            if (hasPermission)
            {
                await _next(context);
                return;
            }

            // User does not have permission, show access denied page
            context.Response.Redirect("/AccessDenied");
        }

        private string GetRequiredPermission(string path)
        {
            // Map paths to required permissions
            if (path.StartsWith("/admin/"))
            {
                return "system.manage_settings";
            }

            if (path.StartsWith("/hod_dashboard/"))
            {
                if (path.Contains("applications"))
                    return "accreditation.view";
                if (path.Contains("schools"))
                    return "departments.view";
                if (path.Contains("inspections"))
                    return "inspections.view";
                if (path.Contains("claims"))
                    return "claims.view";

                // Default for HOD dashboard
                return "departments.view";
            }

            if (path.StartsWith("/applications/"))
            {
                if (path.Contains("/create"))
                    return "accreditation.create";
                if (path.Contains("/edit"))
                    return "accreditation.edit";
                if (path.Contains("/approve"))
                    return "accreditation.approve";
                if (path.Contains("/reject"))
                    return "accreditation.reject";

                return "accreditation.view";
            }

            if (path.StartsWith("/reports/"))
            {
                if (path.Contains("/create"))
                    return "reports.create";
                if (path.Contains("/export"))
                    return "reports.export";

                return "reports.view";
            }

            if (path.StartsWith("/schools/"))
            {
                if (path.Contains("/create"))
                    return "schools.create";
                if (path.Contains("/edit"))
                    return "schools.edit";
                if (path.Contains("/delete"))
                    return "schools.delete";

                return "schools.view";
            }

            // Default: no specific permission required
            return null;
        }
    }

    // Extension method to add the middleware to the app's request pipeline
    public static class PermissionAuthorizationMiddlewareExtensions
    {
        public static IApplicationBuilder UsePermissionAuthorization(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<PermissionAuthorizationMiddleware>();
        }
    }
}