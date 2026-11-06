using System.ComponentModel;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Driver;
using ModelContextProtocol.Server;

namespace Automatica.Core.Mcp.Tools
{
    [McpServerToolType]
    public class StructureTools
    {
        private readonly IAreaCache _areaCache;
        private readonly ICategoryCache _categoryCache;
        private readonly INodeInstanceCache _nodeInstanceCache;
        private readonly McpAccess _access;
        private readonly NodeDescriber _describer;

        public StructureTools(IAreaCache areaCache, ICategoryCache categoryCache, INodeInstanceCache nodeInstanceCache, McpAccess access, NodeDescriber describer)
        {
            _describer = describer;
            _areaCache = areaCache;
            _categoryCache = categoryCache;
            _nodeInstanceCache = nodeInstanceCache;
            _access = access;
        }

        [McpServerTool(Name = "list_areas", Title = "List areas", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Lists the areas (e.g. building, floors, rooms) with their parent and the number of data points assigned to each area.")]
        public IList<AreaInfo> ListAreas()
        {
            var areas = _areaCache.All().Where(a => _access.CanSee(a.This2UserGroup)).ToList();
            var byId = areas.ToDictionary(a => a.ObjId);
            var dataPointCounts = CountDataPoints(a => a.This2AreaInstance);

            return areas.Select(a => new AreaInfo(a.ObjId, a.Name, a.Description, a.This2Parent, BuildPath(a.ObjId, byId),
                    dataPointCounts.GetValueOrDefault(a.ObjId)))
                .OrderBy(a => a.Path)
                .ToList();
        }

        [McpServerTool(Name = "list_categories", Title = "List categories", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Lists the categories (e.g. lights, heating, blinds) with the number of data points assigned to each category.")]
        public IList<CategoryInfo> ListCategories()
        {
            var dataPointCounts = CountDataPoints(a => a.This2CategoryInstance);

            return _categoryCache.All()
                .Where(a => _access.CanSee(a.This2UserGroup))
                .Select(a => new CategoryInfo(a.ObjId, a.Name, a.Description, a.This2CategoryGroupNavigation?.Name, dataPointCounts.GetValueOrDefault(a.ObjId)))
                .OrderBy(a => a.Name)
                .ToList();
        }

        private Dictionary<Guid, int> CountDataPoints(Func<EF.Models.NodeInstance, Guid?> selector)
        {
            return _describer.AllNodes()
                .Where(a => !a.IsDeleted && _describer.IsDataPoint(a) && selector(a).HasValue)
                .GroupBy(a => selector(a)!.Value)
                .ToDictionary(a => a.Key, a => a.Count());
        }

        private static string BuildPath(Guid id, IReadOnlyDictionary<Guid, EF.Models.Areas.AreaInstance> byId)
        {
            var names = new List<string>();
            var guard = 0;
            Guid? current = id;

            while (current.HasValue && byId.TryGetValue(current.Value, out var area) && guard++ < 32)
            {
                names.Add(area.Name);
                current = area.This2Parent;
            }

            names.Reverse();
            return string.Join(" / ", names);
        }
    }

    public record AreaInfo(Guid Id, string Name, string Description, Guid? ParentId, string Path, int DataPointCount);

    public record CategoryInfo(Guid Id, string Name, string Description, string Group, int DataPointCount);
}
