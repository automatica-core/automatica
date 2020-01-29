using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cloud;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Automatica.Core.WebApi.Controllers
{
    public class PluginState
    {
        public Plugin LoadedPlugin { get; set; }
        public Plugin CloudPlugin { get; set; }
    }

    [Route("webapi/plugins")]
    public class PluginsController : BaseController
    {
        private readonly ICloudApi _api;

        public PluginsController(AutomaticaContext dbContext, ICloudApi api) : base(dbContext)
        {
            _api = api;
        }

        [HttpGet, Route("plugins")]
        public async Task<IList<PluginState>> GetPlugins()
        {
            var plugins = await _api.GetLatestPlugins();
            var ret = new List<PluginState>();
            var loadedPlugins = DbContext.Plugins.AsNoTracking().Where(a => a.Loaded).ToList();

            
            foreach (var cloudPlugin in plugins)
            {
                var loadedPlugin = loadedPlugins.SingleOrDefault(a => a.PluginGuid == cloudPlugin.PluginGuid);

                ret.Add(new PluginState
                {
                    CloudPlugin = cloudPlugin,
                    LoadedPlugin = loadedPlugin
                });
            }


            return ret;
        }
    }
}
