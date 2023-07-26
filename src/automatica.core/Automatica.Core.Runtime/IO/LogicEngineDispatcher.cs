using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Runtime.Abstraction.Plugins.Logic;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("Automatica.Core.Tests")]

namespace Automatica.Core.Runtime.IO
{
    internal class LogicEngineDispatcher : ILogicEngineDispatcher
    {
        private readonly ILinkCache _linkCache;
        private readonly IDispatcher _dispatcher;
        private readonly ILogicInstancesStore _logicInstancesStore;
        private readonly IDriverNodesStore _driverNodesStore;
        private readonly INodeInstanceCache _nodeInstanceCache;
        private readonly ILogicInterfaceInstanceCache _logicInterfaceInstanceCache;
        private readonly ILogger<LogicEngineDispatcher> _logger;
        private readonly IRuleInstanceVisuNotify _ruleInstanceVisuNotifier;
        private readonly IRemanentHandler _remanentHandler;
        private readonly object _lock = new object();

        public LogicEngineDispatcher(ILinkCache linkCache, 
            IDispatcher dispatcher, 
            ILogicInstancesStore logicInstancesStore, 
            IDriverNodesStore driverNodesStore,
            INodeInstanceCache nodeInstanceCache,
            ILogicInterfaceInstanceCache logicInterfaceInstanceCache,
            ILogger<LogicEngineDispatcher> logger,
            IRuleInstanceVisuNotify ruleInstanceVisuNotifier,
            IRemanentHandler remanentHandler)
        {
            _linkCache = linkCache;
            _dispatcher = dispatcher;
            _logicInstancesStore = logicInstancesStore;
            _driverNodesStore = driverNodesStore;
            _nodeInstanceCache = nodeInstanceCache;
            _logicInterfaceInstanceCache = logicInterfaceInstanceCache;
            _logger = logger;
            _ruleInstanceVisuNotifier = ruleInstanceVisuNotifier;
            _remanentHandler = remanentHandler;
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
            var list = new List<string> { node.Name };
            GetFullName(node, list);
            list.Reverse();
            return String.Join("-", list);
        }

        public async Task<bool> Load()
        {
            var data = _linkCache.All();

            foreach (var entry in data)
            {
                if (entry.This2NodeInstance2RulePageInput.HasValue && entry.This2NodeInstance2RulePageOutput.HasValue) // node 2 node
                {
                    var sourceNode = _nodeInstanceCache.Get(entry.This2NodeInstance2RulePageOutputNavigation.This2NodeInstance);
                    var targetNode = _nodeInstanceCache.Get(entry.This2NodeInstance2RulePageInputNavigation.This2NodeInstance);

                    if (sourceNode == null || targetNode == null)
                    {
                        _logger.LogError($"{nameof(sourceNode)} - ({entry.This2NodeInstance2RulePageOutput}) || {nameof(targetNode)} - ({entry.This2NodeInstance2RulePageInput}) is empty - invalid configuration ");
                        continue;
                    }
                    
                    SystemLogger.Instance.LogInformation($"Node2Node - \"{GetFullName(sourceNode)}\" is mapped to \"{GetFullName(targetNode)}\"");
                    await _dispatcher.RegisterDispatch(DispatchableType.NodeInstance, sourceNode.ObjId, async (dispatchable, o) =>
                    {
                        await _remanentHandler.SaveValueAsync(targetNode.ObjId, o);
                        ValueDispatched(dispatchable, o, targetNode.ObjId);
                    });
                }
                else if (entry.This2RuleInterfaceInstanceInput.HasValue && entry.This2RuleInterfaceInstanceOutput.HasValue) // rule 2 rule
                {
                    var targetNode = _logicInterfaceInstanceCache.Get(entry.This2RuleInterfaceInstanceInput.Value);
                    var sourceNode = _logicInterfaceInstanceCache.Get(entry.This2RuleInterfaceInstanceOutput.Value);

                    if (sourceNode == null || targetNode == null)
                    {
                        _logger.LogError($"{nameof(sourceNode)} - ({entry.This2RuleInterfaceInstanceOutput}) || {nameof(targetNode)} - ({entry.This2RuleInterfaceInstanceInput}) is empty - invalid configuration ");
                        continue;
                    }

                    var inputId = sourceNode.ObjId;
                    SystemLogger.Instance.LogInformation($"Rule2Rule - {sourceNode.This2RuleInstanceNavigation.Name} is mapped to {targetNode.This2RuleInstanceNavigation.Name}");
                    await _dispatcher.RegisterDispatch(DispatchableType.RuleInstance, inputId, (dispatchable, o) =>
                    {
                        ValueDispatchToRule(dispatchable, o.Value, targetNode.This2RuleInstance, targetNode);
                    });
                }
                else if (entry.This2RuleInterfaceInstanceInput.HasValue && entry.This2NodeInstance2RulePageOutput.HasValue) // node 2 rule
                {
                    var sourceNode = _nodeInstanceCache.Get(entry.This2NodeInstance2RulePageOutputNavigation.This2NodeInstance);
                    var targetNode = _logicInterfaceInstanceCache.Get(entry.This2RuleInterfaceInstanceInput.Value);


                    if (sourceNode == null || targetNode == null)   
                    {
                        _logger.LogError($"{nameof(sourceNode)} - ({entry.This2NodeInstance2RulePageOutput}) || {nameof(targetNode)} - ({entry.This2RuleInterfaceInstanceInput}) is empty - invalid configuration ");
                        continue;
                    }

                    SystemLogger.Instance.LogInformation($"Node2Rule - \"{GetFullName(sourceNode)}\" is mapped to {targetNode.This2RuleInstanceNavigation.Name}");
                    await _dispatcher.RegisterDispatch(DispatchableType.NodeInstance, sourceNode.ObjId, (dispatchable, o) =>
                    {
                        ValueDispatchToRule(dispatchable, o.Value, targetNode.This2RuleInstance, targetNode);
                    });
                }
                else if (entry.This2NodeInstance2RulePageInput.HasValue && entry.This2RuleInterfaceInstanceOutput.HasValue) // rule 2 node
                {
                    var targetNode = _nodeInstanceCache.Get(entry.This2NodeInstance2RulePageInputNavigation.This2NodeInstance);
                    var sourceNode= _logicInterfaceInstanceCache.Get(entry.This2RuleInterfaceInstanceOutput.Value);


                    if (sourceNode == null || targetNode == null)
                    {
                        _logger.LogError($"{nameof(sourceNode)} - ({entry.This2RuleInterfaceInstanceOutput}) || {nameof(targetNode)} - ({entry.This2NodeInstance2RulePageInput}) is empty - invalid configuration ");
                        continue;
                    }

                    var inputId = sourceNode.ObjId;
                    SystemLogger.Instance.LogInformation($"Rule2Node - {sourceNode.This2RuleInstanceNavigation.Name} is mapped to \"{GetFullName(targetNode)}\"");
                    await _dispatcher.RegisterDispatch(DispatchableType.RuleInstance, inputId, async (dispatchable, o) =>
                    {
                        ValueDispatched(dispatchable, o, targetNode.ObjId);
                    });
                }
            }

            var logicInterfaces= _logicInterfaceInstanceCache.All();

            foreach (var logicInterface in logicInterfaces)
            {
                await _dispatcher.RegisterDispatch(DispatchableType.Visualization, logicInterface.ObjId,
                    (dispatchable, o) => { ValueDispatchToRule(dispatchable, o.Value, logicInterface.This2RuleInstance, logicInterface); });
            }

            return true;
        }

        public async Task Unlink(Guid linkId)
        {
            var entry = _linkCache.Get(linkId);

            if (entry == null)
            {
                return;
            }

            if (entry.This2NodeInstance2RulePageInput.HasValue && entry.This2NodeInstance2RulePageOutput.HasValue) // node 2 node
            {
                var sourceNode = _nodeInstanceCache.Get(entry.This2NodeInstance2RulePageOutputNavigation.This2NodeInstance);
                var targetNode = _nodeInstanceCache.Get(entry.This2NodeInstance2RulePageInputNavigation.This2NodeInstance);

                if (sourceNode == null || targetNode == null)
                {
                    _logger.LogError($"{nameof(sourceNode)} - ({entry.This2NodeInstance2RulePageOutput}) || {nameof(targetNode)} - ({entry.This2NodeInstance2RulePageInput}) is empty - invalid configuration ");
                    return;
                }

                SystemLogger.Instance.LogInformation($"UnRegister Node2Node - \"{GetFullName(sourceNode)}\" is mapped to \"{GetFullName(targetNode)}\"");
                await _dispatcher.UnRegisterDispatch(DispatchableType.NodeInstance, sourceNode.ObjId);
            }
            else if (entry.This2RuleInterfaceInstanceInput.HasValue && entry.This2RuleInterfaceInstanceOutput.HasValue) // rule 2 rule
            {
                var targetNode = _logicInterfaceInstanceCache.Get(entry.This2RuleInterfaceInstanceInput.Value);
                var sourceNode = _logicInterfaceInstanceCache.Get(entry.This2RuleInterfaceInstanceOutput.Value);

                if (sourceNode == null || targetNode == null)
                {
                    _logger.LogError($"{nameof(sourceNode)} - ({entry.This2RuleInterfaceInstanceOutput}) || {nameof(targetNode)} - ({entry.This2RuleInterfaceInstanceInput}) is empty - invalid configuration ");
                    return;
                }

                var inputId = sourceNode.ObjId;
                SystemLogger.Instance.LogInformation($"UnRegister Rule2Rule - {sourceNode.This2RuleInstanceNavigation.Name} is mapped to {targetNode.This2RuleInstanceNavigation.Name}");
                await _dispatcher.UnRegisterDispatch(DispatchableType.RuleInstance, inputId);
            }
            else if (entry.This2RuleInterfaceInstanceInput.HasValue && entry.This2NodeInstance2RulePageOutput.HasValue) // node 2 rule
            {
                var sourceNode = _nodeInstanceCache.Get(entry.This2NodeInstance2RulePageOutputNavigation.This2NodeInstance);
                var targetNode = _logicInterfaceInstanceCache.Get(entry.This2RuleInterfaceInstanceInput.Value);


                if (sourceNode == null || targetNode == null)
                {
                    _logger.LogError($"{nameof(sourceNode)} - ({entry.This2NodeInstance2RulePageOutput}) || {nameof(targetNode)} - ({entry.This2RuleInterfaceInstanceInput}) is empty - invalid configuration ");
                    return;
                }

                SystemLogger.Instance.LogInformation($"UnRegister Node2Rule - \"{GetFullName(sourceNode)}\" is mapped to {targetNode.This2RuleInstanceNavigation.Name}");
                await _dispatcher.UnRegisterDispatch(DispatchableType.NodeInstance, sourceNode.ObjId);
            }
            else if (entry.This2NodeInstance2RulePageInput.HasValue && entry.This2RuleInterfaceInstanceOutput.HasValue) // rule 2 node
            {
                var targetNode = _nodeInstanceCache.Get(entry.This2NodeInstance2RulePageInputNavigation.This2NodeInstance);
                var sourceNode = _logicInterfaceInstanceCache.Get(entry.This2RuleInterfaceInstanceOutput.Value);


                if (sourceNode == null || targetNode == null)
                {
                    _logger.LogError($"{nameof(sourceNode)} - ({entry.This2RuleInterfaceInstanceOutput}) || {nameof(targetNode)} - ({entry.This2NodeInstance2RulePageInput}) is empty - invalid configuration ");
                    return;
                }

                var inputId = sourceNode.ObjId;
                SystemLogger.Instance.LogInformation($"UnRegister Rule2Node - {sourceNode.This2RuleInstanceNavigation.Name} is mapped to \"{GetFullName(targetNode)}\"");
                await _dispatcher.UnRegisterDispatch(DispatchableType.RuleInstance, inputId);
            }
        }


        private void ValueDispatchToRule(IDispatchable dispatchable, object o, Guid toRule, RuleInterfaceInstance toInterface)
        {
            lock (_lock)
            {
                Task.Run(async () =>
                {
                    await _ruleInstanceVisuNotifier.NotifyValueChanged(toInterface, o);
                }).ConfigureAwait(false);

                foreach (var rule in _logicInstancesStore.Dictionary())
                {
                    if (rule.Key.ObjId == toRule)
                    {
                        try
                        {
                            SystemLogger.Instance.LogDebug(
                                $"ValueDispatchToRule: {dispatchable.Name} write value {o} to {toInterface.This2RuleInterfaceTemplateNavigation.Name}");
                            var ruleResults = rule.Value.ValueChanged(toInterface, dispatchable, o);

                            foreach (var result in ruleResults)
                            {
                                Task.Run(async () =>
                                {
                                    await _dispatcher.DispatchValue(result.Instance, result.Value);
                                    await _ruleInstanceVisuNotifier.NotifyValueChanged(result.Instance.RuleInterfaceInstance, result.Value);
                                }).ConfigureAwait(false);
                            }
                        }
                        catch (Exception e)
                        {
                            SystemLogger.Instance.LogError(e, $"Error writing value ({o}) to {dispatchable.Name}");
                        }
                    }
                }
            }
        }

        private void ValueDispatched(IDispatchable dispatchable, DispatchValue o, Guid to)
        {
            foreach (var node in _driverNodesStore.All())
            {
                if (node.Id == to)
                {
                    try
                    {
                        SystemLogger.Instance.LogInformation(
                            $"ValueDispatched: {dispatchable.Name} write value {o} to {node.Name}-{node.Id}");
                        node.WriteValue(dispatchable, o);
                    }
                    catch(Exception e)
                    {
                        SystemLogger.Instance.LogError(e, $"Error writing value ({o}) to {dispatchable.Name}");
                    }
                }
            }
        }

        public void Dispose()
        {
            _dispatcher.ClearRegistrations();
        }
    }
}
