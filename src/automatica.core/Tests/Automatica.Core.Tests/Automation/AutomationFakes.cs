using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Automation.Definition;
using Automatica.Core.Automation.Engine;
using Automatica.Core.Control.Base;
using Automatica.Core.EF.Models.Automations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Automatica.Core.Tests.Automation
{
    internal sealed class FakeAutomationEnvironment : IAutomationEnvironment
    {
        private readonly ConcurrentDictionary<Guid, object> _values = new();
        private readonly ConcurrentDictionary<Guid, List<Func<AutomationValueChange, Task>>> _subscribers = new();

        public FakeAutomationEnvironment(FakeTimeProvider time)
        {
            FakeTime = time;
        }

        public FakeTimeProvider FakeTime { get; }
        public TimeProvider Time => FakeTime;

        public Dictionary<Guid, AutomationNode> Nodes { get; } = new();
        public Dictionary<Guid, IControl> Controls { get; } = new();
        public ConcurrentQueue<(Guid Id, object Value)> Writes { get; } = new();
        public ConcurrentQueue<(string Subject, string Body)> Notifications { get; } = new();

        /// <summary>
        /// A write comes back as a read value, like a real device
        /// </summary>
        public bool ReadBackWrites { get; set; }

        public bool HasLocation { get; set; } = true;

        /// <summary>
        /// Blinds with automatic shading: control id → shading right now
        /// </summary>
        public ConcurrentDictionary<Guid, bool> Shading { get; } = new();
        public ConcurrentDictionary<Guid, bool> ShadingEnabled { get; } = new();

        public bool? IsShading(Guid controlId) => Shading.TryGetValue(controlId, out var shading) ? shading : null;

        public Task<bool> SetShadingAsync(Guid controlId, bool enabled, CancellationToken token)
        {
            if (!Shading.ContainsKey(controlId))
            {
                return Task.FromResult(false);
            }

            ShadingEnabled[controlId] = enabled;
            return Task.FromResult(true);
        }

        public Guid AddNode(string name, AutomationValueType type = AutomationValueType.Boolean, bool readable = true, bool writeable = true, object value = null)
        {
            var id = Guid.NewGuid();
            Nodes[id] = new AutomationNode(id, name, readable, writeable, type);
            if (value != null)
            {
                _values[id] = value;
            }
            return id;
        }

        public int SubscriberCount(Guid id) => _subscribers.TryGetValue(id, out var list) ? list.Count : 0;

        /// <summary>
        /// A value read from the device
        /// </summary>
        public async Task PublishAsync(Guid id, object value)
        {
            _values[id] = value;
            List<Func<AutomationValueChange, Task>> callbacks;
            lock (_subscribers)
            {
                callbacks = _subscribers.TryGetValue(id, out var list) ? list.ToList() : new();
            }

            foreach (var callback in callbacks)
            {
                await callback(new AutomationValueChange(id, value));
            }
        }

        public object GetValue(Guid id) => _values.TryGetValue(id, out var value) ? value : null;

        public Task<IDisposable> SubscribeAsync(Guid id, Func<AutomationValueChange, Task> callback)
        {
            lock (_subscribers)
            {
                _subscribers.GetOrAdd(id, _ => new()).Add(callback);
            }

            return Task.FromResult<IDisposable>(new Unsubscriber(() =>
            {
                lock (_subscribers)
                {
                    _subscribers[id].Remove(callback);
                }
            }));
        }

        public AutomationNode GetNode(Guid id) => Nodes.TryGetValue(id, out var node) ? node : null;

        public async Task WriteAsync(Guid id, object value, AutomationWriteSource source, CancellationToken token)
        {
            Writes.Enqueue((id, value));
            if (ReadBackWrites)
            {
                await PublishAsync(id, value);
            }
            else
            {
                _values[id] = value;
            }
        }

        public IControl GetControl(Guid id) => Controls.TryGetValue(id, out var control) ? control : null;

        public IReadOnlyList<IControl> GetControls(Guid? area, bool includeSubAreas, Guid? category) => Controls.Values.ToList();

        public bool AreaExists(Guid id) => true;
        public bool CategoryExists(Guid id) => true;

        /// <summary>
        /// Sunrise 07:00, sunset 17:00, dawn 06:30, dusk 17:30 local time every day
        /// </summary>
        public DateTimeOffset? GetSunTime(DateOnly date, SunEvent sunEvent)
        {
            var time = sunEvent switch
            {
                SunEvent.Sunrise => TimeSpan.FromHours(7),
                SunEvent.Sunset => TimeSpan.FromHours(17),
                SunEvent.Dawn => TimeSpan.FromHours(6.5),
                _ => TimeSpan.FromHours(17.5)
            };
            var local = date.ToDateTime(TimeOnly.MinValue).Add(time);
            return new DateTimeOffset(local, Time.LocalTimeZone.GetUtcOffset(local));
        }

        public Task NotifyAsync(Guid automationId, string subject, string body, NotifySeverity severity, CancellationToken token)
        {
            Notifications.Enqueue((subject, body));
            return Task.CompletedTask;
        }

        public ConcurrentQueue<(IList<string> To, string Subject, string Body)> Emails { get; } = new();

        public bool EmailResult { get; set; } = true;

        public Task<bool> SendEmailAsync(IList<string> to, string subject, string body)
        {
            Emails.Enqueue((to, subject, body));
            return Task.FromResult(EmailResult);
        }

        private sealed class Unsubscriber(Action action) : IDisposable
        {
            public void Dispose() => action();
        }
    }

    internal sealed class InMemoryAutomationStore : IAutomationStore
    {
        public ConcurrentDictionary<Guid, EF.Models.Automations.Automation> Automations { get; } = new();
        public ConcurrentQueue<AutomationRun> Runs { get; } = new();

        public EF.Models.Automations.Automation Add(AutomationDefinition definition, AutomationMode mode = AutomationMode.Single, bool enabled = true, string name = "Test")
        {
            var automation = new EF.Models.Automations.Automation
            {
                ObjId = Guid.NewGuid(),
                Name = name,
                IsEnabled = enabled,
                Mode = mode,
                MaxRuns = 10,
                Definition = AutomationJson.Serialize(definition)
            };
            Automations[automation.ObjId] = automation;
            return automation;
        }

        public Task<IReadOnlyList<EF.Models.Automations.Automation>> LoadAllAsync(CancellationToken token = default) =>
            Task.FromResult<IReadOnlyList<EF.Models.Automations.Automation>>(Automations.Values.ToList());

        public Task<EF.Models.Automations.Automation> LoadAsync(Guid id, CancellationToken token = default) =>
            Task.FromResult(Automations.TryGetValue(id, out var automation) ? automation : null);

        public Task<IReadOnlyCollection<Guid>> GetIdsAsync(CancellationToken token = default) =>
            Task.FromResult<IReadOnlyCollection<Guid>>(Automations.Keys.ToList());

        public Task SetEnabledAsync(Guid id, bool enabled, CancellationToken token = default)
        {
            if (Automations.TryGetValue(id, out var automation))
            {
                automation.IsEnabled = enabled;
            }
            return Task.CompletedTask;
        }

        public Task SaveRunAsync(AutomationRun run, int keep, CancellationToken token = default)
        {
            Runs.Enqueue(run);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AutomationRun>> GetRunsAsync(Guid automationId, int take, CancellationToken token = default) =>
            Task.FromResult<IReadOnlyList<AutomationRun>>(Runs.Where(a => a.This2Automation == automationId).OrderByDescending(a => a.StartedAt).Take(take).ToList());
    }

    internal sealed class FakeSwitch : ISwitch
    {
        private readonly Dictionary<Guid, Action> _callbacks = new();

        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; init; } = "Light";
        public SwitchState State { get; private set; } = SwitchState.Off;
        public Guid InputId { get; } = Guid.NewGuid();
        public Guid OutputId { get; } = Guid.NewGuid();

        public Guid RegisterValueCallback(Action callback)
        {
            var id = Guid.NewGuid();
            _callbacks[id] = callback;
            return id;
        }

        public void UnregisterValueCallback(Guid id) => _callbacks.Remove(id);

        public Task<bool> SwitchAsync(bool state, CancellationToken cancellationToken = default) => SwitchAsync(state ? SwitchState.On : SwitchState.Off, cancellationToken);

        public Task<bool> SwitchAsync(SwitchState state, CancellationToken cancellationToken = default)
        {
            State = state;
            foreach (var callback in _callbacks.Values.ToList())
            {
                callback();
            }
            return Task.FromResult(true);
        }

        public Task<bool> SwitchOnAsync(CancellationToken cancellationToken = default) => SwitchAsync(SwitchState.On, cancellationToken);
        public Task<bool> SwitchOffAsync(CancellationToken cancellationToken = default) => SwitchAsync(SwitchState.Off, cancellationToken);
    }

    internal static class AutomationTestHelper
    {
        public static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value);

        public static AutomationEngine CreateEngine(FakeAutomationEnvironment environment, InMemoryAutomationStore store) =>
            new(environment, store, NullLogger<AutomationEngine>.Instance);

        public static FakeTimeProvider CreateTime(DateTimeOffset? start = null)
        {
            var time = new FakeTimeProvider(start ?? new DateTimeOffset(2026, 11, 9, 20, 0, 0, TimeSpan.Zero));
            time.SetLocalTimeZone(TimeZoneInfo.Utc);
            return time;
        }

        /// <summary>
        /// Waits (real time) until <paramref name="condition"/> holds, the runs execute on the thread pool
        /// </summary>
        public static async Task Eventually(Func<bool> condition, string because, int timeoutMs = 5000)
        {
            var until = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (!condition())
            {
                if (DateTime.UtcNow > until)
                {
                    throw new Xunit.Sdk.XunitException($"Timed out waiting: {because}");
                }
                await Task.Delay(10);
            }
        }

        /// <summary>
        /// Gives the thread pool time to (not) do something
        /// </summary>
        public static Task Settle() => Task.Delay(150);
    }
}
