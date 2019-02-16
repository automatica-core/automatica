using System;

namespace Automatica.Core.EF.Models.AutomaticDesigner
{
    public enum ConditionType
    {
        Time,
        NodeInstance
    }

    public enum TimeConditionValue
    {
        Sunset,
        Sunrise
    }

    public enum ConditaionEval
    {
        Equals,
        NotEquals,
        Greater,
        Smaller,
        GreaterEqual,
        SmallerEqual
    }

    public class AutomaticCondition
    {
        public Guid ObjId { get; set; }
        public string Name { get; set; } //optional
        public ConditaionEval Eval { get; set; }

        public ConditionType Type { get; set; }
        public TimeConditionValue TimeConditionValue { get; set; }

        public Guid? This2NodeInstance { get; set; }
        public NodeInstance This2NodeInstanceNavigation { get; set; }
    }
}
