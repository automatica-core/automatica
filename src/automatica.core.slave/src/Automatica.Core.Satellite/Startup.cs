using System;
using System.IO;
using Automatica.Core.Satellite.Abstraction.Config;
using Automatica.Core.Satellite.Runtime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Automatica.Core.Satellite
{
    public static class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<SatelliteRuntime, SatelliteRuntime>();
            services.AddSingleton<IHostedService>(provider => provider.GetService<SatelliteRuntime>());

            var configurationBuilder = new ConfigurationBuilder();
            var configuration = configurationBuilder.Add<WritableJsonConfigurationSource>(
                (Action<WritableJsonConfigurationSource>)(s =>
                {
                    s.FileProvider = null;
                    s.Path = "appsettings.json";
                    s.Optional = false;
                    s.ReloadOnChange = true;
                    s.ResolveFileProvider();
                })).Build();


            var satelliteId = configuration["SatelliteId"];
            Guid satelliteGuid;

            if (String.IsNullOrEmpty(satelliteId))
            {
                satelliteGuid = Guid.NewGuid();
                configuration["SatelliteId"] = satelliteGuid.ToString();
            }
            else
            {
                satelliteGuid = Guid.Parse(satelliteId);
            }

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(config =>
                {
                    config.RequireHttpsMetadata = false;
                    config.SaveToken = true;
                    config.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(satelliteGuid.ToByteArray()),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                });


            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            services.AddControllers();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public static void Configure(WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "wwwroot");
            if (!Directory.Exists(wwwrootPath))
            {
                wwwrootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
            }

            app.UseSwagger();
            app.UseSwaggerUI();
           
            app.UseAuthorization();

            app.MapPost("/test", ([FromBody] string test) =>
            {

            });

            app.Use(async (context, next) => {
                await next();
                if (context.Response.StatusCode == 404 &&
                    !Path.HasExtension(context.Request.Path.Value) &&
                    !context.Request.Path.Value.StartsWith("/api/"))
                {
                    context.Request.Path = "/index.html";
                    await next();
                }
            });

            if (Directory.Exists(wwwrootPath))
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(wwwrootPath)
                });
            }
            else
            {
                Console.WriteLine($"Could not find wwwroot directory ({wwwrootPath})");
            }


        }
    }
}
