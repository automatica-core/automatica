using System;

namespace P3.Driver.EepParser.Model
{
    public class DataField : BaseModel
    {
        public Type Parent { get; set; }
        public string Data { get; set; }
        public string ShortCut { get; set; }
        public string Description { get; set; }

        public int Offset { get; set; }
        public int Length { get; set; }

        public string Unit { get; set; }

        public Range Range { get; set; }

        public Scale Scale { get; set; }

        public Enumeration Enumeration { get; set; }

        public DataField(Model.Type parent)
        {
            Parent = parent;
        }
    }
}
