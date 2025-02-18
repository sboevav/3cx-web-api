using WebAPI.business;
using WebAPI.config;
using WebAPI.auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace WebAPI;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));

        services.AddHttpClient<SsoClient>((provider, client) =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            client.BaseAddress = new Uri(configuration["AppSettings:SsoUrl"]);
        });

        services.AddSingleton<SsoClient>();
        services.AddSingleton<PublicKeyCache>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => 
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
                                rsaParametersTask.Wait(); // Wait на результ
                                var rsaParameters = rsaParametersTask.Result;
                                return new[] { new RsaSecurityKey(rsaParameters) };
                            }
                        };
                    }
                );
       
        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, JwtBearerOptionsSetup>();

        services.AddCors(options =>
        {
            options.AddPolicy("CustomCorsPolicy", builder =>
            {
                builder.AllowAnyOrigin()
                    .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                    .WithHeaders("Content-Type", "Authorization", "Content-Length", "X-Requested-With", "x-request-id");
            });
        });

        var configurationFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "3CXPhoneSystem.ini");
        var configurationService = new ConfigurationService(configurationFilePath);
        services.AddSingleton(configurationService);
        services.AddSingleton<PbxService>();
        services.AddControllers();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });
        
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();
        app.UseCors("CustomCorsPolicy");
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}