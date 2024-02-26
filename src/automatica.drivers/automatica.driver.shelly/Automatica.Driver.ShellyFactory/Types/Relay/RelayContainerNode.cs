using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;

namespace Automatica.Driver.ShellyFactory.Types.Relay
{

    internal class RelayContainerNode : ShellyContainerNode
    {
        public int RelayId { get; private set; }
        public int ResetTimer { get; private set; }

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

            var resetProperty = GetPropertyValue("shelly-relay-flip-back-timer", 0);

            if (resetProperty != null)
            {
                ResetTimer = Convert.ToInt32(resetProperty);
            }
            return base.Init(token);
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
                    return new GenericValueNode<bool?, bool?>(ctx, Client, 
                        async (o, client) =>
                        {
                            await Task.CompletedTask;
                            await Client.Client.SetRelayState(RelayId, o!.Value, ResetTimer,CancellationToken.None);
                            var relayState = await Client.Client.GetRelayState(RelayId, CancellationToken.None);
                            return relayState;
                        },
                        async client => await client.GetRelayState(RelayId, CancellationToken.None),
                        async (client, notifyStatusEvent) =>
                        {
                            return await client.GetRelayState(RelayId, CancellationToken.None);
                        });
                case "relay-timer":
                    return new GenericValueNode<bool, bool>(ctx, Client,
                        async (o, client) =>
                        {
                            await Task.CompletedTask;
                            return false;
                        },
                        async client => await client.GetHasUpdate(default));
            }

            return null;
        }
    }
}
