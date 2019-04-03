using Automatica.Core.Model;
using System;
using System.Globalization;

namespace Automatica.Core.EF.Models.Trendings
{
    public enum TrendingTypes
    {
        Average = 0,
        Raw = 1,
        Max = 2,
        Min = 3
    }
    public class Trending : TypedObject
    {
        public Guid ObjId { get; set; }
        public Guid This2NodeInstance { get; set; }

        public double Value { get; set; }
        public DateTime Timestamp { get; set; }

        public string TimestampIso => Timestamp.ToUniversalTime().ToString("o");

        public string Source { get; set; }
        public NodeInstance This2NodeInstanceNavigation { get; set; }
    }
}
