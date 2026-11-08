using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Operations.Common;

namespace P3.Logic.Operations.Garage
{
    /// <summary>
    /// Sends one command to all linked Garage / Gate blocks (their central input, which ignores DisPc) and counts open and
    /// closed doors from their state outputs.
    /// The blocks picked in the "gates" parameter get the command on their central input directly and are counted from
    /// their state, polled every <see cref="PollInterval"/>.
    /// </summary>
    public class GarageCentralLogic : DispatchingLogic
    {
        internal static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

        private readonly int?[] _states = new int?[GarageCentralLogicFactory.InputStates.Length];
        private readonly System.Timers.Timer _poll = new() { AutoReset = true, Interval = PollInterval.TotalMilliseconds };
        private IReadOnlyList<Guid> _gates = Array.Empty<Guid>();
        private bool _running;
        private bool _locked;
        private bool _disablePeriphery;

        public GarageCentralLogic(ILogicContext context) : base(context)
        {
            _poll.Elapsed += (_, _) => Poll();
        }

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (Sync)
            {
                _running = true;
                SendCounts();
            }
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            lock (Sync)
            {
                _running = false;
                _poll.Stop();
            }
            return base.Stop(instance, token);
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (TemplateOf(instance) != GarageCentralLogicFactory.ParamGates)
            {
                return;
            }

            lock (Sync)
            {
                _gates = CentralTargets.Ids(value);
                if (_running && _gates.Count > 0)
                {
                    _poll.Start();
                }
                else
                {
                    _poll.Stop();
                }
                SendCounts();
            }
        }

        /// <summary>
        /// The picked doors are not linked: their state is polled
        /// </summary>
        internal void Poll()
        {
            lock (Sync)
            {
                if (_running)
                {
                    SendCounts();
                }
            }
        }

        private List<GarageGateLogic> Gates() => CentralTargets.Resolve(Context, _gates).OfType<GarageGateLogic>().ToList();

        /// <summary>
        /// The states of the linked doors (state inputs) and of the picked ones
        /// </summary>
        private List<int> KnownStates() =>
            _states.Where(a => a.HasValue).Select(a => a.Value).Concat(Gates().Select(a => (int)a.State)).ToList();

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (Sync)
            {
                var template = TemplateOf(instance);
                var stateIndex = Array.IndexOf(GarageCentralLogicFactory.InputStates, template);
                if (stateIndex >= 0)
                {
                    var state = LogicValue.ToDouble(value);
                    _states[stateIndex] = state.HasValue ? (int)state.Value : null;
                    SendCounts();
                }
                else if (template == GarageCentralLogicFactory.InputLock)
                {
                    _locked = LogicValue.IsTruthy(value);
                    if (_locked)
                    {
                        Command(GarageCommand.Stop);
                    }
                }
                else if (template == GarageCentralLogicFactory.InputDisablePeriphery)
                {
                    _disablePeriphery = LogicValue.IsTruthy(value);
                }
                else if (LogicValue.IsTruthy(value) && !_locked && (!_disablePeriphery || IsFromVisu(source)))
                {
                    if (template == GarageCentralLogicFactory.InputToggle)
                    {
                        Command(Toggle());
                    }
                    else if (template == GarageCentralLogicFactory.InputCompleteOpen)
                    {
                        Command(GarageCommand.Open);
                    }
                    else if (template == GarageCentralLogicFactory.InputCompleteClose)
                    {
                        Command(GarageCommand.Close);
                    }
                    else if (template == GarageCentralLogicFactory.InputPartialOpen)
                    {
                        Command(GarageCommand.PartialOpen);
                    }
                    else if (template == GarageCentralLogicFactory.InputStop)
                    {
                        Command(GarageCommand.Stop);
                    }
                }
            }
            return new List<ILogicOutputChanged>();
        }

        /// <summary>
        /// Stop when a door moves, otherwise close when a door is open, otherwise open
        /// </summary>
        internal GarageCommand Toggle()
        {
            var known = KnownStates().Select(a => (GarageState)a).ToList();
            if (known.Any(a => a is GarageState.Opening or GarageState.Closing))
            {
                return GarageCommand.Stop;
            }
            return known.Any(a => a is GarageState.Open or GarageState.PartiallyOpen) ? GarageCommand.Close : GarageCommand.Open;
        }

        private void Command(GarageCommand command)
        {
            SendAlways(GarageCentralLogicFactory.OutputCommand, (int)command);

            var from = Interface(GarageCentralLogicFactory.OutputCommand);
            foreach (var gate in Gates())
            {
                CentralTargets.Feed(gate, GarageGateLogicFactory.InputCentral, (int)command, from);
            }
            SendCounts();
        }

        private void SendCounts()
        {
            var known = KnownStates();
            Send(GarageCentralLogicFactory.OutputOpenCount, known.Count(a => a != (int)GarageState.Closed));
            Send(GarageCentralLogicFactory.OutputClosedCount, known.Count(a => a == (int)GarageState.Closed));
        }
    }
}
