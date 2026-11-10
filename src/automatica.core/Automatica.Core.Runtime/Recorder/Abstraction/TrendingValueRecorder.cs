using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Runtime.Recorder.Base;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Recorder.Abstraction
{
    internal class TrendingValueRecorder : IDataRecorder
    {
        private readonly BaseDataRecorderWriter _recorderWriter;
        private readonly List<double> _values = new List<double>();

        private double? _lastValue;

        private readonly Timer _timer = new Timer();
        private readonly object _lock = new object();

        private string _lastSource = "";
        public TrendingValueRecorder(NodeInstance instance, BaseDataRecorderWriter recorderWriter)
        {
            Instance = instance;
            _recorderWriter = recorderWriter;
        }

        public Task Start()
        {
            if (!Instance.Trending)
            {
                return Task.CompletedTask;
            }

            lock (_lock)
            {
                if (_timerStarted)
                {
                    return Task.CompletedTask;
                }
                _timerStarted = true;
            }

            if (Instance.TrendingInterval == 0)
            {
                Instance.TrendingInterval = 1;
            }
            _timer.Interval = Instance.TrendingInterval * 1000;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
            return Task.CompletedTask;
        }

        private bool _timerStarted;

        private void _timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            RecordValue();
        }

        internal void RecordValue()
        {
            lock (_lock)
            {
                if (!_lastValue.HasValue)
                {
                    return;
                }
                _recorderWriter.SaveValue(Instance, _lastValue.Value, _lastSource).ConfigureAwait(false);
                _lastValue = null;
                _values.Clear();
            }
        }

        /// <summary>
        /// On/off values are recorded as 1 and 0: bool.ToString() is "True", which no number parser accepts, so switches
        /// and states never had a history.
        /// </summary>
        internal static bool TryGetNumber(object value, out double number)
        {
            switch (value)
            {
                case bool b:
                    number = b ? 1 : 0;
                    return true;
                case string text when bool.TryParse(text, out var parsed):
                    number = parsed ? 1 : 0;
                    return true;
                case IConvertible convertible and not string:
                    try
                    {
                        number = convertible.ToDouble(CultureInfo.InvariantCulture);
                        return true;
                    }
                    catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
                    {
                        break;
                    }
            }

            return double.TryParse(value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number);
        }

        public void ValueChanged(DispatchValue value, string source)
        {
            lock (_lock)
            {
                try
                {
                    if (value == null || value.Value == null)
                    {
                        return;
                    }

                    _lastSource = source;
                    if (TryGetNumber(value.Value, out var dblValue))
                    {
                        switch (Instance.TrendingType)
                        {
                            case EF.Models.Trendings.TrendingTypes.Average:
                                _values.Add(dblValue);

                                double val = 0;
                                foreach (var v in _values)
                                {
                                    val += v;
                                }

                                _lastValue = val / _values.Count;

                                break;
                            case EF.Models.Trendings.TrendingTypes.Raw:
                                _lastValue = dblValue;
                                break;
                            case EF.Models.Trendings.TrendingTypes.Max:
                                _lastValue = _lastValue == null ? dblValue : Math.Max(dblValue, _lastValue.Value);
                                break;
                            case EF.Models.Trendings.TrendingTypes.Min:
                                _lastValue = _lastValue == null ? dblValue : Math.Min(dblValue, _lastValue.Value);
                                break;
                            case EF.Models.Trendings.TrendingTypes.OnChange:
                                if (_lastValue == null)
                                {
                                    _lastValue = dblValue;
                                    _recorderWriter.SaveValue(Instance, _lastValue.Value, _lastSource).ConfigureAwait(false); ;
                                }
                                else
                                {
                                    if (dblValue != _lastValue.Value)
                                    {
                                        _lastValue = dblValue;
                                        _recorderWriter.SaveValue(Instance, _lastValue.Value, _lastSource).ConfigureAwait(false); ;
                                    }
                                }

                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    _recorderWriter.Logger.LogError(e, "Error while recording value");
                }
            }
        }


        public Task Stop()
        {
            lock (_lock)
            {
                _timerStarted = false;
            }
            _timer.Elapsed -= _timer_Elapsed;
            _timer.Stop();
            return Task.CompletedTask;
        }

        public NodeInstance Instance { get; }
    }
}
