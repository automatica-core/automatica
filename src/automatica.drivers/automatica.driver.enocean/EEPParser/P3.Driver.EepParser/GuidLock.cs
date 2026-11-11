using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace P3.Driver.EepParser
{
    /// <summary>
    /// Pins the generated template guids (eep-guids.json, keyed by the static field name).
    /// The computed guids depend on the position of a type/case/data field, so without the lock a new or reordered
    /// field would move the guids of existing templates and break existing node instances.
    /// </summary>
    public class GuidLock
    {
        private readonly string _path;
        private readonly SortedDictionary<string, string> _locked;
        private readonly HashSet<Guid> _used = new HashSet<Guid>();
        private readonly HashSet<string> _resolved = new HashSet<string>();

        private GuidLock(string path, SortedDictionary<string, string> locked)
        {
            _path = path;
            _locked = locked;
            foreach (var guid in locked.Values)
            {
                if (!_used.Add(Guid.Parse(guid)))
                {
                    throw new InvalidDataException($"Duplicate guid {guid} in {path}");
                }
            }
        }

        public static GuidLock Load(string path)
        {
            var locked = File.Exists(path)
                ? JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path))
                : null;
            return new GuidLock(path, new SortedDictionary<string, string>(locked ?? new Dictionary<string, string>(), StringComparer.Ordinal));
        }

        /// <param name="name">the static field name, e.g. TypeA5_02_05_1Guid</param>
        /// <param name="computed">the position based guid</param>
        /// <param name="bumpIndexFromEnd">the guid byte (from the end) that is counted up if the computed guid is taken</param>
        public Guid Resolve(string name, Guid computed, int bumpIndexFromEnd)
        {
            if (!_resolved.Add(name))
            {
                throw new InvalidOperationException($"{name} resolved twice");
            }

            if (_locked.TryGetValue(name, out var locked))
            {
                return Guid.Parse(locked);
            }

            var guid = computed;
            var bytes = guid.ToByteArray();
            var tries = 0;
            while (_used.Contains(guid))
            {
                if (++tries > 255)
                {
                    throw new InvalidOperationException($"No free guid for {name}");
                }
                bytes[bytes.Length - bumpIndexFromEnd]++;
                guid = new Guid(bytes);
            }

            _used.Add(guid);
            _locked[name] = guid.ToString();
            return guid;
        }

        public void Save()
        {
            File.WriteAllText(_path, JsonConvert.SerializeObject(_locked, Formatting.Indented).Replace("\r\n", "\n").Replace("\n", "\r\n") + "\r\n");
        }
    }
}
