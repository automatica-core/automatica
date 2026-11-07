using System;
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models.Automations
{
    public enum AutomationReferenceType
    {
        NodeInstance = 0,
        Control = 1,
        Automation = 2,
        Area = 3,
        Category = 4
    }

    public enum AutomationReferenceRole
    {
        Trigger = 0,
        Condition = 1,
        Action = 2
    }

    /// <summary>
    /// Index of the objects an <see cref="Automation"/> uses, for "where used" and to warn before an object is deleted.
    /// Rebuilt from the definition on every save.
    /// </summary>
    public class AutomationReference : TypedObject
    {
        public Guid ObjId { get; set; }

        public Guid This2Automation { get; set; }
        public Automation This2AutomationNavigation { get; set; }

        public Guid ReferenceId { get; set; }
        public AutomationReferenceType ReferenceType { get; set; }
        public AutomationReferenceRole Role { get; set; }
    }
}
