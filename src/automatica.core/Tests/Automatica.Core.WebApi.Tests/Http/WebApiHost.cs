using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Threading.RateLimiting;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Automatica.Core.HyperSeries;
using Automatica.Core.Internals.Core;
using Automatica.Core.Internals.UserHelper;
using Automatica.Core.Model.Models.User;
using Automatica.Core.Runtime;
using Automatica.Core.Runtime.Database;
using Automatica.Core.Runtime.RemoteConnect;
using Automatica.Core.WebApi.Authentication;
using Automatica.Core.WebApi.Camera;
using Automatica.Core.WebApi.Controllers;
using Automatica.Core.WebApi.Converter;
using Automatica.Core.WebApi.Exceptions;
using Automatica.Push;
using Automatica.Push.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using MQTTnet.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using User = Automatica.Core.Model.Models.User.User;

namespace Automatica.Core.WebApi.Tests.Http
{
    /// <summary>
    /// The web api controllers in an in-process <see cref="TestServer"/> with the authentication, authorization policies,
    /// filters, rate limiter and json settings of <c>Automatica.Core/Startup.cs</c> (copied from there: the startup itself
    /// needs the config files, Remote Connect, mDNS and the drivers) and EF Core on a SQLite database in a temp directory.
    /// <para>Keep <see cref="ConfigureServices"/> and <see cref="ConfigurePipeline"/> in sync with the startup.</para>
    /// The runtime parts that need a running server (core server, broker) are mocks, hosted services are not started.
    /// </summary>
    public sealed class WebApiHost : IDisposable
    {
        public const string RemoteIpHeader = "X-Test-Remote-Ip";

        /// <summary>
        /// A password that fulfills the password policy (test value)
        /// </summary>
        public const string StrongPassword = "Correct-Horse-7";

        private readonly string _tempDirectory;
        private readonly IHost _host;
        private int _userCounter;

        public IConfigurationRoot Configuration { get; }
        public IServiceProvider Services => _host.Services;
        public TestServer Server { get; }

        public Mock<ICoreServer> CoreServer { get; } = new();
        public Mock<IRemoteServerHandler> RemoteServerHandler { get; } = new();

        /// <summary>
        /// Client ids reported as connected by the (mocked) broker
        /// </summary>
        public List<string> ConnectedSatellites { get; } = new();

        /// <param name="includeTestControllers">also the controllers of the test assembly</param>
        /// <param name="settings">additional configuration values</param>
        /// <param name="configureServices">replaces services after the default registrations (mocks)</param>
        public WebApiHost(bool includeTestControllers = false, IDictionary<string, string> settings = null,
            Action<IServiceCollection> configureServices = null)
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "automatica-webapi-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);

            var values = new Dictionary<string, string>
            {
                ["ConnectionStrings:AutomaticaDatabaseType"] = "sqlite",
                ["ConnectionStrings:AutomaticaDatabaseSqlite"] = $"Data Source={Path.Combine(_tempDirectory, "automatica.db")}",
                ["server:database_logs"] = "false"
            };
            if (settings != null)
            {
                foreach (var setting in settings)
                {
                    values[setting.Key] = setting.Value;
                }
            }

            Configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
            RemoteServerHandler.Setup(a => a.GetConnectedClients()).Returns(() => ConnectedSatellites.ToList());

            _host = new HostBuilder()
                .UseEnvironment("Testing")
                .UseContentRoot(_tempDirectory)
                .ConfigureWebHost(web =>
                {
                    web.UseTestServer();
                    web.ConfigureServices(services =>
                    {
                        ConfigureServices(services, includeTestControllers);
                        configureServices?.Invoke(services);
                    });
                    web.Configure(ConfigurePipeline);
                })
                .Build();

            DatabaseInit.EnsureDatabaseCreated(_host.Services);
            _host.Start();
            Server = _host.GetTestServer();
        }

        private void ConfigureServices(IServiceCollection services, bool includeTestControllers)
        {
            services.AddSingleton(Configuration);
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddLogging(a => a.SetMinimumLevel(LogLevel.Warning));

            services.AddDbContext<AutomaticaContext>();
            services.AddHyperSeries();
            services.AddAutomaticaCoreService(Configuration, false);
            services.AddAutomaticaPushServices(Configuration, false);

            // --- Startup.ConfigureServices (security relevant part) ---
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy(AuthenticationController.LoginRateLimitPolicy, context =>
                    RateLimitPartition.GetFixedWindowLimiter(LoginThrottle.GetClientKey(context),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));
            });

            services.AddSingleton<UserClaimsProvider>();
            services.AddSingleton<LoginThrottle>();

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
                        IssuerSigningKey = new SymmetricSecurityKey(JwtSigningKey.Get()),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                    config.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            // the token in the query string only for the SignalR hubs and the camera live view (WebSockets cannot send headers)
                            var accessToken = context.Request.Query["access_token"];
                            if (!string.IsNullOrEmpty(accessToken) &&
                                (context.HttpContext.Request.Path.StartsWithSegments("/signalr") || CameraController.IsCameraWebSocket(context.HttpContext.Request)))
                            {
                                context.Token = accessToken;
                            }

                            return Task.CompletedTask;
                        },
                        OnTokenValidated = ValidateTokenUser
                    };
                });

            var mvc = services.AddMvcCore(config =>
                {
                    config.Filters.Add(new AuthorizeFilter());
                    config.Filters.Add(new NonAdminResponseFilter(new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver(),
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        Converters = { new ByteArrayToLongConverter() }
                    }));
                })
                .AddAuthorization(options =>
                {
                    options.DefaultPolicy = PendingPasswordChange.BuildDefaultPolicy();
                    options.AddPolicy(AccountController.PasswordChangePolicy, policy => policy.RequireAuthenticatedUser());
                    options.AddPolicy(Role.AdminRole, policy => policy.RequireRole(Role.AdminRole));
                    options.AddPolicy(Role.ViewerRole,
                        policy => policy.RequireRole(Role.ViewerRole, Role.AdminRole, Role.VisuRole));
                    options.AddPolicy(Role.VisuRole, policy => policy.RequireRole(Role.VisuRole));
                })
                // only the web api controllers (the test host has no entry assembly with controllers)
                .ConfigureApplicationPartManager(manager => manager.ApplicationParts.Clear())
                .AddApplicationPart(typeof(BaseController).GetTypeInfo().Assembly);

            if (includeTestControllers)
            {
                mvc.AddApplicationPart(typeof(WebApiHost).Assembly);
            }

            mvc.AddControllersAsServices()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.Converters.Add(new ByteArrayToLongConverter());
                });

            services.AddControllers(options =>
                options.Filters.Add(new HttpResponseExceptionFilter(NullLogger.Instance)));
            services.AddSingleton(sp => new Go2RtcSettings(sp.GetRequiredService<IConfiguration>()));
            services.AddSingleton<Go2RtcProxy>();
            // --- end of the startup part ---

            // runtime parts that need a running server
            services.RemoveAll<IHostedService>();
            services.AddSingleton(CoreServer.Object);
            services.AddSingleton(RemoteServerHandler.Object);
            services.AddSingleton(new Mock<IRemoteConnectService>().Object);
            services.AddSingleton(new MqttServerFactory().CreateMqttServer(new MqttServerOptionsBuilder().Build()));

            var clientProxy = new Mock<IClientProxy>();
            var hubClients = new Mock<IHubClients>();
            hubClients.SetupGet(a => a.All).Returns(clientProxy.Object);
            hubClients.Setup(a => a.Group(It.IsAny<string>())).Returns(clientProxy.Object);
            hubClients.Setup(a => a.Groups(It.IsAny<IReadOnlyList<string>>())).Returns(clientProxy.Object);
            hubClients.Setup(a => a.Clients(It.IsAny<IReadOnlyList<string>>())).Returns(clientProxy.Object);
            var dataHub = new Mock<IHubContext<DataHub>>();
            dataHub.SetupGet(a => a.Clients).Returns(hubClients.Object);
            services.AddSingleton(dataHub.Object);
            var telegramHub = new Mock<IHubContext<TelegramHub>>();
            telegramHub.SetupGet(a => a.Clients).Returns(hubClients.Object);
            services.AddSingleton(telegramHub.Object);
        }

        /// <summary>
        /// Copy of Startup.ValidateTokenUser
        /// </summary>
        private static Task ValidateTokenUser(TokenValidatedContext context)
        {
            if (context.Principal?.Identity is not ClaimsIdentity identity)
            {
                context.Fail("Invalid token");
                return Task.CompletedTask;
            }

            var provider = context.HttpContext.RequestServices.GetRequiredService<UserClaimsProvider>();
            var error = provider.Refresh(identity);
            if (error != null)
            {
                context.Fail(error);
            }

            return Task.CompletedTask;
        }

        private static void ConfigurePipeline(IApplicationBuilder app)
        {
            // the test server has no connection address: requests come from loopback (like through the Remote Connect tunnel),
            // a test can pick another address with a header
            app.Use((context, next) =>
            {
                var requested = context.Request.Headers[RemoteIpHeader].ToString();
                context.Connection.RemoteIpAddress = string.IsNullOrEmpty(requested) ? IPAddress.Loopback : IPAddress.Parse(requested);
                return next(context);
            });

            app.UseWebSockets();
            app.UseRouting();
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => endpoints.MapControllerRoute("webapi", ""));
        }

        public HttpClient CreateClient(string token = null, string forwardedFor = null, string remoteIp = null)
        {
            var client = Server.CreateClient();
            if (token != null)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            if (forwardedFor != null)
            {
                client.DefaultRequestHeaders.Add("X-Forwarded-For", forwardedFor);
            }

            if (remoteIp != null)
            {
                client.DefaultRequestHeaders.Add(RemoteIpHeader, remoteIp);
            }

            return client;
        }

        /// <summary>
        /// A real login over HTTP, from an own client address (own rate limit partition)
        /// </summary>
        public Task<HttpResponseMessage> LoginAsync(string userName, string password)
        {
            var n = System.Threading.Interlocked.Increment(ref _userCounter);
            var client = CreateClient(forwardedFor: $"100.64.{n / 250 % 250}.{n % 250 + 1}");
            return client.PostAsync("webapi/auth/login", HttpTestExtensions.Json(new UserAuthData { Username = userName, Password = password }));
        }

        public AutomaticaContext CreateDbContext() => new(Configuration);

        public T GetService<T>() => Services.GetRequiredService<T>();

        public string UniqueName(string prefix) => $"{prefix}{System.Threading.Interlocked.Increment(ref _userCounter)}";

        public Guid RoleId(string roleKey)
        {
            using var db = CreateDbContext();
            var role = db.Roles.AsNoTracking().SingleOrDefault(a => a.Key == roleKey);
            if (role != null)
            {
                return role.ObjId;
            }

            // the viewer role is not part of the initial data
            role = new Role
            {
                ObjId = Guid.NewGuid(),
                Key = roleKey,
                Name = roleKey,
                Description = roleKey,
                CreatedAt = DateTimeOffset.Now,
                ModifiedAt = DateTimeOffset.Now
            };
            db.Roles.Add(role);
            db.SaveChanges();
            return role.ObjId;
        }

        public Guid CreateUserGroup(string name, params string[] roleKeys)
        {
            using var db = CreateDbContext();
            var group = new UserGroup
            {
                ObjId = Guid.NewGuid(),
                Name = name,
                Description = "",
                CreatedAt = DateTimeOffset.Now,
                ModifiedAt = DateTimeOffset.Now
            };
            db.UserGroups.Add(group);
            foreach (var roleKey in roleKeys)
            {
                db.UserGroup2Roles.Add(new UserGroup2Role { This2UserGroup = group.ObjId, This2Role = RoleId(roleKey) });
            }

            db.SaveChanges();
            InvalidateCaches();
            return group.ObjId;
        }

        /// <summary>
        /// Creates a user directly in the database (direct roles and group memberships)
        /// </summary>
        public Guid CreateUser(string userName, string password, string[] roleKeys, Guid[] groups = null, bool mustChangePassword = false)
        {
            var roleIds = roleKeys.Select(RoleId).ToList();
            using var db = CreateDbContext();
            var salt = UserHelper.GenerateNewSalt();
            var user = new User
            {
                ObjId = Guid.NewGuid(),
                UserName = userName,
                FirstName = "Test",
                LastName = userName,
                Description = "",
                Salt = salt,
                Password = UserHelper.CreatePasswordHash(password, salt),
                SecurityStamp = UserHelper.NewSecurityStamp(),
                MustChangePassword = mustChangePassword,
                CreatedAt = DateTimeOffset.Now,
                ModifiedAt = DateTimeOffset.Now
            };
            db.Users.Add(user);
            foreach (var roleId in roleIds)
            {
                db.User2Roles.Add(new User2Role { This2User = user.ObjId, This2Role = roleId });
            }

            foreach (var group in groups ?? Array.Empty<Guid>())
            {
                db.User2Groups.Add(new User2Group { This2User = user.ObjId, This2UserGroup = group });
            }

            db.SaveChanges();
            InvalidateCaches();
            return user.ObjId;
        }

        /// <summary>
        /// A token of the user issued like the login does (<see cref="TokenIssuer"/>), without using up the login rate limit
        /// </summary>
        public string TokenFor(Guid userId)
        {
            using var db = CreateDbContext();
            var user = db.Users.AsNoTracking().Include(a => a.InverseThis2Roles).ThenInclude(a => a.This2RoleNavigation)
                .Single(a => a.ObjId == userId);
            return TokenIssuer.Login(db, user).Token;
        }

        public string CreateAdminToken(out Guid userId)
        {
            userId = CreateUser(UniqueName("admin"), StrongPassword, new[] { Role.AdminRole });
            return TokenFor(userId);
        }

        public string CreateAdminToken() => CreateAdminToken(out _);

        public string CreateVisuToken(out Guid userId, params Guid[] groups)
        {
            userId = CreateUser(UniqueName("visu"), StrongPassword, new[] { Role.VisuRole }, groups);
            return TokenFor(userId);
        }

        public string CreateVisuToken(params Guid[] groups) => CreateVisuToken(out _, groups);

        /// <summary>
        /// Clears the claims cache (30 s) and the user caches after direct database changes
        /// </summary>
        public void InvalidateCaches()
        {
            Services.GetRequiredService<UserClaimsProvider>().Invalidate();
            Services.GetRequiredService<Automatica.Core.Internals.Cache.Common.IUserCache>().Clear();
            Services.GetRequiredService<Automatica.Core.Internals.Cache.Common.IUserGroupsCache>().Clear();
        }

        public void Dispose()
        {
            try
            {
                _host.StopAsync().GetAwaiter().GetResult();
            }
            catch
            {
                // ignored, the test is over
            }

            _host.Dispose();
            SqliteConnection.ClearAllPools();
            try
            {
                Directory.Delete(_tempDirectory, true);
            }
            catch (IOException)
            {
                // a connection of another test may still hold the pool, the temp directory is cleaned by the OS
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    /// <summary>
    /// One host per test class (the database init takes a while), the tests of the class use their own users and objects.
    /// </summary>
    public sealed class WebApiHostFixture : IDisposable
    {
        public WebApiHost Host { get; } = new();

        public void Dispose() => Host.Dispose();
    }

    public sealed class WebApiHostWithTestControllersFixture : IDisposable
    {
        public WebApiHost Host { get; } = new(includeTestControllers: true);

        public void Dispose() => Host.Dispose();
    }

    public static class HttpTestExtensions
    {
        private static readonly JsonSerializerSettings Settings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        public static StringContent Json(object value) =>
            new(value == null ? "null" : JsonConvert.SerializeObject(value, Settings), Encoding.UTF8, "application/json");

        public static StringContent RawJson(string json) => new(json, Encoding.UTF8, "application/json");

        public static Task<HttpResponseMessage> PostJson(this HttpClient client, string url, object value) => client.PostAsync(url, Json(value));

        public static Task<HttpResponseMessage> PutJson(this HttpClient client, string url, object value) => client.PutAsync(url, Json(value));

        public static Task<HttpResponseMessage> PatchJson(this HttpClient client, string url, object value) => client.PatchAsync(url, Json(value));

        public static async Task<JToken> ReadJson(this HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            return string.IsNullOrEmpty(body) ? JValue.CreateNull() : JToken.Parse(body);
        }

        public static async Task<T> Read<T>(this HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(body, Settings);
        }
    }
}
