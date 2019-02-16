using System;
using System.Collections.Generic;
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models
{
    public class PropertyType : TypedObject
    {

        public long Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Meta { get; set; }
        
    }
}
