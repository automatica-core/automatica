using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Visu;
using Automatica.Core.Driver.LeanMode;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals;
using Automatica.Core.Runtime;
using Automatica.Core.Visu;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MQTTnet.Server;

namespace Automatica.Core.CI.CreateDatabase
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
            services.AddDbContext<AutomaticaContext>();

            services.AddSingleton<IConfigurationRoot>(Configuration);

            services.AddSingleton(new LocalizationProvider(SystemLogger.Instance));
            services.AddSingleton<IVisualisationFactory, VisuTempInit>();
            services.AddSingleton<ILearnMode, EmptyLearnMode>();
            services.AddSingleton<IMqttServer, EmptyMqttServer>();

            services.AddAutomaticaCoreService(Configuration, false);

            services.AddSingleton<ILogger>(NullLogger.Instance);

            services.AddSignalRCore();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
           // nothing to be done here
        }

        
    }
}
