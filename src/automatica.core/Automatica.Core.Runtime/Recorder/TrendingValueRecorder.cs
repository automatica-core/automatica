using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Runtime.Recorder
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

            if(Instance.TrendingInterval == 0)
            {
                Instance.TrendingInterval = 1;
            }
            _timer.Interval = Instance.TrendingInterval * 1000;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
            return Task.CompletedTask;
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
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
                _recorderWriter.SaveValue(Instance, _lastValue.Value, _lastSource);
                _lastValue = null;
                _values.Clear();
            }
        }

        public void ValueChanged(DispatchValue value, string source)
        {
            lock (_lock)
            {
                if (value == null)
                {
                    return;
                }

                _lastSource = source;
                if (double.TryParse(value.Value.ToString(), CultureInfo.InvariantCulture, out var dblValue))
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
                                _recorderWriter.SaveValue(Instance, _lastValue.Value, _lastSource);
                            }
                            else
                            {
                                if (dblValue != _lastValue.Value)
                                {
                                    _lastValue = dblValue;
                                    _recorderWriter.SaveValue(Instance, _lastValue.Value, _lastSource);
                                }
                            }
                            break;  
                    }
                }
            }
        }


        public Task Stop()
        {
            _timer.Elapsed -= _timer_Elapsed;
            _timer.Stop();
            return Task.CompletedTask;
        }

        public NodeInstance Instance { get; }
    }
}
