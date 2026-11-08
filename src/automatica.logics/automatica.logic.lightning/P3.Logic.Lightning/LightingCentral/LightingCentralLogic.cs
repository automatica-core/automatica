using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Control.Base;
using Automatica.Core.Logic;
using Microsoft.Extensions.Logging;
using P3.Logic.Lightning.Common;
using P3.Logic.Lightning.LightingController;
using F = P3.Logic.Lightning.LightingCentral.LightingCentralLogicFactory;

namespace P3.Logic.Lightning.LightingCentral
{
    /// <summary>
    /// +, -, off and on are forwarded as a pulse (1 then 0, the lighting controllers react on the rising edge), the mood number and
    /// "disable motion" as their value. The active inputs are counted.
    /// The rooms picked in the "lights" parameter get the same commands directly: lighting controllers on their inputs,
    /// switches and dimmers on, off (and moods 0 / 99). They are counted from their state, polled every
    /// <see cref="PollInterval"/>.
    /// </summary>
    public class LightingCentralLogic : ClockedLogic
    {
        private readonly Dictionary<Guid, bool> _edges = new();
        internal static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

        private readonly bool[] _active = new bool[F.Rooms];
        private IReadOnlyList<Guid> _lights = Array.Empty<Guid>();

        public LightingCentralLogic(ILogicContext context) : base(context)
        {
        }

        protected override void ApplyParameter(Guid template, object value)
        {
            if (template == F.ParamLights)
            {
                _lights = CentralTargets.Ids(value);
                if (Running)
                {
                    PublishCount();
                }
            }
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
                PulseControllers(F.MoodPlusOut, ids => ids.MoodPlus);
            }
            else if (template == F.MoodMinus && Rising(template, value))
            {
                PulseOut(F.MoodMinusOut);
                PulseControllers(F.MoodMinusOut, ids => ids.MoodMinus);
            }
            else if (template == F.Off && Rising(template, value))
            {
                PulseOut(F.OffOut);
                PulseControllers(F.OffOut, ids => ids.Off);
                SwitchAll(false);
            }
            else if (template == F.On && Rising(template, value))
            {
                PulseOut(F.OnOut);
                PulseControllers(F.OnOut, ids => ids.On);
                SwitchAll(true);
            }
            else if (template == F.Mood)
            {
                var mood = LightingValues.ToInt(value, -1);
                if (mood >= 0)
                {
                    // the same mood again must reach the controllers again (they may have changed in between)
                    Pulse(Interface(F.MoodOut), mood);
                    FeedControllers(F.MoodOut, ids => ids.MoodSelect, mood);
                    if (mood == Mood.AllOff || mood == Mood.AllOn)
                    {
                        SwitchAll(mood == Mood.AllOn);
                    }
                }
            }
            else if (template == F.DisableMotion)
            {
                var disable = LightingValues.IsTruthy(value);
                Pulse(Interface(F.DisableMotionOut), disable);
                FeedControllers(F.DisableMotionOut, ids => ids.DisableMotion, disable);
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

        private List<IControl> Lights() => CentralTargets.Resolve(Context, _lights);

        /// <summary>
        /// A pulse (1 then 0) on an input of every picked lighting controller, like a link from <paramref name="output"/>
        /// </summary>
        private void PulseControllers(Guid output, Func<LightingControllerIds, Guid> input)
        {
            var from = Interface(output);
            foreach (var controller in Lights().OfType<LightingControllerLogic>())
            {
                CentralTargets.Feed(controller, input(controller.Ids), true, from);
                CentralTargets.Feed(controller, input(controller.Ids), false, from);
            }
            PublishCount();
        }

        private void FeedControllers(Guid output, Func<LightingControllerIds, Guid> input, object value)
        {
            var from = Interface(output);
            foreach (var controller in Lights().OfType<LightingControllerLogic>())
            {
                CentralTargets.Feed(controller, input(controller.Ids), value, from);
            }
            PublishCount();
        }

        /// <summary>
        /// Picked switches and dimmers (all on = dimmers to 100 %, like mood 99 of a lighting controller)
        /// </summary>
        private void SwitchAll(bool on)
        {
            foreach (var light in Lights().OfType<ISwitch>())
            {
                var task = on && light is IDimmer dimmer ? dimmer.DimAsync(100) : on ? light.SwitchOnAsync() : light.SwitchOffAsync();
                task.ContinueWith(t => Context.Logger.LogWarning(t.Exception, $"Lighting central {Context.RuleInstance.Name}: switching {light.Name} failed"),
                    TaskContinuationOptions.OnlyOnFaulted);
            }
            PublishCount();
        }

        private static bool IsOn(IControl control) => control switch
        {
            LightingControllerLogic controller => controller.AnyLightOn,
            ISwitch light => light.State == SwitchState.On,
            _ => false
        };

        private void PublishCount()
        {
            var count = _active.Count(a => a) + Lights().Count(IsOn);
            Output(Interface(F.ActiveCount), count);
            Output(Interface(F.AnyActive), count > 0);
        }

        protected override DateTime? OnTick(DateTime now)
        {
            if (_lights.Count == 0)
            {
                return null;
            }

            // the picked rooms are not linked: their state is polled
            PublishCount();
            return now + PollInterval;
        }
    }
}
