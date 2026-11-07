using System;
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models.Automations
{
    /// <summary>
    /// A finished run of an <see cref="Automation"/> with its steps (JSON), only the latest runs per automation are kept.
    /// </summary>
    public class AutomationRun : TypedObject
    {
        public Guid ObjId { get; set; }

        public Guid This2Automation { get; set; }
        public Automation This2AutomationNavigation { get; set; }

        /// <summary>UTC</summary>
        public DateTime StartedAt { get; set; }
        /// <summary>UTC</summary>
        public DateTime? FinishedAt { get; set; }

        public AutomationRunState State { get; set; }

        /// <summary>
        /// Id of the trigger that started the run, empty for a manual run
        /// </summary>
        public string TriggerId { get; set; }

        /// <summary>
        /// JSON array of the steps (trigger, conditions, actions) with their results
        /// </summary>
        public string Steps { get; set; }

        public string Error { get; set; }
    }
}
