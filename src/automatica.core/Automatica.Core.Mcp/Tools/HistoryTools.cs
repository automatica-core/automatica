using System.ComponentModel;
using Automatica.Core.EF.Models;
using Automatica.Core.HyperSeries;
using Automatica.Core.Internals.Cache.Driver;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Automatica.Core.Mcp.Tools
{
    [McpServerToolType]
    public class HistoryTools
    {
        private const int MaxRowsRead = 50_000;
        private const int MaxPoints = 2_000;

        private readonly IConfiguration _configuration;
        private readonly INodeInstanceCache _nodeInstanceCache;
        private readonly IHyperSeriesRepository _hyperSeriesRepository;
        private readonly McpAccess _access;

        public HistoryTools(IConfiguration configuration, INodeInstanceCache nodeInstanceCache, IHyperSeriesRepository hyperSeriesRepository, McpAccess access)
        {
            _configuration = configuration;
            _nodeInstanceCache = nodeInstanceCache;
            _hyperSeriesRepository = hyperSeriesRepository;
            _access = access;
        }

        [McpServerTool(Name = "get_history", Title = "Get value history", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Returns the recorded values (trending) of a data point in a time range, with min/max/average. Long ranges are reduced to at most maxPoints buckets (average, min and max per bucket).")]
        public async Task<HistoryResult> GetHistory(
            [Description("Data point id (it must have trending enabled, see search_datapoints withHistoryOnly).")] Guid id,
            [Description("Start of the range (ISO 8601). Default: 24 hours before 'to'.")] DateTime? from = null,
            [Description("End of the range (ISO 8601). Default: now.")] DateTime? to = null,
            [Description("Maximum number of points to return (10-2000).")] int maxPoints = 500,
            CancellationToken cancellationToken = default)
        {
            var node = _nodeInstanceCache.Get(id);
            if (node == null || node.IsDeleted || !_access.CanSee(node.This2UserGroup))
            {
                throw new McpException($"Data point {id} not found");
            }

            var end = to ?? DateTime.Now;
            var start = from ?? end.AddHours(-24);
            if (start >= end)
            {
                throw new McpException("'from' must be before 'to'");
            }

            maxPoints = Math.Clamp(maxPoints, 10, MaxPoints);

            await using var db = new AutomaticaContext(_configuration);
            var rows = await db.Trendings.AsNoTracking()
                .Where(a => a.This2NodeInstance == id && a.Timestamp >= start && a.Timestamp <= end)
                .OrderBy(a => a.Timestamp)
                .Select(a => new { a.Timestamp, a.Value })
                .Take(MaxRowsRead + 1)
                .ToListAsync(cancellationToken);

            var truncated = rows.Count > MaxRowsRead;
            if (truncated)
            {
                rows.RemoveAt(rows.Count - 1);
            }

            HistoryStatistics statistics = null;
            if (rows.Count > 0)
            {
                statistics = new HistoryStatistics(rows.Count, rows.Min(a => a.Value), rows.Max(a => a.Value), rows.Average(a => a.Value),
                    new HistoryPoint(rows[0].Timestamp, rows[0].Value, null, null),
                    new HistoryPoint(rows[^1].Timestamp, rows[^1].Value, null, null));
            }

            IList<HistoryPoint> points;
            var aggregated = rows.Count > maxPoints;
            if (aggregated)
            {
                // bucket over the span that actually has data, a wide requested range must not collapse it into a few points
                var first = rows[0].Timestamp;
                var bucketTicks = Math.Max(1, (rows[^1].Timestamp - first).Ticks / maxPoints + 1);
                points = rows
                    .GroupBy(a => (a.Timestamp - first).Ticks / bucketTicks)
                    .OrderBy(a => a.Key)
                    .Select(a => new HistoryPoint(first.AddTicks(a.Key * bucketTicks), a.Average(b => b.Value), a.Min(b => b.Value), a.Max(b => b.Value)))
                    .ToList();
            }
            else
            {
                points = rows.Select(a => new HistoryPoint(a.Timestamp, a.Value, null, null)).ToList();
            }

            return new HistoryResult(id, node.Name, start, end, node.Trending, aggregated, truncated, statistics, points);
        }

        [McpServerTool(Name = "get_aggregated_history", Title = "Get aggregated history", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Returns aggregated values (e.g. hourly or daily) of a data point from the HyperSeries (TimescaleDB) recorder. Only works when that recorder is configured.")]
        public async Task<AggregatedHistoryResult> GetAggregatedHistory(
            [Description("Data point id.")] Guid id,
            [Description("Aggregation: Raw, Hourly, Daily, Weekly, Monthly or Yearly.")] AggregationType aggregation = AggregationType.Hourly,
            [Description("Start of the range (ISO 8601), optional.")] DateTime? from = null,
            [Description("End of the range (ISO 8601), optional.")] DateTime? to = null,
            [Description("Number of values when no range is given (1-1000).")] int count = 24)
        {
            var node = _nodeInstanceCache.Get(id);
            if (node == null || node.IsDeleted || !_access.CanSee(node.This2UserGroup))
            {
                throw new McpException($"Data point {id} not found");
            }

            if (!_hyperSeriesRepository.IsActivated)
            {
                throw new McpException("The HyperSeries recorder is not configured on this server, use get_history instead.");
            }

            var values = await _hyperSeriesRepository.GetAggregatedValues(aggregation, id, from, to, Math.Clamp(count, 1, 1000));
            return new AggregatedHistoryResult(id, node.Name, aggregation.ToString(),
                values.Select(a => new AggregatedPoint(a.Timestamp, a.AverageValue, a.MinValue, a.MaxValue, a.DifferenceValue, a.Count)).ToList());
        }
    }

    public record HistoryPoint(DateTime Timestamp, double Value, double? Min, double? Max);

    public record HistoryStatistics(int Count, double Min, double Max, double Average, HistoryPoint First, HistoryPoint Last);

    public record HistoryResult(
        Guid Id,
        string Name,
        DateTime From,
        DateTime To,
        bool TrendingEnabled,
        bool Aggregated,
        bool Truncated,
        HistoryStatistics Statistics,
        IList<HistoryPoint> Points);

    public record AggregatedPoint(DateTimeOffset Timestamp, double Average, double Min, double Max, double Difference, int Count);

    public record AggregatedHistoryResult(Guid Id, string Name, string Aggregation, IList<AggregatedPoint> Values);
}
