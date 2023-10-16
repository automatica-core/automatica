using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Driver.Shelly.Gen1.Dtos;

namespace Automatica.Driver.ShellyFactory.Types.Relay
{

    internal class RelayContainerNode : ShellyContainerNode<bool>
    {
        public int RelayId { get; private set; }

        public RelayContainerNode(IDriverContext driverContext, ShellyDriverDevice client) : base(driverContext, client)
        {
        }

        public override Task<bool> Init(CancellationToken token = new CancellationToken())
        {
            var channelProperty = GetPropertyValue("shelly-relay-channel", null);

            if (channelProperty != null)
            {
                RelayId = Convert.ToInt32(channelProperty);
            }
            return base.Init(token);
        }

        internal override async Task<bool> GetCorrespondingDataObject()
        {
            return await Client.Client.GetRelayState(RelayId, CancellationToken.None);
        }

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            return Task.CompletedTask;
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            return Task.FromResult(false);
        }

        protected override IDriverNode CreateDriverNodeInternal(IDriverContext ctx)
        {
            var type = ctx.NodeInstance.This2NodeTemplateNavigation.Key;

            switch (type)
            {
                case "relay":
                    return new RelayContainerNode(ctx, Client);
                case "relay-state":
                    return new GenericValueNode<bool, bool>(ctx, Client, 
                        async (o, client) =>
                        {
                            await Task.CompletedTask;
                            await Client.Client.SetRelayState(RelayId, o, CancellationToken.None);
                        },
                        async client => await client.GetRelayState(RelayId, CancellationToken.None));
                case "relay-timer":
                    return new GenericValueNode<bool, bool>(ctx, Client,
                        async (o, client) =>
                        {
                            await Task.CompletedTask;
                        },
                        async client => await client.GetHasUpdate(default));
            }

            return null;
        }
    }
}
