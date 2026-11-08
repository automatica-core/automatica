using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.Logic;
using P3.Logic.Lightning.Common;
using F = P3.Logic.Lightning.LightingCentral.LightingCentralLogicFactory;

namespace P3.Logic.Lightning.LightingCentral
{
    /// <summary>
    /// +, -, off and on are forwarded as a pulse (1 then 0, the lighting controllers react on the rising edge), the mood number and
    /// "disable motion" as their value. The active inputs are counted.
    /// </summary>
    public class LightingCentralLogic : ClockedLogic
    {
        private readonly Dictionary<Guid, bool> _edges = new();
        private readonly bool[] _active = new bool[F.Rooms];

        public LightingCentralLogic(ILogicContext context) : base(context)
        {
        }

        protected override void ApplyParameter(Guid template, object value)
        {
        }

        protected override void OnStart()
        {
            PublishCount();
        }

        private bool Rising(Guid template, object value)
        {
            var state = LightingValues.IsTruthy(value);
            _edges.TryGetValue(template, out var previous);
            _edges[template] = state;
            return state && !previous;
        }

        protected override void OnInput(Guid template, object value, IDispatchable source, DateTime now)
        {
            if (template == F.MoodPlus && Rising(template, value))
            {
                PulseOut(F.MoodPlusOut);
            }
            else if (template == F.MoodMinus && Rising(template, value))
            {
                PulseOut(F.MoodMinusOut);
            }
            else if (template == F.Off && Rising(template, value))
            {
                PulseOut(F.OffOut);
            }
            else if (template == F.On && Rising(template, value))
            {
                PulseOut(F.OnOut);
            }
            else if (template == F.Mood)
            {
                var mood = LightingValues.ToInt(value, -1);
                if (mood >= 0)
                {
                    // the same mood again must reach the controllers again (they may have changed in between)
                    Pulse(Interface(F.MoodOut), mood);
                }
            }
            else if (template == F.DisableMotion)
            {
                Pulse(Interface(F.DisableMotionOut), LightingValues.IsTruthy(value));
            }
            else
            {
                var room = Array.IndexOf(F.Active, template);
                if (room >= 0)
                {
                    _active[room] = LightingValues.IsTruthy(value);
                    PublishCount();
                }
            }
        }

        private void PulseOut(Guid output)
        {
            var iface = Interface(output);
            Pulse(iface, true);
            Pulse(iface, false);
        }

        private void PublishCount()
        {
            var count = _active.Count(a => a);
            Output(Interface(F.ActiveCount), count);
            Output(Interface(F.AnyActive), count > 0);
        }

        protected override DateTime? OnTick(DateTime now) => null;
    }
}
