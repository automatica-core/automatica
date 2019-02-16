using System;
using System.Collections.Generic;
using Automatica.Core.Model;
using Newtonsoft.Json;

namespace Automatica.Core.EF.Models
{

    public enum PropertyConstraint
    {
        None,
        Unique,
        UniqueOnCondition,
        Visible
    }

    public enum PropertyConstraintLevel
    {
        None,
        Info,
        Warn,
        Error
    }

    public enum PropertyConstraintConditionType
    {
        None,
        Unique,
        UniqueRange,
        ParentCondition
    }
    public partial class PropertyTemplate : TypedObject
    {
        
    }
}
