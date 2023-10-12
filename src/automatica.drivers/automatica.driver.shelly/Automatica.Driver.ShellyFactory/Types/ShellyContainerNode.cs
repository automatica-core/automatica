using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Driver.Shelly.Clients;
using Automatica.Driver.Shelly.Dtos;

namespace Automatica.Driver.ShellyFactory.Types
{
    internal abstract class ShellyContainerNode : DriverBase
    {
        public ShellyDriverDevice Client { get; }
        protected ShellyContainerNode(IDriverContext driverContext, ShellyDriverDevice client) : base(driverContext)
        {
            Client = client;
        }

        internal abstract object ReadData(ShellyStatusDto dto);
    }

    internal abstract class ShellyContainerNode<T> : ShellyContainerNode where T : class
    {
        public List<GenericValueNode<T>> ValueNodes { get; }

        public List<ShellyContainerNode> ContainerNodes { get; }

        protected ShellyContainerNode(IDriverContext driverContext, ShellyDriverDevice client) : base(driverContext, client)
        {
            ValueNodes = new List<GenericValueNode<T>>();
            ContainerNodes = new List<ShellyContainerNode>();
        }

        internal abstract T GetCorrespondingDataObject(ShellyStatusDto data);

        internal override object ReadData(ShellyStatusDto dto)
        {
            var dataObj = GetCorrespondingDataObject(dto);

            foreach (var containerNode in ContainerNodes)
            {
                containerNode.ReadData(dto);
            }

            foreach (var valueNode in ValueNodes)
            {
                var value = valueNode.GetValueFromShelly(dataObj);
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
