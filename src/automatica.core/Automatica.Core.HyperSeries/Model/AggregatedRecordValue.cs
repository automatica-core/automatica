using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Automatica.Core.HyperSeries.Model
{
    public class HourByHourAggregatedRecordValue : AggregatedRecordValue
    {

    }
    public class DayByDayAggregatedRecordValue : AggregatedRecordValue
    {

    }
    public class WeekByWeekAggregatedRecordValue : AggregatedRecordValue
    {

    }
    public class MonthByMonthAggregatedRecordValue : AggregatedRecordValue
    {

    }
    public class YearByYearAggregatedRecordValue : AggregatedRecordValue
    {

    }

    [Keyless]
    public abstract class AggregatedRecordValue
    {
        [Column("time")]
        public DateTimeOffset Timestamp { get; set; }

        [Column("avgvalue")]
        public double AverageValue { get; set; }

        [Column("diffvalue")]
        public double DifferenceValue { get; set; }

        [Column("maxvalue")]
        public double MaxValue { get; set; }

        [Column("minvalue")]
        public double MinValue { get; set; }

        [Column("countvalue")]
        public int Count { get; set; }

        [Column("nodeinstanceid")]
        public Guid NodeInstanceId { get; set; }
    }
}
