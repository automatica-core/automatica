using System;
using System.Collections.Concurrent;
using System.IO;
using Automatica.Core.Base.Common;
using Newtonsoft.Json;

namespace P3.Logic.Surveillance.Security
{
    /// <summary>
    /// Keeps the state of a security logic (armed, alarm level, log, access credentials...) across restarts of the server.
    /// </summary>
    public interface ISecurityStateStore
    {
        T Load<T>(Guid logicId) where T : class;
        void Save<T>(Guid logicId, T state) where T : class;
        void Delete(Guid logicId);
    }

    /// <summary>
    /// One JSON file per logic in the persistent directory of the server (&lt;persistent&gt;/logic-state/&lt;id&gt;.json),
    /// written to a temporary file first and then moved over the old one, so a crash never leaves half a file.
    /// The files may hold hashes of access codes, never the codes.
    /// </summary>
    public sealed class FileSecurityStateStore : ISecurityStateStore
    {
        private static readonly JsonSerializerSettings Settings = new()
        {
            TypeNameHandling = TypeNameHandling.None,
            NullValueHandling = NullValueHandling.Ignore
        };

        private readonly string _directory;
        private readonly object _lock = new();

        public FileSecurityStateStore(string directory)
        {
            _directory = directory;
        }

        private static readonly Lazy<FileSecurityStateStore> DefaultStore =
            new(() => new FileSecurityStateStore(Path.Combine(ServerInfo.GetPersistentDirectory(), "logic-state")));

        public static FileSecurityStateStore Default => DefaultStore.Value;

        private string PathOf(Guid logicId) => Path.Combine(_directory, $"{logicId}.json");

        public T Load<T>(Guid logicId) where T : class
        {
            lock (_lock)
            {
                var path = PathOf(logicId);
                if (!File.Exists(path))
                {
                    return null;
                }

                try
                {
                    return JsonConvert.DeserializeObject<T>(File.ReadAllText(path), Settings);
                }
                catch (JsonException)
                {
                    // unreadable state: start fresh rather than not starting the logic at all
                    return null;
                }
            }
        }

        public void Save<T>(Guid logicId, T state) where T : class
        {
            lock (_lock)
            {
                Directory.CreateDirectory(_directory);
                var path = PathOf(logicId);
                var tmp = path + ".tmp";
                File.WriteAllText(tmp, JsonConvert.SerializeObject(state, Formatting.Indented, Settings));
                File.Move(tmp, path, true);
            }
        }

        public void Delete(Guid logicId)
        {
            lock (_lock)
            {
                var path = PathOf(logicId);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }

    /// <summary>
    /// For tests: keeps the serialized state in memory, so a "restart" (new logic instance) sees what the old one saved.
    /// </summary>
    public sealed class MemorySecurityStateStore : ISecurityStateStore
    {
        private readonly ConcurrentDictionary<Guid, string> _states = new();

        public T Load<T>(Guid logicId) where T : class
        {
            return _states.TryGetValue(logicId, out var json) ? JsonConvert.DeserializeObject<T>(json) : null;
        }

        public void Save<T>(Guid logicId, T state) where T : class
        {
            _states[logicId] = JsonConvert.SerializeObject(state);
        }

        public void Delete(Guid logicId)
        {
            _states.TryRemove(logicId, out _);
        }

        public string Raw(Guid logicId) => _states.TryGetValue(logicId, out var json) ? json : null;
    }
}
