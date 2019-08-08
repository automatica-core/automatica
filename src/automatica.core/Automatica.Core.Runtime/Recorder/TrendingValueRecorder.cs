using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
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
            lock (_lock)
            {
                if(!_lastValue.HasValue)
                {
                    return;
                }
                _recorderWriter.SaveValue(Instance, _lastValue.Value, _lastSource);
                _lastValue = null;
                _values.Clear();
            }
        }

        public void ValueChanged(object value, string source)
        {
            lock (_lock)
            {
                _lastSource = source;
                if (value is double dblValue)
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
                            if (_lastValue == null)
                            {
                                _lastValue = dblValue;
                            }
                            else
                            {
                                _lastValue = Math.Max(dblValue, _lastValue.Value);
                            }
                            break;
                        case EF.Models.Trendings.TrendingTypes.Min:
                            if (_lastValue == null)
                            {
                                _lastValue = dblValue;
                            }
                            else
                            {
                                _lastValue = Math.Min(dblValue, _lastValue.Value);
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
