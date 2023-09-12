using System;
using System.Collections.Generic;
using Automatica.Core.Base.Cache;
using Automatica.Core.Common.Update;
using Automatica.Core.Driver;
using Automatica.Core.Runtime.Abstraction.Plugins.Driver;

namespace Automatica.Core.Runtime.Core.Plugins.Drivers
{
    internal class DriverFactoryStore : GuidStoreBase<IDriverFactory>, IDriverFactoryStore
    {
        private readonly IDictionary<Guid, PluginManifest> _driverPluginStore = new Dictionary<Guid, PluginManifest>();
        public void AddDriver(IDriverFactory driver, PluginManifest pluginManifest)
        {
            if (!_driverPluginStore.ContainsKey(driver.DriverGuid))
            {
                _driverPluginStore.Add(driver.DriverGuid, pluginManifest);
            }

            Add(driver.DriverGuid, driver);
        }

        public PluginManifest? GetManifestForDriver(Guid driverGuid)
        {
            if (_driverPluginStore.TryGetValue(driverGuid, out var driver))
            {
                return driver;
            }

            return null;
        }
    }
}
