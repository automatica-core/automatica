using System;
using Automatica.Core.Base.Cache;
using Automatica.Core.Common.Update;
using Automatica.Core.Driver;

namespace Automatica.Core.Runtime.Abstraction.Plugins.Driver
{
    internal interface IDriverFactoryStore : IStore<IDriverFactory>
    {
        void AddDriver(IDriverFactory driver, PluginManifest pluginManifest);

        PluginManifest? GetManifestForDriver(Guid driverGuid);

        /// <summary>
        /// Removes the factory and its manifest, e.g. before a new version of the plugin is loaded
        /// </summary>
        void RemoveDriver(Guid driverGuid);
    }
}
