using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using Automatica.Core.Satellite.Abstraction.Config;
using Automatica.Core.Satellite.Abstraction.Model;
using Automatica.Core.Satellite.Runtime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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
                    s.Path = Debugger.IsAttached ? "appsettings.Development.json" : "appsettings.json";
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

            services.AddAuthorization();
            services.AddAuthorizationBuilder()
                .AddPolicy("admin", policy =>
                    policy
                        .RequireRole("admin")
                        .RequireClaim("scope", "admin"));


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

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapGet("/webapi/ready", () =>
            {
                var user = app.Configuration["user"];
                var password = app.Configuration["password"];

                if (String.IsNullOrEmpty(user) || String.IsNullOrEmpty(password))
                {
                    return Results.Ok(new { Ready = false});
                }
                return Results.Ok(new { Ready = true });
            });

            app.MapPost("/webapi/setup", ([FromBody] UserAuthData authData) =>
            {
                if (authData == null || String.IsNullOrEmpty(authData.Username) ||
                    String.IsNullOrEmpty(authData.Password))
                {
                    return Results.BadRequest();
                }

                app.Configuration["user"] = authData.Username;
                var salt = UserAuthData.GenerateNewSalt();

                app.Configuration["salt"] = salt;
                var hash = UserAuthData.HashPassword(authData.Password, salt);

                app.Configuration["password"] = hash;

                return Results.NoContent();
            });

            app.MapPost("/webapi/login", ([FromBody] UserAuthData authData) =>
            {
                if (authData == null || String.IsNullOrEmpty(authData.Username) ||
                    String.IsNullOrEmpty(authData.Password))
                {
                    return Results.BadRequest();
                }

                var user = app.Configuration["user"];
                var password = app.Configuration["password"];

                if (String.IsNullOrEmpty(user) || String.IsNullOrEmpty(password))
                {
                    return Results.BadRequest();
                }

                if (authData.Username != user)
                {
                    return Results.BadRequest();
                }

                var salt = app.Configuration["salt"];
                var hash = UserAuthData.HashPassword(authData.Password, salt);

                if (hash != password)
                {
                    return Results.BadRequest();
                }

                var claimsIdentity = new ClaimsIdentity(new List<Claim>
                {
                    new(ClaimTypes.Role, "admin"),
                    new (ClaimTypes.NameIdentifier, authData.Username),
                    new (ClaimTypes.Name, authData.Username)
                });

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Guid.Parse(app.Configuration["SatelliteId"]!).ToByteArray();
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = claimsIdentity,
                    Expires = DateTime.UtcNow.AddDays(7),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var userToken = tokenHandler.WriteToken(token);


                return Results.Ok(userToken);
            });

            app.MapGet("/webapi/user", [Authorize] (ClaimsPrincipal user) =>
            {
                if (user == null || user.Identity == null)
                {
                    return Results.BadRequest();
                }

                return Results.Ok(new { user = user.Identity.Name });
            });

            app.MapGet("/webapi/config", [Authorize] () => new Config
            {
                ServerUrl = app.Configuration["server:master"],
                ClientId = app.Configuration["server:clientId"],
                ClientKey = app.Configuration["server:clientKey"],
                DockerTag = app.Configuration["server:dockerTag"],
                Port = Convert.ToInt32(app.Configuration["server:port"])
            });

            app.MapPost("/webapi/config", [Authorize] async ([FromBody] Config config) =>
            {
                app.Configuration["server:master"] = config.ServerUrl;
                app.Configuration["server:clientId"] = config.ClientId;
                app.Configuration["server:clientKey"] = config.ClientKey;
                app.Configuration["server:dockerTag"] = config.DockerTag;
                app.Configuration["server:port"] = config.Port.ToString();

                var satelliteRuntime = app.Services.GetRequiredService<SatelliteRuntime>();
                await satelliteRuntime.Restart();

                return Results.NoContent();
            });

            app.MapGet("/webapi/status", [Authorize] () =>
            {
                var satelliteRuntime = app.Services.GetRequiredService<SatelliteRuntime>();
                var statusObject = new
                {
                    Connected = satelliteRuntime.Connected,
                    ContainerStarted = satelliteRuntime.ContainerStarted,
                    RunningImages = satelliteRuntime.RunningImages.Values
                };

                return statusObject;
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
