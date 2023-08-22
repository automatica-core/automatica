using System.Runtime.CompilerServices;
using Automatica.Core.HyperSeries.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

[assembly:InternalsVisibleTo("Automatica.Core.Tests")]

namespace Automatica.Core.HyperSeries
{
    internal class HyperSeriesRepository : IHyperSeriesRepository
    {
        public HyperSeriesContext Context { get; }
        public bool IsActivated { get; }

        public HyperSeriesRepository(HyperSeriesContext context, IConfiguration config,
            ILogger<HyperSeriesRepository> logger)
        {
            Context = context;
            if (string.IsNullOrEmpty(config["db:hyperSeriesHost"]) ||
                string.IsNullOrEmpty(config["db:hyperSeriesUser"]) ||
                string.IsNullOrEmpty(config["db:hyperSeriesPassword"]) ||
                string.IsNullOrEmpty(config["db:hyperSeriesDatabase"]) ||
                string.IsNullOrEmpty(config["db:hyperSeriesPort"]))
            {
                logger.LogWarning("No hyperseries db params are set....");
                if(string.IsNullOrEmpty(config.GetConnectionString($"HyperSeriesConnection")))
                {
                    logger.LogWarning("No hyperseries connection string is set....");
                    IsActivated = false;
                    return;
                }
            }

            try
            {
                context.Database.Migrate();
                IsActivated = true;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Could not migrate db");
                IsActivated = false;
            }

        }

        public Task Add(RecordValue recordValue)
        {
            return Context.AddRecordValue(recordValue);
        }

        public async Task<List<AggregatedRecordValue>> GetAggregatedValues(AggregationType aggregationType, Guid id, DateTime? startDate, DateTime? endDate, int count)
        {
            var ret = new List<AggregatedRecordValue>();

            if (startDate.HasValue)
            {
                startDate = new DateTime(startDate.Value.Year, startDate.Value.Month, startDate.Value.Day, 0, 0, 0, DateTimeKind.Utc);
            }

            if (endDate.HasValue)
            {
                endDate = new DateTime(endDate.Value.Year, endDate.Value.Month, endDate.Value.Day, 23, 59, 59, DateTimeKind.Utc);
            }
            
            switch (aggregationType)
            {
                case AggregationType.Raw:
                    List<RecordValue> values;
                    if (startDate.HasValue && endDate.HasValue)
                        values = Context.RecordValues.Where(a => a.NodeInstanceId == id && a.Timestamp >= startDate && a.Timestamp <= endDate).Take(count).OrderByDescending(a => a.Timestamp).ToList();
                    else if (startDate.HasValue && !endDate.HasValue)
                        values = Context.RecordValues.Where(a => a.NodeInstanceId == id && a.Timestamp >= startDate).Take(count).OrderByDescending(a => a.Timestamp).ToList();
                    else if (!startDate.HasValue && endDate.HasValue)
                        values = Context.RecordValues.Where(a => a.NodeInstanceId == id && a.Timestamp <= endDate).Take(count).OrderByDescending(a => a.Timestamp).ToList();
                    else
                        values = Context.RecordValues.Where(a => a.NodeInstanceId == id).Take(count).OrderByDescending(a => a.Timestamp).ToList();

                    foreach (var recordValue in values)
                    {
                        ret.Add(new DayByDayAggregatedRecordValue
                        {
                            AverageValue = recordValue.Value,
                            Count = 1,
                            DifferenceValue = 0,
                            MaxValue = recordValue.Value,
                            MinValue = recordValue.Value,
                            NodeInstanceId =   recordValue.NodeInstanceId,
                            Timestamp = recordValue.Timestamp
                        });
                    }

                    break;
                case AggregationType.Hourly:
                    ret.AddRange(await BuildTimeBasedQuery(Context.HourByHourAggregatedValues, id, startDate, endDate, count));
                    break;
                case AggregationType.Daily:
                    ret.AddRange(await BuildTimeBasedQuery(Context.DayByDayAggregatedValues, id, startDate, endDate, count));
                    break;
                case AggregationType.Weekly:
                    ret.AddRange(await BuildTimeBasedQuery(Context.WeekByWeekAggregatedValues, id, startDate, endDate, count));
                    break;
                case AggregationType.Monthly:
                    ret.AddRange(await BuildTimeBasedQuery(Context.MonthByMonthAggregatedValues, id, startDate, endDate, count));
                    break;
                case AggregationType.Yearly:
                    ret.AddRange(await BuildTimeBasedQuery(Context.YearByYearAggregatedValues, id, startDate, endDate, count));
                    break;
             
            }

            return ret;
        }

        private async Task<List<T>> BuildTimeBasedQuery<T>(DbSet<T> dbSet, Guid id, DateTime? startDate,
            DateTime? endDate, int count) where T : AggregatedRecordValue
        {
            await Task.CompletedTask;

            if(startDate.HasValue && endDate.HasValue)
                return dbSet.Where(a => a.NodeInstanceId == id && a.Timestamp >= startDate && a.Timestamp <= endDate).Take(count).OrderByDescending(a => a.Timestamp).ToList();
            if(startDate.HasValue && !endDate.HasValue)
                return dbSet.Where(a => a.NodeInstanceId == id && a.Timestamp >= startDate).Take(count).OrderByDescending(a => a.Timestamp).ToList();
            if (!startDate.HasValue && endDate.HasValue)
                return dbSet.Where(a => a.NodeInstanceId == id && a.Timestamp <= endDate).Take(count).OrderByDescending(a => a.Timestamp).ToList();

            return dbSet.Where(a => a.NodeInstanceId == id).Take(count).OrderByDescending(a => a.Timestamp).ToList();
        }
    }
}
