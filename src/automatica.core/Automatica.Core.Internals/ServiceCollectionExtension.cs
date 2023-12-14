using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Internals.Cloud;
using Automatica.Core.Internals.Configuration;
using Automatica.Core.Internals.Templates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Automatica.Core.Internals
{
    public static class ServiceCollectionExtension
    {
        public static IConfigurationBuilder AddDatabaseConfiguration(this IConfigurationBuilder builder)
        {
            builder.Add(new DatabaseConfigurationSource());
            return builder;
        }

        public static void AddInternals(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<INodeInstanceCache, NodeInstanceCache>();
            services.AddSingleton<INodeInstanceService, NodeInstanceService>();

            services.AddSingleton<ISettingsCache, SettingsCache>();

            services.AddSingleton<IUserCache, UserCache>();
            services.AddSingleton<IUserGroupsCache, UserGroupsCache>();


            services.AddSingleton<ICategoryCache, CategoryCache>();
            services.AddSingleton<ICategoryGroupCache, CategoryGroupCache>();

            services.AddSingleton<IAreaCache, AreaCache>();
            services.AddSingleton<IAreaTemplateCache, AreaTemplateCache>();

            services.AddSingleton<ILogicInstanceCache, LogicInstanceCache>();
            services.AddSingleton<ILogicPageCache, LogicPageCache>();
            services.AddSingleton<ILogicTemplateCache, LogicTemplateCache>();
            services.AddSingleton<ILogicCacheFacade, LogicCacheFacade>();
            services.AddSingleton<ILogicInterfaceInstanceCache, LogicInterfaceInstanceCache>();
            services.AddSingleton<ILogicNodeInstanceCache, LogicNodeInstanceCache>();

            services.AddSingleton<ILinkCache, LinkCache>();

            services.AddTransient<ServerCloudApi>();
            services.AddTransient<CloudApi>();
            services.AddTransient<TextToSpeechApi>();

            services.AddTransient<IServerCloudApi>(a => a.GetRequiredService<ServerCloudApi>());
            services.AddTransient<ICloudApi>(a => a.GetRequiredService<CloudApi>());
            services.AddTransient<ITextToSpeechApi>(a => a.GetRequiredService<TextToSpeechApi>());

            services.AddTransient<SettingsFactory>();
        }
    }
}
