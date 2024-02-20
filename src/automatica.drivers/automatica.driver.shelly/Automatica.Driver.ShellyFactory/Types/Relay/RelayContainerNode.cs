using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;

namespace Automatica.Driver.ShellyFactory.Types.Relay
{

    internal class RelayContainerNode : ShellyContainerNode
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
                            await Client.Client.SetRelayState(RelayId, o!.Value, CancellationToken.None);
                        },
                        async client => await client.GetRelayState(RelayId, CancellationToken.None),
                        notifyStatusEvent =>
                        {
                            if(notifyStatusEvent.GetSwitch(RelayId) != null)
                                return notifyStatusEvent.GetValueFromSwitch(RelayId, a => a.Output);
                            return null;
                        });
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
