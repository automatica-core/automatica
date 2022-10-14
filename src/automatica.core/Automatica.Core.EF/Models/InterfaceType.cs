using System;
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models
{
    public class InterfaceType : TypedObject
    {
        public Guid Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int MaxChilds { get; set; }
        public int MaxInstances { get; set; }

        public bool IsDriverInterface { get; set; }

        public bool CanProvideBoardType { get; set; }

        public Guid FactoryReference { get; set; }
    }
}
