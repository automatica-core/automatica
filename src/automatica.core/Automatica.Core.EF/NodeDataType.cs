using System;
using System.Collections.Generic;
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models
{
    public class NodeDataTypeEnumAttribute : Attribute
    {
        public NodeDataTypeEnumAttribute(string name)
        {
            Name = $"NODE_DATA_TYPE.{name.ToUpper()}.NAME";
            Description = $"NODE_DATA_TYPE.{name.ToUpper()}.DESCRIPTION";
        }

        public string Name { get; }
        public string Description { get; }
    }

    public partial class NodeDataType : TypedObject
    {
       
    }
}
