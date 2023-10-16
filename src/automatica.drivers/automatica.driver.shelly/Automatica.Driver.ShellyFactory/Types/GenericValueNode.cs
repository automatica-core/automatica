using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Driver.Shelly.Common;

namespace Automatica.Driver.ShellyFactory.Types
{
    internal abstract class GenericValueNode<TValueObject> : DriverBase
    {
        protected GenericValueNode(IDriverContext driverContext) : base(driverContext)
        {
        }
        
        internal abstract Task<object> GetValueFromShelly(IShellyClient shellyClient);
    }

    internal class GenericValueNode<T, T2> : GenericValueNode<T2>
    {
        private readonly ShellyDriverDevice _client;
        private readonly Func<T, IShellyClient, Task> _writeValueTask;
        private readonly Func<IShellyClient, Task<T>> _getValueTask;

        public GenericValueNode(IDriverContext driverContext, ShellyDriverDevice client, Func<T, IShellyClient, Task> writeValueTask, Func<IShellyClient, Task<T>> getValueTas) : base(driverContext)
        {
            _client = client;
            _writeValueTask = writeValueTask;
            _getValueTask = getValueTas;
        }

        internal override async Task<object> GetValueFromShelly(IShellyClient shellyClient)
        {
            return await _getValueTask(shellyClient);
        }

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            await _writeValueTask.Invoke((T)value, _client.Client);
            await writeContext.DispatchValue(value, token);
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            await Parent.Read(token); //delegate to parent, we need our parent to give us our base value
            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
