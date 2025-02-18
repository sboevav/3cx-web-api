using WebAPI.business;
using WebAPI.config;
using WebAPI.auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
        services.AddSingleton<AuthTokenValidatorSso>();
        services.AddControllers();

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
        
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });
        
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();
        app.UseMiddleware<TokenValidationMiddleware>();
        app.UseCors("CustomCorsPolicy");
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}