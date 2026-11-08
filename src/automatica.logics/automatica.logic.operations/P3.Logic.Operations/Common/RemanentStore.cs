using System;
using System.Security.Cryptography;
using System.Text;
using Automatica.Core.Base.IO;

namespace P3.Logic.Operations.Common
{
    /// <summary>
    /// Keeps a text value of a logic instance across restarts: it is dispatched on a remanent dispatchable, the core saves it
    /// in the remanent directory and restores it into the dispatcher before the logics start (<see cref="IDispatcher.GetValue(Guid)"/>).
    /// The id is derived from the rule instance and a name, nothing is linked to it.
    /// </summary>
    internal sealed class RemanentStore : IDispatchable
    {
        private readonly IDispatcher _dispatcher;

        public RemanentStore(IDispatcher dispatcher, Guid ruleInstanceId, string name)
        {
            _dispatcher = dispatcher;
            Id = DeriveId(ruleInstanceId, name);
            Name = name;
        }

        public DispatchableSource Source => DispatchableSource.RuleInstance;
        public DispatchableType Type => DispatchableType.RuleInstance;
        public string Name { get; }
        public Guid Id { get; }
        public bool IsRemanent => true;

        internal static Guid DeriveId(Guid ruleInstanceId, string name)
        {
            var bytes = Encoding.UTF8.GetBytes($"{ruleInstanceId:N}/{name}");
            var hash = MD5.HashData(bytes);
            return new Guid(hash);
        }

        public string Load()
        {
            try
            {
                return _dispatcher.GetValue(Id)?.Value as string;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void Save(string value)
        {
            _dispatcher.DispatchValue(this, value);
        }
    }
}
