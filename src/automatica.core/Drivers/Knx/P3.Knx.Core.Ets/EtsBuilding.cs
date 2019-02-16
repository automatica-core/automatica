using System.Xml.Linq;

namespace P3.Knx.Core.Ets
{
    public class EtsBuilding : EtsBuildingPart
    {
        internal EtsBuilding(XElement e, XNamespace ns, EtsProjectParser.EtsProjectParserContext context) : base(e, ns, context)
        {
        }

        public override void Aggregate(XElement e)
        {
            foreach (var x in e.Elements(Ns + "BuildingPart"))
            {
                var buildingPart = new EtsBuildingPart(x, Ns, Context);
                buildingPart.Aggregate(x);
            }

            base.Aggregate(e);
        }
    }
}
