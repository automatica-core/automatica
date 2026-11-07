using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Control.Base;
using Automatica.Core.Shading.Definition;
using Automatica.Core.Shading.Engine;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Automatica.Core.Tests.Shading
{
    internal sealed class FakeShadingEnvironment : IShadingEnvironment
    {
        private readonly ConcurrentDictionary<Guid, ShadingValue> _values = new();
        private readonly ConcurrentDictionary<Guid, List<Func<object, Task>>> _subscribers = new();

        public FakeShadingEnvironment(FakeTimeProvider time)
        {
            FakeTime = time;
        }

        public FakeTimeProvider FakeTime { get; }
        public TimeProvider Time => FakeTime;

        /// <summary>
        /// Vienna
        /// </summary>
        public (double Latitude, double Longitude)? Location { get; set; } = (48.2082, 16.3738);

        public Dictionary<Guid, IBlind> Blinds { get; } = new();

        public void SetValue(Guid id, object value, DateTimeOffset? timestamp = null)
        {
            _values[id] = new ShadingValue(value, timestamp ?? FakeTime.GetUtcNow());
        }

        public async Task PublishAsync(Guid id, object value)
        {
            SetValue(id, value);
            List<Func<object, Task>> callbacks;
            lock (_subscribers)
            {
                callbacks = _subscribers.TryGetValue(id, out var list) ? list.ToList() : new();
            }

            foreach (var callback in callbacks)
            {
                await callback(value);
            }
        }

        public int SubscriberCount(Guid id) => _subscribers.TryGetValue(id, out var list) ? list.Count : 0;

        public ShadingValue GetValue(Guid id) => _values.TryGetValue(id, out var value) ? value : null;

        public Task<IDisposable> SubscribeAsync(Guid id, Func<object, Task> callback)
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

        public IBlind GetBlind(Guid controlId) => Blinds.TryGetValue(controlId, out var blind) ? blind : null;

        private sealed class Unsubscriber(Action action) : IDisposable
        {
            public void Dispose() => action();
        }
    }

    /// <summary>
    /// A blind that reaches every position at once (no feedback), unless moved by <see cref="MoveByHand"/>
    /// </summary>
    internal sealed class FakeBlind : ISlatBlind, IControlValueCallback
    {
        private readonly Dictionary<Guid, Action> _callbacks = new();

        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; init; } = "Blind";
        public bool HasSlats { get; init; }

        public bool IsMoving { get; private set; }
        public int Direction { get; private set; }
        public int Position { get; set; }
        public int SlatPosition { get; set; }

        public List<(int? Position, int? Slat)> Commands { get; } = new();

        public Task StopAsync(CancellationToken token = default)
        {
            IsMoving = false;
            return Task.CompletedTask;
        }

        public Task MoveUpAsync(CancellationToken token = default) => MoveAbsoluteAsync(0, token);
        public Task MoveDownAsync(CancellationToken token = default) => MoveAbsoluteAsync(100, token);

        public Task MoveAbsoluteAsync(int pos, CancellationToken token = default)
        {
            Commands.Add((pos, null));
            Position = pos;
            return Task.CompletedTask;
        }

        public Task MoveSlatAsync(int pos, CancellationToken token = default)
        {
            Commands.Add((null, pos));
            SlatPosition = pos;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Wall switch or visualization: the position feedback changes
        /// </summary>
        public void MoveByHand(int position, int? slat = null)
        {
            Position = position;
            if (slat.HasValue)
            {
                SlatPosition = slat.Value;
            }
            Fire();
        }

        /// <summary>
        /// Up/down from the visualization: the blind starts moving
        /// </summary>
        public void StartMovingByHand()
        {
            IsMoving = true;
            Fire();
        }

        private void Fire()
        {
            foreach (var callback in _callbacks.Values.ToList())
            {
                callback();
            }
        }

        public int CallbackCount => _callbacks.Count;

        public Guid RegisterValueCallback(Action callback)
        {
            var id = Guid.NewGuid();
            _callbacks[id] = callback;
            return id;
        }

        public void UnregisterValueCallback(Guid id) => _callbacks.Remove(id);
    }

    internal sealed class InMemoryShadingStore : IShadingStore
    {
        public ShadingSettingsDefinition Settings { get; set; } = new();
        public List<FacadeConfig> Facades { get; } = new();
        public List<BlindConfig> Blinds { get; } = new();

        public Task SetPausedAsync(Guid controlId, bool paused, CancellationToken token = default)
        {
            var index = Blinds.FindIndex(a => a.ControlId == controlId);
            if (index >= 0)
            {
                Blinds[index] = Blinds[index] with { IsPaused = paused };
            }
            return Task.CompletedTask;
        }

        public Task<ShadingConfiguration> LoadAsync(CancellationToken token = default)
        {
            // round trip through JSON like the database
            return Task.FromResult(new ShadingConfiguration
            {
                Settings = ShadingJson.Parse<ShadingSettingsDefinition>(ShadingJson.Serialize(Settings)),
                Facades = Facades.Select(a => a with { Definition = ShadingJson.Parse<FacadeDefinition>(ShadingJson.Serialize(a.Definition)) }).ToList(),
                Blinds = Blinds.Select(a => a with { Definition = ShadingJson.Parse<BlindDefinition>(ShadingJson.Serialize(a.Definition)) }).ToList()
            });
        }
    }

    internal static class ShadingTestHelper
    {
        public static FakeTimeProvider CreateTime(DateTimeOffset start)
        {
            var time = new FakeTimeProvider(start);
            time.SetLocalTimeZone(TimeZoneInfo.Utc);
            return time;
        }

        public static ShadingEngine CreateEngine(FakeShadingEnvironment environment, InMemoryShadingStore store) =>
            new(environment, store, NullLogger<ShadingEngine>.Instance, interval: null);
    }
}
