using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Cache.Logic
{
    internal class LogicPageCache : AbstractCache<RulePage>, ILogicPageCache
    {
        private readonly ILogicNodeInstanceCache _nodeInstanceCache;
        private readonly ILogicInterfaceInstanceCache _logicInstanceCache;

        private readonly IDictionary<Guid, RulePage> _rulePageCache = new Dictionary<Guid, RulePage>();

        private readonly IDictionary<Guid, IDictionary<Guid, RuleInstance>> _ruleInstanceCache = new Dictionary<Guid, IDictionary<Guid, RuleInstance>>();
        private readonly IDictionary<Guid, IDictionary<Guid, NodeInstance2RulePage>> _nodeInstanceRefCache = new Dictionary<Guid, IDictionary<Guid, NodeInstance2RulePage>>();

        private readonly IDictionary<Guid, IDictionary<Guid, Link>> _linkCache = new Dictionary<Guid, IDictionary<Guid, Link>>();

        public LogicPageCache(IConfiguration configuration, ILogicNodeInstanceCache nodeInstanceCache, ILogicInterfaceInstanceCache logicInstanceCache) : base(configuration)
        {
            _nodeInstanceCache = nodeInstanceCache;
            _logicInstanceCache = logicInstanceCache;
        }

        protected override IQueryable<RulePage> GetAll(AutomaticaContext context)
        {
            return context.RulePages.Include(a => a.This2RulePageTypeNavigation)
                .Include(a => a.Link)
                    .ThenInclude(a => a.This2NodeInstance2RulePageInputNavigation)
                    .ThenInclude(a => a.This2NodeInstanceNavigation)
                    .ThenInclude(a => a.This2NodeTemplateNavigation)
                .Include(a => a.Link)
                    .ThenInclude(a => a.This2NodeInstance2RulePageOutputNavigation)
                    .ThenInclude(a => a.This2NodeInstanceNavigation)
                    .ThenInclude(a => a.This2NodeTemplateNavigation)
                .Include(a => a.Link)
                    .ThenInclude(a => a.This2RuleInterfaceInstanceInputNavigation)
                    .ThenInclude(a => a.This2RuleInstanceNavigation)
                .Include(a => a.Link)
                    .ThenInclude(a => a.This2RuleInterfaceInstanceOutputNavigation)
                    .ThenInclude(a => a.This2RuleInstanceNavigation)
                .Include(a => a.NodeInstance2RulePage).ThenInclude(b => b.This2NodeInstanceNavigation)
                .ThenInclude(x => x.PropertyInstance)
                .Include(a => a.NodeInstance2RulePage).ThenInclude(b => b.This2NodeInstanceNavigation)
                .ThenInclude(b => b.This2NodeTemplateNavigation)
                .Include(a => a.RuleInstance).ThenInclude(a => a.This2RuleTemplateNavigation)
                .ThenInclude(a => a.RuleInterfaceTemplate)
                .Include(a => a.RuleInstance).ThenInclude(a => a.RuleInterfaceInstance)
                .ThenInclude(a => a.This2RuleInterfaceTemplateNavigation)
                .ThenInclude(a => a.This2RuleInterfaceDirectionNavigation)
                .Include(a => a.RuleInstance).ThenInclude(a => a.This2AreaInstanceNavigation)
                .Include(a => a.RuleInstance).ThenInclude(a => a.This2CategoryInstanceNavigation);
        }

        public override void Add(Guid key, RulePage value)
        {
            if (!_rulePageCache.ContainsKey(key))
            {
                _rulePageCache.Add(key, value);
            }

            if (!_ruleInstanceCache.ContainsKey(key))
            {
                _ruleInstanceCache.Add(key, new Dictionary<Guid, RuleInstance>());
            }

            if (!_nodeInstanceRefCache.ContainsKey(key))
            {
                _nodeInstanceRefCache.Add(key, new Dictionary<Guid, NodeInstance2RulePage>());
            }
            if (!_linkCache.ContainsKey(key))
            {
                _linkCache.Add(key, new Dictionary<Guid, Link>());
            }

            foreach (var ruleInstance in value.RuleInstance)
            {
                if (!_ruleInstanceCache[key].ContainsKey(ruleInstance.ObjId))
                {
                    _ruleInstanceCache[key].Add(ruleInstance.ObjId, ruleInstance);
                }
            }
            foreach (var ruleInstance in value.NodeInstance2RulePage)
            {
                if (!_nodeInstanceRefCache[key].ContainsKey(ruleInstance.ObjId))
                {
                    _nodeInstanceRefCache[key].Add(ruleInstance.ObjId, ruleInstance);
                }
            }

            foreach (var link in value.Link)
            {
                if (!_linkCache[key].ContainsKey(link.ObjId))
                {
                    _linkCache[key].Add(link.ObjId, link);
                }
            }

            base.Add(key, value);
        }

        protected override Guid GetKey(RulePage obj)
        {
            return obj.ObjId;
        }

        public void AddRuleInstance(RuleInstance ruleInstance)
        {
            if (_rulePageCache.TryGetValue(ruleInstance.This2RulePage, out var value))
            {
                value.RuleInstance.Add(ruleInstance);
            }

            if (_ruleInstanceCache.TryGetValue(ruleInstance.This2RulePage, out var value1))
            {
                value1.Add(ruleInstance.ObjId, ruleInstance);
            }
        }

        public void AddNodeInstance(NodeInstance2RulePage nodeInstance)
        {
            if (_rulePageCache.TryGetValue(nodeInstance.This2RulePage, out var value))
            {
                value.NodeInstance2RulePage.Add(nodeInstance);
            }

            if (_ruleInstanceCache.ContainsKey(nodeInstance.This2RulePage))
            {
                _nodeInstanceRefCache[nodeInstance.This2RulePage].Add(nodeInstance.ObjId, nodeInstance);
            }
        }

        public void UpdateRuleInstance(RuleInstance ruleInstance)
        {
            if (_ruleInstanceCache.ContainsKey(ruleInstance.This2RulePage))
            {
                if(_ruleInstanceCache[ruleInstance.This2RulePage].ContainsKey(ruleInstance.ObjId))
                {
                    _ruleInstanceCache[ruleInstance.This2RulePage][ruleInstance.ObjId] = ruleInstance;
                }
            }

            if (_rulePageCache.TryGetValue(ruleInstance.This2RulePage, out var page))
            {
                if(page.RuleInstance.Any(a => a.ObjId == ruleInstance.ObjId)) {

                    var existing = page.RuleInstance.First(a => a.ObjId == ruleInstance.ObjId);

                    existing.X = ruleInstance.X;
                    existing.Y = ruleInstance.Y;

                    existing.Name = ruleInstance.Name;
                }

            }
        }

        public void UpdateNodeInstance(NodeInstance2RulePage nodeInstance)
        {
            if (_nodeInstanceRefCache.ContainsKey(nodeInstance.This2RulePage))
            {
                if (_nodeInstanceRefCache[nodeInstance.This2RulePage].ContainsKey(nodeInstance.ObjId))
                {
                    _nodeInstanceRefCache[nodeInstance.This2RulePage][nodeInstance.ObjId] = nodeInstance;
                }
            }
            if (_rulePageCache.TryGetValue(nodeInstance.This2RulePage, out var page))
            {
                var existing = page.NodeInstance2RulePage.First(a => a.ObjId == nodeInstance.ObjId);

                existing.X = nodeInstance.X;
                existing.Y = nodeInstance.Y;
            }
        }

        public void AddLink(Link link)
        {
            if (_rulePageCache.TryGetValue(link.This2RulePage, out var value))
            {
                value.Link.Add(link);
            }

            if (_linkCache.ContainsKey(link.This2RulePage))
            {
                if (!_linkCache[link.This2RulePage].ContainsKey(link.ObjId))
                {
                    _linkCache[link.This2RulePage].Add(link.ObjId, link);
                }
            }

            InitLink(link);
        }

        public void UpdateLink(Link link)
        {
            if (_linkCache.ContainsKey(link.This2RulePage))
            {
                if (_linkCache[link.This2RulePage].ContainsKey(link.ObjId))
                {
                    _linkCache[link.This2RulePage][link.ObjId] = link;
                }
            }
            if (_rulePageCache.TryGetValue(link.This2RulePage, out var page))
            {
                var existing = page.Link.First(a => a.ObjId == link.ObjId);

                existing.This2RuleInterfaceInstanceInput = link.This2RuleInterfaceInstanceInput;
                existing.This2RuleInterfaceInstanceOutput = link.This2RuleInterfaceInstanceOutput;
                existing.This2NodeInstance2RulePageInput = link.This2NodeInstance2RulePageInput;
                existing.This2NodeInstance2RulePageOutput = link.This2NodeInstance2RulePageOutput;
                InitLink(existing);
            }
        }

        private void InitLink(Link link)
        {
            if (link.This2RuleInterfaceInstanceInput.HasValue)
            {
                link.This2RuleInterfaceInstanceInputNavigation =
                    _logicInstanceCache.Get(link.This2RuleInterfaceInstanceInput.Value);
            }
            if (link.This2RuleInterfaceInstanceOutput.HasValue)
            {
                link.This2RuleInterfaceInstanceOutputNavigation =
                    _logicInstanceCache.Get(link.This2RuleInterfaceInstanceOutput.Value);
            }

            if (link.This2NodeInstance2RulePageInput.HasValue)
            {
                link.This2NodeInstance2RulePageInputNavigation =
                    _nodeInstanceCache.Get(link.This2NodeInstance2RulePageInput.Value);
            }

            if (link.This2NodeInstance2RulePageOutput.HasValue)
            {
                link.This2NodeInstance2RulePageOutputNavigation =
                    _nodeInstanceCache.Get(link.This2NodeInstance2RulePageOutput.Value);
            }
        }

        public void RemoveNodeInstance(NodeInstance2RulePage nodeInstance)
        {
            if (_nodeInstanceRefCache.ContainsKey(nodeInstance.This2RulePage))
            {
                if (_nodeInstanceRefCache[nodeInstance.This2RulePage].ContainsKey(nodeInstance.ObjId))
                {
                    _nodeInstanceRefCache[nodeInstance.This2RulePage].Remove(nodeInstance.ObjId);
                }
            }
            if (_rulePageCache.TryGetValue(nodeInstance.This2RulePage, out var page))
            {
                var existing = page.NodeInstance2RulePage.First(a => a.ObjId == nodeInstance.ObjId);

                page.NodeInstance2RulePage.Remove(existing);
            }
        }

        public void RemoveRuleInstance(RuleInstance ruleInstance)
        {
            if (_ruleInstanceCache.ContainsKey(ruleInstance.This2RulePage))
            {
                if (_ruleInstanceCache[ruleInstance.This2RulePage].ContainsKey(ruleInstance.ObjId))
                {
                    _ruleInstanceCache[ruleInstance.This2RulePage].Remove(ruleInstance.ObjId);
                }
            }

            if (_rulePageCache.TryGetValue(ruleInstance.This2RulePage, out var page))
            {
                var existing = page.RuleInstance.First(a => a.ObjId == ruleInstance.ObjId);

                page.RuleInstance.Remove(existing);
            }
        }

        public void RemoveLink(Link link)
        {
            if (_linkCache.ContainsKey(link.This2RulePage))
            {
                if (_linkCache[link.This2RulePage].ContainsKey(link.ObjId))
                {
                    _linkCache[link.This2RulePage][link.ObjId] = link;
                }
            }
            if (_rulePageCache.TryGetValue(link.This2RulePage, out var page))
            {
                if (page.Link.Any(a => a.ObjId == link.ObjId))
                {
                    var existing = page.Link.First(a => a.ObjId == link.ObjId);

                    page.Link.Remove(existing);
                }
            }
        }
    }
}
