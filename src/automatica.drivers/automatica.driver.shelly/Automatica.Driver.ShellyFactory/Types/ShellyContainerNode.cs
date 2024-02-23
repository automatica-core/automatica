using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Driver.Shelly.Gen2.Models;

namespace Automatica.Driver.ShellyFactory.Types
{

    internal abstract class ShellyContainerNode : DriverBase
    {
        public List<GenericValueNode> ValueNodes { get; }

        public List<ShellyContainerNode> ContainerNodes { get; }

        public ShellyDriverDevice Client { get; }

        protected ShellyContainerNode(IDriverContext driverContext, ShellyDriverDevice client) : base(driverContext)
        {
            Client = client;
            ValueNodes = new List<GenericValueNode>();
            ContainerNodes = new List<ShellyContainerNode>();
        }


        internal async Task ReadData()
        {
            foreach (var containerNode in ContainerNodes)
            {
                await containerNode.ReadData();
            }

            foreach (var valueNode in ValueNodes)
            {
                var value = await valueNode.GetValueFromShelly(Client.Client);
                valueNode.DispatchRead(value);
            }

        }

        internal async Task FromStatusUpdate(NotifyStatusEvent statusEvent)
        {
            foreach (var containerNode in ContainerNodes)
            {
                await containerNode.FromStatusUpdate(statusEvent);
            }

            foreach (var valueNode in ValueNodes)
            {
                var value = await valueNode.FromStatusUpdate(statusEvent);
                if(value != null)
                    valueNode.DispatchRead(value);
            }
        }

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
           return Task.CompletedTask;
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            return Task.FromResult(false);
        }

        protected abstract IDriverNode CreateDriverNodeInternal(IDriverContext ctx);

        public sealed override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var node = CreateDriverNodeInternal(ctx);

            if (node != null)
            {
                if(node is GenericValueNode genericValueNode)
                    ValueNodes.Add(genericValueNode);
                else if (node is ShellyContainerNode containerNode)
                    ContainerNodes.Add(containerNode);
            }

            return node;
        }
    }
}
