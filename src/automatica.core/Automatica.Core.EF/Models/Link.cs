using System;
using Automatica.Core.Model;
using Newtonsoft.Json;


namespace Automatica.Core.EF.Models
{
    public class Link : TypedObject
    {
        private NodeInstance2RulePage _this2NodeInstance2RulePageInputNavigation;
        private RuleInterfaceInstance _this2RuleInterfaceInstanceOutputNavigation;
        private RuleInterfaceInstance _this2RuleInterfaceInstanceInputNavigation;
        private NodeInstance2RulePage _this2NodeInstance2RulePageOutputNavigation;

        public Guid ObjId { get; set; }
        public Guid This2RulePage { get; set; }
        public Guid? This2RuleInterfaceInstanceInput { get; set; }
        public Guid? This2RuleInterfaceInstanceOutput { get; set; }
        public Guid? This2NodeInstance2RulePageInput { get; set; }
        public Guid? This2NodeInstance2RulePageOutput { get; set; }
        public bool IsDeleted { get; set; }

        [JsonIgnore]
        public RulePage This2RulePageNavigation { get; set; }

        public RuleInterfaceInstance This2RuleInterfaceInstanceInputNavigation
        {
            get => _this2RuleInterfaceInstanceInputNavigation;
            set
            {
                if (value != null)
                {
                    This2RuleInterfaceInstanceInput = value.ObjId;
                }

                _this2RuleInterfaceInstanceInputNavigation = value;
            }
        }

        public RuleInterfaceInstance This2RuleInterfaceInstanceOutputNavigation
        {
            get => _this2RuleInterfaceInstanceOutputNavigation;
            set {

                if (value != null)
                {
                    This2RuleInterfaceInstanceOutput = value.ObjId;
                }

                _this2RuleInterfaceInstanceOutputNavigation = value;
            }
        }

        public NodeInstance2RulePage This2NodeInstance2RulePageInputNavigation
        {
            get => _this2NodeInstance2RulePageInputNavigation;
            set
            {
                if (value != null)
                {
                    This2NodeInstance2RulePageInput = value.ObjId;
                }

                _this2NodeInstance2RulePageInputNavigation = value;
            }
        }

        public NodeInstance2RulePage This2NodeInstance2RulePageOutputNavigation
        {
            get => _this2NodeInstance2RulePageOutputNavigation;
            set
            {
                if (value != null)
                {
                    This2NodeInstance2RulePageOutput = value.ObjId;
                }

                _this2NodeInstance2RulePageOutputNavigation = value;
            }
        }
    }
}
