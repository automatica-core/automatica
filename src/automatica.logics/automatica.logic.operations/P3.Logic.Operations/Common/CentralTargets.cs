using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.Control.Base;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace P3.Logic.Operations.Common
{
    /// <summary>
    /// The blocks picked in the "controls" parameter of a central (the same picker as the Matter and HomeKit bridges).
    /// The central writes its commands to their inputs as if its outputs were linked to them and reads their state back,
    /// so nothing has to be linked by hand.
    /// Twin copy in P3.Logic.Lightning and P3.Logic.Sonos (separate split repos): keep them identical apart from the namespace.
    /// </summary>
    internal static class CentralTargets
    {
        /// <summary>
        /// The control ids of a <c>RuleInterfaceParameterDataType.Controls</c> parameter value (object, JSON string or JObject)
        /// </summary>
        public static IReadOnlyList<Guid> Ids(object parameterValue)
        {
            try
            {
                return parameterValue switch
                {
                    ControlConfiguration configuration => configuration.Controls ?? new List<Guid>(),
                    string json when !string.IsNullOrWhiteSpace(json) => JsonConvert.DeserializeObject<ControlConfiguration>(json)?.Controls ?? new List<Guid>(),
                    JToken token => token.ToObject<ControlConfiguration>()?.Controls ?? new List<Guid>(),
                    _ => Array.Empty<Guid>()
                };
            }
            catch (Exception)
            {
                return Array.Empty<Guid>();
            }
        }

        /// <summary>
        /// The picked controls that are running (a deleted block is skipped), never the central itself
        /// </summary>
        public static List<IControl> Resolve(ILogicContext context, IReadOnlyList<Guid> ids)
        {
            if (ids == null || ids.Count == 0 || context.ControlContext == null)
            {
                return new List<IControl>();
            }

            try
            {
                var controls = context.ControlContext.GetAsync(ids.ToList()).GetAwaiter().GetResult();
                return controls?.Where(a => a != null && a.Id != context.RuleInstance.ObjId).ToList() ?? new List<IControl>();
            }
            catch (Exception e)
            {
                context.Logger.LogWarning(e, $"Could not resolve the controls of {context.RuleInstance.Name}");
                return new List<IControl>();
            }
        }

        /// <summary>
        /// Writes <paramref name="value"/> to the input <paramref name="inputTemplate"/> of a picked logic, the way a link from
        /// <paramref name="from"/> (an output of the central) delivers it. Outputs the logic returns are dispatched.
        /// </summary>
        public static void Feed(Automatica.Core.Logic.Logic target, Guid inputTemplate, object value, RuleInterfaceInstance from)
        {
            var input = target.Context.RuleInstance.RuleInterfaceInstance.FirstOrDefault(a => a.This2RuleInterfaceTemplate == inputTemplate);
            if (input == null)
            {
                return;
            }

            try
            {
                IDispatchable source = from?.This2RuleInstanceNavigation != null ? new LogicInterfaceInstanceDispatchable(from) : null;
                var results = target.ValueChanged(input, source, value);
                foreach (var result in results ?? new List<ILogicOutputChanged>())
                {
                    target.Context.Dispatcher.DispatchValue(result.Instance, result.Value);
                }
            }
            catch (Exception e)
            {
                target.Context.Logger.LogWarning(e, $"Could not send {value} to {target.Context.RuleInstance.Name}");
            }
        }
    }
}
