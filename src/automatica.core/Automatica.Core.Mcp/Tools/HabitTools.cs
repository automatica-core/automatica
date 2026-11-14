using System.ComponentModel;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Habits;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Automatica.Core.Mcp.Tools
{
    /// <summary>
    /// Read access to what the habit recorder stored. It shows what the residents did and when, so it is gated like the logs.
    /// </summary>
    [McpServerToolType]
    public class HabitTools
    {
        private readonly INodeInstanceCache _nodeInstanceCache;
        private readonly ILogicInterfaceInstanceCache _logicInterfaceInstanceCache;
        private readonly ILogicInstanceCache _logicInstanceCache;
        private readonly NodeDescriber _describer;
        private readonly McpAccess _access;
        private readonly IConfiguration _configuration;

        public HabitTools(INodeInstanceCache nodeInstanceCache, ILogicInterfaceInstanceCache logicInterfaceInstanceCache, ILogicInstanceCache logicInstanceCache,
            NodeDescriber describer, McpAccess access, IConfiguration configuration)
        {
            _nodeInstanceCache = nodeInstanceCache;
            _logicInterfaceInstanceCache = logicInterfaceInstanceCache;
            _logicInstanceCache = logicInstanceCache;
            _describer = describer;
            _access = access;
            _configuration = configuration;
        }

        [McpServerTool(Name = "get_habit_stats", Title = "Habit recorder statistics", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("What the habit recorder stored in the last hours: rows per kind (Visualization = visu taps, DataPoint = value changes reported by drivers, Measurement = 15 min averages of decimal data points) and the data points with the most rows, to judge the volume and spot noisy data points. Administrators only (API key: setting 'mcpAllowLogs').")]
        public async Task<HabitStats> GetHabitStats(
            [Description("How many hours back, 1 to 720 (default 24).")] int hours = 24,
            [Description("How many data points to list, 1 to 200 (default 30).")] int top = 30,
            CancellationToken cancellationToken = default)
        {
            EnsureAllowed();
            hours = Math.Clamp(hours, 1, 720);
            top = Math.Clamp(top, 1, 200);
            var since = DateTime.UtcNow.AddHours(-hours);

            await using var db = new AutomaticaContext(_configuration);
            var rows = db.HabitActions.Where(a => a.UtcTime >= since);
            var byKind = await rows.GroupBy(a => a.Origin).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
            var dataPoints = await rows.Select(a => a.TargetId).Distinct().CountAsync(cancellationToken);
            var topTargets = await rows.GroupBy(a => new { a.TargetId, a.Origin })
                .Select(g => new { g.Key.TargetId, g.Key.Origin, Count = g.Count(), Last = g.Max(a => a.UtcTime) })
                .OrderByDescending(a => a.Count)
                .Take(top)
                .ToListAsync(cancellationToken);

            return new HabitStats(
                hours,
                byKind.Sum(a => a.Count),
                dataPoints,
                byKind.ToDictionary(a => a.Key.ToString(), a => a.Count),
                topTargets.Select(a =>
                {
                    var (name, path) = Describe(a.TargetId);
                    return new HabitTarget(a.TargetId, name, path, a.Origin.ToString(), a.Count, ToLocal(a.Last));
                }).ToList());
        }

        [McpServerTool(Name = "get_habit_actions", Title = "Recorded habit actions", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("The rows the habit recorder stored, newest first: time, data point, kind, value (the average for measurements, with min and max), area and the active operating modes. Filter by data point and kind to see what a habit would be built from. Administrators only (API key: setting 'mcpAllowLogs').")]
        public async Task<IList<HabitRow>> GetHabitActions(
            [Description("Only this data point or logic input (optional).")] Guid? id = null,
            [Description("Only this kind: Visualization, DataPoint or Measurement (optional).")] string kind = null,
            [Description("How many hours back, 1 to 720 (default 24).")] int hours = 24,
            [Description("How many rows, 1 to 1000 (default 100).")] int limit = 100,
            CancellationToken cancellationToken = default)
        {
            EnsureAllowed();
            HabitActionOrigin? origin = null;
            if (!string.IsNullOrWhiteSpace(kind))
            {
                origin = Enum.TryParse<HabitActionOrigin>(kind, true, out var parsed) && Enum.IsDefined(parsed)
                    ? parsed
                    : throw new McpException($"Unknown kind '{kind}', use Visualization, DataPoint or Measurement");
            }

            var since = DateTime.UtcNow.AddHours(-Math.Clamp(hours, 1, 720));
            await using var db = new AutomaticaContext(_configuration);
            var query = db.HabitActions.AsNoTracking().Where(a => a.UtcTime >= since);
            if (id.HasValue)
            {
                query = query.Where(a => a.TargetId == id.Value);
            }
            if (origin.HasValue)
            {
                query = query.Where(a => a.Origin == origin.Value);
            }

            var actions = await query.OrderByDescending(a => a.UtcTime).Take(Math.Clamp(limit, 1, 1000)).ToListAsync(cancellationToken);
            return actions.Select(a => new HabitRow(ToLocal(a.UtcTime), a.TargetId, Describe(a.TargetId).Name, a.Origin.ToString(), a.Value, a.Min, a.Max,
                _describer.GetAreaName(a.This2AreaInstance), a.OperatingModes)).ToList();
        }

        private void EnsureAllowed()
        {
            if (!_access.CanReadLogs)
            {
                throw new McpException(_access.IsApiKeyCaller
                    ? "Habit data is disabled for the MCP API key. Enable the setting 'mcpAllowLogs' or log in as an administrator."
                    : "Habit data is available to administrators only");
            }
        }

        private (string Name, string Path) Describe(Guid id)
        {
            var node = _nodeInstanceCache.Get(id);
            if (node != null)
            {
                return _access.CanSee(node.This2UserGroup) ? (node.Name, _describer.GetPath(node)) : (null, null);
            }

            var logicInterface = _logicInterfaceInstanceCache.Get(id);
            if (logicInterface == null)
            {
                return (null, null);
            }

            var logic = _logicInstanceCache.Get(logicInterface.This2RuleInstance);
            return (logicInterface.This2RuleInterfaceTemplateNavigation?.Name, logic == null ? null : $"Logic {logic.Name}");
        }

        private static DateTime ToLocal(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
    }

    public record HabitStats(int Hours, int Rows, int DataPoints, IDictionary<string, int> RowsByKind, IList<HabitTarget> Top);

    public record HabitTarget(Guid Id, string Name, string Path, string Kind, int Rows, DateTime LastRow);

    public record HabitRow(DateTime Time, Guid TargetId, string Name, string Kind, string Value, double? Min, double? Max, string Area, string OperatingModes);
}
