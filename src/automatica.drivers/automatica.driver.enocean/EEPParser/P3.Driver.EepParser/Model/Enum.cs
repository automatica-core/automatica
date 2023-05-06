using System.Collections.Generic;

namespace P3.Driver.EepParser.Model
{
    public class EnumerationItem
    {
        public long? Min { get; set; }
        public long? Max { get; set; }
        public string Description { get; set; }

        public int? Value { get; set; }
    }

    public class Enumeration
    {
        public EnumerationItem First { get; set; }
        public EnumerationItem Second { get; set; }
    }
}
