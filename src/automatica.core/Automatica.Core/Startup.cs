using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.License;
using Automatica.Core.Base.Localization;
using Automatica.Core.EF.Models;
using Automatica.Core.Runtime.Core;
using Automatica.Core.Runtime.IO;
using Automatica.Core.WebApi;
using Automatica.Core.WebApi.Converter;
using Automatica.Discovery;
using Automatica.Push.Hubs;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Cors;
using Microsoft.AspNetCore.Mvc.Cors.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Automatica.Core.Base.Visu;
using Automatica.Core.Visu;
using Automatica.Core.Driver.Monitor;
using Automatica.Core.Driver.LeanMode;
using Microsoft.AspNetCore.Http.Features;
using Automatica.Core.WebApi.Exceptions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Automatica.Core.Internals.License;
using Automatica.Core.Internals.Cloud;
using Automatica.Core.Internals.Core;
using Microsoft.AspNetCore.SignalR;
using Automatica.Core.Push;
using Automatica.Core.Internals;
using Automatica.Core.Internals.Logger;
using Automatica.Core.Model.Models.User;
using Automatica.Push.LearnMode;
using Automatica.Core.WebApi.Converter.MessagePack;
using Microsoft.AspNetCore.ResponseCompression;
using MQTTnet.AspNetCore;
using System.Net;
using MQTTnet.Server;

namespace Automatica.Core
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMqttTcpServerAdapter();

            services.AddDbContext<AutomaticaContext>();
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
                    config.RequireHttpsMetadata = false;
                    config.SaveToken = true;
                    config.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(ServerInfo.ServerUid.ToByteArray()),
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

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", b =>
                {
                    b.AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowAnyOrigin()
                        .AllowCredentials();
                });
            });

            services.AddMvcCore(config =>
            {
                config.Filters.Add(new AuthorizeFilter());

            }).AddAuthorization(options =>
            {
                options.AddPolicy(Role.AdminRole, policy => policy.RequireRole(Role.AdminRole));
                options.AddPolicy(Role.ViewerRole, policy => policy.RequireRole(Role.ViewerRole, Role.AdminRole, Role.VisuRole));
                options.AddPolicy(Role.VisuRole, policy => policy.RequireRole(Role.VisuRole));
            }).AddApplicationPart(typeof(BaseController).GetTypeInfo().Assembly).AddControllersAsServices().AddJsonOptions(options =>
            {
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.SerializerSettings.Converters.Add(new ByteArrayToLongConverter());
            }).AddApplicationPart(typeof(DiscoveryDeviceDescriptionController).GetTypeInfo().Assembly).AddControllersAsServices().AddJsonOptions(options =>
            {
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.SerializerSettings.Converters.Add(new ByteArrayToLongConverter());
            }).AddJsonFormatters().AddMessagePackFormatters();

            services.Configure<MvcOptions>(options =>
            {
                options.Filters.Add(new CorsAuthorizationFilterFactory("CorsPolicy"));
            });

            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue; // In case of multipart
            });

            services.AddSingleton(new LocalizationProvider(SystemLogger.Instance));

            services.AddSingleton<DiscoveryService>();
            services.AddSingleton<IVisualisationFactory, VisuTempInit>();
            services.AddSingleton<ITelegramMonitor, TelegramMonitor>();
            services.AddSingleton<IServerCloudApi, CloudApi>();
            services.AddSingleton<ICloudApi, CloudApi>();
            services.AddSingleton<ILicenseContext, LicenseContext>();
            services.AddSingleton<ILicenseContract>(provider => provider.GetService<ILicenseContext>());
            services.AddSingleton<ILearnMode, LearnMode>();


            if (!HybridSupport.IsElectronActive)
            {
                services.AddSingleton<IHostedService>(provider => provider.GetService<DiscoveryService>());

                services.AddSingleton<CoreServer, CoreServer>();
                services.AddSingleton<IHostedService>(provider => provider.GetService<CoreServer>());
                services.AddSingleton<INotifyDriver>(provider => provider.GetService<CoreServer>());
                services.AddSingleton<IRuleVisualisation>(provider => provider.GetService<CoreServer>());
                services.AddSingleton<ICoreServer>(provider => provider.GetService<CoreServer>());
            }
            services.AddSingleton<IDispatcher, Dispatcher>();

            services.Replace(ServiceDescriptor.Singleton(typeof(ILogger), typeof(CoreLogger)));
            services.Replace(ServiceDescriptor.Singleton(typeof(ILoggerFactory), typeof(CoreLoggerFactory)));

            var builder = new ConfigurationBuilder()
                .SetBasePath(ServerInfo.GetConfigDirectory())
                .AddJsonFile("appsettings.json");

            Configuration = builder.Build();

            services.TryAddTransient<CorsAuthorizationFilter, CorsAuthorizationFilter>();


            services.AddSingleton<IUserIdProvider, NameUserIdProvider>();

            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            }).AddJsonProtocol(options =>
            {
            }); ;

            var mqttServerOptions = new MqttServerOptions()
            {
                ConnectionValidator = new MqttServerConnectionValidatorDelegate(a => CoreServer.ValidateConnection(a, Configuration, SystemLogger.Instance))
            };

            mqttServerOptions.DefaultEndpointOptions.BoundInterNetworkAddress = IPAddress.Any;
            mqttServerOptions.DefaultEndpointOptions.BoundInterNetworkV6Address = IPAddress.None;
            mqttServerOptions.DefaultEndpointOptions.IsEnabled = true;
            mqttServerOptions.DefaultEndpointOptions.Port = 1883;
            mqttServerOptions.DefaultEndpointOptions.ConnectionBacklog = 1000;

            services.AddHostedMqttServer(mqttServerOptions);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            app.UseCors("CorsPolicy");
            app.UseResponseCompression();


            var port = ServerInfo.WebPort;

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = SameSiteMode.Strict
            });
            app.UseAuthentication();


            string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "wwwroot");
            if (HybridSupport.IsElectronActive)
            {
                wwwrootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "wwwroot");
                SystemLogger.Instance.LogInformation($"Electron is active {wwwrootPath}");
            }
            if (!Directory.Exists(wwwrootPath))
            {
                wwwrootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
            }


            app.UseSignalR(routes =>
            {
                routes.MapHub<DataHub>("/signalr/dataHub", options =>
                {
                    options.ApplicationMaxBufferSize = 1024 * 1024;
                });
                routes.MapHub<TelegramHub>("/signalr/telegramHub");
                routes.MapHub<UpdateHub>("/signalr/updateHub");
            });



            SystemLogger.Instance.LogInformation($"wwwroot directory {wwwrootPath}");
            app.Map("/webapi", appBuilder =>
            {
                appBuilder.UseCors("CorsPolicy");
                appBuilder.UseAuthentication();
                appBuilder.UseMiddleware<WebApiErrorMiddleware>();
                appBuilder.UseMvc();
            });
            
            app.Use(async (context, next) => {
                await next();
                if (context.Response.StatusCode == 404 &&
                    !Path.HasExtension(context.Request.Path.Value) &&
                    !context.Request.Path.Value.StartsWith("/api/"))
                {
                    context.Request.Path = "/index.html";
                    context.Response.Cookies.Append("Electron", HybridSupport.IsElectronActive.ToString());
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



            if (HybridSupport.IsElectronActive)
            {
                Task.Run(async () => await ElectronBootstrap(port));
            }
        }

        public async Task ElectronBootstrap(string port)
        {
            var browserWindow = await Electron.WindowManager.CreateWindowAsync($"http://localhost:{port}");
            browserWindow.SetTitle("Automatica.Core");   

            browserWindow.WebContents.OpenDevTools(new OpenDevToolsOptions
            {
                Mode = DevToolsMode.undocked
            });
        }
        
    }
}
