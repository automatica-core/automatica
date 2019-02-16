using System;
using Automatica.Core.Model;
using Newtonsoft.Json;

namespace Automatica.Core.EF.Models
{
    public class BoardInterface : TypedObject
    {
        public Guid ObjId { get; set; }
        public Guid This2BoardType { get; set; }
        public Guid This2InterfaceType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Meta { get; set; }

        [JsonIgnore]
        public BoardType This2BoardTypeNavigation { get; set; }
        public InterfaceType This2InterfaceTypeNavigation { get; set; }
    }
}
