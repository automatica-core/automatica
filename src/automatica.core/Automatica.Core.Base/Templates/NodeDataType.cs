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
        Integer = 1,
        [NodeDataTypeEnum("Double")]
        Double = 2,
        [NodeDataTypeEnum("String")]
        String = 3,
        [NodeDataTypeEnum("Boolean")]
        Boolean = 4,
        [NodeDataTypeEnum("DateTime")]
        DateTime = 5,
        [NodeDataTypeEnum("Time")]
        Time = 6,
        [NodeDataTypeEnum("Date")]
        Date = 7,
        [NodeDataTypeEnum("WindowState")]
        WindowState = 8
    }
}
