using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;

namespace Automatica.Driver.ShellyFactory.Types.Roller
{
    internal class RollerContainerNode : ShellyContainerNode<object>
    {
        public RollerContainerNode(IDriverContext driverContext, ShellyDriverDevice client) : base(driverContext, client)
        {
        }


        internal override Task<object> GetCorrespondingDataObject()
        {
            throw new System.NotImplementedException();
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
                case "roller":
                    return new RollerContainerNode(ctx, Client);
                case "roller-position":
                    break;
                case "roller-state":
                    break;
                case "roller-stop-reason":
                    break;
                case "roller-last-direction":
                    break;
            }

            return null;
        }
    }
}
