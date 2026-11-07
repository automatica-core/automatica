using Automatica.Core.EF.Models;

namespace Automatica.Core.Internals.Core
{
    /// <summary>
    /// A downloaded and staged plugin package (in <see cref="Automatica.Core.Base.Common.ServerInfo.GetPluginUpdateDirectory"/>) that should be installed
    /// </summary>
    public record PluginUpdate(Plugin Plugin, string PackageFile);
}
