using System.ComponentModel;
using System.Text.Json;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Calendar;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Automatica.Core.Mcp.Tools
{
    [McpServerToolType]
    public class DataPointTools
    {
        private const int MaxResults = 500;

        private readonly INodeInstanceCache _nodeInstanceCache;
        private readonly ILogicInterfaceInstanceCache _logicInterfaceInstanceCache;
        private readonly ILogicInstanceCache _logicInstanceCache;
        private readonly IDispatcher _dispatcher;
        private readonly NodeDescriber _describer;
        private readonly McpAccess _access;
        private readonly ILogger<DataPointTools> _logger;

        public DataPointTools(INodeInstanceCache nodeInstanceCache, ILogicInterfaceInstanceCache logicInterfaceInstanceCache, IDispatcher dispatcher,
            NodeDescriber describer, McpAccess access, ILogger<DataPointTools> logger, ILogicInstanceCache logicInstanceCache)
        {
            _logicInstanceCache = logicInstanceCache;
            _nodeInstanceCache = nodeInstanceCache;
            _logicInterfaceInstanceCache = logicInterfaceInstanceCache;
            _dispatcher = dispatcher;
            _describer = describer;
            _access = access;
            _logger = logger;
        }

        [McpServerTool(Name = "search_datapoints", Title = "Search data points", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Finds data points (nodes that carry a value, e.g. a switch, a temperature or a meter) by name, path, area or category. Returns ids, paths and current values.")]
        public IList<NodeSummary> SearchDataPoints(
            [Description("Text to look for in the name, description, path, area or category (case insensitive). Leave empty to list all.")] string query = null,
            [Description("Only data points in this area (or one of its sub areas), see list_areas.")] Guid? areaId = null,
            [Description("Only data points in this category, see list_categories.")] Guid? categoryId = null,
            [Description("Only data points that can be written.")] bool writeableOnly = false,
            [Description("Only data points that record history (trending).")] bool withHistoryOnly = false,
            [Description("Maximum number of results (1-500).")] int limit = 50)
        {
            limit = Math.Clamp(limit, 1, MaxResults);
            var areaIds = areaId.HasValue ? _describer.GetAreaWithChildren(areaId.Value) : null;

            var result = new List<NodeSummary>();
            foreach (var node in _describer.AllNodes())
            {
                if (node.IsDeleted || !_describer.IsDataPoint(node) || !_access.CanSee(node.This2UserGroup))
                {
                    continue;
                }

                if (writeableOnly && !node.IsWriteable || withHistoryOnly && !node.Trending)
                {
                    continue;
                }

                if (areaIds != null && (!node.This2AreaInstance.HasValue || !areaIds.Contains(node.This2AreaInstance.Value)))
                {
                    continue;
                }

                if (categoryId.HasValue && node.This2CategoryInstance != categoryId)
                {
                    continue;
                }

                var summary = _describer.Describe(node, includeValue: true);
                if (!string.IsNullOrWhiteSpace(query) && !Matches(query, node, summary))
                {
                    continue;
                }

                result.Add(summary);
                if (result.Count >= limit)
                {
                    break;
                }
            }

            return result;
        }

        [McpServerTool(Name = "browse_nodes", Title = "Browse the node tree", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Lists the children of a node in the configuration tree (drivers, devices, data points). Without parentId it starts at the root.")]
        public NodeChildren BrowseNodes([Description("Id of the parent node, empty for the root.")] Guid? parentId = null)
        {
            var parent = parentId.HasValue ? _nodeInstanceCache.Get(parentId.Value) : _nodeInstanceCache.Root;
            if (parent == null || parent.IsDeleted || !_access.CanSee(parent.This2UserGroup))
            {
                throw new McpException($"Node {parentId} not found");
            }

            var children = (parent.InverseThis2ParentNodeInstanceNavigation ?? new List<NodeInstance>())
                .Where(a => !a.IsDeleted && _access.CanSee(a.This2UserGroup))
                .OrderBy(a => a.Name)
                .Select(a => _describer.Describe(a, includeValue: true))
                .ToList();

            return new NodeChildren(_describer.Describe(parent, includeValue: true), children);
        }

        [McpServerTool(Name = "get_datapoint", Title = "Get data point details", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Returns all details of a node: path, type, area, category, configuration properties, current value and children.")]
        public NodeDetails GetDataPoint([Description("Node id.")] Guid id)
        {
            var node = GetVisibleNode(id);
            var properties = (node.PropertyInstance ?? new List<PropertyInstance>())
                .Where(a => a.This2PropertyTemplateNavigation != null)
                .Select(a => new NodeProperty(
                    a.This2PropertyTemplateNavigation.Key,
                    a.This2PropertyTemplateNavigation.Name,
                    ReadPropertyValue(a)))
                .ToList();

            var children = (node.InverseThis2ParentNodeInstanceNavigation ?? new List<NodeInstance>())
                .Where(a => !a.IsDeleted && _access.CanSee(a.This2UserGroup))
                .Select(a => new NodeReference(a.ObjId, a.Name))
                .ToList();

            return new NodeDetails(_describer.Describe(node, includeValue: true), node.Description, node.This2ParentNodeInstance, node.TrendingInterval,
                node.Trending ? node.TrendingType.ToString() : null, node.State.ToString(), properties, children);
        }

        [McpServerTool(Name = "get_current_values", Title = "Get current values", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Returns the live values of data points or logic interfaces by id.")]
        public IList<IdValue> GetCurrentValues([Description("Ids of data points or logic interfaces (max. 500).")] Guid[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                throw new McpException("ids must not be empty");
            }

            return ids.Take(MaxResults).Distinct().Select(id =>
            {
                var node = _nodeInstanceCache.Get(id);
                if (node != null && !_access.CanSee(node.This2UserGroup))
                {
                    return new IdValue(id, null, null, false);
                }

                var logicInterface = node == null ? _logicInterfaceInstanceCache.Get(id) : null;
                if (node == null && (logicInterface == null || !CanSeeLogic(logicInterface)))
                {
                    // unknown ids and logics of other user groups look the same
                    return new IdValue(id, null, null, false);
                }

                var value = McpValues.ToCurrentValue(_dispatcher.GetValue(id));
                return new IdValue(id, node?.Name ?? logicInterface?.This2RuleInterfaceTemplateNavigation?.Name, value, true);
            }).ToList();
        }

        [McpServerTool(Name = "write_value", Title = "Write a value", Destructive = true, Idempotent = true, OpenWorld = true)]
        [Description("Writes a value to a writeable data point (or a logic input), exactly like a user in the visualization. This acts on the real installation (lights, heating, blinds, ...): confirm with the user before switching anything with a physical effect.")]
        public async Task<WriteResult> WriteValue(
            [Description("Id of the data point or logic input interface.")] Guid id,
            [Description("The value: true/false for switches, a number for dimmers, set points or analog values, or a string.")] JsonElement value)
        {
            if (!_access.CanWrite)
            {
                throw new McpException("Writing is disabled for the MCP API key. Enable the setting 'mcpAllowWrite' or log in with a user token.");
            }

            object converted;
            try
            {
                converted = McpValues.FromJson(value);
            }
            catch (ArgumentException e)
            {
                throw new McpException(e.Message);
            }

            var node = _nodeInstanceCache.Get(id);
            var isRemanent = false;
            if (node != null)
            {
                if (node.IsDeleted || !_access.CanSee(node.This2UserGroup))
                {
                    throw new McpException($"Data point {id} not found");
                }

                if (!node.IsWriteable)
                {
                    throw new McpException($"Data point '{node.Name}' is not writeable");
                }

                isRemanent = node.IsRemanent;
            }
            else
            {
                var logicInterface = _logicInterfaceInstanceCache.Get(id);
                if (logicInterface == null || !CanSeeLogic(logicInterface))
                {
                    throw new McpException($"Data point or logic interface {id} not found");
                }

                if (logicInterface.This2RuleInterfaceTemplateNavigation?.This2RuleInterfaceDirection != (long)LogicInterfaceDirection.Input)
                {
                    throw new McpException("Only logic inputs can be written");
                }
            }

            var previous = McpValues.ToCurrentValue(_dispatcher.GetValue(id));

            // same path as a write from the visualization (DataHub.SetValue)
            var dispatchable = new McpDispatchable(_access.CallerName, id, isRemanent);
            await _dispatcher.DispatchValue(dispatchable,
                new DispatchValue(id, DispatchableType.Visualization, converted, DateTimeHelper.ProviderInstance.GetLocalNow().DateTime, DispatchValueSource.User));

            _logger.LogInformation($"{_access.CallerName} wrote {converted} to {node?.Name ?? id.ToString()}");

            return new WriteResult(id, node?.Name, McpValues.ToJsonValue(converted), previous);
        }

        /// <summary>
        /// Same rule as the DataHub: the user group of the logic the interface belongs to
        /// </summary>
        private bool CanSeeLogic(RuleInterfaceInstance logicInterface)
        {
            var logic = _logicInstanceCache.Get(logicInterface.This2RuleInstance);
            return logic != null && _access.CanSee(logic.This2UserGroup);
        }

        private NodeInstance GetVisibleNode(Guid id)
        {
            var node = _nodeInstanceCache.Get(id);
            if (node == null || node.IsDeleted || !_access.CanSee(node.This2UserGroup))
            {
                throw new McpException($"Node {id} not found");
            }

            return node;
        }

        private static object ReadPropertyValue(PropertyInstance property)
        {
            if (property.This2PropertyTemplateNavigation.This2PropertyType == (long)PropertyTemplateType.Password)
            {
                return string.IsNullOrEmpty(property.ValueString) ? null : "***";
            }

            try
            {
                return McpValues.ToJsonValue(property.Value);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool Matches(string query, NodeInstance node, NodeSummary summary)
        {
            var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var haystack = string.Join(" ", node.Name, node.Description, node.VisuName, summary.Path, summary.Area, summary.Category, summary.Type);
            return terms.All(term => haystack.Contains(term, StringComparison.OrdinalIgnoreCase));
        }
    }

    public record NodeChildren(NodeSummary Node, IList<NodeSummary> Children);

    public record NodeReference(Guid Id, string Name);

    public record NodeProperty(string Key, string Name, object Value);

    public record NodeDetails(
        NodeSummary Node,
        string Description,
        Guid? ParentId,
        int TrendingInterval,
        string TrendingType,
        string State,
        IList<NodeProperty> Properties,
        IList<NodeReference> Children);

    public record IdValue(Guid Id, string Name, CurrentValue CurrentValue, bool Known);

    public record WriteResult(Guid Id, string Name, object WrittenValue, CurrentValue PreviousValue);
}
