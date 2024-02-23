 using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Driver.Shelly.Common;
using Automatica.Driver.Shelly.Gen2.Models;

namespace Automatica.Driver.ShellyFactory.Types
{
    internal abstract class GenericValueNode : DriverBase
    {
        protected GenericValueNode(IDriverContext driverContext) : base(driverContext)
        {
        }
        internal abstract Task<object> GetValueFromShelly(IShellyClient shellyClient);
        internal abstract Task<object> FromStatusUpdate(NotifyStatusEvent statusEvent);
    }

    internal abstract class GenericValueNode<TValueObject> : GenericValueNode
    {
        protected GenericValueNode(IDriverContext driverContext) : base(driverContext)
        {
        }
        
    }

    internal class GenericValueNode<T, T2> : GenericValueNode<T2>
    {
        private readonly ShellyDriverDevice _client;
        private readonly Func<T, IShellyClient, Task<T>> _writeValueTask;
        private readonly Func<IShellyClient, Task<T>> _getValueTask;
        private readonly Func<NotifyStatusEvent, T> _fromStatusUpdate;

        public GenericValueNode(IDriverContext driverContext, 
            ShellyDriverDevice client, 
            Func<T, IShellyClient, Task<T>> writeValueTask,
            Func<IShellyClient, Task<T>> getValueTask, 
            Func<NotifyStatusEvent, T> fromStatusUpdate = null) : base(driverContext)
        {
            _client = client;
            _writeValueTask = writeValueTask;
            _getValueTask = getValueTask;
            _fromStatusUpdate = fromStatusUpdate;
        }

        internal override async Task<object> GetValueFromShelly(IShellyClient shellyClient)
        {
            return await _getValueTask(shellyClient);
        }

        internal override async Task<object> FromStatusUpdate(NotifyStatusEvent statusEvent)
        {
            await Task.CompletedTask;
            if (_fromStatusUpdate != null)
            {
                try
                {
                    return _fromStatusUpdate(statusEvent);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            var result = await _writeValueTask.Invoke((T)value, _client.Client);
            await writeContext.DispatchValue(value, token);

            DispatchRead(result);
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
