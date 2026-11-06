using System.ComponentModel;
using Automatica.Core.Base.Common;
using Automatica.Core.HyperSeries;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;
using ModelContextProtocol.Server;

namespace Automatica.Core.Mcp.Tools
{
    [McpServerToolType]
    public class SystemTools
    {
        private readonly INodeInstanceCache _nodeInstanceCache;
        private readonly ILogicInstanceCache _logicInstanceCache;
        private readonly IAreaCache _areaCache;
        private readonly ICategoryCache _categoryCache;
        private readonly ISettingsCache _settingsCache;
        private readonly IHyperSeriesRepository _hyperSeriesRepository;
        private readonly McpAccess _access;
        private readonly NodeDescriber _describer;

        public SystemTools(INodeInstanceCache nodeInstanceCache, ILogicInstanceCache logicInstanceCache, IAreaCache areaCache, ICategoryCache categoryCache,
            ISettingsCache settingsCache, IHyperSeriesRepository hyperSeriesRepository, McpAccess access, NodeDescriber describer)
        {
            _describer = describer;
            _nodeInstanceCache = nodeInstanceCache;
            _logicInstanceCache = logicInstanceCache;
            _areaCache = areaCache;
            _categoryCache = categoryCache;
            _settingsCache = settingsCache;
            _hyperSeriesRepository = hyperSeriesRepository;
            _access = access;
        }

        [McpServerTool(Name = "get_system_info", Title = "Get server overview", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Returns an overview of the Automatica server: version, uptime, project name, number of drivers, data points, logics, areas and categories, and whether this caller may write values.")]
        public SystemInfo GetSystemInfo()
        {
            var nodes = _describer.AllNodes().ToList();
            var drivers = _nodeInstanceCache.Root?.InverseThis2ParentNodeInstanceNavigation?
                .Where(a => !a.IsDeleted)
                .SelectMany(a => a.InverseThis2ParentNodeInstanceNavigation ?? new List<EF.Models.NodeInstance>())
                .Where(a => !a.IsDeleted)
                .Select(a => new DriverInfo(a.ObjId, a.Name, _describer.GetTemplate(a)?.Name, a.IsDisabled, a.State.ToString()))
                .ToList() ?? new List<DriverInfo>();

            return new SystemInfo(
                "Automatica.Core",
                ServerInfo.GetServerVersion(),
                _settingsCache.GetByKey("projectName")?.ValueText,
                ServerInfo.StartupTime,
                DateTime.Now - ServerInfo.StartupTime,
                DateTime.Now,
                nodes.Count(_describer.IsDataPoint),
                nodes.Count(a => _describer.IsDataPoint(a) && a.Trending),
                _logicInstanceCache.All().Count(a => !a.IsDeleted),
                _areaCache.All().Count,
                _categoryCache.All().Count,
                _hyperSeriesRepository.IsActivated,
                _access.CanWrite,
                drivers);
        }
    }

    public record DriverInfo(Guid Id, string Name, string Type, bool Disabled, string State);

    public record SystemInfo(
        string Product,
        string Version,
        string ProjectName,
        DateTime StartedAt,
        TimeSpan Uptime,
        DateTime ServerTime,
        int DataPointCount,
        int DataPointsWithHistory,
        int LogicCount,
        int AreaCount,
        int CategoryCount,
        bool AggregatedHistoryAvailable,
        bool CanWriteValues,
        IList<DriverInfo> Drivers);
}
