
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Core.Base.IO
{
    public enum DispatchValueSource
    {
        Read,
        Write,
        User
    }
    public class DispatchValue
    {


        public DispatchValue(Guid id, DispatchableType dispatchableType, object value, DateTime timestamp, DispatchValueSource valueSource)
        {
            Id = id;
            Type = dispatchableType;
            Value = value;
            Timestamp = timestamp;
            ValueSource = valueSource;
        }

        public DispatchableType Type { get; set; }
        public Guid Id { get; set; }
        public object Value { get; set; }
        public DateTime Timestamp { get; set; }
        public DispatchValueSource ValueSource { get; }

        public override string ToString()
        {
            return $"{Type}:{Timestamp}:{Value} - {ValueSource}";
        }
    }

    public interface IDispatcher
    {
        Task Init(CancellationToken token = default);

        Task DispatchValue(IDispatchable self, object value, DispatchValueSource valueSource = DispatchValueSource.Read);
        Task DispatchValue(IDispatchable self, DispatchValue value);

        Task UnRegisterDispatch(DispatchableType type, Guid id);
        Task RegisterDispatch(DispatchableType type, Guid id, Action<IDispatchable, DispatchValue> callback);
        Task RegisterDispatch(Guid id, Action<IDispatchable, DispatchValue> callback);
        Task UnRegisterDispatch(Guid id, Action<IDispatchable, DispatchValue> callback);

        IDictionary<Guid, DispatchValue> GetValues();

        IDictionary<Guid, DispatchValue> GetValues(DispatchableType type);

        DispatchValue GetValue(Guid id);
        DispatchValue GetValue(DispatchableType type, Guid id);

        Task ClearRegistrations();
        Task ClearValues();
    }
}
