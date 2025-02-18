using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebAPI.auth;

public class TokenValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuthTokenValidatorSso _tokenValidator;

    public TokenValidationMiddleware(RequestDelegate next, AuthTokenValidatorSso tokenValidator)
    {
        _next = next;
        _tokenValidator = tokenValidator;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method == "OPTIONS")
        {
            await _next(context);
            return;
        }

        // Don't need token validation for the PbxController requests
        var endpoint = context.GetEndpoint();
        if (endpoint == null || endpoint.DisplayName == null || endpoint.DisplayName.Contains("PbxController"))
        {
            await _next(context);
            return;
        }

        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

        if (string.IsNullOrEmpty(token))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Token is missing");
            return;
        }

        var isValid = await _tokenValidator.Validate(token);
        if (!isValid)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid token");
            return;
        }

        await _next(context);
    }
}