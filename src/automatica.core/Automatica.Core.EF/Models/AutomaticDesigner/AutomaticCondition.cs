using System;

namespace Automatica.Core.EF.Models.AutomaticDesigner
{
    public enum ConditionType
    {
        Time = 1,
        NodeInstance = 2
    }

    public enum TimeConditionValue
    {
        Sunset = 0,
        Sunrise = 1,
        Dawn = 2,
        Dusk = 3,
        FixedTime = 4,
        FixedTimeRange = 5
    }

    public enum ConditionEval
    {
        Equals = 0,
        NotEquals = 1,
        Greater = 2,
        Smaller = 3,
        GreaterEqual = 4,
        SmallerEqual = 5,
        Between = 6
    }

    public class AutomaticCondition
    {
        public Guid ObjId { get; set; }
        public string Name { get; set; } //optional
        public ConditionEval Eval { get; set; }

        public ConditionType Type { get; set; }
        public TimeConditionValue TimeConditionValue { get; set; }

        public Guid? This2NodeInstance { get; set; }
        public NodeInstance This2NodeInstanceNavigation { get; set; }
        
        public string EvalValue { get; set; }

    }
}
