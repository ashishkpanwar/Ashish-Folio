using Ashish_Backend_Folio.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Ashish_Backend_Folio.Middlewares
{
    public class JwtValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var sub = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                if (!string.IsNullOrEmpty(sub))
                {
                    var user = await userManager.FindByIdAsync(sub);

                    // Check lockout or inactive status
                    if (user == null || (user.LockoutEnabled && await userManager.IsLockedOutAsync(user)))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("User not active");
                        return; // short-circuit pipeline
                    }
                }
            }

            await _next(context);
        }
    }

    public static class JwtValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseJwtValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtValidationMiddleware>();
        }
    }

}
