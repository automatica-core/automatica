using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.StringOperations.Common;

namespace P3.Logic.StringOperations.StatusMonitor
{
    /// <summary>
    /// Keeps the last value of every object on Stat and maps it to the first state whose value matches
    /// (numbers compare as numbers, text ignores the case, "a|b" matches a or b). Objects without a matching state count as rest.
    /// Txlc and the activity log get "name: state text" when an object changes to a defined state; the first value of an
    /// object (startup) is no change. The log lives in memory only, like on the Miniserver.
    /// </summary>
    public class StatusMonitorRule : TextLogicBase
    {
        internal sealed class MonitoredObject
        {
            public Guid Id { get; init; }
            public string Name { get; set; }
            public object Value { get; set; }
            /// <summary>1..10, 0 = rest</summary>
            public int State { get; set; }
            public DateTimeOffset Changed { get; set; }
        }

        internal sealed record LogEntry(DateTimeOffset Time, Guid Id, string Name, int State, string Text);

        private readonly object _sync = new();
        private readonly Dictionary<Guid, MonitoredObject> _objects = new();
        private readonly LinkedList<LogEntry> _log = new();
        private readonly int?[] _sentCounts = new int?[StatusMonitorLogicFactory.StateCount + 1]; // [0] = rest
        private string _lastChange = string.Empty;

        public StatusMonitorRule(ILogicContext context) : base(context)
        {
        }

        internal IReadOnlyCollection<MonitoredObject> Objects
        {
            get
            {
                lock (_sync)
                {
                    return _objects.Values.ToList();
                }
            }
        }

        internal IReadOnlyList<LogEntry> Log
        {
            get
            {
                lock (_sync)
                {
                    return _log.ToList();
                }
            }
        }

        private int LogLimit => (int)Math.Clamp(ParameterDouble(StatusMonitorLogicFactory.ParamLogEntries, StatusMonitorLogicFactory.DefaultLogEntries),
            0, StatusMonitorLogicFactory.MaxLogEntries);

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate != StatusMonitorLogicFactory.InputStatus)
            {
                return NoChanges();
            }

            lock (_sync)
            {
                var changes = new List<ILogicOutputChanged>();
                var id = source?.Id ?? instance.ObjId;
                var now = Context.TimeProvider.GetUtcNow();
                var state = StateOf(value);

                if (!_objects.TryGetValue(id, out var monitored))
                {
                    monitored = new MonitoredObject { Id = id, Name = source?.Name, Value = value, State = state, Changed = now };
                    _objects[id] = monitored;
                }
                else
                {
                    monitored.Value = value;
                    if (!string.IsNullOrEmpty(source?.Name))
                    {
                        monitored.Name = source.Name;
                    }
                    if (monitored.State != state)
                    {
                        monitored.State = state;
                        monitored.Changed = now;
                        if (state > 0)
                        {
                            AddChange(changes, monitored, now);
                        }
                    }
                }

                changes.AddRange(CountChanges());
                return changes;
            }
        }

        private void AddChange(List<ILogicOutputChanged> changes, MonitoredObject monitored, DateTimeOffset now)
        {
            var text = StateText(monitored.State);
            _lastChange = string.IsNullOrEmpty(monitored.Name) ? text : $"{monitored.Name}: {text}";
            var change = Changed(StatusMonitorLogicFactory.OutputLastChange, _lastChange);
            if (change != null)
            {
                changes.Add(change);
            }

            var limit = LogLimit;
            if (limit > 0)
            {
                _log.AddFirst(new LogEntry(now, monitored.Id, monitored.Name, monitored.State, text));
                while (_log.Count > limit)
                {
                    _log.RemoveLast();
                }
            }
        }

        protected override void OnParameterChanged(Guid template, object value)
        {
            lock (_sync)
            {
                if (template == StatusMonitorLogicFactory.ParamLogEntries)
                {
                    var limit = LogLimit;
                    while (_log.Count > limit)
                    {
                        _log.RemoveLast();
                    }
                    return;
                }

                if (Array.IndexOf(StatusMonitorLogicFactory.ParamValue, template) < 0)
                {
                    return;
                }

                // other state values: map every object again
                foreach (var monitored in _objects.Values)
                {
                    monitored.State = StateOf(monitored.Value);
                }

                if (Started && _objects.Count > 0)
                {
                    Dispatch(CountChanges());
                }
            }
        }

        /// <summary>
        /// The counts that differ from the ones sent last (all of them the first time)
        /// </summary>
        private IList<ILogicOutputChanged> CountChanges()
        {
            var counts = new int[StatusMonitorLogicFactory.StateCount + 1];
            foreach (var monitored in _objects.Values)
            {
                counts[monitored.State]++;
            }

            var changes = new List<ILogicOutputChanged>();
            for (var i = 0; i < counts.Length; i++)
            {
                if (_sentCounts[i] == counts[i])
                {
                    continue;
                }
                _sentCounts[i] = counts[i];
                var change = Changed(i == 0 ? StatusMonitorLogicFactory.OutputRest : StatusMonitorLogicFactory.OutputCount[i - 1], counts[i]);
                if (change != null)
                {
                    changes.Add(change);
                }
            }
            return changes;
        }

        /// <summary>
        /// 1..10 for the first state whose value matches, 0 = rest
        /// </summary>
        internal int StateOf(object value)
        {
            for (var i = 0; i < StatusMonitorLogicFactory.StateCount; i++)
            {
                var stateValue = TextValue.NullIfEmpty(Parameter(StatusMonitorLogicFactory.ParamValue[i]));
                if (stateValue != null && Matches(value, stateValue))
                {
                    return i + 1;
                }
            }
            return 0;
        }

        internal static bool Matches(object value, string stateValue)
        {
            var valueIsNumber = TextValue.TryToDouble(value, out var number);
            var text = TextValue.ToText(value).Trim();

            foreach (var alternative in stateValue.Split('|'))
            {
                var candidate = alternative.Trim();
                if (valueIsNumber && TextValue.TryParse(candidate, out var candidateNumber))
                {
                    if (Math.Abs(number - candidateNumber) < 1e-9)
                    {
                        return true;
                    }
                    continue;
                }
                if (value is bool b && bool.TryParse(candidate, out var candidateBool))
                {
                    if (b == candidateBool)
                    {
                        return true;
                    }
                    continue;
                }
                if (string.Equals(text, candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        internal string StateText(int state)
        {
            if (state == 0)
            {
                return ParameterText(StatusMonitorLogicFactory.ParamRestText);
            }
            var text = ParameterText(StatusMonitorLogicFactory.ParamText[state - 1]);
            return string.IsNullOrEmpty(text) ? TextValue.NullIfEmpty(Parameter(StatusMonitorLogicFactory.ParamValue[state - 1])) ?? string.Empty : text;
        }

        private string StateColor(int state)
        {
            return state == 0
                ? ParameterText(StatusMonitorLogicFactory.ParamRestColor)
                : ParameterText(StatusMonitorLogicFactory.ParamColor[state - 1]);
        }

        public override object GetDataForVisu()
        {
            var data = base.GetDataForVisu() as IDictionary<string, object> ?? new Dictionary<string, object>();

            lock (_sync)
            {
                var counts = new int[StatusMonitorLogicFactory.StateCount + 1];
                foreach (var monitored in _objects.Values)
                {
                    counts[monitored.State]++;
                }

                var states = new List<object>();
                for (var i = 1; i <= StatusMonitorLogicFactory.StateCount; i++)
                {
                    if (TextValue.NullIfEmpty(Parameter(StatusMonitorLogicFactory.ParamValue[i - 1])) == null)
                    {
                        continue;
                    }
                    states.Add(new Dictionary<string, object>
                    {
                        ["state"] = i,
                        ["text"] = StateText(i),
                        ["color"] = StateColor(i),
                        ["count"] = counts[i]
                    });
                }

                data["monitor"] = new Dictionary<string, object>
                {
                    ["states"] = states,
                    ["rest"] = new Dictionary<string, object>
                    {
                        ["state"] = 0,
                        ["text"] = StateText(0),
                        ["color"] = StateColor(0),
                        ["count"] = counts[0]
                    },
                    ["objects"] = _objects.Values
                        .OrderBy(a => a.State == 0 ? int.MaxValue : a.State)
                        .ThenBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
                        .Select(a => new Dictionary<string, object>
                        {
                            ["id"] = a.Id,
                            ["name"] = a.Name,
                            ["value"] = TextValue.ToText(a.Value),
                            ["state"] = a.State,
                            ["changed"] = a.Changed
                        }).ToList(),
                    ["log"] = _log.Select(a => new Dictionary<string, object>
                    {
                        ["time"] = a.Time,
                        ["name"] = a.Name,
                        ["state"] = a.State,
                        ["text"] = a.Text
                    }).ToList(),
                    ["lastChange"] = _lastChange
                };
            }

            return data;
        }
    }
}
