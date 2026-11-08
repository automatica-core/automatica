namespace Automatica.Logic.Metering.Meters
{
    /// <summary>
    /// Turns a pulse input into a number of pulses. A bool counts its rising edges (S0 contact on a binary input);
    /// a number is a pulse counter: the increase since the last value counts, the first value and a lower value
    /// (counter reset) are the new starting point.
    /// </summary>
    public sealed class PulseCounter
    {
        private const string CounterKey = "pulseCounter";

        private bool _lastState;
        private double? _lastCounter;

        /// <summary>the pulses this value adds (0 for none)</summary>
        public double Pulses(object value)
        {
            switch (value)
            {
                case null:
                    return 0;
                case bool b:
                {
                    var pulse = b && !_lastState;
                    _lastState = b;
                    return pulse ? 1 : 0;
                }
                default:
                {
                    if (EnergyMonitor.EnergyMonitorRule.ToDouble(value) is not { } counter)
                    {
                        return 0;
                    }
                    var last = _lastCounter;
                    _lastCounter = counter;
                    if (last == null || counter <= last.Value)
                    {
                        return 0;
                    }
                    return counter - last.Value;
                }
            }
        }

        public void Save(System.Collections.Generic.IDictionary<string, double> values, string prefix = "")
        {
            if (_lastCounter.HasValue)
            {
                values[prefix + CounterKey] = _lastCounter.Value;
            }
        }

        public void Load(System.Collections.Generic.IReadOnlyDictionary<string, double> values, string prefix = "")
        {
            _lastCounter = values.TryGetValue(prefix + CounterKey, out var counter) ? counter : null;
            _lastState = false;
        }
    }
}
