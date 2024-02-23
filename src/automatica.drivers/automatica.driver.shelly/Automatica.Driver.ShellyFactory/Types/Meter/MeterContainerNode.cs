using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;

namespace Automatica.Driver.ShellyFactory.Types.Meter
{
    internal class MeterContainerNode : ShellyContainerNode
    {
        public int RelayId { get; private set; }

        public MeterContainerNode(IDriverContext driverContext, ShellyDriverDevice client) : base(driverContext, client)
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
                case "meter":
                    return new MeterContainerNode(ctx, Client);
                case "meter-power":
                    return new GenericValueNode<double, bool>(ctx, Client,
                        async (o, client) =>
                        {
                            await Task.CompletedTask;
                            return 0;
                        },
                        async client => await client.GetRelayPower(RelayId), @event => @event.GetValueFromSwitch(RelayId, a => a.Power));
                case "meter-overpower":
                    return new GenericValueNode<bool, bool>(ctx, Client,
                        async (o, client) =>
                        {
                            await Task.CompletedTask;
                            return false;
                        }, _ => Task.FromResult(false));
                case "meter-is-valid":

                    return new GenericValueNode<bool, bool>(ctx, Client,
                        async (o, client) =>
                        {
                            await Task.CompletedTask;
                            return false;
                        }, _ => Task.FromResult(true));
                case "meter-timestamp":
                    return new GenericValueNode<long, bool>(ctx, Client,
                        async (o, client) =>
                        {
                            await Task.CompletedTask;
                            return 0;
                        },
                        async client => await client.GetRelayEnergyTimestamp(RelayId));
                case "meter-total":
                    return new GenericValueNode<double, bool>(ctx, Client,
                        async (o, client) =>
                        {
                            await Task.CompletedTask;
                            return 0;
                        },
                        async client => await client.GetRelayEnergy(RelayId));
            }

            return null;
        }
    }
}
