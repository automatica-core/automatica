using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Control.Base;
using Automatica.Core.Logic;
using Automatica.Core.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace P3.Logic.Surveillance.Security
{
    public enum ControlReaction
    {
        /// <summary>
        /// Switches on, dimmers to 100 %
        /// </summary>
        LightsOn,
        LightsOff,
        /// <summary>
        /// Opens blinds (escape routes, visibility)
        /// </summary>
        BlindsUp
    }

    /// <summary>
    /// Reactions of the alarms on linked controls (switches, dimmers, blinds) picked in a "controls" parameter.
    /// </summary>
    public static class ControlReactions
    {
        /// <summary>
        /// The control ids of a <c>RuleInterfaceParameterDataType.Controls</c> parameter value (object, JSON string or JObject)
        /// </summary>
        public static IReadOnlyList<Guid> Ids(object parameterValue)
        {
            try
            {
                switch (parameterValue)
                {
                    case null:
                        return Array.Empty<Guid>();
                    case ControlConfiguration configuration:
                        return configuration.Controls ?? new List<Guid>();
                    case string json when !string.IsNullOrWhiteSpace(json):
                        return JsonConvert.DeserializeObject<ControlConfiguration>(json)?.Controls ?? new List<Guid>();
                    case JToken token:
                        return token.ToObject<ControlConfiguration>()?.Controls ?? new List<Guid>();
                    default:
                        return Array.Empty<Guid>();
                }
            }
            catch (Exception)
            {
                return Array.Empty<Guid>();
            }
        }

        /// <summary>
        /// Applies the reaction to the controls, in the background (never under the lock of a logic)
        /// </summary>
        public static Task ApplyAsync(ILogicContext context, IReadOnlyList<Guid> controlIds, ControlReaction reaction)
        {
            if (controlIds == null || controlIds.Count == 0 || context.ControlContext == null)
            {
                return Task.CompletedTask;
            }

            return Task.Run(async () =>
            {
                try
                {
                    var controls = await context.ControlContext.GetAsync(controlIds.ToList());
                    foreach (var control in controls ?? new List<IControl>())
                    {
                        try
                        {
                            await Apply(control, reaction);
                        }
                        catch (Exception e)
                        {
                            context.Logger.LogWarning(e, $"Alarm reaction {reaction} on control {control?.Name} failed");
                        }
                    }
                }
                catch (Exception e)
                {
                    context.Logger.LogWarning(e, $"Alarm reaction {reaction} failed");
                }
            });
        }

        private static Task Apply(IControl control, ControlReaction reaction)
        {
            switch (reaction)
            {
                case ControlReaction.LightsOn when control is IDimmer dimmer:
                    return dimmer.DimAsync(100);
                case ControlReaction.LightsOn when control is ISwitch @switch:
                    return @switch.SwitchOnAsync();
                case ControlReaction.LightsOff when control is ISwitch @switch:
                    return @switch.SwitchOffAsync();
                case ControlReaction.BlindsUp when control is IBlind blind:
                    return blind.MoveUpAsync();
                default:
                    return Task.CompletedTask;
            }
        }
    }
}
