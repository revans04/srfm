// Filters/AuthorizeFirebaseAttribute.cs
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class AuthorizeFirebaseAttribute : ActionFilterAttribute
{
    private const string UserIdItemKey = "UserId";
    private const string UserEmailItemKey = "UserEmail";

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var authHeader = context.HttpContext.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new UnauthorizedObjectResult("Invalid Authorization header: Bearer token required");
            return;
        }

        var token = authHeader.Substring("Bearer ".Length);
        try
        {
            var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
            context.HttpContext.Items[UserIdItemKey] = decodedToken.Uid;

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            if (!string.IsNullOrWhiteSpace(email))
            {
                context.HttpContext.Items[UserEmailItemKey] = email;
            }

            await next();
        }
        catch (Exception ex)
        {
            context.Result = new UnauthorizedObjectResult($"Token verification failed: {ex.Message}");
        }
    }
}
