using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace P3.Knx.Core.Ets
{
    public class EtsProject : EtsGroup
    {
        public GroupAddressStyle GroupAddressStyle { get; set; }

        public IList<EtsBuilding> Buildings { get; internal set; }

        public EtsProject(XElement e)
            : base(e, null)
        {
            GroupAddressStyle = GroupAddressStyle.Unknown;
            Buildings = new List<EtsBuilding>();
        }
        public override void Aggregate(XElement e)
        {
            base.Aggregate(e);

            XElement pi = e.Elements(e.GetDefaultNamespace() + "ProjectInformation").FirstOrDefault();
            if (pi == null)
                return;

            String groupAddressStyle = GetAttributeValue(pi, "GroupAddressStyle");
            if (String.IsNullOrEmpty(groupAddressStyle))
                return;

            Name = GetAttributeValue(pi, "Name");
            switch (groupAddressStyle)
            {
                case "ThreeLevel":
                    GroupAddressStyle = GroupAddressStyle.ThreeLevel;
                    break;
                case "TwoLevel":
                    GroupAddressStyle = GroupAddressStyle.TwoLevel;
                    break;
                default:
                    GroupAddressStyle = GroupAddressStyle.Free;
                    break;
            }
        }
    }
}
