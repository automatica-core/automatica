using System;

namespace Automatica.Core.EF.Models.AutomaticDesigner
{
    public enum TriggerType
    {
        Value,
        ValueChanged,
        Time,
        Sun
    }

    public class AutomaticTrigger
    {
        public Guid ObjId { get; set; }

        public TriggerType TriggerType { get; set; }
        
        public Guid? This2NodeInstance { get; set; }
        public NodeInstance This2NodeInstanceNavigation { get; set; }

    }
}
