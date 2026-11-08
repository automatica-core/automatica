using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using P3.Logic.StringOperations.Common;

namespace P3.Logic.StringOperations.Tracker
{
    /// <summary>
    /// Event log: each value on I is rendered with the template (bools with the on / off texts when set) and stored newest first.
    /// With "remanent" the entries are written to persistent/logic-data/tracker/&lt;logic id&gt;.json and read back on start.
    /// </summary>
    public class TrackerRule : TextLogicBase
    {
        internal sealed class Entry
        {
            [JsonProperty("time")]
            public DateTimeOffset Time { get; set; }

            [JsonProperty("text")]
            public string Text { get; set; }

            [JsonProperty("source")]
            public string Source { get; set; }
        }

        /// <summary>
        /// Folder of the stored entries; tests point it to a temp folder
        /// </summary>
        internal static string StorageDirectoryOverride { get; set; }

        private readonly object _sync = new();
        private readonly List<Entry> _entries = new(); // newest first
        private bool _reset;

        public TrackerRule(ILogicContext context) : base(context)
        {
        }

        internal IReadOnlyList<Entry> Entries
        {
            get
            {
                lock (_sync)
                {
                    return _entries.ToList();
                }
            }
        }

        private int Limit => (int)Math.Clamp(ParameterDouble(TrackerLogicFactory.ParamEntries, TrackerLogicFactory.DefaultEntries),
            TrackerLogicFactory.MinEntries, TrackerLogicFactory.MaxEntries);

        private bool Remanent => Parameter(TrackerLogicFactory.ParamRemanent) is not { } value || TextValue.IsTruthy(value);

        internal string StoragePath
        {
            get
            {
                var root = StorageDirectoryOverride ?? Path.Combine(ServerInfo.GetPersistentDirectory(), "logic-data", "tracker");
                return Path.Combine(root, $"{Context.RuleInstance.ObjId}.json");
            }
        }

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                _entries.Clear();
                if (Remanent)
                {
                    _entries.AddRange(Load());
                    Trim();
                }

                if (_entries.Count > 0)
                {
                    Dispatch(TrackerLogicFactory.OutputLast, _entries[0].Text);
                    Dispatch(TrackerLogicFactory.OutputCount, _entries.Count);
                }
            }
            return base.Start(instance, token);
        }

        protected override void OnParameterChanged(Guid template, object value)
        {
            if (!Started)
            {
                return;
            }

            lock (_sync)
            {
                if (template == TrackerLogicFactory.ParamEntries && Trim())
                {
                    Save();
                    Dispatch(TrackerLogicFactory.OutputCount, _entries.Count);
                }
                else if (template == TrackerLogicFactory.ParamRemanent)
                {
                    Save();
                }
            }
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == TrackerLogicFactory.InputReset)
            {
                var reset = TextValue.IsTruthy(value);
                var rising = reset && !_reset;
                _reset = reset;
                return rising ? Clear() : NoChanges();
            }

            if (instance.This2RuleInterfaceTemplate != TrackerLogicFactory.InputValue || value == null)
            {
                return NoChanges();
            }

            var text = Render(value, source?.Name);
            if (string.IsNullOrWhiteSpace(text))
            {
                return NoChanges();
            }

            lock (_sync)
            {
                _entries.Insert(0, new Entry { Time = Context.TimeProvider.GetUtcNow(), Text = text, Source = source?.Name });
                Trim();
                Save();

                return new List<ILogicOutputChanged>
                {
                    Changed(TrackerLogicFactory.OutputLast, text),
                    Changed(TrackerLogicFactory.OutputCount, _entries.Count)
                }.Where(a => a != null).ToList();
            }
        }

        internal string Render(object value, string sourceName)
        {
            if (value is bool b)
            {
                var stateText = TextValue.NullIfEmpty(Parameter(b ? TrackerLogicFactory.ParamOnText : TrackerLogicFactory.ParamOffText));
                if (stateText != null)
                {
                    return TextTemplate.Render(stateText, new[] { value }, sourceName ?? string.Empty);
                }
            }

            var template = TextValue.NullIfEmpty(Parameter(TrackerLogicFactory.ParamTemplate)) ?? TrackerLogicFactory.DefaultTemplate;
            return TextTemplate.Render(template, new[] { value }, sourceName ?? string.Empty);
        }

        private IList<ILogicOutputChanged> Clear()
        {
            lock (_sync)
            {
                _entries.Clear();
                Save();
                return new List<ILogicOutputChanged>
                {
                    Changed(TrackerLogicFactory.OutputLast, string.Empty),
                    Changed(TrackerLogicFactory.OutputCount, 0)
                }.Where(a => a != null).ToList();
            }
        }

        /// <returns>true when entries were removed</returns>
        private bool Trim()
        {
            var limit = Limit;
            if (_entries.Count <= limit)
            {
                return false;
            }
            _entries.RemoveRange(limit, _entries.Count - limit);
            return true;
        }

        private List<Entry> Load()
        {
            try
            {
                var path = StoragePath;
                if (!File.Exists(path))
                {
                    return new List<Entry>();
                }
                var entries = JsonConvert.DeserializeObject<List<Entry>>(File.ReadAllText(path)) ?? new List<Entry>();
                return entries.Where(a => a != null && !string.IsNullOrEmpty(a.Text)).OrderByDescending(a => a.Time).ToList();
            }
            catch (Exception e)
            {
                Context.Logger.LogWarning(e, $"Tracker {Context.RuleInstance?.Name}: could not read the stored entries");
                return new List<Entry>();
            }
        }

        private void Save()
        {
            try
            {
                var path = StoragePath;
                if (!Remanent)
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    return;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                // write a temp file and replace: a crash never leaves a half written log
                var temp = $"{path}.{Guid.NewGuid():N}.tmp";
                File.WriteAllText(temp, JsonConvert.SerializeObject(_entries));
                File.Move(temp, path, true);
            }
            catch (Exception e)
            {
                Context.Logger.LogWarning(e, $"Tracker {Context.RuleInstance?.Name}: could not store the entries");
            }
        }

        public override object GetDataForVisu()
        {
            var data = base.GetDataForVisu() as IDictionary<string, object> ?? new Dictionary<string, object>();
            lock (_sync)
            {
                data["entries"] = _entries.Select(a => new Dictionary<string, object>
                {
                    ["time"] = a.Time,
                    ["text"] = a.Text,
                    ["source"] = a.Source
                }).ToList();
                data["limit"] = Limit;
            }
            return data;
        }
    }
}
