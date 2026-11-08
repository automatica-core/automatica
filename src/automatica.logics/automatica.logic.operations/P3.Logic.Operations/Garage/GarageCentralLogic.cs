using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.Garage
{
    /// <summary>
    /// Sends one command to all linked Garage / Gate blocks (their central input, which ignores DisPc) and counts open and
    /// closed doors from their state outputs.
    /// </summary>
    public class GarageCentralLogic : DispatchingLogic
    {
        private readonly int?[] _states = new int?[GarageCentralLogicFactory.InputStates.Length];
        private bool _locked;
        private bool _disablePeriphery;

        public GarageCentralLogic(ILogicContext context) : base(context)
        {
        }

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (Sync)
            {
                SendCounts();
            }
            return base.Start(instance, token);
        }

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
            var known = _states.Where(a => a.HasValue).Select(a => (GarageState)a.Value).ToList();
            if (known.Any(a => a is GarageState.Opening or GarageState.Closing))
            {
                return GarageCommand.Stop;
            }
            return known.Any(a => a is GarageState.Open or GarageState.PartiallyOpen) ? GarageCommand.Close : GarageCommand.Open;
        }

        private void Command(GarageCommand command)
        {
            SendAlways(GarageCentralLogicFactory.OutputCommand, (int)command);
        }

        private void SendCounts()
        {
            var known = _states.Where(a => a.HasValue).Select(a => a.Value).ToList();
            Send(GarageCentralLogicFactory.OutputOpenCount, known.Count(a => a != (int)GarageState.Closed));
            Send(GarageCentralLogicFactory.OutputClosedCount, known.Count(a => a == (int)GarageState.Closed));
        }
    }
}
