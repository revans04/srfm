// Filters/AuthorizeFirebaseAttribute.cs
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

public class AuthorizeFirebaseAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var authHeader = context.HttpContext.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            context.Result = new UnauthorizedObjectResult("Invalid Authorization header: Bearer token required");
            return;
        }

        var token = authHeader.Substring("Bearer ".Length);
        try
        {
            var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
            context.HttpContext.Items["UserId"] = decodedToken.Uid;
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            context.HttpContext.Items["Email"] = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
        }
        catch (Exception ex)
        {
            // Only swallow exceptions raised while *verifying* the token.
            // Downstream controller/filter exceptions must propagate so they
            // surface as real 500s with the actual stack trace instead of
            // being mis-labeled as auth failures.
            Console.WriteLine($"Token verification failed: {ex}");
            context.Result = new UnauthorizedObjectResult($"Token verification failed: {ex.Message}");
            return;
        }

        await next();
    }
}