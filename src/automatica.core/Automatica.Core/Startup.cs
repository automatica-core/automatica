using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Automatica.Core.HyperSeries;
using Automatica.Core.WebApi;
using Automatica.Discovery;
using Automatica.Push.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Automatica.Core.WebApi.Exceptions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Automatica.Core.Push;
using Automatica.Core.Internals;
using Automatica.Core.Logging;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.ResponseCompression;
using Automatica.Core.Runtime;
using Automatica.Core.WebApi.Converter;
using Microsoft.AspNetCore.Mvc;
using MQTTnet.AspNetCore.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Automatica.Core
{
    public class Startup
    {
       
        public IConfiguration Configuration { get; private set; }

        public Startup(IConfiguration config)
        {
            Configuration = config;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMqttTcpServerAdapter();


            services.AddDbContext<AutomaticaContext>();
            services.AddHyperSeries();
            services.AddResponseCompression(options =>
            {
                options.Providers.Add<GzipCompressionProvider>();
            });


            services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Fastest;
            });

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(config =>
                {
                    var serverUid = ServerInfo.ServerUid.ToByteArray();
                    var key = new byte[32];
                    Array.Copy(serverUid, 0, key, 0, 16);
                    Array.Copy(serverUid, 0, key, 16, 16);
                    config.RequireHttpsMetadata = false;
                    config.SaveToken = true;
                    config.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };

                    // We have to hook the OnMessageReceived event in order to
                    // allow the JWT authentication handler to read the access
                    // token from the query string when a WebSocket or 
                    // Server-Sent Events request comes in.
                    config.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];

                            // If the request is for our hub...
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) &&
                                (path.StartsWithSegments("/signalr")))
                            {
                                // Read the token out of the query string
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            services.Configure<MvcOptions>(options =>
            {
            });
            services.AddMvcCore(config => { config.Filters.Add(new AuthorizeFilter()); })
                .AddAuthorization(options =>
                {
                    options.AddPolicy(Role.AdminRole, policy => policy.RequireRole(Role.AdminRole));
                    options.AddPolicy(Role.ViewerRole,
                        policy => policy.RequireRole(Role.ViewerRole, Role.AdminRole, Role.VisuRole));
                    options.AddPolicy(Role.VisuRole, policy => policy.RequireRole(Role.VisuRole));
                })
                .AddApplicationPart(typeof(BaseController).GetTypeInfo().Assembly)
                .AddControllersAsServices()
                .AddApplicationPart(typeof(DiscoveryDeviceDescriptionController).GetTypeInfo().Assembly)
                .AddControllersAsServices()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.Converters.Add(new ByteArrayToLongConverter());
                });

            services.AddControllers(options =>
                options.Filters.Add(new HttpResponseExceptionFilter(SystemLogger.Instance)));


            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue; // In case of multipart
            });

            services.AddSingleton<DiscoveryService>();
            services.AddAutomaticaCoreService(Configuration, false);

            services.Replace(ServiceDescriptor.Singleton(typeof(ILogger), typeof(CoreLogger)));
            services.Replace(ServiceDescriptor.Singleton(typeof(ILoggerFactory), typeof(CoreLoggerFactory)));

            var builder = new ConfigurationBuilder()
                .SetBasePath(ServerInfo.GetConfigDirectory())
                .AddJsonFile(ServerInfo.GetConfigFileName())
                .AddDatabaseConfiguration()
                .AddEnvironmentVariables();

            var configRoot = builder.Build();

            Configuration = configRoot;
            services.AddSingleton(configRoot);
            services.AddSingleton<IConfiguration>(configRoot);

            services.AddSingleton<IUserIdProvider, NameUserIdProvider>();

            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            }).AddJsonProtocol(options =>
            {
            });
            services.AddAutomaticaRemoteConnectWithFrp(a =>
            {
                a.UseWeb = true;
                a.UseSsh = true;

                a.ServerAddress = Configuration["server:remote_connect_url"];
                a.ServerPort = 7000;
                a.LocalIp = "127.0.0.1";
                a.LocalPort = Convert.ToInt32(Configuration["server:port"]);
            });


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = SameSiteMode.Strict
            });


            string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "wwwroot");
          
            if (!Directory.Exists(wwwrootPath))
            {
                wwwrootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
            }
            SystemLogger.Instance.LogInformation($"wwwroot directory {wwwrootPath}");

            if (Configuration["server:useHttpsRedirect"] == "True")
            {
                app.UseHttpsRedirection();
            }

            app.UseRouting();

            app.UseResponseCompression();

            app.UseAuthentication();
            app.UseAuthorization();

//app.UseMiddleware<WebApiErrorMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<DataHub>("/signalr/dataHub", options =>
                {
                    options.ApplicationMaxBufferSize = 1024 * 1024;
                });
                endpoints.MapHub<TelegramHub>("/signalr/telegramHub");
                endpoints.MapHub<UpdateHub>("/signalr/updateHub");
                endpoints.MapHub<LoggingHub>("/signalr/loggingHub");

                endpoints.MapControllerRoute("webapi", "");

            });

            app.Use(async (context, next) => {
                await next();
                if (context.Response.StatusCode == 404 &&
                    !Path.HasExtension(context.Request.Path.Value) &&
                    !context.Request.Path.Value.StartsWith("/api/"))
                {
                    context.Request.Path = "/index.html";
                    context.Response.Cookies.Append("Electron", "false");
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
                SystemLogger.Instance.LogError($"Could not find wwwroot directory ({wwwrootPath})");
            }



            //if (HybridSupport.IsElectronActive)
            //{
            //    Task.Run(async () => await ElectronBootstrap(port));
            //}
        }

        //public async Task ElectronBootstrap(string port)
        //{
        //    var browserWindow = await Electron.WindowManager.CreateWindowAsync($"http://localhost:{port}");
        //    browserWindow.SetTitle("Automatica.Core");   

        //    browserWindow.WebContents.OpenDevTools(new OpenDevToolsOptions
        //    {
        //        Mode = DevToolsMode.undocked
        //    });
        //}
        
    }
}
