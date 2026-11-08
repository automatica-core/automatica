using System;
using System.Security.Cryptography;
using System.Text;
using Automatica.Core.Base.IO;

namespace P3.Logic.Time
{
    /// <summary>
    /// Keeps a string value of a logic across restarts without a core API: the value is dispatched as a remanent
    /// <see cref="IDispatchable"/>, the dispatcher saves it in the remanent directory and restores it before the logics start
    /// (<see cref="IDispatcher.Init"/>), so <see cref="Load"/> finds it in <c>Start</c>.
    /// The id is derived from an interface id of the logic instance, it is never subscribed by links.
    /// </summary>
    internal sealed class RemanentValue
    {
        private readonly IDispatcher _dispatcher;
        private readonly Dispatchable _dispatchable;

        public RemanentValue(IDispatcher dispatcher, Guid owner, string purpose)
        {
            _dispatcher = dispatcher;
            _dispatchable = new Dispatchable(DeriveId(owner, purpose), purpose);
        }

        public Guid Id => _dispatchable.Id;

        public string Load()
        {
            var value = _dispatcher.GetValue(DispatchableType.Control, _dispatchable.Id) ?? _dispatcher.GetValue(_dispatchable.Id);
            return value?.Value?.ToString();
        }

        public void Save(string value)
        {
            _dispatcher.DispatchValue(_dispatchable, value);
        }

        internal static Guid DeriveId(Guid owner, string purpose)
        {
            var bytes = owner.ToByteArray();
            var salt = Encoding.UTF8.GetBytes("P3.Logic.Time.Remanent." + purpose);
            var buffer = new byte[bytes.Length + salt.Length];
            Buffer.BlockCopy(bytes, 0, buffer, 0, bytes.Length);
            Buffer.BlockCopy(salt, 0, buffer, bytes.Length, salt.Length);
            var hash = MD5.HashData(buffer);
            return new Guid(hash);
        }

        private sealed class Dispatchable : IDispatchable
        {
            public Dispatchable(Guid id, string name)
            {
                Id = id;
                Name = name;
            }

            public string Name { get; }
            public Guid Id { get; }
            public DispatchableType Type => DispatchableType.Control;
            public DispatchableSource Source => DispatchableSource.RuleInstance;
            public bool IsRemanent => true;
        }
    }
}
