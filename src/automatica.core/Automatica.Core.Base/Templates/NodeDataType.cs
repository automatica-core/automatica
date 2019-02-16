using Automatica.Core.EF.Models;

namespace Automatica.Core.Base.Templates
{
    /// <summary>
    /// Available NodeDataTypes
    /// </summary>
    public enum NodeDataType
    {
        [NodeDataTypeEnum("NoAttribute")]
        NoAttribute = 0,
        [NodeDataTypeEnum("Integer")]
        Integer,
        [NodeDataTypeEnum("Double")]
        Double,
        [NodeDataTypeEnum("String")]
        String,
        [NodeDataTypeEnum("Boolean")]
        Boolean,
        [NodeDataTypeEnum("DateTime")]
        DateTime,
        [NodeDataTypeEnum("Time")]
        Time,
        [NodeDataTypeEnum("Date")]
        Date
    }
}
