using System;

namespace Automatica.Core.EF.Models.AutomaticDesigner
{
    public enum ActionWriteType
    {
        NodeInstance,
        Group,
        Category,
        Area
    }
    public class AutomaticAction
    {
        public Guid ObjId { get; set; }
        public string Name { get; set; }
        
    }
}
