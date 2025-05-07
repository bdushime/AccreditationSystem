using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace AccreditationSystem.Middleware
{
    public class AuthRedirectMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string[] _protectedPaths = new[]
        {
            "/Admin/Schools",
            "/Admin/TrackApplication",
            "/Admin/SubmitClaim",
            "/Admin/TVETTrades",
            "/Admin/GeneralCombination",
            "/Admin/NewSchoolRegistration",
            "/Admin/AccreditedSchool"
        };

        public AuthRedirectMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string path = context.Request.Path.Value.TrimEnd('/');

            // Check if the path is one of the protected paths and the user is not authenticated
            if (Array.Exists(_protectedPaths, p => path.Equals(p, StringComparison.OrdinalIgnoreCase))
                && context.Session.GetInt32("UserId") == null)
            {
                // Redirect to login page with return URL
                string returnUrl = System.Net.WebUtility.UrlEncode(context.Request.Path.Value);
                context.Response.Redirect($"/Auth/Login?ReturnUrl={returnUrl}");
                return;
            }

            // Continue processing the request
            await _next(context);
        }
    }

    // Extension method to make it easier to add the middleware to the pipeline
    public static class AuthRedirectMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthRedirectMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthRedirectMiddleware>();
        }
    }
}