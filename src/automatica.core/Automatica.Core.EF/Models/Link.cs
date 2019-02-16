using System;
using Automatica.Core.Model;
using MessagePack;
using Newtonsoft.Json;


namespace Automatica.Core.EF.Models
{
    public class Link : TypedObject
    {

        public Guid ObjId { get; set; }
        public Guid This2RulePage { get; set; }
        public Guid? This2RuleInterfaceInstanceInput { get; set; }
        public Guid? This2RuleInterfaceInstanceOutput { get; set; }
        public Guid? This2NodeInstance2RulePageInput { get; set; }
        public Guid? This2NodeInstance2RulePageOutput { get; set; }
        public bool IsDeleted { get; set; }

        [JsonIgnore, IgnoreMember]
        public RulePage This2RulePageNavigation { get; set; }

        public RuleInterfaceInstance This2RuleInterfaceInstanceInputNavigation { get; set; }
        public RuleInterfaceInstance This2RuleInterfaceInstanceOutputNavigation { get; set; }
        public NodeInstance2RulePage This2NodeInstance2RulePageInputNavigation { get; set; }
        public NodeInstance2RulePage This2NodeInstance2RulePageOutputNavigation { get; set; }
    }
}
