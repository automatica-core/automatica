using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace P3.Knx.Core.Ets
{
    public class EtsDatapoint : EtsEntity
    {
        public EtsGroup Parent { get; }
        public int Address { get; }
        public bool HasDeviceLinked { get; set; }

        private readonly IList<String> _datapointTypes = new List<string>();

        public EtsDatapoint(XElement e, EtsGroup parent)
            : base(e)
        {
            Parent = parent;
            AddDatapointTypeOrSize(GetAttributeValue(e, "DatapointType"));

            Address = int.Parse(GetAttributeValue(e, "Address"));
            GroupIndex = parent.Level == 0 ? Address % 2048 : Address % 256;
        }
        public override void Aggregate(XElement e)
        {
            base.Aggregate(e);
            AddDatapointTypeOrSize(GetAttributeValue(e, "DatapointType"));
        }

        public string GetGroupAddress()
        {
            if (Parent?.Parent != null)
            {
                return $"{Parent.Parent.GetMainGroup()}/{Parent.GetMiddleGroup()}/{GetAddress()}";
            }

            return String.Empty;
        }

        public IEnumerable<String> DatapointTypes => _datapointTypes;

        public void AddDatapointTypeOrSize(string type)
        {
            if (String.IsNullOrEmpty(type))
                return;

            if (type.StartsWith("DP", StringComparison.InvariantCultureIgnoreCase))
            {
                foreach (string splitted in type.Split(' '))
                    if (!_datapointTypes.Contains(splitted))
                        _datapointTypes.Add(splitted);
            }
            else
            {
                if (int.TryParse(type.Split(' ')[0], out var newDatapointSize))
                {
                    if (type.Contains("Byte"))
                        newDatapointSize *= 8;
                    if (DatapointSizeBits == 0 || newDatapointSize < DatapointSizeBits)
                        DatapointSizeBits = newDatapointSize;
                }
            }
        }

        public int GetAddress()
        {
            return Address & 0xFF;
        }




        public int DatapointSizeBits { get; private set; }

        public IEnumerable<Tuple<int, int>> DatapointTypesSplitted { get { return _datapointTypes.Select(dpt => SplitDatapoint(dpt)).ToList(); } }

        private Tuple<int, int> SplitDatapoint(string dpt)
        {
            // e.g. DPT-1 or DPST-1-1
            String[] splitted = dpt.Split('-');
            int main = -1;
            int sub = -1;
            if (splitted.Length > 1)
                main = int.Parse(splitted[1]);
            if (splitted.Length > 2)
                sub = int.Parse(splitted[2]);

            return new Tuple<int, int>(main, sub);
        }
    }
}
