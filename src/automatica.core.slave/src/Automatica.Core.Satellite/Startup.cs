using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Automatica.Core.Satellite.Abstraction.Model;
using Automatica.Core.Satellite.Runtime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Automatica.Core.Satellite
{
    public static class Startup
    {
        private const string Issuer = "automatica-satellite";
        private const string LoginRateLimit = "login";

        private static readonly object SetupLock = new();
        private static string _setupToken;
        private static byte[] _jwtKey;

        public static void ConfigureServices(IServiceCollection services, IHostEnvironment environment)
        {
            services.AddSingleton<SatelliteRuntime, SatelliteRuntime>();
            services.AddSingleton<IHostedService>(provider => provider.GetService<SatelliteRuntime>());

            // random per-install key (the satellite id used before is not secret)
            _jwtKey = SatelliteSecrets.GetJwtKey();

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
                        IssuerSigningKey = new SymmetricSecurityKey(_jwtKey),
                        ValidateIssuer = true,
                        ValidIssuer = Issuer,
                        ValidateAudience = true,
                        ValidAudience = Issuer,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                });

            services.AddAuthorization();
            services.AddAuthorizationBuilder()
                .AddPolicy("admin", policy =>
                    policy
                        .RequireRole("admin")
                        .RequireClaim("scope", "admin"));

            // login and setup: 5 attempts per minute and client address
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy(LoginRateLimit, context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
            });

            if (environment.IsDevelopment())
            {
                services.AddEndpointsApiExplorer();
                services.AddSwaggerGen();
            }

            services.AddControllers();
        }

        private static bool IsSetUp(IConfiguration configuration)
        {
            return !String.IsNullOrEmpty(configuration["user"]) && !String.IsNullOrEmpty(configuration["password"]);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public static void Configure(WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "wwwroot");
            if (!Directory.Exists(wwwrootPath))
            {
                wwwrootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
            }

            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();

            if (!IsSetUp(app.Configuration))
            {
                // only who can read the satellite's console/log can create the admin account
                _setupToken = UserAuthData.GenerateToken(18);
                var message = $"No satellite user configured. Setup token for the first login: {_setupToken}";
                Console.WriteLine(message);
                app.Logger.LogWarning(message);
            }

            app.MapGet("/webapi/ready", () => Results.Ok(new { Ready = IsSetUp(app.Configuration) }));

            app.MapPost("/webapi/setup", ([FromBody] UserAuthData authData) =>
            {
                if (authData == null || String.IsNullOrEmpty(authData.Username) || String.IsNullOrEmpty(authData.Password))
                {
                    return Results.BadRequest();
                }
                if (authData.Password.Length < UserAuthData.MinimumPasswordLength)
                {
                    return Results.BadRequest($"The password needs at least {UserAuthData.MinimumPasswordLength} characters");
                }

                lock (SetupLock)
                {
                    // setup works exactly once: while no user exists, and only with the token from the log
                    if (IsSetUp(app.Configuration) || _setupToken == null)
                    {
                        return Results.Conflict();
                    }
                    if (!UserAuthData.FixedTimeEquals(authData.SetupToken?.Trim(), _setupToken))
                    {
                        return Results.Unauthorized();
                    }

                    app.Configuration["user"] = authData.Username;
                    app.Configuration["salt"] = "";
                    app.Configuration["password"] = UserAuthData.HashPassword(authData.Password);
                    _setupToken = null;
                }

                app.Logger.LogInformation($"Satellite user {authData.Username} created");
                return Results.NoContent();
            }).RequireRateLimiting(LoginRateLimit);

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

                // the hash is always computed, so a wrong user name takes as long as a wrong password
                var passwordOk = UserAuthData.VerifyPassword(authData.Password, password, app.Configuration["salt"], out var needsRehash);
                var userOk = UserAuthData.FixedTimeEquals(authData.Username, user);

                if (!passwordOk || !userOk)
                {
                    return Results.BadRequest();
                }

                if (needsRehash)
                {
                    app.Configuration["password"] = UserAuthData.HashPassword(authData.Password);
                    app.Configuration["salt"] = "";
                }

                var claimsIdentity = new ClaimsIdentity(new List<Claim>
                {
                    new(ClaimTypes.Role, "admin"),
                    new (ClaimTypes.NameIdentifier, user),
                    new (ClaimTypes.Name, user)
                });

                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = claimsIdentity,
                    Issuer = Issuer,
                    Audience = Issuer,
                    Expires = DateTime.UtcNow.AddHours(12),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(_jwtKey),
                        SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var userToken = tokenHandler.WriteToken(token);

                return Results.Ok(userToken);
            }).RequireRateLimiting(LoginRateLimit);

            app.MapGet("/webapi/user", [Authorize] (ClaimsPrincipal user) =>
            {
                if (user == null || user.Identity == null)
                {
                    return Results.BadRequest();
                }

                return Results.Ok(new { user = user.Identity.Name });
            });

            // the broker key is write-only
            app.MapGet("/webapi/config", [Authorize] () => new Config
            {
                ServerUrl = app.Configuration["server:master"],
                ClientId = app.Configuration["server:clientId"],
                ClientKey = null,
                HasClientKey = !String.IsNullOrEmpty(app.Configuration["server:clientKey"]),
                DockerTag = app.Configuration["server:dockerTag"],
                Port = Int32.TryParse(app.Configuration["server:port"], out var port) ? port : 0,
                UseTls = Boolean.TryParse(app.Configuration["server:useTls"], out var useTls) ? useTls : null,
                TlsPort = Int32.TryParse(app.Configuration["server:tlsPort"], out var tlsPort) ? tlsPort : 8883,
                TlsFingerprint = app.Configuration["server:tlsFingerprint"]
            });

            app.MapPost("/webapi/config", [Authorize] async ([FromBody] Config config) =>
            {
                if (config == null)
                {
                    return Results.BadRequest();
                }
                if (!String.IsNullOrEmpty(config.ClientKey) && config.ClientKey.Length < 16)
                {
                    return Results.BadRequest("The client key needs at least 16 characters");
                }

                string fingerprint = null;
                if (!String.IsNullOrWhiteSpace(config.TlsFingerprint))
                {
                    fingerprint = SatelliteRuntime.NormalizeFingerprint(config.TlsFingerprint);
                    if (fingerprint == null)
                    {
                        return Results.BadRequest("The fingerprint must be a SHA-256 hash (64 hex characters)");
                    }
                }

                app.Configuration["server:master"] = config.ServerUrl;
                app.Configuration["server:clientId"] = config.ClientId;
                if (!String.IsNullOrEmpty(config.ClientKey))
                {
                    app.Configuration["server:clientKey"] = config.ClientKey;
                }
                app.Configuration["server:dockerTag"] = config.DockerTag;
                app.Configuration["server:port"] = config.Port.ToString();
                app.Configuration["server:useTls"] = config.UseTls?.ToString().ToLowerInvariant();
                app.Configuration["server:tlsPort"] = (config.TlsPort > 0 ? config.TlsPort : 8883).ToString();
                app.Configuration["server:tlsFingerprint"] = fingerprint;

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
                    !context.Request.Path.Value.StartsWith("/api/") &&
                    !context.Request.Path.Value.StartsWith("/webapi/"))
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
