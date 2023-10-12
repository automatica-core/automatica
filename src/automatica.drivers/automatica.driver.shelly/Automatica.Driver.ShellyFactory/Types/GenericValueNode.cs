using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Driver.Shelly.Clients;

namespace Automatica.Driver.ShellyFactory.Types
{
    internal abstract class GenericValueNode<TValueObject> : DriverBase
    {
        protected GenericValueNode(IDriverContext driverContext) : base(driverContext)
        {
        }
        
        internal abstract object GetValueFromShelly(TValueObject value);
    }

    internal class GenericValueNode<T, T2> : GenericValueNode<T2>
    {
        private readonly ShellyDriverDevice _client;
        private readonly Func<T, ShellyClient, Task> _writeValueTask;
        private readonly Func<T2, T> _getValueFromDto;

        public GenericValueNode(IDriverContext driverContext, ShellyDriverDevice client, Func<T, ShellyClient, Task> writeValueTask, Func<T2, T> getValueFromDto) : base(driverContext)
        {
            _client = client;
            _writeValueTask = writeValueTask;
            _getValueFromDto = getValueFromDto;
        }

        internal override object GetValueFromShelly(T2 value)
        {
            return _getValueFromDto(value);
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
