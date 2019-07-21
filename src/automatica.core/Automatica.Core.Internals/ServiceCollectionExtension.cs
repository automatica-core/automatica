using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Automatica.Core.Internals
{
    public static class ServiceCollectionExtension
    {
        public static void AddInternals(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<INodeTemplateCache, NodeTemplateCache>();
            services.AddSingleton<INodeInstanceCache, NodeInstanceCache>();

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
        }
    }
}
