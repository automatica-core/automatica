using System;
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models
{
    public enum NotificationSeverity
    {
        Info,
        Warning, 
        Error
    }
    public class Notification : TypedObject
    {
        public Guid ObjId { get; set; }
        public Guid? This2NodeInstance { get; set; }
        public NodeInstance? This2NodeInstanceNavigation { get; set; }


        public Guid This2RuleInstance { get; set; }
        public RuleInstance This2RuleInstanceNavigation { get; set; }

        public string Subject { get; set; }
        public string Body { get; set; }
        public NotificationSeverity Severity { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? DismissDate { get; set; }

    }
}
