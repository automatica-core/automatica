using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Automatica.Core.Base.Common;

namespace Automatica.Logic.Metering.Meters
{
    /// <summary>what a meter keeps over a restart: its registers and a few values of its own (e.g. the last pulse counter)</summary>
    public sealed class MeterState
    {
        public Dictionary<string, MeterRegisterState> Registers { get; set; } = new();
        public Dictionary<string, double> Values { get; set; } = new();
    }

    public interface IMeterStateStore
    {
        /// <summary>the saved state, null when there is none (or it cannot be read)</summary>
        MeterState Load(Guid logicInstance);

        void Save(Guid logicInstance, MeterState state);
    }

    /// <summary>One JSON file per logic instance in persistent/logics/metering, written to a temp file first.</summary>
    public sealed class FileMeterStateStore : IMeterStateStore
    {
        private readonly string _directory;

        public FileMeterStateStore(string directory = null)
        {
            _directory = directory ?? Path.Combine(ServerInfo.GetPersistentDirectory(), "logics", "metering");
        }

        public MeterState Load(Guid logicInstance)
        {
            var file = FileName(logicInstance);
            if (!File.Exists(file))
            {
                return null;
            }
            return JsonSerializer.Deserialize<MeterState>(File.ReadAllText(file));
        }

        public void Save(Guid logicInstance, MeterState state)
        {
            Directory.CreateDirectory(_directory);
            var file = FileName(logicInstance);
            var temp = file + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(state));
            File.Move(temp, file, true);
        }

        private string FileName(Guid logicInstance) => Path.Combine(_directory, logicInstance.ToString("D") + ".json");
    }

    /// <summary>keeps the state in memory only (tests)</summary>
    public sealed class MemoryMeterStateStore : IMeterStateStore
    {
        private readonly ConcurrentDictionary<Guid, string> _states = new();

        public MeterState Load(Guid logicInstance) =>
            _states.TryGetValue(logicInstance, out var json) ? JsonSerializer.Deserialize<MeterState>(json) : null;

        public void Save(Guid logicInstance, MeterState state) => _states[logicInstance] = JsonSerializer.Serialize(state);

        public int Count => _states.Count;
    }
}
