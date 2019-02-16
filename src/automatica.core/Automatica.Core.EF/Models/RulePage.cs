using System;
using System.Collections.Generic;
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models
{
    public class RulePage : TypedObject
    {
        public RulePage()
        {
            Link = new List<Link>();
            NodeInstance2RulePage = new List<NodeInstance2RulePage>();
            RuleInstance = new List<RuleInstance>();
        }
        public Guid ObjId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public long This2RulePageType { get; set; }
        public bool IsDeleted { get; set; }

        public RulePageType This2RulePageTypeNavigation { get; set; }
        public List<Link> Link { get; set; }
        public List<NodeInstance2RulePage> NodeInstance2RulePage { get; set; }
        public List<RuleInstance> RuleInstance { get; set; }
    }
}
