using Automatica.Core.Automation.Definition;
using Automatica.Core.Base.Calendar;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.Control.Base;
using Automatica.Core.Control.Cache;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Notification;
using Microsoft.Extensions.Logging;
using Innovative.SolarCalculator;
using Automatica.Core.Shading.Engine;
using Automatica.Core.Internals.OperatingModes;

namespace Automatica.Core.Automation.Engine
{
    /// <summary>
    /// The engine's view of the running core: values from the dispatcher, writes like a logic link (driver node) or
    /// like the visualization (logic inputs), controls from the control cache.
    /// </summary>
    internal sealed class CoreAutomationEnvironment : IAutomationEnvironment
    {
        private readonly IDispatcher _dispatcher;
        private readonly IDriverNodesStore _driverNodesStore;
        private readonly INodeInstanceCache _nodeInstanceCache;
        private readonly ILogicInterfaceInstanceCache _logicInterfaceInstanceCache;
        private readonly ILogicInstanceCache _logicInstanceCache;
        private readonly IAreaCache _areaCache;
        private readonly ICategoryCache _categoryCache;
        private readonly IControlCache _controlCache;
        private readonly ISettingsCache _settingsCache;
        private readonly INotificationManager _notificationManager;
        private readonly IServerCloudApi _cloudApi;
        private readonly IRemanentHandler _remanentHandler;
        private readonly IShadingEngine _shadingEngine;
        private readonly IOperatingModeService _operatingModes;
        private readonly ILogger<CoreAutomationEnvironment> _logger;

        public CoreAutomationEnvironment(IDispatcher dispatcher, IDriverNodesStore driverNodesStore, INodeInstanceCache nodeInstanceCache,
            ILogicInterfaceInstanceCache logicInterfaceInstanceCache, ILogicInstanceCache logicInstanceCache, IAreaCache areaCache,
            ICategoryCache categoryCache, IControlCache controlCache, ISettingsCache settingsCache, INotificationManager notificationManager,
            IServerCloudApi cloudApi, IRemanentHandler remanentHandler, IShadingEngine shadingEngine, ILogger<CoreAutomationEnvironment> logger,
            IOperatingModeService operatingModes = null)
        {
            _dispatcher = dispatcher;
            _driverNodesStore = driverNodesStore;
            _nodeInstanceCache = nodeInstanceCache;
            _logicInterfaceInstanceCache = logicInterfaceInstanceCache;
            _logicInstanceCache = logicInstanceCache;
            _areaCache = areaCache;
            _categoryCache = categoryCache;
            _controlCache = controlCache;
            _settingsCache = settingsCache;
            _notificationManager = notificationManager;
            _cloudApi = cloudApi;
            _remanentHandler = remanentHandler;
            _shadingEngine = shadingEngine;
            _operatingModes = operatingModes;
            _logger = logger;
        }

        public TimeProvider Time => DateTimeHelper.ProviderInstance;

        public object GetValue(Guid id)
        {
            return _dispatcher.GetValue(id)?.Value;
        }

        public async Task<IDisposable> SubscribeAsync(Guid id, Func<AutomationValueChange, Task> callback)
        {
            Task OnValue(IDispatchable _, DispatchValue value)
            {
                // like the links: only values read from the devices (or logic outputs), not the writes of the visualization
                return value.ValueSource == DispatchValueSource.Read ? callback(new AutomationValueChange(id, value.Value)) : Task.CompletedTask;
            }

            var node = await _dispatcher.Subscribe(DispatchableType.NodeInstance, id, OnValue);
            var logic = await _dispatcher.Subscribe(DispatchableType.RuleInstance, id, OnValue);
            return new CompositeDisposable(node, logic);
        }

        public AutomationNode GetNode(Guid id)
        {
            var node = _nodeInstanceCache.Get(id);
            if (node != null && !node.IsDeleted)
            {
                return new AutomationNode(id, node.Name, node.IsReadable, node.IsWriteable,
                    ToValueType(node.This2NodeTemplateNavigation?.This2NodeDataType));
            }

            var logicInterface = _logicInterfaceInstanceCache.Get(id);
            if (logicInterface != null)
            {
                var direction = (LogicInterfaceDirection)(logicInterface.This2RuleInterfaceTemplateNavigation?.This2RuleInterfaceDirection ?? 0);
                var logicName = _logicInstanceCache.Get(logicInterface.This2RuleInstance)?.Name;
                var name = logicInterface.This2RuleInterfaceTemplateNavigation?.Name ?? id.ToString();
                return new AutomationNode(id, logicName == null ? name : $"{logicName} - {name}",
                    direction == LogicInterfaceDirection.Output, direction == LogicInterfaceDirection.Input, AutomationValueType.Unknown);
            }

            return null;
        }

        private static AutomationValueType ToValueType(long? dataType)
        {
            return (Base.Templates.NodeDataType?)dataType switch
            {
                Base.Templates.NodeDataType.Boolean => AutomationValueType.Boolean,
                Base.Templates.NodeDataType.Integer or Base.Templates.NodeDataType.Double => AutomationValueType.Number,
                Base.Templates.NodeDataType.String => AutomationValueType.String,
                _ => AutomationValueType.Unknown
            };
        }

        public async Task WriteAsync(Guid id, object value, AutomationWriteSource source, CancellationToken token)
        {
            var dispatchable = new AutomationDispatchable(source);
            var now = Time.GetLocalNow().DateTime;

            var driverNode = _driverNodesStore.Get(id);
            if (driverNode != null)
            {
                var dispatchValue = new DispatchValue(id, DispatchableType.NodeInstance, value, now, DispatchValueSource.Write);
                if (_nodeInstanceCache.Get(id) is { IsRemanent: true })
                {
                    await _remanentHandler.SaveValueAsync(id, dispatchValue, token);
                }

                _logger.LogInformation($"Automation {source.AutomationName} writes {value} to {driverNode.Name}");
                await driverNode.WriteValue(dispatchable, dispatchValue, token);
                return;
            }

            if (_logicInterfaceInstanceCache.Get(id) != null)
            {
                // same path as a write from the visualization to a logic input
                _logger.LogInformation($"Automation {source.AutomationName} writes {value} to logic input {id}");
                await _dispatcher.DispatchValue(new AutomationDispatchable(source, id),
                    new DispatchValue(id, DispatchableType.Visualization, value, now, DispatchValueSource.User));
                return;
            }

            throw new InvalidOperationException("Datapoint not found or not loaded");
        }

        public IControl GetControl(Guid id)
        {
            return _controlCache.Get(id);
        }

        public Guid? FindLogicOutput(Guid logicId, string key)
        {
            var logic = _logicInstanceCache.Get(logicId);
            var output = logic?.RuleInterfaceInstance?.FirstOrDefault(a =>
                a.This2RuleInterfaceTemplateNavigation != null &&
                a.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection == (long)LogicInterfaceDirection.Output &&
                string.Equals(a.This2RuleInterfaceTemplateNavigation.Key, key, StringComparison.OrdinalIgnoreCase));
            return output?.ObjId;
        }

        public IReadOnlyList<IControl> GetControls(Guid? area, bool includeSubAreas, Guid? category)
        {
            var result = new List<IControl>();
            foreach (var control in _controlCache.All())
            {
                var logic = _logicInstanceCache.Get(control.Id);
                if (logic == null)
                {
                    continue;
                }

                if (category.HasValue && logic.This2CategoryInstance != category.Value)
                {
                    continue;
                }

                if (area.HasValue && !IsInArea(logic.This2AreaInstance, area.Value, includeSubAreas))
                {
                    continue;
                }

                result.Add(control);
            }

            return result;
        }

        private bool IsInArea(Guid? itemArea, Guid area, bool includeSubAreas)
        {
            var current = itemArea;
            var hops = 0;
            while (current.HasValue && hops++ < 64)
            {
                if (current.Value == area)
                {
                    return true;
                }

                if (!includeSubAreas)
                {
                    return false;
                }

                current = _areaCache.Get(current.Value)?.This2Parent;
            }

            return false;
        }

        public bool AreaExists(Guid id) => _areaCache.Get(id) != null;

        public bool CategoryExists(Guid id) => _categoryCache.Get(id) != null;

        private (double Latitude, double Longitude) Location =>
            (_settingsCache.GetByKey("Latitude")?.ValueDouble ?? 0, _settingsCache.GetByKey("Longitude")?.ValueDouble ?? 0);

        public bool HasLocation
        {
            get
            {
                var (latitude, longitude) = Location;
                return latitude != 0 || longitude != 0;
            }
        }

        public DateTimeOffset? GetSunTime(DateOnly date, SunEvent sunEvent)
        {
            var (latitude, longitude) = Location;
            var noon = LocalTime.At(Time, date, TimeSpan.FromHours(12));

            var solarTimes = new SolarTimes(noon, latitude, longitude);
            DateTime local;
            try
            {
                local = sunEvent switch
                {
                    SunEvent.Sunrise => solarTimes.Sunrise,
                    SunEvent.Sunset => solarTimes.Sunset,
                    SunEvent.Dawn => solarTimes.DawnCivil,
                    SunEvent.Dusk => solarTimes.DuskCivil,
                    _ => throw new ArgumentOutOfRangeException(nameof(sunEvent))
                };
            }
            catch (Exception e) when (e is ArgumentOutOfRangeException or OverflowException)
            {
                return null;
            }

            if (local.Date != date.ToDateTime(TimeOnly.MinValue))
            {
                // polar day/night: the calculated time belongs to another day
                return null;
            }

            return new DateTimeOffset(local, Time.LocalTimeZone.GetUtcOffset(local));
        }

        public bool? IsOperatingModeActive(string mode)
        {
            if (_operatingModes == null || !_operatingModes.ModeExists(mode))
            {
                return null;
            }
            return _operatingModes.IsActive(mode);
        }

        public bool? IsShading(Guid controlId)
        {
            return _shadingEngine.IsConfigured(controlId) ? _shadingEngine.IsShading(controlId) : null;
        }

        public Task<bool> SetShadingAsync(Guid controlId, bool enabled, CancellationToken token)
        {
            return _shadingEngine.SetAutomaticAsync(controlId, enabled, token);
        }

        public Task NotifyAsync(Guid automationId, string subject, string body, NotifySeverity severity, CancellationToken token)
        {
            var notificationSeverity = severity switch
            {
                NotifySeverity.Warning => NotificationSeverity.Warning,
                NotifySeverity.Error => NotificationSeverity.Error,
                _ => NotificationSeverity.Info
            };
            return _notificationManager.CreateAutomationNotification(automationId, body, subject, notificationSeverity, token);
        }

        public Task<bool> SendEmailAsync(IList<string> to, string subject, string body)
        {
            return _cloudApi.SendEmail(to, subject, body);
        }

        private sealed class CompositeDisposable(params IDisposable[] items) : IDisposable
        {
            public void Dispose()
            {
                foreach (var item in items)
                {
                    item?.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Sender of the values an automation writes
    /// </summary>
    internal sealed class AutomationDispatchable : IDispatchable
    {
        public AutomationDispatchable(AutomationWriteSource source, Guid? id = null)
        {
            Name = $"Automation {source.AutomationName}";
            Id = id ?? source.AutomationId;
        }

        public DispatchableSource Source => DispatchableSource.Visualization;
        public DispatchableType Type => DispatchableType.Visualization;
        public string Name { get; }
        public Guid Id { get; }
        public bool IsRemanent => false;
    }
}
