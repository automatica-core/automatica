
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

        public DispatchValue()
        {
            
        }
        public DispatchValue(Guid id, DispatchableType dispatchableType, object value, DateTime timestamp, DispatchValueSource valueSource)
        {
            Id = id;
            Type = dispatchableType;
            Value = value;
            Timestamp = timestamp;
            ValueSource = valueSource;
        }

        public DispatchValue(DispatchValue copy)
        {
            Id = copy.Id;
            Type = copy.Type;
            Value = copy.Value;
            Timestamp = copy.Timestamp;
            ValueSource = copy.ValueSource;
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

        /// <summary>
        /// Removes all registrations of <paramref name="id"/>, including the ones of other subscribers (links, trendings,...).
        /// Prefer <see cref="UnRegisterDispatch(DispatchableType, Guid, Action{IDispatchable, DispatchValue})"/> or the token returned by <see cref="Subscribe"/>.
        /// </summary>
        Task UnRegisterDispatch(DispatchableType type, Guid id);

        /// <summary>
        /// Removes exactly the registration that was added with <see cref="RegisterDispatch"/> for this <paramref name="callback"/> instance.
        /// </summary>
        Task UnRegisterDispatch(DispatchableType type, Guid id, Action<IDispatchable, DispatchValue> callback)
        {
            // fallback for implementations that only know the remove-all overload
            return UnRegisterDispatch(type, id);
        }

        Task RegisterDispatch(DispatchableType type, Guid id, Action<IDispatchable, DispatchValue> callback);

        /// <summary>
        /// Registers an async callback for values of <paramref name="id"/>. The callbacks of one subscription are invoked one after
        /// another in dispatch order, exceptions are caught and logged by the dispatcher.
        /// </summary>
        /// <returns>A token, disposing it removes exactly this registration</returns>
        async Task<IDisposable> Subscribe(DispatchableType type, Guid id, Func<IDispatchable, DispatchValue, Task> callback)
        {
            // fallback for implementations that predate subscriptions
            Action<IDispatchable, DispatchValue> action = (dispatchable, value) => { _ = callback(dispatchable, value); };
            await RegisterDispatch(type, id, action);
            return new DispatchSubscriptionToken(() => UnRegisterDispatch(type, id, action));
        }

        IDictionary<Guid, DispatchValue> GetValues();

        IDictionary<Guid, DispatchValue> GetValues(DispatchableType type);

        DispatchValue GetValue(Guid id);
        DispatchValue GetValue(DispatchableType type, Guid id);

        Task ClearRegistrations();
        Task ClearValues();
    }
}
