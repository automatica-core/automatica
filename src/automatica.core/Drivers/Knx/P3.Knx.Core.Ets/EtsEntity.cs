using System;
using System.Xml.Linq;

namespace P3.Knx.Core.Ets
{
    public class EtsEntity
    {
        public string Id { get; protected set; }
        public string Name { get; set; }
        public string Description { get; protected set; }
        public int GroupIndex { get; protected set; }

        public int RangeStart { get; protected set; }
        public int RangeEnd { get; protected set; }

        public int GetMiddleGroup()
        {
            return (RangeStart & 0x700) >> 8;
        }
        public int GetMainGroup()
        {
            return (RangeStart & 0x7800) >> 11;
        }

        protected EtsEntity(XElement e)
        {
            Id = "";
            Name = "";
            Description = "";
            AggregateInternal(e);
        }
        protected string GetAttributeValue(XElement e, String attributeName)
        {
            XAttribute a = e.Attribute(attributeName);
            return a != null ? a.Value : "";
        }
        public virtual void Aggregate(XElement e)
        {
            AggregateInternal(e);
        }
        private void AggregateInternal(XElement e)
        {
            var id = GetAttributeValue(e, "Id");
            if (!String.IsNullOrEmpty(id))
                Id = id;

            var name = GetAttributeValue(e, "Name");
            if (!String.IsNullOrEmpty(name))
                Name = name;

            var desc = GetAttributeValue(e, "Description");
            if (!String.IsNullOrEmpty(desc))
                Description = desc;



            var rangeStart = GetAttributeValue(e, "RangeStart");
            if (!String.IsNullOrEmpty(rangeStart))
                RangeStart = Convert.ToInt32(rangeStart);
           
            var rangeEnd = GetAttributeValue(e, "RangeEnd");
            if (!String.IsNullOrEmpty(rangeEnd))
                RangeEnd = Convert.ToInt32(rangeEnd);
        }
    }
}
