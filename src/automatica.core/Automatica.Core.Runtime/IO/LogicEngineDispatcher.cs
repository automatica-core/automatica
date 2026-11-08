using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
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
using DispatchValue = Automatica.Core.Base.IO.DispatchValue;

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

        /// <summary>
        /// One lock per logic instance, a logic processes one input value after the other and its outputs are dispatched in order.
        /// Different logics run in parallel.
        /// </summary>
        private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _ruleLocks = new();

        private readonly object _registrationsLock = new object();

        // link is the link id (empty for the visualization registrations of logic interfaces), id the source and target the target of the registration
        private readonly List<DispatchRegistration> _dispatchRegistrations = new List<DispatchRegistration>();

        private sealed record DispatchRegistration(Guid Link, DispatchableType Type, Guid Id, Guid Target, IDisposable Token);

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

            await Unload();
            return await Load();
        }

        private async Task Register(Guid link, DispatchableType type, Guid id, Guid target, Func<IDispatchable, DispatchValue, Task> callback)
        {
            var token = await _dispatcher.Subscribe(type, id, callback);
            lock (_registrationsLock)
            {
                _dispatchRegistrations.Add(new DispatchRegistration(link, type, id, target, token));
            }
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
                    await Register(entry.ObjId, DispatchableType.NodeInstance, sourceNode.ObjId, targetNode.ObjId, async (dispatchable, o) =>
                    {
                        if (o.ValueSource == DispatchValueSource.Read)
                        {
                            var logicNodeInstance = _logicNodeInstanceCache.Get(entry.This2NodeInstance2RulePageInputNavigation.ObjId);

                            var dispatchValue = new DispatchValue(o);
                            if (logicNodeInstance is { Inverted: true } && o.Value is bool bValueInt)
                            {
                                dispatchValue.Value = !bValueInt;
                            }

                            if (targetNode.IsRemanent)
                            {
                                await _remanentHandler.SaveValueAsync(targetNode.ObjId, dispatchValue);
                            }
                            await ValueDispatched(dispatchable, dispatchValue, targetNode.ObjId);
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
                    await Register(entry.ObjId, DispatchableType.RuleInstance, inputId, targetNode.ObjId, async (dispatchable, o) =>
                    {
                        if (o.ValueSource == DispatchValueSource.Read)
                        {
                            // never modify the dispatched value, it is shared with all other registrations and the value store
                            var value = new DispatchValue(o);
                            if (targetNode.Inverted && value.Value is bool bValueInt)
                            {
                                value.Value = !bValueInt;
                            }
                            await ValueDispatchToRule(dispatchable, value, targetNode.This2RuleInstance, targetNode);
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
                    await Register(entry.ObjId, DispatchableType.NodeInstance, sourceNode.ObjId, targetNode.ObjId, async (dispatchable, o) =>
                    {
                        var currentTarget = _logicInterfaceInstanceCache.Get(entry.This2RuleInterfaceInstanceInput.Value);
                        if (currentTarget == null)
                        {
                            _logger.LogWarning($"Node2Rule - logic interface {entry.This2RuleInterfaceInstanceInput.Value} not found, ignore value from {dispatchable.Name}");
                            return;
                        }

                        if (o.ValueSource == DispatchValueSource.Read)
                        {
                            var value = new DispatchValue(o);
                            if (currentTarget.Inverted && value.Value is bool bValueInt)
                            {
                                value.Value = !bValueInt;
                            }
                            await ValueDispatchToRule(dispatchable, value, currentTarget.This2RuleInstance, currentTarget);
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
                    await Register(entry.ObjId, DispatchableType.RuleInstance, inputId, targetNode.ObjId, async (dispatchable, o) =>
                    {
                        if (o.ValueSource == DispatchValueSource.Read)
                        {
                            var logicNodeInstance = _logicNodeInstanceCache.Get(entry.This2NodeInstance2RulePageInputNavigation.ObjId);

                            var value = new DispatchValue(o);
                            if (logicNodeInstance is { Inverted: true } && value.Value is bool bValueInt)
                            {
                                value.Value = !bValueInt;
                            }
                            await ValueDispatched(dispatchable, value, targetNode.ObjId);
                        }
                    });
                }
            }

            var logicInterfaces= _logicInterfaceInstanceCache.All();

            foreach (var logicInterface in logicInterfaces)
            {
                await RegisterVisualization(logicInterface);
            }

            return true;
        }

        public async Task RegisterLogicInterfaces(RuleInstance logicInstance)
        {
            if (logicInstance?.RuleInterfaceInstance == null)
            {
                return;
            }

            foreach (var logicInterface in logicInstance.RuleInterfaceInstance)
            {
                await RegisterVisualization(_logicInterfaceInstanceCache.Get(logicInterface.ObjId) ?? logicInterface);
            }
        }

        /// <summary>
        /// Values the visualization writes to a logic interface (inputs and parameters) go to the logic. Tracked like the links
        /// (so a reload or <see cref="Unlink"/> removes it) and registered once per interface.
        /// </summary>
        private async Task RegisterVisualization(RuleInterfaceInstance logicInterface)
        {
            var interfaceId = logicInterface.ObjId;
            lock (_registrationsLock)
            {
                if (_dispatchRegistrations.Any(a => a.Link == Guid.Empty && a.Type == DispatchableType.Visualization && a.Id == interfaceId))
                {
                    return;
                }
            }

            var token = await _dispatcher.Subscribe(DispatchableType.Visualization, interfaceId, (dispatchable, o) =>
            {
                // the cached interface, the logic may have been reloaded since the registration
                var current = _logicInterfaceInstanceCache.Get(interfaceId) ?? logicInterface;
                return ValueDispatchToRule(dispatchable, o, current.This2RuleInstance, current);
            });

            lock (_registrationsLock)
            {
                // two registrations raced: keep the first one
                if (_dispatchRegistrations.Any(a => a.Link == Guid.Empty && a.Type == DispatchableType.Visualization && a.Id == interfaceId))
                {
                    token.Dispose();
                    return;
                }
                _dispatchRegistrations.Add(new DispatchRegistration(Guid.Empty, DispatchableType.Visualization, interfaceId, interfaceId, token));
            }
        }

        /// <summary>
        /// Removes all registrations of the link <paramref name="id"/> or of all links from/to the node/logic interface <paramref name="id"/>.
        /// Registrations of other subscribers (e.g. trendings) are not touched.
        /// </summary>
        public Task Unlink(Guid id)
        {
            List<DispatchRegistration> registrations;
            lock (_registrationsLock)
            {
                registrations = _dispatchRegistrations.Where(a => a.Link == id || a.Id == id || a.Target == id).ToList();
                foreach (var reg in registrations)
                {
                    _dispatchRegistrations.Remove(reg);
                }
            }

            if (registrations.Count == 0)
            {
                _logger.LogWarning($"Could not find dispatcher registration for {id}");
                return Task.CompletedTask;
            }

            foreach (var reg in registrations)
            {
                _logger.LogInformation($"Unregister Dispatch for {reg.Type} from {reg.Id} to {reg.Target}");
                reg.Token.Dispose();
            }

            return Task.CompletedTask;
        }

        public Task Unload()
        {
            List<DispatchRegistration> registrations;
            lock (_registrationsLock)
            {
                registrations = _dispatchRegistrations.ToList();
                _dispatchRegistrations.Clear();
            }

            foreach (var reg in registrations)
            {
                reg.Token.Dispose();
            }

            return Task.CompletedTask;
        }

        private void NotifyRuleInterface(RuleInterfaceInstance instance, DispatchValue value)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _ruleInstanceVisuNotifier.NotifyValueChanged(instance, value);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error notifying value change of logic interface {instance.ObjId}");
                }
            });
        }

        private async Task ValueDispatchToRule(IDispatchable dispatchable, DispatchValue o, Guid toRule, RuleInterfaceInstance toInterface)
        {
            NotifyRuleInterface(toInterface, o);

            var ruleLock = _ruleLocks.GetOrAdd(toRule, _ => new SemaphoreSlim(1, 1));
            await ruleLock.WaitAsync().ConfigureAwait(false);
            try
            {
                var rule = _logicInstancesStore.GetByRuleInstanceId(toRule);

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(
                        $"ValueDispatchToRule: {rule.Key.ObjId} {rule.GetHashCode()} {dispatchable.Name} write value {o} to {toInterface.This2RuleInterfaceTemplateNavigation.Name} {toInterface.ObjId}");
                }

                IList<ILogicOutputChanged> logicResults;
                if (o is DispatchValue dispatchValue)
                {
                    logicResults = rule.Value.ValueChanged(toInterface, dispatchable, dispatchValue);
                }
                else
                {
                    logicResults = rule.Value.ValueChanged(toInterface, dispatchable, o);
                }

                foreach (var result in logicResults)
                {
                    var value = result.Value;
                    var interfaceInstance = rule.Key.RuleInterfaceInstance.Single(a => a.ObjId == result.Instance.RuleInterfaceInstance.ObjId);


                    if (interfaceInstance.Inverted && value is bool bValueInt)
                    {
                        value = !bValueInt;
                    }

                    try
                    {
                        // awaited (inside the logic lock), the outputs of consecutive input values must not overtake each other
                        await _dispatcher.DispatchValue(result.Instance, value);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"Error dispatching output ({value}) of {rule.Key.Name}");
                    }

                    NotifyRuleInterface(interfaceInstance, new DispatchValue(result.Instance.Id, DispatchableType.RuleInstance, value, DateTime.Now, DispatchValueSource.Read));
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error writing value ({o}) to {dispatchable.Name}");
            }
            finally
            {
                ruleLock.Release();
            }
        }

        private async Task ValueDispatched(IDispatchable dispatchable, DispatchValue o, Guid to)
        {
            var node = _driverNodesStore.Get(to);

            if (node == null)
            {
                _logger.LogError($"Could not dispatch value ({o.Value}) to {to}, we could not find id....");
                return;
            }
            try
            {
                using var token = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug($"ValueDispatched: {dispatchable.Name} write value {o} to {node.Name}-{node.Id}");
                }
                await node.WriteValue(dispatchable, o, token.Token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error writing value ({o}) to {dispatchable.Name}");
            }
        }

        public void Dispose()
        {
            lock (_registrationsLock)
            {
                _dispatchRegistrations.Clear();
            }
            _dispatcher.ClearRegistrations();
        }
    }
}
