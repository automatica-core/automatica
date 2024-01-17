using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Automatica.Core.Logging;

public class CoreLoggerSettings : ICoreLoggerSettings
{
    private readonly object _lock = new();

    private bool _isInitialized;

    private readonly Dictionary<string, LogLevel> _logLevels = new ();
    private readonly LogLevel _defaultLogLevel;

    public CoreLoggerSettings(IConfiguration? config)
    {
        if (config == null)
        {
            _defaultLogLevel = LogLevel.Information;
        }
        else
        {
            _defaultLogLevel = CoreLogger.Parse(config["server:log_level"]!);
        }

        Init();
    }

    public LogLevel GetLogLevel(string facility)
    {
        lock (_lock)
        {
            if (!_isInitialized)
            {
                Init();
            }

            if (_logLevels.TryGetValue(facility, out var level))
            {
                return level;
            }

            return _defaultLogLevel;
        }

        
    }

    public void Save(IDictionary<string, ICoreLogger> loggers)
    {
        lock (_lock)
        {
            var dic = new Dictionary<string, LogLevel>();

            foreach (var entry in loggers)
            {
                dic.Add(entry.Key, entry.Value.LogLevel);

                _logLevels[entry.Key] = entry.Value.LogLevel;
            }

            var json = JsonConvert.SerializeObject(dic);
            File.WriteAllText("log_levels.json", json);
        }
    }

    private void Init()
    {
        lock (_lock)
        {
            if (File.Exists("log_levels.json"))
            {
                var json = File.ReadAllText("log_levels.json");
                var levels = JsonConvert.DeserializeObject<Dictionary<string, LogLevel>>(json);

                foreach (var (key, value) in levels)
                {
                    _logLevels.Add(key, value);
                }
            }
            _isInitialized = true;
        }
    }
}