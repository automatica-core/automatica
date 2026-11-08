using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.Templates;

namespace P3.Logic.Operations.Energy.EnergyManager
{
    public enum EnergyLoadType
    {
        [EnumName("OPERATIONS.ENERGY_MANAGER.LOAD.TYPE.DIGITAL")]
        Digital = 0,
        [EnumName("OPERATIONS.ENERGY_MANAGER.LOAD.TYPE.ANALOG")]
        Analog = 1
    }

    internal sealed class EnergyLoadConfig
    {
        public int Priority { get; set; }

        /// <summary>
        /// kW the load draws when it is switched on (digital), or its minimum power (analog). 0 = load not used.
        /// </summary>
        public double OnPower { get; set; }

        /// <summary>
        /// kW maximum of an analog load
        /// </summary>
        public double MaxPower { get; set; }

        public EnergyLoadType Type { get; set; }
        public TimeSpan MinOn { get; set; }
        public TimeSpan MinOff { get; set; }

        public bool Configured => OnPower > 0;

        public bool IsAnalog => Type == EnergyLoadType.Analog && MaxPower > OnPower;

        public double FullPower => IsAnalog ? MaxPower : OnPower;
    }

    internal sealed class EnergyLoadState
    {
        public bool On { get; set; }

        /// <summary>
        /// kW the load is assumed to draw: the power setpoint of an analog load, the switch-on power of a digital one, 0 when off
        /// </summary>
        public double Setpoint { get; set; }

        public DateTimeOffset? ChangedAt { get; set; }
        public DateTimeOffset? PendingSince { get; set; }
        public DateTimeOffset? SetpointChangedAt { get; set; }
        public bool Manual { get; set; }
        public bool Locked { get; set; }

        /// <summary>
        /// On under the control of the manager (not forced by the manual input)
        /// </summary>
        public bool AutoOn => On && !Manual && !Locked;
    }

    internal sealed class EnergyManagerSettings
    {
        public bool InvertGrid { get; set; }
        public double Reserve { get; set; } = EnergyManagerLogicFactory.DefaultReserve;
        public double Hysteresis { get; set; } = EnergyManagerLogicFactory.DefaultHysteresis;
        public TimeSpan OnDelay { get; set; } = TimeSpan.FromSeconds(EnergyManagerLogicFactory.DefaultOnDelay);
        public TimeSpan OffDelay { get; set; } = TimeSpan.FromSeconds(EnergyManagerLogicFactory.DefaultOffDelay);
        public TimeSpan ModulationInterval { get; set; } = TimeSpan.FromSeconds(EnergyManagerLogicFactory.DefaultModulationInterval);
        public double MinSoc { get; set; }
    }

    /// <summary>
    /// Uses PV surplus for loads by priority (1 = highest).
    /// <para>
    /// The surplus is measured with the managed loads running, so the manager works with
    /// spare = surplus - reserve (+ battery power) and adds the power of its own loads back where it needs the total.
    /// </para>
    /// <list type="bullet">
    /// <item>Spare below -hysteresis (importing): loads are shed lowest priority first after the switch-off delay, as many as needed at once.</item>
    /// <item>Otherwise the highest-priority off load that fits (switch-on power + hysteresis) is switched on after the switch-on delay,
    /// one load per delay. A higher-priority load may take the power of lower-priority loads, which then switch off together with it.</item>
    /// <item>Analog loads get a power setpoint between switch-on and max power from the spare power, by priority.
    /// Lower setpoints apply at once, higher ones at most once per modulation interval.</item>
    /// <item>Minimum on/off times hold a load in its state. After every switch, all delays start again, so the meter can settle.</item>
    /// <item>Locked loads are off, manual loads are on (full power), both at once and outside the manager's budget.</item>
    /// </list>
    /// </summary>
    internal sealed class EnergyManagerEngine
    {
        public const int LoadCount = 12;

        /// <summary>
        /// kW, smaller setpoint changes are not sent
        /// </summary>
        public const double SetpointStep = 0.05;

        private DateTimeOffset? _lastSwitchAt;
        private long _measurement;
        private long _modulatedAt = -1;

        public EnergyManagerEngine()
        {
            for (var i = 0; i < LoadCount; i++)
            {
                Loads[i] = new EnergyLoadConfig { Priority = i + 1 };
                States[i] = new EnergyLoadState();
            }
        }

        public EnergyManagerSettings Settings { get; } = new();
        public EnergyLoadConfig[] Loads { get; } = new EnergyLoadConfig[LoadCount];
        public EnergyLoadState[] States { get; } = new EnergyLoadState[LoadCount];

        private double? _grid;
        private double? _surplus;
        private double? _battery;

        public double? Grid { get => _grid; set { _grid = value; _measurement++; } }
        public double? Surplus { get => _surplus; set { _surplus = value; _measurement++; } }
        public double? Battery { get => _battery; set { _battery = value; _measurement++; } }
        public double? Soc { get; set; }
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// kW the manager can give to its loads (surplus - reserve + the power of its running loads)
        /// </summary>
        public double Available { get; private set; }

        /// <summary>
        /// kW assumed for the loads the manager runs
        /// </summary>
        public double Allocated { get; private set; }

        /// <summary>
        /// Measured surplus (kW, + = export) including the allowed battery power; null without a measurement
        /// </summary>
        internal double? MeasuredSurplus()
        {
            double? surplus = Surplus;
            if (!surplus.HasValue && Grid.HasValue)
            {
                surplus = Settings.InvertGrid ? Grid.Value : -Grid.Value;
            }
            if (!surplus.HasValue)
            {
                return null;
            }

            if (Battery.HasValue)
            {
                // battery first below the minimum SoC: its charging power is not available, its discharging power always counts
                var battery = Soc.HasValue && Soc.Value < Settings.MinSoc ? Math.Min(Battery.Value, 0) : Battery.Value;
                surplus += battery;
            }
            return surplus;
        }

        private IEnumerable<int> ByPriority() => Enumerable.Range(0, LoadCount).Where(i => Loads[i].Configured)
            .OrderBy(i => Loads[i].Priority).ThenBy(i => i);

        private bool InMinOn(int i, DateTimeOffset now) => States[i].On && States[i].ChangedAt.HasValue && now - States[i].ChangedAt.Value < Loads[i].MinOn;
        private bool InMinOff(int i, DateTimeOffset now) => !States[i].On && States[i].ChangedAt.HasValue && now - States[i].ChangedAt.Value < Loads[i].MinOff;

        private bool IsForced(int i) => States[i].Locked || States[i].Manual || !Enabled;

        /// <summary>
        /// Runs one step: forced states, then shedding or one switch-on, then the analog setpoints
        /// </summary>
        public void Evaluate(DateTimeOffset now)
        {
            ApplyForced(now);

            var auto = ByPriority().Where(i => !IsForced(i)).ToList();
            var ownPower = auto.Where(i => States[i].On).Sum(i => States[i].Setpoint);
            var measured = MeasuredSurplus();

            var baseSpare = measured.HasValue ? measured.Value - Settings.Reserve : double.NegativeInfinity;

            // power above the switch-on power of the running analog loads, by position in the priority order
            double ModulationBelow(int rank) => auto.Skip(rank + 1).Where(i => States[i].On && Loads[i].IsAnalog).Sum(i => States[i].Setpoint - Loads[i].OnPower);

            // spare power with every running analog load at its switch-on power
            var spare = baseSpare + ModulationBelow(-1);

            Available = measured.HasValue ? Math.Max(0, baseSpare + ownPower) : 0;

            var shed = new HashSet<int>();
            int? start = null;
            var victims = new List<int>();

            if (spare < -Settings.Hysteresis)
            {
                var need = -spare;
                foreach (var i in auto.AsEnumerable().Reverse().Where(i => States[i].On && !InMinOn(i, now)))
                {
                    if (need <= 0)
                    {
                        break;
                    }
                    shed.Add(i);
                    need -= Loads[i].OnPower;
                }
            }
            else if (!Soc.HasValue || Soc.Value >= Settings.MinSoc)
            {
                foreach (var candidate in auto.Where(i => !States[i].On && !InMinOff(i, now)))
                {
                    // a candidate may only take the modulated power of lower-priority analog loads
                    var candidateSpare = baseSpare + ModulationBelow(auto.IndexOf(candidate));
                    var required = Loads[candidate].OnPower + Settings.Hysteresis;
                    if (candidateSpare >= required)
                    {
                        start = candidate;
                        break;
                    }

                    // take the power of lower-priority running loads, lowest first
                    var free = candidateSpare;
                    var taken = new List<int>();
                    foreach (var lower in auto.AsEnumerable().Reverse()
                                 .TakeWhile(i => i != candidate)
                                 .Where(i => States[i].On && !InMinOn(i, now)))
                    {
                        taken.Add(lower);
                        free += Loads[lower].OnPower;
                        if (free >= required)
                        {
                            break;
                        }
                    }
                    if (free >= required)
                    {
                        start = candidate;
                        victims = taken;
                        break;
                    }
                }
            }

            // pending times: a load waits while its wanted state differs from its state
            foreach (var i in auto)
            {
                var wanted = shed.Contains(i) ? false : start == i || States[i].On;
                if (wanted == States[i].On)
                {
                    States[i].PendingSince = null;
                }
                else
                {
                    States[i].PendingSince ??= now;
                }
            }

            var settled = !_lastSwitchAt.HasValue;
            if (start.HasValue && Due(States[start.Value].PendingSince, Settings.OnDelay, now) && (settled || now - _lastSwitchAt.Value >= Settings.OnDelay))
            {
                foreach (var victim in victims)
                {
                    Switch(victim, false, now);
                }
                Switch(start.Value, true, now);
                RestartPending(now);
            }
            else if (shed.Count > 0 && shed.All(i => Due(States[i].PendingSince, Settings.OffDelay, now)) && (settled || now - _lastSwitchAt.Value >= Settings.OffDelay))
            {
                foreach (var i in shed)
                {
                    Switch(i, false, now);
                }
                RestartPending(now);
            }

            Modulate(auto, measured, now);

            Allocated = auto.Where(i => States[i].On).Sum(i => States[i].Setpoint);
        }

        private static DateTimeOffset? Max(DateTimeOffset? a, DateTimeOffset? b)
        {
            if (!a.HasValue)
            {
                return b;
            }
            return !b.HasValue || a.Value >= b.Value ? a : b;
        }

        private static bool Due(DateTimeOffset? since, TimeSpan delay, DateTimeOffset now) => since.HasValue && now - since.Value >= delay;

        private void RestartPending(DateTimeOffset now)
        {
            _lastSwitchAt = now;
            foreach (var state in States)
            {
                if (state.PendingSince.HasValue)
                {
                    state.PendingSince = now;
                }
            }
        }

        private void Switch(int i, bool on, DateTimeOffset now)
        {
            var state = States[i];
            state.PendingSince = null;
            if (state.On == on)
            {
                return;
            }
            state.On = on;
            state.ChangedAt = now;
            state.Setpoint = on ? Loads[i].OnPower : 0;
            state.SetpointChangedAt = now;
            // the setpoints wait for a meter value that shows the switch
            _modulatedAt = _measurement;
        }

        /// <summary>
        /// Locked, manual and disabled loads switch at once
        /// </summary>
        private void ApplyForced(DateTimeOffset now)
        {
            for (var i = 0; i < LoadCount; i++)
            {
                var state = States[i];
                if (!Loads[i].Configured)
                {
                    if (state.On)
                    {
                        Switch(i, false, now);
                    }
                    continue;
                }

                if (state.Locked || (!state.Manual && !Enabled))
                {
                    Switch(i, false, now);
                }
                else if (state.Manual)
                {
                    Switch(i, true, now);
                    state.Setpoint = Loads[i].FullPower;
                }
            }
        }

        private void Modulate(IList<int> auto, double? measured, DateTimeOffset now)
        {
            var analog = auto.Where(i => States[i].On && Loads[i].IsAnalog).ToList();
            // without a new meter value since the last change the measurement does not show it yet
            if (analog.Count == 0 || _measurement <= _modulatedAt)
            {
                return;
            }

            // spare power for the part above the switch-on power of the running analog loads
            var pool = measured.HasValue
                ? measured.Value - Settings.Reserve + analog.Sum(i => States[i].Setpoint - Loads[i].OnPower)
                : 0;

            foreach (var i in analog)
            {
                var load = Loads[i];
                var state = States[i];
                var extra = Math.Clamp(pool, 0, load.MaxPower - load.OnPower);
                pool -= extra;
                var target = Math.Round(load.OnPower + extra, 3);

                var lower = target < state.Setpoint;
                var atBound = target <= load.OnPower || target >= load.MaxPower;
                if (Math.Abs(target - state.Setpoint) < SetpointStep && !(atBound && target != state.Setpoint))
                {
                    continue;
                }
                // raise at most once per interval, and not until the meter has seen the last switch
                var lastChange = Max(state.SetpointChangedAt, _lastSwitchAt);
                if (!lower && lastChange.HasValue && now - lastChange.Value < Settings.ModulationInterval)
                {
                    continue;
                }
                state.Setpoint = target;
                state.SetpointChangedAt = now;
                _modulatedAt = _measurement;
            }
        }
    }
}
