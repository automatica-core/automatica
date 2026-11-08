using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;

namespace P3.Logic.StringOperations.Common
{
    /// <summary>
    /// The status, status monitor and tracker tiles are enum values of newer cores (<see cref="VisuMobileObjectTemplateTypes"/>).
    /// They are looked up by name, so the plugin also builds and runs against core packages without them; such a core keeps
    /// the default (label) tile, which shows the status output.
    /// </summary>
    internal static class VisuTemplates
    {
        internal const string Status = "Status";
        internal const string StatusMonitor = "StatusMonitor";
        internal const string Tracker = "Tracker";

        /// <returns>false when the core does not know the tile yet</returns>
        internal static bool TryUse(ILogicTemplateFactory factory, Guid logicGuid, string templateName)
        {
            if (!Enum.TryParse(templateName, false, out VisuMobileObjectTemplateTypes template)
                || !Enum.IsDefined(typeof(VisuMobileObjectTemplateTypes), template))
            {
                return false;
            }
            factory.ChangeDefaultVisuTemplate(logicGuid, template);
            return true;
        }
    }
}
