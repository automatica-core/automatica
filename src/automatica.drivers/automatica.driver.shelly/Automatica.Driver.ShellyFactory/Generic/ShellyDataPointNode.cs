#nullable enable annotations
using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Driver.ShellyFactory.Catalog;
using Microsoft.Extensions.Logging;

namespace Automatica.Driver.ShellyFactory.Generic
{
    /// <summary>
    /// A value of a component (output, power, temperature...). Writes go to the device, the device answers with a notification / the next poll.
    /// </summary>
    internal sealed class ShellyDataPointNode : DriverBase
    {
        private readonly ShellyComponentNode _component;
        private readonly object _lock = new();
        private object? _lastValue;
        private bool _hasValue;

        public ShellyDataPoint DataPoint { get; }

        public ShellyDataPointNode(IDriverContext driverContext, ShellyComponentNode component, ShellyDataPoint dataPoint) : base(driverContext)
        {
            _component = component;
            DataPoint = dataPoint;
        }

        public object? LastValue
        {
            get
            {
                lock (_lock)
                {
                    return _lastValue;
                }
            }
        }

        /// <summary>
        /// Dispatches the value when it differs from the last one (polls repeat unchanged values).
        /// </summary>
        public void DispatchIfChanged(object? value)
        {
            if (value == null)
            {
                return;
            }

            lock (_lock)
            {
                if (_hasValue && Equals(_lastValue, value))
                {
                    return;
                }

                _lastValue = value;
                _hasValue = true;
            }

            DispatchRead(value);
        }

        /// <summary>
        /// Events (single_push...) are dispatched every time, also when the same event comes twice.
        /// </summary>
        public void DispatchEvent(string eventName)
        {
            lock (_lock)
            {
                _lastValue = eventName;
                _hasValue = true;
            }

            DispatchRead(eventName);
        }

        internal void ResetLastValue()
        {
            lock (_lock)
            {
                _hasValue = false;
                _lastValue = null;
            }
        }

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = default)
        {
            if (!DataPoint.IsWriteable)
            {
                DriverContext.Logger.LogWarning($"{FullName}: {DataPoint.Key} is read only");
                return;
            }

            try
            {
                await _component.WriteAsync(DataPoint, value, token);
                if (!DataPoint.IsCommand)
                {
                    await writeContext.DispatchValue(value, token);
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                DriverContext.Logger.LogError($"{FullName}: could not write {value} to {DataPoint.Key}: {e.Message}");
            }
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            var value = LastValue;
            if (value != null)
            {
                DispatchRead(value);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null!;
        }
    }
}
