using System.Xml.Linq;

namespace P3.Knx.Core.Ets
{
    public class EtsGroupAddressRef : EtsEntity
    {
        private readonly EtsProjectParser.EtsProjectParserContext _context;
        public string RefId { get; set; }
        public string Role { get; set; }

        public EtsDatapoint Datapoint { get; private set; }

        internal EtsGroupAddressRef(XElement e, EtsProjectParser.EtsProjectParserContext context) : base(e)
        {
            _context = context;
        }

        public override void Aggregate(XElement e)
        {
            RefId = GetAttributeValue(e, "RefId");
            Role = GetAttributeValue(e, "Role");

            if (_context.Datapoints.ContainsKey(RefId))
            {
                Datapoint = _context.Datapoints[RefId];
            }

            base.Aggregate(e);
        }
    }
}
