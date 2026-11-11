using System.Collections.Generic;

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

        /// <summary>
        /// Enum items that carry their own range + scale (e.g. D2-14 sensors: 0..250 => 0..2000 ppm, 255 = error).
        /// Raw values outside every segment are status codes and have no physical value.
        /// </summary>
        public List<ScaleSegment> Segments { get; set; } = new List<ScaleSegment>();

        /// <summary>
        /// Two state enum given as two value ranges (A5-07-01 PIRS: 0..127 off, 128..255 on), dispatched as bool.
        /// </summary>
        public bool BoolEnum { get; set; }

        public DataField(Model.Type parent)
        {
            Parent = parent;
        }
    }

    public class ScaleSegment
    {
        public long RangeMin { get; set; }
        public long RangeMax { get; set; }
        public decimal ScaleMin { get; set; }
        public decimal ScaleMax { get; set; }
    }
}
