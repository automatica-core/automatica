using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Logic;
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
        private readonly ILogicNodeInstanceCache _logicNodeInstanceCache;
        private readonly object _lock = new object();

        private readonly List<(DispatchableType type, Guid id, Guid target)> _dispatchRegistrations =
            new List<(DispatchableType type, Guid id, Guid target)>(); //we need to add the target id as well, to correctly remove the registrations

        public LogicEngineDispatcher(ILinkCache linkCache, 
            IDispatcher dispatcher, 
            ILogicInstancesStore logicInstancesStore, 
            IDriverNodesStore driverNodesStore,
            INodeInstanceCache nodeInstanceCache,
            ILogicInterfaceInstanceCache logicInterfaceInstanceCache,
            ILogger<LogicEngineDispatcher> logger,
            IRuleInstanceVisuNotify ruleInstanceVisuNotifier,
            IRemanentHandler remanentHandler,
            ILogicNodeInstanceCache logicNodeInstanceCache)
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
            _logicNodeInstanceCache = logicNodeInstanceCache;
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

        public async Task<bool> Reload()
        {
            _linkCache.ClearAndLoad();

            foreach (var registration in _dispatchRegistrations)
            {
                await _dispatcher.UnRegisterDispatch(registration.type, registration.id);
            }

            _dispatchRegistrations.Clear();
            return await Load();
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
                    
                    _logger.LogInformation($"Node2Node - \"{GetFullName(sourceNode)}\" {sourceNode.ObjId} is mapped to \"{GetFullName(targetNode)}\" {targetNode.ObjId}");
                    _dispatchRegistrations.Add((DispatchableType.NodeInstance, sourceNode.ObjId, targetNode.ObjId));
                    await _dispatcher.RegisterDispatch(DispatchableType.NodeInstance, sourceNode.ObjId, async (dispatchable, o) =>
                    {
                        if (o.ValueSource == DispatchValueSource.Read)
                        {
                            var logicNodeInstance = _logicNodeInstanceCache.Get(entry.This2NodeInstance2RulePageInputNavigation.ObjId);

                            if (logicNodeInstance is { Inverted: true } && o.Value is bool bValueInt)
                            {
                                o.Value = !bValueInt;
                            }

                            await _remanentHandler.SaveValueAsync(targetNode.ObjId, o);
                            ValueDispatched(dispatchable, o, targetNode.ObjId);
                        }
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
                    _logger.LogInformation($"Rule2Rule - {sourceNode.This2RuleInstanceNavigation.Name} {sourceNode.ObjId} is mapped to {targetNode.This2RuleInstanceNavigation.Name} {targetNode.ObjId}");
                    _dispatchRegistrations.Add((DispatchableType.RuleInstance, inputId, targetNode.ObjId));
                    await _dispatcher.RegisterDispatch(DispatchableType.RuleInstance, inputId, (dispatchable, o) =>
                    {
                        if (o.ValueSource == DispatchValueSource.Read)
                        {
                            if (targetNode.Inverted && o.Value is bool bValueInt)
                            {
                                o.Value = !bValueInt;
                            }
                            ValueDispatchToRule(dispatchable, o.Value, targetNode.This2RuleInstance, targetNode);
                        }
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

                    _logger.LogInformation($"Node2Rule - \"{GetFullName(sourceNode)}\" {sourceNode.ObjId} is mapped to {targetNode.This2RuleInstanceNavigation.Name} {targetNode.ObjId}");
                    _dispatchRegistrations.Add((DispatchableType.NodeInstance, sourceNode.ObjId, targetNode.ObjId));
                    await _dispatcher.RegisterDispatch(DispatchableType.NodeInstance, sourceNode.ObjId, (dispatchable, o) =>
                    {
                        targetNode = _logicInterfaceInstanceCache.Get(entry.This2RuleInterfaceInstanceInput.Value);
                        if (o.ValueSource == DispatchValueSource.Read)
                        {
                            var value = o.Value;
                            if (targetNode.Inverted && value is bool bValueInt)
                            {
                                value = !bValueInt;
                            }
                            ValueDispatchToRule(dispatchable, value, targetNode.This2RuleInstance, targetNode);
                        }
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
                    _logger.LogInformation($"Rule2Node - {sourceNode.This2RuleInstanceNavigation.Name} {sourceNode.This2RuleInstanceNavigation.ObjId} is mapped to \"{GetFullName(targetNode)} {targetNode.ObjId}\"");
                    _dispatchRegistrations.Add((DispatchableType.RuleInstance, inputId, targetNode.ObjId));
                    await _dispatcher.RegisterDispatch(DispatchableType.RuleInstance, inputId,  (dispatchable, o) =>
                    {
                        if (o.ValueSource == DispatchValueSource.Read)
                        {
                            var logicNodeInstance = _logicNodeInstanceCache.Get(entry.This2NodeInstance2RulePageInputNavigation.ObjId);

                            var value = new DispatchValue(o);
                            if (logicNodeInstance is { Inverted: true } && value.Value is bool bValueInt)
                            {
                                value.Value = !bValueInt;
                            }
                            ValueDispatched(dispatchable, value, targetNode.ObjId);
                        }
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

        public async Task Unlink(Guid id)
        {
            (DispatchableType type, Guid id, Guid target) reg;
            if (_dispatchRegistrations.Any(a => a.id == id))
            {
                reg = _dispatchRegistrations.First(a => a.id == id);
            }
            else if (_dispatchRegistrations.Any(a => a.target == id))
            {
                reg = _dispatchRegistrations.First(a => a.target == id);
            }
            else
            {
                _logger.LogWarning($"Could not find dispatcher registration for {id}");
                return;
            }

            _logger.LogInformation($"Unregister Dispatch for {reg.type} from {reg.id} to {reg.target}");
            await _dispatcher.UnRegisterDispatch(reg.type, reg.id);
            _dispatchRegistrations.Remove(reg);
        }

        public async Task Unload()
        {
            foreach (var reg in _dispatchRegistrations)
            {
                await _dispatcher.UnRegisterDispatch(reg.type, reg.id);
            }

            _dispatchRegistrations.Clear();
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
                            _logger.LogDebug(
                                $"ValueDispatchToRule: {rule.Key.ObjId} {rule.GetHashCode()} {dispatchable.Name} write value {o} to {toInterface.This2RuleInterfaceTemplateNavigation.Name} {toInterface.ObjId}");

                            
                            var ruleResults = rule.Value.ValueChanged(toInterface, dispatchable, o);

                            foreach (var result in ruleResults)
                            {
                                var value = result.Value;
                                if (result.Instance.RuleInterfaceInstance.Inverted && value is bool bValueInt)
                                {
                                    value = !bValueInt;
                                }
                                Task.Run(async () =>
                                {
                                    await _dispatcher.DispatchValue(result.Instance, value);
                                    await _ruleInstanceVisuNotifier.NotifyValueChanged(result.Instance.RuleInterfaceInstance, value);
                                }).ConfigureAwait(false);
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, $"Error writing value ({o}) to {dispatchable.Name}");
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
                        var token = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                        _logger.LogInformation(
                            $"ValueDispatched: {dispatchable.Name} write value {o} to {node.Name}-{node.Id}");
                        node.WriteValue(dispatchable, o, token.Token);
                    }
                    catch(Exception e)
                    {
                        _logger.LogError(e, $"Error writing value ({o}) to {dispatchable.Name}");
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
