using System;
using System.Collections.Generic;
using Automatica.Core.EF.Models.Areas;
using Automatica.Core.EF.Models.Categories;
using Automatica.Core.Model;
using Automatica.Core.Model.Models.User;
using MessagePack;
using Newtonsoft.Json;


namespace Automatica.Core.EF.Models
{
    public class RuleInstance : TypedObject
    {
        public RuleInstance()
        {
            RuleInterfaceInstance = new HashSet<RuleInterfaceInstance>();
        }


        public Guid ObjId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid This2RuleTemplate { get; set; }
        public Guid This2RulePage { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public bool IsDeleted { get; set; }

        public UserGroup This2UserGroupNavigation { get; set; }

        public bool UseInVisu { get; set; }
        public Guid? This2UserGroup { get; set; }
        public Guid? This2AreaInstance { get; set; }
        public bool IsFavorite { get; set; }
        public int Rating { get; set; }

        public AreaInstance This2AreaInstanceNavigation { get; set; }

        public Guid? This2CategoryInstance { get; set; }
        public CategoryInstance This2CategoryInstanceNavigation { get; set; }

        [JsonIgnore, IgnoreMember]
        public RulePage This2RulePageNavigation { get; set; }

        public RuleTemplate This2RuleTemplateNavigation { get; set; }
        public ICollection<RuleInterfaceInstance> RuleInterfaceInstance { get; set; }

        public static RuleInstance CreateFromTemplate(RuleTemplate template)
        {
            if (template == null)
            {
                throw new ArgumentException($"{nameof(template)} cannot be null");
            }

            var ruleInstance = new RuleInstance
            {
                This2RuleTemplate = template.ObjId,
                This2RuleTemplateNavigation = template,
                Name = template.Name,
                Description = template.Description,
                ObjId = Guid.NewGuid()
            };

            foreach (var ruleInterfaceTemplate in template.RuleInterfaceTemplate)
            {
                var ruleInterface = new RuleInterfaceInstance
                {
                    ObjId = Guid.NewGuid(),
                    This2RuleInterfaceTemplate = ruleInterfaceTemplate.ObjId,
                    This2RuleInterfaceTemplateNavigation = ruleInterfaceTemplate,
                    Value = ruleInterfaceTemplate.DefaultValue,
                    This2RuleInstance = ruleInstance.ObjId
                };

                ruleInstance.RuleInterfaceInstance.Add(ruleInterface);

            }

            return ruleInstance;
        }
    }
}
