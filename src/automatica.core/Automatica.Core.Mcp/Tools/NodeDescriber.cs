using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Driver;

namespace Automatica.Core.Mcp.Tools
{
    /// <summary>
    /// Turns cached node instances into small, cycle free objects for the MCP client.
    /// </summary>
    public class NodeDescriber
    {
        public static readonly Guid ValueInterfaceType = GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value);

        private readonly INodeInstanceCache _nodeInstanceCache;
        private readonly IAreaCache _areaCache;
        private readonly ICategoryCache _categoryCache;
        private readonly IDispatcher _dispatcher;
        private readonly INodeTemplateCache _nodeTemplateCache;

        public NodeDescriber(INodeInstanceCache nodeInstanceCache, IAreaCache areaCache, ICategoryCache categoryCache, IDispatcher dispatcher, INodeTemplateCache nodeTemplateCache)
        {
            _nodeTemplateCache = nodeTemplateCache;
            _nodeInstanceCache = nodeInstanceCache;
            _areaCache = areaCache;
            _categoryCache = categoryCache;
            _dispatcher = dispatcher;
        }

        /// <summary>
        /// Nodes added at runtime are cached without their template navigation, so fall back to the template cache.
        /// </summary>
        public NodeTemplate GetTemplate(NodeInstance node)
        {
            return node.This2NodeTemplateNavigation ?? (node.This2NodeTemplate.HasValue ? _nodeTemplateCache.Get(node.This2NodeTemplate.Value) : null);
        }

        /// <summary>
        /// All nodes of the configuration tree. <see cref="INodeInstanceCache"/>.All() only holds the root,
        /// the other nodes hang below it.
        /// </summary>
        public IEnumerable<NodeInstance> AllNodes()
        {
            var root = _nodeInstanceCache.Root;
            if (root == null)
            {
                yield break;
            }

            var visited = new HashSet<Guid>();
            var stack = new Stack<NodeInstance>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (node.IsDeleted || !visited.Add(node.ObjId))
                {
                    continue;
                }

                yield return node;

                var children = node.InverseThis2ParentNodeInstanceNavigation;
                if (children == null)
                {
                    continue;
                }

                for (var i = children.Count - 1; i >= 0; i--)
                {
                    stack.Push(children[i]);
                }
            }
        }

        public bool IsDataPoint(NodeInstance node)
        {
            return GetTemplate(node)?.ProvidesInterface2InterfaceType == ValueInterfaceType;
        }

        public string GetPath(NodeInstance node)
        {
            var names = new List<string>();
            var current = node;
            var guard = 0;

            while (current != null && guard++ < 32)
            {
                names.Add(current.Name);
                current = current.This2ParentNodeInstance.HasValue ? _nodeInstanceCache.Get(current.This2ParentNodeInstance.Value) : null;
            }

            names.Reverse();
            return string.Join(" / ", names);
        }

        /// <summary>
        /// The area and all of its sub areas (a building contains its floors, a floor its rooms).
        /// </summary>
        public HashSet<Guid> GetAreaWithChildren(Guid areaId)
        {
            var result = new HashSet<Guid> { areaId };
            var all = _areaCache.All();
            var added = true;

            while (added)
            {
                added = false;
                foreach (var area in all)
                {
                    if (area.This2Parent.HasValue && result.Contains(area.This2Parent.Value) && result.Add(area.ObjId))
                    {
                        added = true;
                    }
                }
            }

            return result;
        }

        public string GetAreaName(Guid? areaId) => areaId.HasValue && _areaCache.Contains(areaId.Value) ? _areaCache.Get(areaId.Value)?.Name : null;

        public string GetCategoryName(Guid? categoryId) => categoryId.HasValue && _categoryCache.Contains(categoryId.Value) ? _categoryCache.Get(categoryId.Value)?.Name : null;

        public NodeSummary Describe(NodeInstance node, bool includeValue)
        {
            var isDataPoint = IsDataPoint(node);
            CurrentValue value = null;
            if (includeValue && isDataPoint)
            {
                value = McpValues.ToCurrentValue(_dispatcher.GetValue(DispatchableType.NodeInstance, node.ObjId));
            }

            return new NodeSummary(
                node.ObjId,
                node.Name,
                GetPath(node),
                GetTemplate(node)?.Name,
                isDataPoint,
                node.IsReadable,
                node.IsWriteable,
                node.IsDisabled,
                node.Trending,
                GetAreaName(node.This2AreaInstance),
                GetCategoryName(node.This2CategoryInstance),
                node.InverseThis2ParentNodeInstanceNavigation?.Count(a => !a.IsDeleted) ?? 0,
                value);
        }
    }

    public record NodeSummary(
        Guid Id,
        string Name,
        string Path,
        string Type,
        bool IsDataPoint,
        bool Readable,
        bool Writeable,
        bool Disabled,
        bool HasHistory,
        string Area,
        string Category,
        int ChildCount,
        CurrentValue CurrentValue);
}
