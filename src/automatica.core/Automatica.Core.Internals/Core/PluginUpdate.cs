using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Internals.Core
{
    /// <summary>
    /// A downloaded and staged plugin package (in <see cref="Automatica.Core.Base.Common.ServerInfo.GetPluginUpdateDirectory"/>) that should be installed
    /// </summary>
    public record PluginUpdate(Plugin Plugin, string PackageFile);

    /// <summary>
    /// The outcome of <see cref="ICoreServer.UpdatePlugins"/>
    /// </summary>
    /// <param name="RestartRequired">a restart is still required, the packages that need it stay staged and are installed on the next start</param>
    /// <param name="Failed">the updates that were not installed (e.g. the package is not trusted), a restart would not change that</param>
    public record PluginUpdateSummary(bool RestartRequired, IReadOnlyCollection<PluginUpdate> Failed)
    {
        public static PluginUpdateSummary None { get; } = new(false, Array.Empty<PluginUpdate>());

        public bool IsFailed(PluginUpdate update) => Failed.Contains(update);
    }
}
