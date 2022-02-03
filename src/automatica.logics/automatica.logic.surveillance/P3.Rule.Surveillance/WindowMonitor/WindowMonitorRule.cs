using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using Newtonsoft.Json.Linq;

[assembly: InternalsVisibleTo("P3.Rule.Surveillance.Tests")]

namespace P3.Rule.Surveillance.WindowMonitor
{
    internal enum WindowState
    {
        Undefined,
        Open = 1,
        Closed = 2,
        Tilt = 3,
        Locked = 4,
        Unlocked = 5
    }

    public class WindowMonitorRule : Automatica.Core.Rule.Rule
    {
        internal IDictionary<Guid, Tuple<IDispatchable, WindowState>> States { get; } = new Dictionary<Guid, Tuple<IDispatchable, WindowState>>();

        public WindowMonitorRule(IRuleContext context) : base(context)
        {

        }

        public override object GetDataForVisu()
        {
            var jArray = new JArray();

            foreach(var d in States)
            {
                var jObject = new JObject();
                jObject.Add(new JProperty("Id", d.Value.Item1.Id));
                jObject.Add(new JProperty("Name", d.Value.Item1.Name));
                jObject.Add(new JProperty("Value", d.Value.Item2));

                jArray.Add(jObject);
            }

            return jArray;
        }

        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var windowState = WindowState.Undefined;
            if (instance.This2RuleInterfaceTemplate == WindowMonitorFactory.Ct || instance.This2RuleInterfaceTemplate == WindowMonitorFactory.Cti)
            {
                var boolValue = !Convert.ToBoolean(value);

                if (instance.This2RuleInterfaceTemplate == WindowMonitorFactory.Cti)
                {
                    boolValue = !boolValue;
                }
                windowState = boolValue ? WindowState.Tilt : WindowState.Closed;
            }
            if (instance.This2RuleInterfaceTemplate == WindowMonitorFactory.Co || instance.This2RuleInterfaceTemplate == WindowMonitorFactory.Coi)
            {
                var boolValue = !Convert.ToBoolean(value);

                if(instance.This2RuleInterfaceTemplate == WindowMonitorFactory.Coi)
                {
                    boolValue = !boolValue;
                }

                windowState = boolValue ? WindowState.Open : WindowState.Closed;
            }
            if (instance.This2RuleInterfaceTemplate == WindowMonitorFactory.Cl || instance.This2RuleInterfaceTemplate == WindowMonitorFactory.Cli)
            {
                var boolValue = !Convert.ToBoolean(value);

                if (instance.This2RuleInterfaceTemplate == WindowMonitorFactory.Cli)
                {
                    boolValue = !boolValue;
                }
                windowState = boolValue ? WindowState.Unlocked : WindowState.Locked;
            }

            if (!States.ContainsKey(source.Id))
            {
                States.Add(source.Id, new Tuple<IDispatchable, WindowState>(source, windowState));
            }
            else
            {
                States[source.Id] = new Tuple<IDispatchable, WindowState>(source, windowState);
            }

            // TODO: Notify only if a state changes???

            Context.Notify.NotifyValueChanged(Context.RuleInstance.RuleInterfaceInstance.First(), GetDataForVisu());

            return new List<IRuleOutputChanged>();
        }

    }
}
