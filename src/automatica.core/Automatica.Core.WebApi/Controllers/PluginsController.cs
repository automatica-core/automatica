using Automatica.Core.Base.Common;
using Automatica.Core.Common.Update;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cloud;
using Automatica.Core.Internals.Cloud.Model;
using Automatica.Core.Internals.Core;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Automatica.Core.WebApi.Controllers
{
    public class PluginState
    {
        public EF.Models.Plugin LoadedPlugin { get; set; }
        public EF.Models.Plugin CloudPlugin { get; set; }
    }

    [Route("plugins")]
    public class PluginsController : BaseController
    {
        private readonly ICloudApi _api;
        private readonly ICoreServer _coreServer;

        public PluginsController(AutomaticaContext dbContext, ICloudApi api, ICoreServer coreServer) : base(dbContext)
        {
            _api = api;
            _coreServer = coreServer;
        }

        [HttpGet, Route("plugins")]
        public async Task<IList<PluginState>> GetPlugins()
        {
            var plugins = await _api.GetLatestPlugins();
            var ret = new List<PluginState>();
            var loadedPlugins = DbContext.Plugins.Where(a => a.Loaded).ToList();

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
