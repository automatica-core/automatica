using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Calendar;
using Automatica.Core.Base.IO;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Habits;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Internals.OperatingModes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Internals.Habits
{
    public interface IHabitRecorder
    {
        /// <summary>
        /// A user wrote a data point or logic input in a visualization. Ignored while <c>habitsEnabled</c> is off.
        /// </summary>
        void RecordVisu(Guid targetId, object value, Guid? userId);
    }

    /// <summary>
    /// Records actions for habit suggestions: visu taps and every value change that drivers report for any data point.
    /// Values of the same target within <see cref="MergeWindow"/> of its first value (a dimmer drag, a double tap, a sensor
    /// that changes every second) become one action with the last value. Decimal data points (power, temperature, ...) are
    /// context, not actions: they are stored as the time-weighted average of each <see cref="SlotLength"/> slot in which they
    /// changed, a slot without a row kept its value. Clock data points are skipped. Off by default; switching it off deletes everything.
    /// </summary>
    public class HabitRecorder : IHabitRecorder, IHostedService
    {
        public const string EnabledKey = "habitsEnabled";
        public const string LogDaysKey = "habitsLogDays";
        public const int DefaultLogDays = 90;

        internal static readonly TimeSpan MergeWindow = TimeSpan.FromSeconds(10);
        internal static readonly TimeSpan SlotLength = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan ScanInterval = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);

        private readonly IDbContextFactory<AutomaticaContext> _contextFactory;
        private readonly ISettingsCache _settings;
        private readonly INodeInstanceCache _nodes;
        private readonly ILogicInterfaceInstanceCache _logicInterfaces;
        private readonly ILogicInstanceCache _logics;
        private readonly IOperatingModeService _operatingModes;
        private readonly IDispatcher _dispatcher;
        private readonly ILogger<HabitRecorder> _logger;

        private readonly object _lock = new();
        private readonly Dictionary<Guid, HabitAction> _pending = new();
        private readonly Dictionary<Guid, IDisposable> _subscriptions = new();
        private readonly Dictionary<Guid, string> _lastSeen = new();
        /// <summary>Nodes marked <see cref="NodeInstance.HabitExcluded"/> or below one, as of the last scan; replaced, never changed</summary>
        private volatile HashSet<Guid> _excluded = new();
        private readonly Dictionary<Guid, Slot> _slots = new();

        private sealed class Slot
        {
            /// <summary>Utc, aligned to <see cref="SlotLength"/></summary>
            public DateTime Start;
            /// <summary>Since when the value is known in this slot (the start, unless it is the first value ever)</summary>
            public DateTime From;
            /// <summary>When <see cref="Value"/> was reported, or the slot start for a value carried over</summary>
            public DateTime Since;
            public double Value;
            /// <summary>Value * seconds up to <see cref="Since"/></summary>
            public double Sum;
            public double Min;
            public double Max;
            public bool Changed;
        }

        private CancellationTokenSource _cts;
        private Task _loop;

        internal TimeProvider Time { get; set; } = DateTimeHelper.ProviderInstance;

        public HabitRecorder(IDbContextFactory<AutomaticaContext> contextFactory, ISettingsCache settings, INodeInstanceCache nodes,
            ILogicInterfaceInstanceCache logicInterfaces, ILogicInstanceCache logics, IOperatingModeService operatingModes,
            IDispatcher dispatcher, ILogger<HabitRecorder> logger)
        {
            _contextFactory = contextFactory;
            _settings = settings;
            _nodes = nodes;
            _logicInterfaces = logicInterfaces;
            _logics = logics;
            _operatingModes = operatingModes;
            _dispatcher = dispatcher;
            _logger = logger;
        }

        internal bool IsEnabled => _settings.GetByKey(EnabledKey)?.ValueInt is > 0;

        private int LogDays => _settings.GetByKey(LogDaysKey)?.ValueInt is > 0 and var days ? days : DefaultLogDays;

        public void RecordVisu(Guid targetId, object value, Guid? userId) => Record(targetId, value, HabitActionOrigin.Visualization, userId);

        internal void Record(Guid targetId, object value, HabitActionOrigin origin, Guid? userId)
        {
            if (!IsEnabled || _excluded.Contains(targetId))
            {
                return;
            }

            var now = Time.GetUtcNow();
            var text = Convert.ToString(value, CultureInfo.InvariantCulture);

            lock (_lock)
            {
                if (_pending.TryGetValue(targetId, out var pending) && now.UtcDateTime - pending.UtcTime < MergeWindow)
                {
                    pending.Value = text;
                    return;
                }
            }

            var action = NewAction(targetId, now.UtcDateTime, text, origin, userId);

            List<HabitAction> due;
            lock (_lock)
            {
                // a pending action of this target older than the window is finished, write it before starting the next one
                due = TakeDue(targetId);
                _pending[targetId] = action;
            }

            if (due.Count > 0)
            {
                _ = SaveAsync(due);
            }
        }

        private HabitAction NewAction(Guid targetId, DateTime utc, string value, HabitActionOrigin origin, Guid? userId)
        {
            var (area, category) = AreaAndCategory(targetId);
            return new HabitAction
            {
                ObjId = Guid.NewGuid(),
                UtcTime = utc,
                LocalTime = TimeZoneInfo.ConvertTimeFromUtc(utc, Time.LocalTimeZone),
                TargetId = targetId,
                This2AreaInstance = area,
                This2CategoryInstance = category,
                Value = value,
                Origin = origin,
                UserId = userId,
                OperatingModes = string.Join(",", _operatingModes.ActiveModes)
            };
        }

        internal void Measure(Guid targetId, double value)
        {
            if (!IsEnabled)
            {
                return;
            }

            var now = Time.GetUtcNow().UtcDateTime;
            var closed = new List<HabitAction>();
            lock (_lock)
            {
                if (!_slots.TryGetValue(targetId, out var slot))
                {
                    _slots[targetId] = new Slot { Start = SlotStart(now), From = now, Since = now, Value = value, Min = value, Max = value, Changed = true };
                    return;
                }

                if (now >= slot.Start + SlotLength)
                {
                    slot = Close(targetId, slot, now, closed);
                }

                slot.Sum += slot.Value * (now - slot.Since).TotalSeconds;
                slot.Since = now;
                slot.Value = value;
                slot.Min = Math.Min(slot.Min, value);
                slot.Max = Math.Max(slot.Max, value);
                slot.Changed = true;
            }

            if (closed.Count > 0)
            {
                _ = SaveAsync(closed);
            }
        }

        private static DateTime SlotStart(DateTime utc) => new(utc.Ticks - utc.Ticks % SlotLength.Ticks, DateTimeKind.Utc);

        /// <summary>
        /// Ends the slot at its end (or now, if that is earlier), adds its row if the value changed in it and starts the slot of
        /// <paramref name="now"/> with the value carried over.
        /// </summary>
        private Slot Close(Guid targetId, Slot slot, DateTime now, List<HabitAction> closed)
        {
            if (slot.Changed)
            {
                var end = slot.Start + SlotLength < now ? slot.Start + SlotLength : now;
                var seconds = (end - slot.From).TotalSeconds;
                var average = seconds > 0 ? (slot.Sum + slot.Value * (end - slot.Since).TotalSeconds) / seconds : slot.Value;
                var action = NewAction(targetId, slot.Start, Math.Round(average, 3).ToString(CultureInfo.InvariantCulture), HabitActionOrigin.Measurement, null);
                action.Min = slot.Min;
                action.Max = slot.Max;
                closed.Add(action);
            }

            var start = SlotStart(now);
            return _slots[targetId] = new Slot { Start = start, From = start, Since = start, Value = slot.Value, Min = slot.Value, Max = slot.Value };
        }

        private List<HabitAction> TakeDue(Guid? alsoTarget = null)
        {
            var now = Time.GetUtcNow().UtcDateTime;
            var due = _pending.Where(a => a.Key == alsoTarget || now - a.Value.UtcTime >= MergeWindow).ToList();
            foreach (var entry in due)
            {
                _pending.Remove(entry.Key);
            }
            return due.Select(a => a.Value).ToList();
        }

        private (Guid? Area, Guid? Category) AreaAndCategory(Guid targetId)
        {
            var node = _nodes.Get(targetId);
            if (node != null)
            {
                return (node.This2AreaInstance, node.This2CategoryInstance);
            }

            var logicInterface = _logicInterfaces.Get(targetId);
            var logic = logicInterface == null ? null : _logics.Get(logicInterface.This2RuleInstance);
            return (logic?.This2AreaInstance, logic?.This2CategoryInstance);
        }

        internal async Task FlushAsync(bool all = false)
        {
            List<HabitAction> due;
            lock (_lock)
            {
                due = all ? _pending.Values.ToList() : TakeDue();
                if (all)
                {
                    _pending.Clear();
                }

                var now = Time.GetUtcNow().UtcDateTime;
                foreach (var (id, slot) in _slots.Where(a => all || now >= a.Value.Start + SlotLength).ToList())
                {
                    Close(id, slot, now, due);
                }
            }

            await SaveAsync(due);
        }

        private async Task SaveAsync(List<HabitAction> actions)
        {
            if (actions.Count == 0)
            {
                return;
            }

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                context.HabitActions.AddRange(actions);
                await context.SaveChangesAsync();
                _logger.LogDebug($"Saved {actions.Count} habit actions");
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not save {actions.Count} habit actions");
            }
        }

        /// <summary>
        /// Subscribes every node (new ones within a scan interval, deleted ones are dropped) while enabled, none while disabled.
        /// Nodes without a value (drivers, devices) never dispatch, a subscription costs nothing there. A node marked
        /// <see cref="NodeInstance.HabitExcluded"/> is left out together with everything below it (a whole driver or device).
        /// </summary>
        internal async Task ScanDataPointsAsync()
        {
            var wanted = new HashSet<Guid>();
            var excluded = new HashSet<Guid>();
            if (IsEnabled)
            {
                foreach (var (node, isExcluded) in AllNodes().Where(a => !a.Node.IsDeleted))
                {
                    (isExcluded ? excluded : wanted).Add(node.ObjId);
                }
            }
            _excluded = excluded;

            foreach (var id in _subscriptions.Keys.Where(a => !wanted.Contains(a)).ToList())
            {
                _subscriptions[id].Dispose();
                _subscriptions.Remove(id);
                lock (_lock)
                {
                    _lastSeen.Remove(id);
                    _slots.Remove(id);
                }
            }

            var before = _subscriptions.Count;
            foreach (var id in wanted.Where(a => !_subscriptions.ContainsKey(a)))
            {
                _subscriptions[id] = await _dispatcher.Subscribe(DispatchableType.NodeInstance, id, OnDataPointValue);
            }

            if (_subscriptions.Count != before)
            {
                _logger.LogInformation($"Habit recorder watches {_subscriptions.Count} data points (enabled: {IsEnabled})");
            }
        }

        private IEnumerable<(NodeInstance Node, bool Excluded)> AllNodes()
        {
            // the cache's All() only holds the root, every other node hangs below it
            var stack = new Stack<(NodeInstance Node, bool Excluded)>(_nodes.All().Select(a => (a, a.HabitExcluded)));
            var seen = new HashSet<Guid>();
            while (stack.TryPop(out var entry))
            {
                if (!seen.Add(entry.Node.ObjId))
                {
                    continue;
                }

                yield return entry;
                foreach (var child in entry.Node.InverseThis2ParentNodeInstanceNavigation ?? Enumerable.Empty<NodeInstance>())
                {
                    stack.Push((child, entry.Excluded || child.HabitExcluded));
                }
            }
        }

        internal Task OnDataPointValue(IDispatchable source, DispatchValue value)
        {
            // what the driver reports (a pressed button, a sensor), not writes of logics or the visu (recorded in the DataHub)
            if (value.ValueSource != DispatchValueSource.Read)
            {
                return Task.CompletedTask;
            }

            var type = (NodeDataType?)_nodes.Get(value.Id)?.This2NodeTemplateNavigation?.This2NodeDataType;
            if (type is NodeDataType.DateTime or NodeDataType.Time or NodeDataType.Date)
            {
                // clocks change every second and every row carries its time anyway
                return Task.CompletedTask;
            }

            var text = Convert.ToString(value.Value, CultureInfo.InvariantCulture);
            lock (_lock)
            {
                // polling drivers report the same value again and again, that is no change
                // shortcut: a button that sends the same value twice (always "on") counts once, use the visu or a toggle until the
                // miner needs every press
                if (_lastSeen.TryGetValue(value.Id, out var last) && last == text)
                {
                    return Task.CompletedTask;
                }
                _lastSeen[value.Id] = text;
            }

            if (type == NodeDataType.Double && AsNumber(value.Value) is { } number)
            {
                Measure(value.Id, number);
            }
            else
            {
                Record(value.Id, value.Value, HabitActionOrigin.DataPoint, null);
            }
            return Task.CompletedTask;
        }

        private static double? AsNumber(object value)
        {
            try
            {
                var number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return double.IsFinite(number) ? number : null;
            }
            catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
            {
                return null;
            }
        }

        internal async Task CleanupAsync()
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                // switched off: everything goes
                var cutoff = IsEnabled ? Time.GetUtcNow().UtcDateTime.AddDays(-LogDays) : DateTime.MaxValue;

                // in SQL and a day per statement: the table holds tens of millions of rows, loading them (or one huge delete) would not end well
                var deleted = 0;
                while (await context.HabitActions.MinAsync(a => (DateTime?)a.UtcTime) is { } oldest && oldest < cutoff)
                {
                    var end = oldest.AddDays(1) < cutoff ? oldest.AddDays(1) : cutoff;
                    deleted += await context.HabitActions.Where(a => a.UtcTime < end).ExecuteDeleteAsync();
                }

                if (deleted > 0)
                {
                    _logger.LogInformation($"Deleted {deleted} habit actions");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not clean up habit actions");
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = new CancellationTokenSource();
            _loop = RunAsync(_cts.Token);
            _logger.LogInformation($"Habit recorder started (enabled: {IsEnabled})");
            return Task.CompletedTask;
        }

        private async Task RunAsync(CancellationToken token)
        {
            // the first scan waits a scan interval, so the values drivers read on startup are not taken as changes
            var nextScan = Time.GetUtcNow() + ScanInterval;
            var nextCleanup = Time.GetUtcNow() + TimeSpan.FromMinutes(5);
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(FlushInterval, Time, token);
                    await FlushAsync();

                    var now = Time.GetUtcNow();
                    if (now >= nextScan)
                    {
                        nextScan = now + ScanInterval;
                        await ScanDataPointsAsync();
                    }
                    if (now >= nextCleanup)
                    {
                        nextCleanup = now + CleanupInterval;
                        await CleanupAsync();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // stopping
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Habit recorder stopped");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_cts == null)
            {
                return;
            }

            await _cts.CancelAsync();
            await _loop;
            foreach (var subscription in _subscriptions.Values)
            {
                subscription.Dispose();
            }
            _subscriptions.Clear();
            await FlushAsync(all: true);
        }
    }
}
