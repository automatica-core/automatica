using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals;
using Automatica.Core.Runtime.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.IO
{
    public class RuleEngineDispatcher : IDisposable
    {
        private readonly AutomaticaContext _dbContext;
        private readonly CoreServer _coreServer;
        private readonly IDispatcher _dispatcher;
        private readonly object _lock = new object();

        public RuleEngineDispatcher(AutomaticaContext dbContext, CoreServer coreServer, IDispatcher dispatcher)
        {
            _dbContext = dbContext;
            _coreServer = coreServer;
            _dispatcher = dispatcher;
        }

        private void GetFullName(NodeInstance node, List<string> names)
        {
            if (node.This2ParentNodeInstanceNavigation == null)
            {
                return;
            }
            names.Add(node.This2ParentNodeInstanceNavigation.Name);
            GetFullName(node.This2ParentNodeInstanceNavigation, names);
        }

        private string GetFullName(NodeInstance node)
        {
            var list = new List<string>();
            list.Add(node.Name);
            GetFullName(node, list);
            list.Reverse();
            return list.Join("-");
        }

        public bool Load()
        {
            var data = _dbContext.Links.ToList();

            foreach (var entry in data)
            {
                if (entry.This2NodeInstance2RulePageInput.HasValue && entry.This2NodeInstance2RulePageOutput.HasValue) // node 2 node
                {
                    var sourceNode = _dbContext.NodeInstance2RulePages.Include(a => a.This2NodeInstanceNavigation).SingleOrDefault(a =>
                        a.ObjId == entry.This2NodeInstance2RulePageOutput.Value);

                    var targetNode = _dbContext.NodeInstance2RulePages.Include(a => a.This2NodeInstanceNavigation).SingleOrDefault(a =>
                        a.ObjId == entry.This2NodeInstance2RulePageInput.Value);
                    if (sourceNode == null || targetNode == null)
                    {
                        throw new ArgumentException($"{nameof(sourceNode)} && {nameof(targetNode)} is empty - invalid configuration ");
                    }

                    var inputId = sourceNode.This2NodeInstance;
                    SystemLogger.Instance.LogInformation($"Node2Node - \"{GetFullName(sourceNode.This2NodeInstanceNavigation)}\" is mapped to \"{GetFullName(targetNode.This2NodeInstanceNavigation)}\"");
                    _dispatcher.RegisterDispatch(DispatchableType.NodeInstance, inputId, (dispatchable, o) =>
                    {
                        ValueDispatched(dispatchable, o, targetNode.This2NodeInstance);
                    });
                }
                else if (entry.This2RuleInterfaceInstanceInput.HasValue && entry.This2RuleInterfaceInstanceOutput.HasValue) // rule 2 rule
                {
                    var targetNode = _dbContext.RuleInterfaceInstances.Include(a => a.This2RuleInstanceNavigation).SingleOrDefault(a =>
                        a.ObjId == entry.This2RuleInterfaceInstanceInput.Value);

                    var sourceNode = _dbContext.RuleInterfaceInstances.Include(a => a.This2RuleInstanceNavigation).SingleOrDefault(a =>
                        a.ObjId == entry.This2RuleInterfaceInstanceOutput.Value);
                    if (sourceNode == null || targetNode == null)
                    {
                        throw new ArgumentException($"{nameof(sourceNode)} && {nameof(targetNode)} is empty - invalid configuration ");
                    }

                    var inputId = entry.This2RuleInterfaceInstanceOutput.Value;
                    SystemLogger.Instance.LogInformation($"Rule2Rule - {sourceNode.This2RuleInstanceNavigation.Name} is mapped to {targetNode.This2RuleInstanceNavigation.Name}");
                    _dispatcher.RegisterDispatch(DispatchableType.RuleInstance, inputId, (dispatchable, o) =>
                    {
                        ValueDispatchToRule(dispatchable, o, targetNode.This2RuleInstance, targetNode);
                    });
                }
                else if (entry.This2RuleInterfaceInstanceInput.HasValue && entry.This2NodeInstance2RulePageOutput.HasValue) // node 2 rule
                {
                    var targetNode = _dbContext.RuleInterfaceInstances.Include(a => a.This2RuleInstanceNavigation).SingleOrDefault(a =>
                        a.ObjId == entry.This2RuleInterfaceInstanceInput.Value);

                    var sourceNode = _dbContext.NodeInstance2RulePages.Include(a => a.This2NodeInstanceNavigation).SingleOrDefault(a =>
                        a.ObjId == entry.This2NodeInstance2RulePageOutput.Value);
                    if (sourceNode == null || targetNode == null)
                    {
                        throw new ArgumentException($"{nameof(sourceNode)} && {nameof(targetNode)} is empty - invalid configuration ");
                    }

                    var inputId = sourceNode.This2NodeInstance;
                    SystemLogger.Instance.LogInformation($"Node2Rule - \"{GetFullName(sourceNode.This2NodeInstanceNavigation)}\" is mapped to {targetNode.This2RuleInstanceNavigation.Name}");
                    _dispatcher.RegisterDispatch(DispatchableType.NodeInstance, inputId, (dispatchable, o) =>
                    {
                        ValueDispatchToRule(dispatchable, o, targetNode.This2RuleInstance, targetNode);
                    });
                }
                else if (entry.This2NodeInstance2RulePageInput.HasValue && entry.This2RuleInterfaceInstanceOutput.HasValue) // rule 2 node
                {

                    var targetNode = _dbContext.NodeInstance2RulePages.Include(a => a.This2NodeInstanceNavigation).SingleOrDefault(a =>
                        a.ObjId == entry.This2NodeInstance2RulePageInput.Value);

                    var sourceNode = _dbContext.RuleInterfaceInstances.Include(a => a.This2RuleInstanceNavigation).SingleOrDefault(a =>
                        a.ObjId == entry.This2RuleInterfaceInstanceOutput.Value);

                    if (sourceNode == null || targetNode == null)
                    {
                        throw new ArgumentException($"{nameof(sourceNode)} && {nameof(targetNode)} is empty - invalid configuration ");
                    }

                    var inputId = entry.This2RuleInterfaceInstanceOutput.Value;
                    SystemLogger.Instance.LogInformation($"Rule2Node - {sourceNode.This2RuleInstanceNavigation.Name} is mapped to \"{GetFullName(targetNode.This2NodeInstanceNavigation)}\"");
                    _dispatcher.RegisterDispatch(DispatchableType.RuleInstance, inputId, (dispatchable, o) =>
                    {
                        ValueDispatched(dispatchable, o, targetNode.This2NodeInstance);
                    });
                }
            }

            return true;
        }



        private void ValueDispatchToRule(IDispatchable dispatchable, object o, Guid toRule, RuleInterfaceInstance toInterface)
        {
            lock (_lock)
            {
                foreach (var rule in _coreServer.Rules)
                {
                    if (rule.Key.ObjId == toRule)
                    {
                        SystemLogger.Instance.LogDebug($"ValueDispatchToRule: {dispatchable.Name} write value {o} to {toInterface.This2RuleInterfaceTemplateNavigation.Name}");
                        var ruleResults = rule.Value.ValueChanged(toInterface, dispatchable, o);

                        foreach (var result in ruleResults)
                        {
                            _dispatcher.DispatchValue(result.Instance, result.Value);
                        }
                    }
                }
            }
        }

        private void ValueDispatched(IDispatchable dispatchable, object o, Guid to)
        {
            foreach (var node in _coreServer.DriverNodes)
            {
                if (node.Id == to)
                {
                    SystemLogger.Instance.LogDebug($"ValueDispatched: {dispatchable.Name} write value {o} to {node.Name}-{node.Id}");
                    node.WriteValue(dispatchable, o);
                }
            }
        }

        public void Dispose()
        {
            _dispatcher.ClearRegistrations();
        }
    }
}
