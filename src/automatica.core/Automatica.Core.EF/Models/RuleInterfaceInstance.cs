using System;
using MessagePack;


namespace Automatica.Core.EF.Models
{
    public partial class RuleInterfaceInstance
    {
        public Guid ObjId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ModifiedAt { get; set; }
        public Guid This2RuleInstance { get; set; }
        public Guid This2RuleInterfaceTemplate { get; set; }
        public bool IsDeleted { get; set; }

        [IgnoreMember]
        public RuleInstance This2RuleInstanceNavigation { get; set; }
        public RuleInterfaceTemplate This2RuleInterfaceTemplateNavigation { get; set; }

        public long? ValueInteger { get; set; }
        public double? ValueDouble { get; set; }
        public string ValueString { get; set; }
        
    }
}
