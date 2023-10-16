using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;

namespace Automatica.Driver.ShellyFactory.Types
{
    internal abstract class ShellyContainerNode : DriverBase
    {
        public ShellyDriverDevice Client { get; }
        protected ShellyContainerNode(IDriverContext driverContext, ShellyDriverDevice client) : base(driverContext)
        {
            Client = client;
        }

        internal abstract Task<object> ReadData();
    }

    internal abstract class ShellyContainerNode<T> : ShellyContainerNode 
    {
        public List<GenericValueNode<T>> ValueNodes { get; }

        public List<ShellyContainerNode> ContainerNodes { get; }

        protected ShellyContainerNode(IDriverContext driverContext, ShellyDriverDevice client) : base(driverContext, client)
        {
            ValueNodes = new List<GenericValueNode<T>>();
            ContainerNodes = new List<ShellyContainerNode>();
        }

        internal abstract Task<T> GetCorrespondingDataObject();

        internal override async Task<object> ReadData()
        {
            var dataObj = await GetCorrespondingDataObject();

            foreach (var containerNode in ContainerNodes)
            {
                await containerNode.ReadData();
            }

            foreach (var valueNode in ValueNodes)
            {
                var value = await valueNode.GetValueFromShelly(Client.Client);
                valueNode.DispatchRead(value);
            }

            return dataObj;
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
                if(node is GenericValueNode<T> genericValueNode)
                    ValueNodes.Add(genericValueNode);
                else if (node is ShellyContainerNode containerNode)
                    ContainerNodes.Add(containerNode);
            }

            return node;
        }
    }
}
