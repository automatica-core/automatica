using System;
using System.Collections.Generic;
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models
{
    public class RuleTemplate : TypedObject
    {
        public RuleTemplate()
        {
            RuleInterfaceTemplate = new List<RuleInterfaceTemplate>();
        }

        public Guid ObjId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ModifiedAt { get; set; }
        public Guid? Owner { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Key { get; set; }
        public string Group { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        public Guid This2DefaultMobileVisuTemplate { get; set; }

        public List<RuleInterfaceTemplate> RuleInterfaceTemplate { get; set; }


        public VisuObjectTemplate This2DefaultMobileVisuTemplateNavigation { get; set; }
    }
}
