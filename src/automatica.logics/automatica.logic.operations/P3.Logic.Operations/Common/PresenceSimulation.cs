using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Newtonsoft.Json;

// Presence simulation, shared by every block that switches lights.
// ⚠ Twin copy of P3.Logic.Lightning/Common/PresenceSimulation.cs (automatica.logic.lightning), used by the Switch and Dimmer logics - the logic
// plugins are separate packages without a shared library. Keep both files identical apart from the namespace and the text prefix.
namespace P3.Logic.Operations.Common
{
    public enum PresenceSimulationMode
    {
        /// <summary>the block is not used in the presence simulation, the PS input is ignored</summary>
        Off = 0,
        /// <summary>Loxone behaviour: a random number of activations (min..max) of random length (min..max minutes) inside the window</summary>
        Random = 1,
        /// <summary>replays what was switched by hand one week ago, random when nothing was recorded around that time</summary>
        Replay = 2
    }

    /// <summary>
    /// The interface template ids of the presence simulation of one logic
    /// </summary>
    public sealed record PresenceSimulationIds(Guid Enable, Guid Mode, Guid Window, Guid MinActivations, Guid MaxActivations, Guid MinDuration, Guid MaxDuration)
    {
        public bool Contains(Guid template) =>
            template == Enable || template == Mode || template == Window || template == MinActivations || template == MaxActivations ||
            template == MinDuration || template == MaxDuration;

        /// <summary>
        /// Ids derived from a fixed prefix - never change the prefix of a released block
        /// </summary>
        public static PresenceSimulationIds Derive(string prefix) => new(
            TemplateIds.Of($"{prefix}.PsEnable"), TemplateIds.Of($"{prefix}.PsMode"), TemplateIds.Of($"{prefix}.PsWindow"),
            TemplateIds.Of($"{prefix}.PsMinActivations"), TemplateIds.Of($"{prefix}.PsMaxActivations"),
            TemplateIds.Of($"{prefix}.PsMinDuration"), TemplateIds.Of($"{prefix}.PsMaxDuration"));
    }

    /// <summary>
    /// What the simulated output should be now (null: the simulation does not touch the output) and when to evaluate again
    /// </summary>
    public readonly record struct PresenceStep(bool? State, DateTime? Next);

    /// <summary>
    /// Presence simulation (modelled on Loxone): while it is enabled (PS input) and the local time is inside the window, the
    /// output is switched on a random number of times (min..max activations) for a random duration (min..max minutes) each,
    /// spread at random over the window - or, in replay mode, as it was switched by hand one week earlier.
    /// When the window closes or PS goes to 0 the output is switched off once.
    /// Everything is calculated from the time passed in, the host block owns the timer.
    /// </summary>
    public sealed class PresenceSimulation
    {
        internal const int ReplayDays = 7;
        internal const int KeepDays = ReplayDays + 1;
        internal const int DefaultMinActivations = 1;
        internal const int DefaultMaxActivations = 4;
        internal const double DefaultMinDuration = 30;
        internal const double DefaultMaxDuration = 180;

        private readonly List<PresenceEvent> _events = new();
        private Random _random = new();
        private bool _running;
        private DateTime? _planWindowStart;
        private List<(DateTime Start, DateTime End)> _plan = new();

        public PresenceSimulationMode Mode { get; set; } = PresenceSimulationMode.Random;
        public TimerPropertyData Window { get; set; } = DefaultWindow();
        public int MinActivations { get; set; } = DefaultMinActivations;
        public int MaxActivations { get; set; } = DefaultMaxActivations;
        public double MinDurationMinutes { get; set; } = DefaultMinDuration;
        public double MaxDurationMinutes { get; set; } = DefaultMaxDuration;

        /// <summary>
        /// The PS input
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// true while the simulation drives the output
        /// </summary>
        public bool IsSimulating => _running;

        internal IReadOnlyList<PresenceEvent> Events => _events;

        /// <summary>
        /// The random activations of the current window
        /// </summary>
        internal IReadOnlyList<(DateTime Start, DateTime End)> Plan => _plan;

        internal void UseRandom(Random random) => _random = random;

        /// <summary>
        /// Called with every change made by hand (not by the simulation), these are replayed one week later
        /// </summary>
        /// <returns>true when the event was recorded</returns>
        public bool Record(DateTime now, bool on)
        {
            if (_running)
            {
                return false;
            }

            Prune(now);
            if (_events.Count > 0 && _events[^1].On == on)
            {
                return false;
            }
            if (_events.Count == 0 && !on)
            {
                return false; // off before anything was on tells nothing
            }

            _events.Add(new PresenceEvent(now, on));
            return true;
        }

        public PresenceStep Evaluate(DateTime now)
        {
            if (Mode == PresenceSimulationMode.Off || !Enabled)
            {
                return Halt(null);
            }

            var window = CalculateWindow(Window, now);
            if (!window.Active)
            {
                return Halt(window.Next);
            }

            _running = true;
            var windowEnd = window.Next;

            if (Mode == PresenceSimulationMode.Replay && HasRecordingAround(now.AddDays(-ReplayDays)))
            {
                var past = now.AddDays(-ReplayDays);
                var state = StateAt(past);
                var next = _events.FirstOrDefault(a => a.At > past)?.At.AddDays(ReplayDays);
                return new PresenceStep(state, Earliest(next, windowEnd));
            }

            if (_planWindowStart != window.Start)
            {
                _planWindowStart = window.Start;
                _plan = CreatePlan(window.Start!.Value, windowEnd!.Value);
            }

            var current = _plan.FirstOrDefault(a => a.Start <= now && now < a.End);
            if (current != default)
            {
                return new PresenceStep(true, Earliest(current.End, windowEnd));
            }

            var upcoming = _plan.Where(a => a.Start > now).Select(a => (DateTime?)a.Start).FirstOrDefault();
            return new PresenceStep(false, Earliest(upcoming, windowEnd));
        }

        /// <summary>
        /// min..max activations of min..max minutes each, in random order with random gaps; they never overlap and fit into the window
        /// </summary>
        internal List<(DateTime Start, DateTime End)> CreatePlan(DateTime start, DateTime end)
        {
            var plan = new List<(DateTime, DateTime)>();
            var length = (end - start).TotalMinutes;
            if (length <= 0)
            {
                return plan;
            }

            var minCount = Math.Max(MinActivations, 0);
            var maxCount = Math.Max(MaxActivations, minCount);
            var count = _random.Next(minCount, maxCount + 1);
            if (count == 0)
            {
                return plan;
            }

            var minDuration = Math.Max(MinDurationMinutes, 1);
            var maxDuration = Math.Max(MaxDurationMinutes, minDuration);
            var durations = Enumerable.Range(0, count).Select(_ => minDuration + _random.NextDouble() * (maxDuration - minDuration)).ToArray();

            var total = durations.Sum();
            if (total > length * 0.9)
            {
                // too much for the window: shrink, keep a tenth of the window for the gaps
                var factor = length * 0.9 / total;
                durations = durations.Select(d => d * factor).ToArray();
                total = durations.Sum();
            }

            var weights = Enumerable.Range(0, count + 1).Select(_ => _random.NextDouble() + 0.05).ToArray();
            var free = length - total;
            var weightSum = weights.Sum();

            var at = start;
            for (var i = 0; i < count; i++)
            {
                at = at.AddMinutes(free * weights[i] / weightSum);
                var stop = at.AddMinutes(durations[i]);
                plan.Add((Truncate(at), Truncate(stop)));
                at = stop;
            }
            return plan.Where(a => a.Item2 > a.Item1).ToList();
        }

        private static DateTime Truncate(DateTime value) => new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, value.Kind);

        private PresenceStep Halt(DateTime? next)
        {
            _planWindowStart = null;
            _plan = new();
            if (!_running)
            {
                return new PresenceStep(null, next);
            }

            _running = false;
            return new PresenceStep(false, next);
        }

        private bool HasRecordingAround(DateTime past)
        {
            return _events.Any(a => a.At >= past.AddHours(-12) && a.At <= past.AddHours(12));
        }

        private bool StateAt(DateTime at)
        {
            var last = _events.LastOrDefault(a => a.At <= at);
            return last?.On ?? false;
        }

        private void Prune(DateTime now)
        {
            var limit = now.AddDays(-KeepDays);
            _events.RemoveAll(a => a.At < limit);
        }

        private static DateTime? Earliest(DateTime? a, DateTime? b)
        {
            if (a == null)
            {
                return b;
            }
            if (b == null)
            {
                return a;
            }
            return a < b ? a : b;
        }

        internal string SerializeEvents()
        {
            return JsonConvert.SerializeObject(_events);
        }

        internal void LoadEvents(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            try
            {
                var events = JsonConvert.DeserializeObject<List<PresenceEvent>>(json);
                if (events == null)
                {
                    return;
                }
                _events.Clear();
                _events.AddRange(events.Where(a => a != null).OrderBy(a => a.At));
            }
            catch (Exception)
            {
                // a broken recording is dropped
            }
        }

        /// <summary>
        /// 18:00 - 23:00 every day (Loxone starts at dusk; use the window to set the start)
        /// </summary>
        public static TimerPropertyData DefaultWindow()
        {
            var today = DateTime.Today;
            return new TimerPropertyData
            {
                StartTime = today.AddHours(18),
                StopTime = today.AddHours(23),
                EnabledDays = Enum.GetValues<DayOfWeek>().ToList()
            };
        }

        /// <summary>
        /// The time window parameter: a <see cref="TimerPropertyData"/> or its JSON
        /// </summary>
        public static TimerPropertyData ParseWindow(object value)
        {
            switch (value)
            {
                case TimerPropertyData data:
                    return data;
                case string json when !string.IsNullOrWhiteSpace(json):
                    try
                    {
                        return JsonConvert.DeserializeObject<TimerPropertyData>(json);
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                default:
                    return null;
            }
        }

        /// <summary>
        /// Whether <paramref name="now"/> (local wall time) is inside the window; Start is the start of the running window, Next its end
        /// or the start of the next window. The same rules as the timer logic: windows can cross midnight, the start day decides.
        /// </summary>
        internal static (bool Active, DateTime? Start, DateTime? Next) CalculateWindow(TimerPropertyData data, DateTime now)
        {
            if (data?.EnabledDays == null || data.EnabledDays.Count == 0)
            {
                return (false, null, null);
            }

            var startTime = ToLocalTimeOfDay(data.StartTime);
            var stopTime = ToLocalTimeOfDay(data.StopTime);

            if (startTime == stopTime)
            {
                return (false, null, null);
            }

            var crossesMidnight = stopTime < startTime;
            var today = now.Date;

            (DateTime start, DateTime end) WindowOf(DateTime day) =>
                (day + startTime, (crossesMidnight ? day.AddDays(1) : day) + stopTime);

            bool IsEnabled(DateTime day) => data.EnabledDays.Contains(day.DayOfWeek);

            foreach (var day in new[] { today.AddDays(-1), today })
            {
                var (start, end) = WindowOf(day);
                if (IsEnabled(day) && start <= now && now < end)
                {
                    return (true, start, end);
                }
            }

            for (var i = 0; i <= 7; i++)
            {
                var day = today.AddDays(i);
                var (start, _) = WindowOf(day);
                if (IsEnabled(day) && start > now)
                {
                    return (false, null, start);
                }
            }

            return (false, null, null);
        }

        private static TimeSpan ToLocalTimeOfDay(DateTime value)
        {
            var local = value.Kind == DateTimeKind.Utc ? TimeZoneInfo.ConvertTimeFromUtc(value, TimeZoneInfo.Local) : value;
            return new TimeSpan(local.Hour, local.Minute, 0);
        }
    }

    public sealed record PresenceEvent(DateTime At, bool On);

    /// <summary>
    /// Wires a <see cref="PresenceSimulation"/> into a logic: its templates, parameters, the PS input and the recording that
    /// survives restarts. The host calls <see cref="Step"/> on every evaluation and applies the state it returns.
    /// </summary>
    public sealed class PresenceSimulationBinding
    {
        internal const string TextPrefix = "OPERATIONS.PRESENCE_SIMULATION";
        internal const string ModeMeta = "ENUM(0," + TextPrefix + ".MODE.OFF;1," + TextPrefix + ".MODE.RANDOM;2," + TextPrefix + ".MODE.REPLAY)";

        private RemanentStore _store;
        private bool? _applied;

        public PresenceSimulationBinding(PresenceSimulationIds ids)
        {
            Ids = ids;
        }

        public PresenceSimulationIds Ids { get; }
        public PresenceSimulation Simulation { get; } = new();

        public bool IsSimulating => Simulation.IsSimulating;

        /// <summary>
        /// Creates the PS input and the parameters with the shared texts OPERATIONS.PRESENCE_SIMULATION.*
        /// </summary>
        public static void CreateTemplates(ILogicTemplateFactory factory, Guid logicGuid, PresenceSimulationIds ids, int inputSort, int paramSort)
        {
            factory.CreateLogicInterfaceTemplate(ids.Enable, $"{TextPrefix}.ENABLE.NAME", $"{TextPrefix}.ENABLE.DESCRIPTION",
                "presenceSimulation", logicGuid, LogicInterfaceDirection.Input, 0, inputSort);

            void SimulationParam(Guid id, string name, string key, int sort, RuleInterfaceParameterDataType type, object value, string meta = null) =>
                factory.CreateParameterLogicInterfaceTemplate(id, $"{TextPrefix}.{name}.NAME", $"{TextPrefix}.{name}.DESCRIPTION", key, logicGuid, sort, type, value, false, meta);

            SimulationParam(ids.Mode, "MODE", "psMode", paramSort, RuleInterfaceParameterDataType.Enum, (int)PresenceSimulationMode.Random, ModeMeta);
            SimulationParam(ids.Window, "WINDOW", "psWindow", paramSort + 1, RuleInterfaceParameterDataType.Timer, JsonConvert.SerializeObject(PresenceSimulation.DefaultWindow()));
            SimulationParam(ids.MinActivations, "MIN_ACTIVATIONS", "psMinActivations", paramSort + 2, RuleInterfaceParameterDataType.Integer, PresenceSimulation.DefaultMinActivations);
            SimulationParam(ids.MaxActivations, "MAX_ACTIVATIONS", "psMaxActivations", paramSort + 3, RuleInterfaceParameterDataType.Integer, PresenceSimulation.DefaultMaxActivations);
            SimulationParam(ids.MinDuration, "MIN_DURATION", "psMinDuration", paramSort + 4, RuleInterfaceParameterDataType.Double, PresenceSimulation.DefaultMinDuration);
            SimulationParam(ids.MaxDuration, "MAX_DURATION", "psMaxDuration", paramSort + 5, RuleInterfaceParameterDataType.Double, PresenceSimulation.DefaultMaxDuration);
        }

        /// <summary>
        /// Loads the recording of the instance, call it when the logic starts
        /// </summary>
        public void Attach(IDispatcher dispatcher, Guid ruleInstanceId)
        {
            _store = new RemanentStore(dispatcher, ruleInstanceId, "presence-simulation");
            Simulation.LoadEvents(_store.Load());
        }

        /// <returns>true when the template belongs to the presence simulation</returns>
        public bool ApplyParameter(Guid template, object value)
        {
            if (template == Ids.Mode)
            {
                var mode = LightingValues.ToInt(value, (int)PresenceSimulationMode.Random);
                Simulation.Mode = Enum.IsDefined(typeof(PresenceSimulationMode), mode) ? (PresenceSimulationMode)mode : PresenceSimulationMode.Random;
            }
            else if (template == Ids.Window)
            {
                Simulation.Window = PresenceSimulation.ParseWindow(value);
            }
            else if (template == Ids.MinActivations)
            {
                Simulation.MinActivations = Math.Max(LightingValues.ToInt(value, PresenceSimulation.DefaultMinActivations), 0);
            }
            else if (template == Ids.MaxActivations)
            {
                Simulation.MaxActivations = Math.Max(LightingValues.ToInt(value, PresenceSimulation.DefaultMaxActivations), 0);
            }
            else if (template == Ids.MinDuration)
            {
                Simulation.MinDurationMinutes = Math.Max(LightingValues.ToDouble(value, PresenceSimulation.DefaultMinDuration), 1);
            }
            else if (template == Ids.MaxDuration)
            {
                Simulation.MaxDurationMinutes = Math.Max(LightingValues.ToDouble(value, PresenceSimulation.DefaultMaxDuration), 1);
            }
            else
            {
                return false;
            }
            return true;
        }

        /// <returns>true when the template is the PS input</returns>
        public bool ApplyInput(Guid template, object value)
        {
            if (template != Ids.Enable)
            {
                return false;
            }
            Simulation.Enabled = LightingValues.IsTruthy(value);
            return true;
        }

        /// <summary>
        /// Records a change made by hand (ignored while the simulation runs)
        /// </summary>
        public void Record(DateTime now, bool on)
        {
            if (Simulation.Record(now, on))
            {
                _store?.Save(Simulation.SerializeEvents());
            }
        }

        /// <summary>
        /// Evaluates the simulation; State is only set when the host has to switch (it changed since the last step)
        /// </summary>
        public PresenceStep Step(DateTime now)
        {
            var step = Simulation.Evaluate(now);
            if (step.State == null)
            {
                _applied = null;
                return step;
            }

            if (_applied == step.State)
            {
                return new PresenceStep(null, step.Next);
            }

            _applied = step.State;
            return step;
        }
    }
}
