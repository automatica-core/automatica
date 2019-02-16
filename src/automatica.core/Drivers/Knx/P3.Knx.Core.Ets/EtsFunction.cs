using System.Collections.Generic;
using System.Xml.Linq;

namespace P3.Knx.Core.Ets
{
    public enum EtsFunctionType
    {
        Custom,
        LightOnOff,
        Dimm,
        Blinds
    }

    public class EtsFunction : EtsEntity
    {
        internal EtsProjectParser.EtsProjectParserContext Context { get; }
        private readonly XNamespace _ns;
        public string Type { get; private set; }
        public EtsFunctionType EtsFunctionType { get; private set; }

        public IList<EtsGroupAddressRef> GroupAddressReferences { get; }

        internal EtsFunction(XElement e, XNamespace ns, EtsProjectParser.EtsProjectParserContext context) : base(e)
        {
            Context = context;
            _ns = ns;

            GroupAddressReferences = new List<EtsGroupAddressRef>();
        }

        public override void Aggregate(XElement e)
        {
            Type = GetAttributeValue(e, "Type");

            switch (Type)
            {
                case "FT-1":
                    EtsFunctionType = EtsFunctionType.LightOnOff;
                    break;
                case "FT-2":
                case "FT-6":
                    EtsFunctionType = EtsFunctionType.Dimm;
                    break;
                case "FT-3":
                case "FT-7":
                    EtsFunctionType = EtsFunctionType.Blinds;
                    break;
                default:
                    EtsFunctionType = EtsFunctionType.Custom;
                    break;
            }

            foreach (var groupAddrRef in e.Descendants(_ns + "GroupAddressRef"))
            {
                var etsGroupRef = new EtsGroupAddressRef(groupAddrRef, Context);
                etsGroupRef.Aggregate(groupAddrRef);
                GroupAddressReferences.Add(etsGroupRef);
            }

            base.Aggregate(e);
        }
    }
}
