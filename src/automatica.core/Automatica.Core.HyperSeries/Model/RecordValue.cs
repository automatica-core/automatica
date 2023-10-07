using Microsoft.EntityFrameworkCore;

namespace Automatica.Core.HyperSeries.Model
{
    [Keyless]
    public record RecordValue
    {
        public DateTimeOffset Timestamp { get; set; }
        public Guid TrendId { get; set; }
        public Guid NodeInstanceId { get; set; }
        public double Value { get; set; }
    }
}
