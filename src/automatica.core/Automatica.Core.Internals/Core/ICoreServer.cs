using Automatica.Core.Common.Update;
using Automatica.Core.EF.Models;
using System;
using System.Collections.Generic;

namespace Automatica.Core.Internals.Core
{
    public interface ICoreServer
    {
        void Update();
        void ReinitAutomaticUpdate();

        NodeInstanceState GetNodeInstanceState(Guid id);

        IList<PluginManifest> GetLoadedPlugins();
        void LoadPlugin(EF.Models.Plugin plugin);
        void Restart();
    }
}
