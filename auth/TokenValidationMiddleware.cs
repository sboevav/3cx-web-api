using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace WebAPI.auth;

public class TokenValidationMiddleware
{
    private readonly ILogger<TokenValidationMiddleware> _logger;
    private readonly RequestDelegate _next;
    private readonly AuthTokenValidatorSso _tokenValidator;

    public TokenValidationMiddleware(ILogger<TokenValidationMiddleware> logger, RequestDelegate next, AuthTokenValidatorSso tokenValidator)
    {
        _logger = logger;
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
            _logger.LogDebug("PbxController request");
            await _next(context);
            return;
        }
        _logger.LogDebug("CmdController request");

        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogError("Token is missing");

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Token is missing");
            return;
        }

        _logger.LogDebug("Token validation...");
        var isValid = await _tokenValidator.Validate(token);
        if (!isValid)
        {
            _logger.LogError("Token validation failed");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid token");
            return;
        }

        await _next(context);
    }
}