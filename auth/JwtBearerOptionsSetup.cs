using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Threading.Tasks;

public class JwtBearerOptionsSetup : IConfigureOptions<JwtBearerOptions>
{
    private readonly PublicKeyCache _publicKeyCache;

    public JwtBearerOptionsSetup(PublicKeyCache publicKeyCache)
    {
        _publicKeyCache = publicKeyCache;
    }

    public void Configure(JwtBearerOptions options)
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                Console.WriteLine($"Token received: {context.Token} on path {context.HttpContext.Request.Path}");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine($"Token validated for user: {context.Principal.Identity.Name}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine($"OnChallenge: {context.Error}, {context.ErrorDescription}");
                return Task.CompletedTask;
            }
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "ch.sncag.sso",
            IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
            {
                var keyId = Guid.Parse(kid);
                var rsaParametersTask = _publicKeyCache.GetPublicKeyAsync(keyId);
                rsaParametersTask.Wait(); 
                var rsaParameters = rsaParametersTask.Result;
                return new[] { new RsaSecurityKey(rsaParameters) };
            }
        };
    }
}