using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Driver.Shelly.Clients;
using Automatica.Driver.Shelly.Dtos;

namespace Automatica.Driver.ShellyFactory.Types.Relay
{

    internal class RelayContainerNode : ShellyContainerNode<RelayDto>
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

        internal override RelayDto GetCorrespondingDataObject(ShellyStatusDto data)
        {
            if (data.Relays.Count >= RelayId + 1)
            {
                return data.Relays[RelayId];
            }

            return null;
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
                    return new GenericValueNode<bool, RelayDto>(ctx, Client, 
                        async (o, client) =>
                        {
                            await Task.CompletedTask;
                            await Client.Client.SetStatus(RelayId, o, CancellationToken.None);
                        },
                        dto => dto.IsOn);
                case "relay-timer":
                    return new GenericValueNode<bool, RelayDto>(ctx, Client,
                        async (o, client) =>
                        {
                            await Task.CompletedTask;
                        },
                        dto => dto.HasTimer);
            }

            return null;
        }
    }
}
