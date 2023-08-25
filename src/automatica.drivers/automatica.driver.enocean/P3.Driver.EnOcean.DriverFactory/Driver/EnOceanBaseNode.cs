using Automatica.Core.Driver;
using P3.Driver.EnOcean.Data.Packets;
using P3.Driver.EnOcean.DriverFactory.Driver.Learned;
using System.Threading.Tasks;
using System.Threading;

namespace P3.Driver.EnOcean.DriverFactory.Driver
{
    public interface IEnOceanBaseNode : IDriverNode
    {
        void TelegramReceived(RadioErp1Packet telegram);
    }

    public abstract class EnOceanBaseNode<T> : DriverBaseT<T>, IEnOceanBaseNode where T: IEnOceanBaseNode
    {
        protected readonly ITeachInManager TeachInManager;

        protected EnOceanBaseNode(IDriverContext driverContext, ITeachInManager teachInManager) : base(driverContext)
        {
            TeachInManager = teachInManager;
        }

        public virtual void TelegramReceived(RadioErp1Packet telegram)
        {
            foreach (var child in ChildrenT)
            {
                child.TelegramReceived(telegram);
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
    }
}
