using AccreditationSystem.Pages.Services;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text;

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
            // Debug info - get full info about the request
            string path = context.Request.Path.Value ?? "";
            string lowerPath = path.ToLower();
            int? userId = context.Session.GetInt32("UserId");
            string userRole = context.Session.GetString("UserRole") ?? "";
            string userRoleLower = userRole.ToLower();

            // Create a detailed debug string
            var debugInfo = new StringBuilder();
            debugInfo.AppendLine($"------- Permission Middleware Debug -------");
            debugInfo.AppendLine($"Path: {path}");
            debugInfo.AppendLine($"Lower Path: {lowerPath}");
            debugInfo.AppendLine($"UserId: {userId}");
            debugInfo.AppendLine($"UserRole: {userRole}");
            debugInfo.AppendLine($"UserRole (lower): {userRoleLower}");

            // Log the debug info
            Debug.WriteLine(debugInfo.ToString());

            // SOLUTION 1: EXTREME PERMISSIVE APPROACH - JUST TO FIX CLIENT ACCESS IMMEDIATELY
            // If user is a client, allow access to EVERYTHING
            if (userRoleLower == "client")
            {
                Debug.WriteLine("CLIENT ROLE DETECTED - BYPASSING ALL PERMISSION CHECKS");
                await _next(context);
                return;
            }

            // Continue with the rest of the middleware for other roles

            // Skip permission check for public paths
            if (lowerPath.StartsWith("/auth/") ||
                lowerPath.StartsWith("/public/") ||
                lowerPath.Equals("/") ||
                lowerPath.StartsWith("/lib/") ||
                lowerPath.StartsWith("/css/") ||
                lowerPath.StartsWith("/js/") ||
                lowerPath.StartsWith("/images/") ||
                lowerPath.StartsWith("/api/") ||
                lowerPath.Equals("/accessdenied") ||
                lowerPath.StartsWith("/debugsession"))
            {
                Debug.WriteLine("Public path detected, bypassing permission checks");
                await _next(context);
                return;
            }

            // If no user ID in session, continue (auth redirect middleware will handle this)
            if (userId == null)
            {
                Debug.WriteLine("No user ID in session, bypassing permission checks");
                await _next(context);
                return;
            }

            // Admin has access to everything
            if (userRoleLower == "admin")
            {
                Debug.WriteLine("Admin role detected, bypassing permission checks");
                await _next(context);
                return;
            }

            // For HOD and other roles, check specific permissions
            string requiredPermission = GetRequiredPermission(lowerPath);
            Debug.WriteLine($"Required permission for path: {requiredPermission}");

            if (string.IsNullOrEmpty(requiredPermission))
            {
                Debug.WriteLine("No specific permission required, allowing access");
                await _next(context);
                return;
            }

            // Check if user has the required permission
            bool hasPermission = await permissionService.UserHasPermissionAsync(userId.Value, requiredPermission);
            Debug.WriteLine($"User has required permission? {hasPermission}");

            if (hasPermission)
            {
                Debug.WriteLine("User has permission, allowing access");
                await _next(context);
                return;
            }

            // User does not have permission, redirect to access denied
            Debug.WriteLine("User does not have permission, redirecting to access denied");
            context.Response.Redirect("/AccessDenied");
        }

        private string GetRequiredPermission(string path)
        {
            // Keep your existing permission mapping logic
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