using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Logic.Metering.EnergyMonitor;
using Microsoft.Extensions.Logging;

namespace Automatica.Logic.Metering.Meters
{
    /// <summary>
    /// Shared part of the meters: named <see cref="MeterRegister"/>s kept over restarts (<see cref="IMeterStateStore"/>),
    /// a timer that books held power, starts new days and lets derived power fall, and sending only changed outputs.
    /// Derived meters map their inputs in <see cref="OnInput"/> and list their outputs in <see cref="CurrentValues"/>.
    /// The state is loaded on the first start and after a stop; a restart without stop (logic edited) keeps the registers.
    /// Parameters come in after the start.
    /// </summary>
    public abstract class MeterRuleBase : Automatica.Core.Logic.Logic
    {
        internal const double TickIntervalMs = 30_000;
        internal static readonly TimeSpan SaveInterval = TimeSpan.FromMinutes(5);

        /// <summary>the store new meters use; tests swap it for an in-memory one</summary>
        internal static Func<IMeterStateStore> StoreFactory { get; set; } = () => new FileMeterStateStore();

        protected readonly object Sync = new();

        private readonly string[] _registerNames;
        private readonly Dictionary<Guid, RuleInterfaceInstance> _outputs;
        /// <summary>last value sent per output, only changes are sent</summary>
        private readonly Dictionary<Guid, double> _sent = new();

        private System.Timers.Timer _timer;
        private DateTimeOffset _lastSave;
        private bool _dirty;

        protected MeterRuleBase(ILogicContext context, params string[] registerNames) : base(context)
        {
            _registerNames = registerNames;
            _outputs = context.RuleInstance.RuleInterfaceInstance
                .Where(a => a.This2RuleInterfaceTemplateNavigation?.This2RuleInterfaceDirection == (long)Automatica.Core.Base.Templates.LogicInterfaceDirection.Output)
                .GroupBy(a => a.This2RuleInterfaceTemplate)
                .ToDictionary(a => a.Key, a => a.First());
            Store = StoreFactory();
            CreateRegisters();
        }

        internal IMeterStateStore Store { get; set; }

        protected Dictionary<string, MeterRegister> Registers { get; private set; }

        internal MeterRegister Register(string name)
        {
            lock (Sync)
            {
                return Registers[name];
            }
        }

        protected DateTimeOffset Now => Context.TimeProvider?.GetUtcNow() ?? DateTimeOffset.UtcNow;

        private Guid StateId => Context.RuleInstance.ObjId;

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (Sync)
            {
                var now = Now;
                if (_timer != null)
                {
                    // Restart (the logic was edited) starts again without a stop: keep counting with the registers we have
                    foreach (var register in Registers.Values)
                    {
                        register.Advance(now);
                    }
                    _sent.Clear();
                    return base.Start(instance, token);
                }

                CreateRegisters();
                MeterState state = null;
                try
                {
                    state = Store?.Load(StateId);
                }
                catch (Exception e)
                {
                    Context.Logger?.LogError(e, $"Could not read the saved state of meter {Context.RuleInstance.Name}, it starts at 0");
                }

                foreach (var (name, register) in Registers)
                {
                    if (state?.Registers != null && state.Registers.TryGetValue(name, out var saved))
                    {
                        register.Load(saved, now);
                    }
                    else
                    {
                        register.Advance(now);
                    }
                }
                LoadValues(state?.Values ?? new Dictionary<string, double>());
                Resume(now);
                _lastSave = now;
                _dirty = false;
                _sent.Clear();

                _timer?.Dispose();
                _timer = new System.Timers.Timer(TickIntervalMs) { AutoReset = true };
                _timer.Elapsed += (_, _) => Tick();
                _timer.Start();
            }
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            lock (Sync)
            {
                _timer?.Stop();
                _timer?.Dispose();
                _timer = null;
                foreach (var register in Registers.Values)
                {
                    register.Advance(Now);
                }
                Save();
            }
            return base.Stop(instance, token);
        }

        protected sealed override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (Sync)
            {
                if (!OnInput(instance.This2RuleInterfaceTemplate, value, Now))
                {
                    return new List<ILogicOutputChanged>();
                }
                _dirty = true;
                return Changes();
            }
        }

        protected sealed override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (Sync)
            {
                OnParameter(instance.This2RuleInterfaceTemplate, value);
            }
            base.ParameterValueChanged(instance, source, value);
        }

        /// <summary>maps an input value, false when the input is not one of this meter</summary>
        protected abstract bool OnInput(Guid template, object value, DateTimeOffset now);

        protected virtual void OnParameter(Guid template, object value)
        {
        }

        /// <summary>after (re)start: hand the power still held (e.g. the logic was edited) to the new registers</summary>
        protected virtual void Resume(DateTimeOffset now)
        {
        }

        /// <summary>all outputs (template, value); null values are not sent</summary>
        protected abstract IEnumerable<(Guid Template, double? Value)> CurrentValues(DateTimeOffset now);

        /// <summary>own values to keep over a restart</summary>
        protected virtual void SaveValues(IDictionary<string, double> values)
        {
        }

        protected virtual void LoadValues(IReadOnlyDictionary<string, double> values)
        {
        }

        /// <summary>books the held power, starts a new day after midnight, sends what changed and saves now and then</summary>
        internal void Tick()
        {
            IList<ILogicOutputChanged> changes;
            lock (Sync)
            {
                var now = Now;
                var day = Registers.Values.FirstOrDefault()?.Day;
                foreach (var register in Registers.Values)
                {
                    register.Advance(now);
                }
                var newDay = Registers.Values.FirstOrDefault()?.Day != day;
                changes = Changes();
                if (changes.Count > 0)
                {
                    _dirty = true;
                }
                if (_dirty && (newDay || now - _lastSave >= SaveInterval))
                {
                    Save();
                }
            }

            foreach (var change in changes)
            {
                Context.Dispatcher.DispatchValue(change.Instance, change.Value);
            }
        }

        public override object GetDataForVisu()
        {
            var data = base.GetDataForVisu() as Dictionary<string, object> ?? new Dictionary<string, object>();
            lock (Sync)
            {
                foreach (var (template, value) in CurrentValues(Now))
                {
                    if (value.HasValue && _outputs.TryGetValue(template, out var output))
                    {
                        data[output.ObjId.ToString()] = Round(value.Value);
                    }
                }
            }
            return data;
        }

        /// <summary>writes the state now (normally every few minutes, at midnight and on stop)</summary>
        internal void Save()
        {
            if (Store == null)
            {
                return;
            }
            try
            {
                var state = new MeterState();
                foreach (var (name, register) in Registers)
                {
                    state.Registers[name] = register.Save();
                }
                SaveValues(state.Values);
                Store.Save(StateId, state);
                _lastSave = Now;
                _dirty = false;
            }
            catch (Exception e)
            {
                Context.Logger?.LogError(e, $"Could not save the state of meter {Context.RuleInstance.Name}");
            }
        }

        private void CreateRegisters()
        {
            var timeZone = Context.TimeProvider?.LocalTimeZone;
            Registers = _registerNames.ToDictionary(a => a, _ => new MeterRegister(timeZone));
        }

        private IList<ILogicOutputChanged> Changes()
        {
            var ret = new List<ILogicOutputChanged>();
            foreach (var (template, raw) in CurrentValues(Now))
            {
                if (!raw.HasValue || !_outputs.TryGetValue(template, out var output))
                {
                    continue;
                }
                var value = Round(raw.Value);
                if (_sent.TryGetValue(template, out var last) && last.Equals(value))
                {
                    continue;
                }
                _sent[template] = value;
                ret.Add(new LogicOutputChanged(output, value));
            }
            return ret;
        }

        /// <summary>power and energy to 3 decimals (1 W / 1 Wh at kW / kWh), + 0 turns -0 into 0</summary>
        private static double Round(double value) => Math.Round(value, 3) + 0d;

        protected static double? ToDouble(object value) => EnergyMonitorRule.ToDouble(value);

        protected static bool ToBool(object value) => EnergyMonitorRule.ToBool(value);

        /// <summary>a factor parameter, 1 when it is empty, 0 or not a number</summary>
        protected static double Factor(object value) => ToDouble(value) is { } d && d != 0 ? d : 1;
    }
}
