using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Calendar;
using Automatica.Core.Base.IO;
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
    /// Records manual actions (visu taps and changes of data points flagged as <see cref="NodeInstance.HabitInput"/>) for
    /// habit suggestions. Writes to the same target within <see cref="MergeWindow"/> (a dimmer drag, a double tap) become one
    /// action with the last value. Off by default; switching it off deletes everything recorded.
    /// </summary>
    public class HabitRecorder : IHabitRecorder, IHostedService
    {
        public const string EnabledKey = "habitsEnabled";
        public const string LogDaysKey = "habitsLogDays";
        public const int DefaultLogDays = 90;

        internal static readonly TimeSpan MergeWindow = TimeSpan.FromSeconds(10);
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
        private readonly Dictionary<Guid, (HabitAction Action, DateTimeOffset LastWrite)> _pending = new();
        private readonly Dictionary<Guid, IDisposable> _inputSubscriptions = new();

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
            if (!IsEnabled)
            {
                return;
            }

            var now = Time.GetUtcNow();
            var text = Convert.ToString(value, CultureInfo.InvariantCulture);

            lock (_lock)
            {
                if (_pending.TryGetValue(targetId, out var pending) && now - pending.LastWrite < MergeWindow)
                {
                    pending.Action.Value = text;
                    _pending[targetId] = (pending.Action, now);
                    return;
                }
            }

            var (area, category) = AreaAndCategory(targetId);
            var action = new HabitAction
            {
                ObjId = Guid.NewGuid(),
                UtcTime = now.UtcDateTime,
                LocalTime = TimeZoneInfo.ConvertTime(now, Time.LocalTimeZone).DateTime,
                TargetId = targetId,
                This2AreaInstance = area,
                This2CategoryInstance = category,
                Value = text,
                Origin = origin,
                UserId = userId,
                OperatingModes = string.Join(",", _operatingModes.ActiveModes)
            };

            List<HabitAction> due;
            lock (_lock)
            {
                // a pending action of this target older than the window is finished, write it before starting the next one
                due = TakeDue(targetId);
                _pending[targetId] = (action, now);
            }

            if (due.Count > 0)
            {
                _ = SaveAsync(due);
            }
        }

        private List<HabitAction> TakeDue(Guid? alsoTarget = null)
        {
            var now = Time.GetUtcNow();
            var due = _pending.Where(a => a.Key == alsoTarget || now - a.Value.LastWrite >= MergeWindow).ToList();
            foreach (var entry in due)
            {
                _pending.Remove(entry.Key);
            }
            return due.Select(a => a.Value.Action).ToList();
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
                due = all ? _pending.Values.Select(a => a.Action).ToList() : TakeDue();
                if (all)
                {
                    _pending.Clear();
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
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not save {actions.Count} habit actions");
            }
        }

        /// <summary>
        /// Subscribes the data points flagged as habit input (and unsubscribes the ones no longer flagged), only while enabled.
        /// </summary>
        internal async Task ScanInputsAsync()
        {
            var wanted = IsEnabled
                ? _nodes.All().Where(a => a.HabitInput && !a.IsDeleted).Select(a => a.ObjId).ToHashSet()
                : new HashSet<Guid>();

            foreach (var id in _inputSubscriptions.Keys.Where(a => !wanted.Contains(a)).ToList())
            {
                _inputSubscriptions[id].Dispose();
                _inputSubscriptions.Remove(id);
            }

            foreach (var id in wanted.Where(a => !_inputSubscriptions.ContainsKey(a)))
            {
                _inputSubscriptions[id] = await _dispatcher.Subscribe(DispatchableType.NodeInstance, id, OnInputValue);
            }
        }

        internal Task OnInputValue(IDispatchable source, DispatchValue value)
        {
            // only what the device reports from the bus (a pressed button), not writes of logics or the visu
            if (value.ValueSource == DispatchValueSource.Read)
            {
                Record(value.Id, value.Value, HabitActionOrigin.Input, null);
            }
            return Task.CompletedTask;
        }

        internal async Task CleanupAsync()
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var query = context.HabitActions.AsQueryable();
                if (IsEnabled)
                {
                    var oldest = Time.GetUtcNow().UtcDateTime.AddDays(-LogDays);
                    query = query.Where(a => a.UtcTime < oldest);
                }

                var old = await query.ToListAsync();
                if (old.Count > 0)
                {
                    context.HabitActions.RemoveRange(old);
                    await context.SaveChangesAsync();
                    _logger.LogInformation($"Deleted {old.Count} habit actions");
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
            return Task.CompletedTask;
        }

        private async Task RunAsync(CancellationToken token)
        {
            // the first input scan waits a scan interval, so the values drivers read on startup are not taken as presses
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
                        await ScanInputsAsync();
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
            foreach (var subscription in _inputSubscriptions.Values)
            {
                subscription.Dispose();
            }
            _inputSubscriptions.Clear();
            await FlushAsync(all: true);
        }
    }
}
