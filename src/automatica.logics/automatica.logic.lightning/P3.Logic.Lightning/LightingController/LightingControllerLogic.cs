using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.Control.Base;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using P3.Logic.Lightning.Common;

namespace P3.Logic.Lightning.LightingController
{
    /// <summary>
    /// Lighting controller, see <see cref="LightingControllerLogicFactory"/>.
    /// <list type="bullet">
    /// <item>moods: 99 all on, 0 all off, -1 manual, user moods 1..89 (parameter or saved from the visualization, kept across restarts)</item>
    /// <item>+ / - cycle the user moods and "all on"; from off + starts with the first, - with the last</item>
    /// <item>motion: rising edge switches the motion mood on when everything is off and it is darker than the threshold,
    /// falling edge starts the overrun (Moet, Met after operation by hand), motion again cancels it; switching off by hand
    /// ignores motion until there was no motion for the automatic timeout (Pto)</item>
    /// <item>command input (visualization): mood:N, next, prev, on, off, set:circuit:value, save:N|new:name, rename:N:name, delete:N</item>
    /// </list>
    /// </summary>
    public enum ChangeSource
    {
        /// <summary>inputs, commands of the visualization</summary>
        Manual,
        /// <summary>motion switched on / the overrun switched off</summary>
        Motion,
        Simulation
    }

    /// <remarks>A control (<see cref="IControl"/>): the lighting central picks its rooms from the controls.</remarks>
    public class LightingControllerLogic : ClockedLogic, IControl
    {
        private readonly LightingControllerIds _ids;
        private readonly Dictionary<Guid, bool> _edges = new();

        private readonly CircuitType[] _types = new CircuitType[LightingControllerIds.Circuits];
        private readonly string[] _names = new string[LightingControllerIds.Circuits];
        private readonly LightValue[] _values = Enumerable.Repeat(LightValue.Off, LightingControllerIds.Circuits).ToArray();

        private readonly RuleInterfaceInstance[] _circuitOutputs = new RuleInterfaceInstance[LightingControllerIds.Circuits];
        private readonly RuleInterfaceInstance[] _kelvinOutputs = new RuleInterfaceInstance[LightingControllerIds.Circuits];
        private RuleInterfaceInstance _moodOutput;
        private RuleInterfaceInstance _activeOutput;
        private RuleInterfaceInstance _visuOutput;

        private List<Mood> _moods = new();
        private bool _moodsSaved;
        private string _moodDefinition = "";
        private int _mood = Mood.AllOff;
        private int _motionMood = Mood.AllOn;
        private double _motionOffDelay = LightingControllerLogicFactory.DefaultMotionOffDelay;
        private double _manualOffDelay = LightingControllerLogicFactory.DefaultManualOffDelay;
        private double _automaticTimeout = LightingControllerLogicFactory.DefaultAutomaticTimeout;
        private double _brightnessThreshold = LightingControllerLogicFactory.DefaultBrightnessThreshold;
        private double? _brightness;
        private bool _motion;
        private bool _disableMotion;
        private DateTime? _motionOffAt;

        /// <summary>the lights that are on were switched on by motion (and not touched by hand since)</summary>
        private bool _startedByMotion;
        /// <summary>switched off by hand: motion is ignored until there was no motion for the automatic timeout</summary>
        private bool _suspended;
        private DateTime? _suspendUntil;

        private RemanentStore _moodStore;
        private RemanentStore _stateStore;

        public LightingControllerLogic(ILogicContext context, LightingControllerIds ids) : base(context)
        {
            _ids = ids;
            PresenceSimulation = new PresenceSimulationBinding(ids.PresenceSimulation);
            _types[0] = CircuitType.Dimmer;
        }

        internal PresenceSimulationBinding PresenceSimulation { get; }

        public Guid Id => Context.RuleInstance.ObjId;
        public string Name => Context.RuleInstance.Name;

        /// <summary>the input templates of this controller (the hotel controller has its own)</summary>
        internal LightingControllerIds Ids => _ids;

        internal int CurrentMood => _mood;
        internal IReadOnlyList<LightValue> Values => _values;
        internal IReadOnlyList<Mood> MoodList => _moods;
        internal DateTime? MotionOffAt => _motionOffAt;
        internal bool MotionSuspended => _suspended;
        internal bool AnyLightOn => Enumerable.Range(0, _values.Length).Any(i => _types[i] != CircuitType.Unused && _values[i].IsOn);

        protected override void OnStart()
        {
            for (var i = 0; i < LightingControllerIds.Circuits; i++)
            {
                _circuitOutputs[i] = Interface(_ids.CircuitOutputs[i]);
                _kelvinOutputs[i] = Interface(_ids.KelvinOutputs[i]);
            }
            _moodOutput = Interface(_ids.ActiveMood);
            _activeOutput = Interface(_ids.AnyOn);
            _visuOutput = Interface(_ids.VisuState);

            var instanceId = Context.RuleInstance.ObjId;
            _moodStore = new RemanentStore(Context.Dispatcher, instanceId, "lighting-controller-moods");
            _stateStore = new RemanentStore(Context.Dispatcher, instanceId, "lighting-controller-state");
            PresenceSimulation.Attach(Context.Dispatcher, instanceId);

            var saved = Mood.Deserialize(_moodStore.Load());
            if (saved != null)
            {
                _moods = saved;
                _moodsSaved = true;
            }
            else
            {
                _moods = Mood.ParseDefinition(_moodDefinition, LightingControllerIds.Circuits);
            }

            RestoreState(_stateStore.Load());

            // nothing is switched on start (a restart must not change the lights), only the state is published
            PublishStatus();
        }

        protected override void ApplyParameter(Guid template, object value)
        {
            if (PresenceSimulation.ApplyParameter(template, value))
            {
                return;
            }

            var typeIndex = Array.IndexOf(_ids.CircuitTypes, template);
            if (typeIndex >= 0)
            {
                var type = LightingValues.ToInt(value, (int)CircuitType.Unused);
                _types[typeIndex] = Enum.IsDefined(typeof(CircuitType), type) ? (CircuitType)type : CircuitType.Unused;
                if (Running)
                {
                    PublishStatus();
                }
                return;
            }

            var nameIndex = Array.IndexOf(_ids.CircuitNames, template);
            if (nameIndex >= 0)
            {
                _names[nameIndex] = value as string ?? "";
                if (Running)
                {
                    PublishStatus();
                }
                return;
            }

            if (template == _ids.Moods)
            {
                _moodDefinition = value as string ?? "";
                if (Running && !_moodsSaved)
                {
                    _moods = Mood.ParseDefinition(_moodDefinition, LightingControllerIds.Circuits);
                    PublishStatus();
                }
            }
            else if (template == _ids.MotionMood)
            {
                _motionMood = LightingValues.ToInt(value, Mood.AllOn);
            }
            else if (template == _ids.MotionOffDelay)
            {
                _motionOffDelay = Math.Max(LightingValues.ToDouble(value, LightingControllerLogicFactory.DefaultMotionOffDelay), 0);
            }
            else if (template == _ids.ManualOffDelay)
            {
                _manualOffDelay = Math.Max(LightingValues.ToDouble(value, LightingControllerLogicFactory.DefaultManualOffDelay), 0);
            }
            else if (template == _ids.AutomaticTimeout)
            {
                _automaticTimeout = Math.Max(LightingValues.ToDouble(value, LightingControllerLogicFactory.DefaultAutomaticTimeout), 0);
                if (_automaticTimeout <= 0)
                {
                    _suspended = false;
                    _suspendUntil = null;
                }
            }
            else if (template == _ids.BrightnessThreshold)
            {
                _brightnessThreshold = Math.Max(LightingValues.ToDouble(value, LightingControllerLogicFactory.DefaultBrightnessThreshold), 0);
            }
        }

        /// <summary>
        /// true on a 0 → 1 change of a pulse input
        /// </summary>
        protected bool RisingEdge(Guid template, object value)
        {
            var state = LightingValues.IsTruthy(value);
            _edges.TryGetValue(template, out var previous);
            _edges[template] = state;
            return state && !previous;
        }

        protected override void OnInput(Guid template, object value, IDispatchable source, DateTime now)
        {
            if (PresenceSimulation.ApplyInput(template, value))
            {
                return;
            }

            if (template == _ids.MoodPlus)
            {
                if (RisingEdge(template, value))
                {
                    Step(+1, now);
                }
            }
            else if (template == _ids.MoodMinus)
            {
                if (RisingEdge(template, value))
                {
                    Step(-1, now);
                }
            }
            else if (template == _ids.On)
            {
                if (RisingEdge(template, value))
                {
                    ActivateMood(Mood.AllOn, now);
                }
            }
            else if (template == _ids.Off)
            {
                if (RisingEdge(template, value))
                {
                    ActivateMood(Mood.AllOff, now);
                }
            }
            else if (template == _ids.MoodSelect)
            {
                var mood = LightingValues.ToInt(value, -1);
                if (mood >= 0)
                {
                    ActivateMood(mood == 0 ? Mood.AllOff : mood, now);
                }
            }
            else if (template == _ids.Motion)
            {
                OnMotion(LightingValues.IsTruthy(value), now);
            }
            else if (template == _ids.Brightness)
            {
                var lux = LightingValues.ToDouble(value, double.NaN);
                _brightness = double.IsNaN(lux) ? null : lux;
            }
            else if (template == _ids.DisableMotion)
            {
                var disable = LightingValues.IsTruthy(value);
                if (disable && !_disableMotion && _startedByMotion && AnyLightOn)
                {
                    // Loxone: lights started by motion start their overrun when motion gets disabled
                    StartOverrun(now);
                }
                _disableMotion = disable;
            }
            else if (template == _ids.Command)
            {
                ExecuteCommand(value as string ?? Convert.ToString(value, CultureInfo.InvariantCulture), now);
            }
            else
            {
                OnOtherInput(template, value, source, now);
            }
        }

        /// <summary>
        /// Inputs of derived blocks
        /// </summary>
        protected virtual void OnOtherInput(Guid template, object value, IDispatchable source, DateTime now)
        {
        }

        /// <summary>
        /// Whether motion may switch the lights on (brightness threshold, derived blocks add their own rules)
        /// </summary>
        protected virtual bool MotionMaySwitchOn()
        {
            if (_disableMotion)
            {
                return false;
            }
            if (_brightnessThreshold <= 0 || _brightness == null)
            {
                return true;
            }
            return _brightness.Value < _brightnessThreshold;
        }

        private void OnMotion(bool motion, DateTime now)
        {
            var rising = motion && !_motion;
            var falling = !motion && _motion;
            _motion = motion;

            if (_disableMotion)
            {
                return;
            }

            if (rising)
            {
                _motionOffAt = null;
                if (_suspended)
                {
                    // every motion restarts the automatic timeout, it runs again when the motion ends
                    _suspendUntil = null;
                    return;
                }
                if (!AnyLightOn && MotionMaySwitchOn())
                {
                    if (ActivateMood(MotionMood, now, ChangeSource.Motion))
                    {
                        _startedByMotion = true;
                    }
                }
            }
            else if (falling)
            {
                if (_suspended)
                {
                    _suspendUntil = now.AddSeconds(_automaticTimeout);
                }
                else if (AnyLightOn)
                {
                    StartOverrun(now);
                }
            }
        }

        /// <summary>
        /// The overrun after motion: Moet (or Met when that is shorter) for lights motion switched on, Met after operation by hand; 0 = never off
        /// </summary>
        private void StartOverrun(DateTime now)
        {
            double delay;
            if (_startedByMotion)
            {
                delay = _motionOffDelay;
                if (delay > 0 && _manualOffDelay > 0 && _manualOffDelay < delay)
                {
                    delay = _manualOffDelay;
                }
            }
            else
            {
                delay = _manualOffDelay;
            }

            _motionOffAt = delay > 0 ? now.AddSeconds(delay) : null;
        }

        /// <summary>
        /// The mood motion switches on
        /// </summary>
        protected virtual int MotionMood => _motionMood;

        protected override DateTime? OnTick(DateTime now)
        {
            if (_motionOffAt.HasValue && now >= _motionOffAt.Value)
            {
                _motionOffAt = null;
                if (!_motion || _disableMotion)
                {
                    ActivateMood(Mood.AllOff, now, ChangeSource.Motion);
                }
            }

            if (_suspendUntil.HasValue && now >= _suspendUntil.Value)
            {
                _suspended = false;
                _suspendUntil = null;
            }

            var step = PresenceSimulation.Step(now);
            if (step.State.HasValue)
            {
                ActivateMood(step.State.Value ? MotionMood : Mood.AllOff, now, ChangeSource.Simulation);
            }

            return Earliest(Earliest(Earliest(_motionOffAt, _suspendUntil), step.Next), OnOtherTick(now));
        }

        /// <summary>
        /// Deadlines of derived blocks
        /// </summary>
        protected virtual DateTime? OnOtherTick(DateTime now) => null;

        protected static DateTime? Earliest(DateTime? a, DateTime? b)
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

        private List<int> CycleList()
        {
            var list = _moods.OrderBy(a => a.Id).Select(a => a.Id).ToList();
            list.Add(Mood.AllOn);
            return list;
        }

        private void Step(int direction, DateTime now)
        {
            var list = CycleList();
            var index = list.IndexOf(_mood);
            int next;
            if (index < 0)
            {
                next = direction > 0 ? list[0] : list[^1];
            }
            else
            {
                next = list[((index + direction) % list.Count + list.Count) % list.Count];
            }
            ActivateMood(next, now);
        }

        /// <summary>
        /// Switches the circuits to the mood; unknown mood numbers are ignored
        /// </summary>
        /// <returns>true when the mood exists</returns>
        protected bool ActivateMood(int id, DateTime now, ChangeSource source = ChangeSource.Manual)
        {
            if (id == Mood.AllOn)
            {
                for (var i = 0; i < _values.Length; i++)
                {
                    _values[i] = _values[i].WithLevel(100);
                }
            }
            else if (id == Mood.AllOff || id == Mood.Manual)
            {
                id = Mood.AllOff;
                for (var i = 0; i < _values.Length; i++)
                {
                    _values[i] = _values[i].WithLevel(0);
                }
            }
            else
            {
                var mood = _moods.FirstOrDefault(a => a.Id == id);
                if (mood == null)
                {
                    Context.Logger.LogDebug($"Lighting controller {Context.RuleInstance.Name}: unknown mood {id}");
                    return false;
                }
                for (var i = 0; i < _values.Length; i++)
                {
                    _values[i] = mood.Values.TryGetValue(i, out var text)
                        ? LightValue.Parse(text, _values[i]) ?? _values[i].WithLevel(0)
                        : _values[i].WithLevel(0);
                }
            }

            _mood = id;
            Changed(now, source);
            return true;
        }

        /// <summary>
        /// Sets one circuit (0 based), the mood becomes "manual"
        /// </summary>
        protected void SetCircuit(int index, object value, DateTime now)
        {
            if (index < 0 || index >= _values.Length || _types[index] == CircuitType.Unused)
            {
                return;
            }
            var parsed = LightValue.Parse(value, _values[index]);
            if (parsed == null)
            {
                return;
            }
            _values[index] = parsed;
            _mood = AnyLightOn ? Mood.Manual : Mood.AllOff;
            Changed(now, ChangeSource.Manual);
        }

        private void Changed(DateTime now, ChangeSource source)
        {
            var on = AnyLightOn;
            if (!on)
            {
                _motionOffAt = null;
                _startedByMotion = false;
            }

            if (source == ChangeSource.Manual)
            {
                // operated by hand: the motion overrun becomes the manual one
                _startedByMotion = false;
                if (!on && _automaticTimeout > 0 && !_disableMotion)
                {
                    _suspended = true;
                    _suspendUntil = _motion ? null : now.AddSeconds(_automaticTimeout);
                }
                else if (on)
                {
                    _suspended = false;
                    _suspendUntil = null;
                    if (!_motion && _motionOffAt.HasValue)
                    {
                        StartOverrun(now);
                    }
                }
            }

            if (source != ChangeSource.Simulation)
            {
                PresenceSimulation.Record(now, on);
            }
            PublishCircuits();
            PublishStatus();
            _stateStore?.Save(SerializeState());
        }

        private void ExecuteCommand(string command, DateTime now)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return;
            }

            var parts = command.Trim().Split(':', 3);
            var verb = parts[0].Trim().ToLowerInvariant();
            int Number(int index, int fallback) => parts.Length > index ? LightingValues.ToInt(parts[index].Trim(), fallback) : fallback;
            string Text(int index) => parts.Length > index ? parts[index].Trim() : "";

            switch (verb)
            {
                case "mood":
                    ActivateMood(Number(1, -1), now);
                    break;
                case "next":
                    Step(+1, now);
                    break;
                case "prev":
                    Step(-1, now);
                    break;
                case "on":
                    ActivateMood(Mood.AllOn, now);
                    break;
                case "off":
                    ActivateMood(Mood.AllOff, now);
                    break;
                case "set":
                    SetCircuit(Number(1, 0) - 1, Text(2), now);
                    break;
                case "save":
                    SaveMood(Text(1), Text(2));
                    break;
                case "rename":
                    RenameMood(Number(1, -1), Text(2));
                    break;
                case "delete":
                    DeleteMood(Number(1, -1));
                    break;
                default:
                    Context.Logger.LogDebug($"Lighting controller {Context.RuleInstance.Name}: unknown command {command}");
                    break;
            }
        }

        internal const int MaxMoods = Mood.MaxUserMood;

        private void SaveMood(string idText, string name)
        {
            int id;
            if (string.IsNullOrEmpty(idText) || idText.Equals("new", StringComparison.OrdinalIgnoreCase) || LightingValues.ToInt(idText, 0) <= 0)
            {
                id = Enumerable.Range(1, MaxMoods).FirstOrDefault(i => _moods.All(m => m.Id != i));
                if (id == 0)
                {
                    return;
                }
            }
            else
            {
                id = LightingValues.ToInt(idText, 0);
                if (id > MaxMoods || Mood.IsBuiltIn(id))
                {
                    return;
                }
            }

            var mood = _moods.FirstOrDefault(a => a.Id == id);
            if (mood == null)
            {
                mood = new Mood { Id = id };
                _moods.Add(mood);
            }
            if (!string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(mood.Name))
            {
                mood.Name = string.IsNullOrWhiteSpace(name) ? $"{id}" : name;
            }

            mood.Values = new Dictionary<int, string>();
            for (var i = 0; i < _values.Length; i++)
            {
                if (_types[i] != CircuitType.Unused && _values[i].IsOn)
                {
                    mood.Values[i] = _values[i].ToText(_types[i]);
                }
            }

            _moods = _moods.OrderBy(a => a.Id).ToList();
            _mood = id;
            SaveMoods();
        }

        private void RenameMood(int id, string name)
        {
            var mood = _moods.FirstOrDefault(a => a.Id == id);
            if (mood == null || string.IsNullOrWhiteSpace(name))
            {
                return;
            }
            mood.Name = name;
            SaveMoods();
        }

        private void DeleteMood(int id)
        {
            if (_moods.RemoveAll(a => a.Id == id) == 0)
            {
                return;
            }
            if (_mood == id)
            {
                _mood = AnyLightOn ? Mood.Manual : Mood.AllOff;
            }
            SaveMoods();
        }

        private void SaveMoods()
        {
            _moodsSaved = true;
            _moodStore?.Save(Mood.Serialize(_moods));
            PublishStatus();
            _stateStore?.Save(SerializeState());
        }

        private void PublishCircuits()
        {
            for (var i = 0; i < _values.Length; i++)
            {
                if (_types[i] == CircuitType.Unused)
                {
                    continue;
                }
                Output(_circuitOutputs[i], _values[i].ToOutput(_types[i]));
                if (_types[i] == CircuitType.TunableWhite)
                {
                    Output(_kelvinOutputs[i], _values[i].Kelvin);
                }
            }
        }

        private void PublishStatus()
        {
            Output(_moodOutput, _mood);
            Output(_activeOutput, AnyLightOn);
            Output(_visuOutput, BuildVisuState());
        }

        /// <summary>
        /// The JSON the visualization control renders
        /// </summary>
        internal string BuildVisuState()
        {
            var moods = _moods.OrderBy(a => a.Id).Select(a => new { id = a.Id, name = a.Name ?? "" }).ToList();
            var circuits = Enumerable.Range(0, _values.Length)
                .Where(i => _types[i] != CircuitType.Unused)
                .Select(i => new
                {
                    index = i + 1,
                    name = string.IsNullOrWhiteSpace(_names[i]) ? "" : _names[i],
                    type = (int)_types[i],
                    level = _values[i].Level,
                    color = _values[i].Color,
                    kelvin = _values[i].Kelvin,
                    on = _values[i].IsOn
                }).ToList();

            return JsonConvert.SerializeObject(new
            {
                mood = _mood,
                moods,
                circuits,
                motion = _motion,
                simulation = PresenceSimulation.IsSimulating
            });
        }

        private string SerializeState()
        {
            return JsonConvert.SerializeObject(new PersistedState { Mood = _mood, Values = _values.ToList() });
        }

        private void RestoreState(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }
            try
            {
                var state = JsonConvert.DeserializeObject<PersistedState>(json);
                if (state?.Values == null)
                {
                    return;
                }
                _mood = state.Mood;
                for (var i = 0; i < Math.Min(state.Values.Count, _values.Length); i++)
                {
                    if (state.Values[i] != null)
                    {
                        _values[i] = state.Values[i];
                    }
                }
            }
            catch (Exception e)
            {
                Context.Logger.LogWarning(e, $"Lighting controller {Context.RuleInstance.Name}: could not restore the state");
            }
        }

        private sealed class PersistedState
        {
            public int Mood { get; set; }
            public List<LightValue> Values { get; set; }
        }
    }
}
