using Automatica.Core.HyperSeries.Model;

namespace Automatica.Core.HyperSeries
{
    public enum AggregationType
    {
        Raw,
        Hourly,
        Daily,
        Weekly,
        Monthly,
        Yearly
    }

    public interface IHyperSeriesRepository
    {
        bool IsActivated { get; }
        Task Add(RecordValue recordValue);

        Task<List<AggregatedRecordValue>> GetAggregatedValues(AggregationType aggregationType, Guid id,
            DateTime? startDate,
            DateTime? endDate,
            int count);
    }
}
