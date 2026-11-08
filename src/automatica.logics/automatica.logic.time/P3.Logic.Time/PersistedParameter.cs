using System;
using Automatica.Core.Base.IO;
using Newtonsoft.Json;

namespace P3.Logic.Time
{
    /// <summary>
    /// A logic parameter that can be changed at runtime (visu, inputs) and keeps the runtime value across restarts.
    /// The configured value (logic editor) wins again as soon as the admin changes it there.
    /// </summary>
    internal sealed class PersistedParameter
    {
        private readonly RemanentValue _store;
        private string _configured;

        public PersistedParameter(IDispatcher dispatcher, Guid parameterInterfaceId, string purpose)
        {
            _store = new RemanentValue(dispatcher, parameterInterfaceId, purpose);
        }

        /// <summary>
        /// true when <paramref name="source"/> is a runtime change (visu) and not the configured value of the logic editor
        /// </summary>
        public static bool IsRuntimeSource(IDispatchable source)
        {
            return source != null && (source.Type == DispatchableType.Visualization || source.Source == DispatchableSource.Visualization);
        }

        /// <summary>
        /// The configured value arrived (start, restart, editor). Returns the value to use: the stored runtime value while the configured
        /// value did not change since it was stored, otherwise the configured value.
        /// </summary>
        public string Configured(string configured)
        {
            _configured = configured ?? "";
            var stored = Read();
            if (stored != null && string.Equals(stored.Base ?? "", _configured, StringComparison.Ordinal))
            {
                return stored.Value;
            }
            return _configured;
        }

        public void Changed(string value)
        {
            _store.Save(JsonConvert.SerializeObject(new Stored { Base = _configured ?? "", Value = value }));
        }

        private Stored Read()
        {
            var text = _store.Load();
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }
            try
            {
                return JsonConvert.DeserializeObject<Stored>(text);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private sealed class Stored
        {
            public string Base { get; set; }
            public string Value { get; set; }
        }
    }
}
