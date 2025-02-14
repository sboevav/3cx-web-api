using WebAPI.business;
using WebAPI.config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using Microsoft.Extensions.Configuration;

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
        services.AddHttpClient();
        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();
        app.UseCors("CustomCorsPolicy");
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
