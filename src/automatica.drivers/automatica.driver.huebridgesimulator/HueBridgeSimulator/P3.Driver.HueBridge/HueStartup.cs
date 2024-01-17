using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using P3.Driver.HueBridge.Api;
using Serilog;
using Serilog.Filters;
using System.Reflection;

namespace P3.Driver.HueBridge
{
    public class LowerCaseContractResolver : DefaultContractResolver
    {
        protected override string ResolvePropertyName(string propertyName)
        {
            return propertyName.ToLower();
        }
    }

    public class HueStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .MinimumLevel.Verbose()
                .Filter.ByExcluding(Matching.FromSource("Microsoft"))
                .CreateLogger();

            services.AddCors();
            services.AddMvc();

            services.AddMvcCore(config =>
                {

                }).AddApplicationPart(typeof(HueController).GetTypeInfo().Assembly).AddControllersAsServices()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new LowerCaseContractResolver();
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                });

        }
        public void Configure(IApplicationBuilder app)
        {
            app.UseMiddleware<SetLightMiddleware>();
            app.UseCors(builder =>
                builder.AllowAnyHeader());

            app.UseMvc();
        }
    }
}
