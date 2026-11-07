using System;
using System.Collections.Generic;
using Automatica.Core.EF.Models.Areas;
using Automatica.Core.EF.Models.Categories;
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models.Automations
{
    /// <summary>
    /// What happens when a trigger fires while a run of the same automation is still active.
    /// </summary>
    public enum AutomationMode
    {
        /// <summary>New triggers are ignored while a run is active</summary>
        Single = 0,
        /// <summary>The active run is stopped and a new one starts</summary>
        Restart = 1,
        /// <summary>New triggers wait and run one after the other</summary>
        Queued = 2,
        /// <summary>Every trigger starts its own run</summary>
        Parallel = 3
    }

    public enum AutomationRunState
    {
        Running = 0,
        Waiting = 1,
        Done = 2,
        ConditionsNotMet = 3,
        Failed = 4,
        Stopped = 5
    }

    /// <summary>
    /// An "if this then that" rule: triggers, conditions and actions. The blocks are stored as a JSON document in
    /// <see cref="Definition"/> (see Automatica.Core.Automation.Definition), the datapoints it uses are indexed in
    /// <see cref="References"/>.
    /// </summary>
    public class Automation : TypedObject
    {
        public Guid ObjId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsEnabled { get; set; }

        public AutomationMode Mode { get; set; }

        /// <summary>
        /// Upper limit of runs for <see cref="AutomationMode.Queued"/> and <see cref="AutomationMode.Parallel"/>
        /// </summary>
        public int MaxRuns { get; set; } = 10;

        public Guid? This2AreaInstance { get; set; }
        public AreaInstance This2AreaInstanceNavigation { get; set; }

        public Guid? This2CategoryInstance { get; set; }
        public CategoryInstance This2CategoryInstanceNavigation { get; set; }

        /// <summary>
        /// JSON document with triggers, conditions and actions
        /// </summary>
        public string Definition { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }

        /// <summary>
        /// Start of the latest run (UTC)
        /// </summary>
        public DateTime? LastRunAt { get; set; }
        public AutomationRunState? LastRunState { get; set; }
        public string LastError { get; set; }

        public ICollection<AutomationReference> References { get; set; } = new List<AutomationReference>();
    }
}
