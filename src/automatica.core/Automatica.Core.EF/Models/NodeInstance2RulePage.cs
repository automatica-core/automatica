using System;
using Automatica.Core.Model;
using Newtonsoft.Json;


namespace Automatica.Core.EF.Models
{
    public class NodeInstance2RulePage : TypedObject
    {

        public Guid ObjId { get; set; }
        public Guid This2RulePage { get; set; }
        public Guid This2NodeInstance { get; set; }
        public bool IsDeleted { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public bool Inverted { get; set; }
        
        public NodeInstance This2NodeInstanceNavigation { get; set; }
        public RulePage This2RulePageNavigation { get; set; }

        
    }
}
