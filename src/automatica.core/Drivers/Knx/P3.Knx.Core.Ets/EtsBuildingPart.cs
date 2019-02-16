using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace P3.Knx.Core.Ets
{
    public enum EtsBuildingType
    {
        Building,
        BuildingPart,
        Floor,
        Room,
        Corridor,
        Stairway
    }
    public class EtsBuildingPart : EtsEntity
    {
        public XNamespace Ns { get; }
        internal EtsProjectParser.EtsProjectParserContext Context { get; }
        public string Type { get; private set; }
        public EtsBuildingType EtsBuildingType { get; private set; }

        public List<EtsBuildingPart> Children { get;}

        public List<EtsFunction> Functions { get;  }

        internal EtsBuildingPart(XElement e, XNamespace ns, EtsProjectParser.EtsProjectParserContext context) : base(e)
        {
            Ns = ns;
            Context = context;
            Children = new List<EtsBuildingPart>();
            Functions = new List<EtsFunction>();
        }

        public override void Aggregate(XElement e)
        {
            Type = GetAttributeValue(e, "Type");

            EtsBuildingType = Enum.TryParse<EtsBuildingType>(Type, out var type) ? type : EtsBuildingType.Room;


            ParseBuildingParts(e);

            foreach (var function in e.Elements(Ns + "Function"))
            {
                var etsFun = new EtsFunction(function, Ns, Context);
                etsFun.Aggregate(function);
                Functions.Add(etsFun);
            }

            base.Aggregate(e);
        }

        private void ParseBuildingParts(XElement p)
        {
            foreach (var buildingPart in p.Elements(Ns + "Space"))
            {
                var bu = new EtsBuildingPart(buildingPart, Ns, Context);
                bu.Aggregate(buildingPart);

                Children.Add(bu);
            }
        }
    }
}
