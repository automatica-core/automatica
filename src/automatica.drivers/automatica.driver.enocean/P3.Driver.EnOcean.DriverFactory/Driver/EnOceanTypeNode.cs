using Automatica.Core.Driver;
using P3.Driver.EnOcean.Data.Packets;
using P3.Driver.EnOcean.DriverFactory.Driver.Data;
using P3.Driver.EnOcean.DriverFactory.Driver.Learned;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.EnOcean.DriverFactory.Driver
{
    public class EnOceanTypeNode : EnOceanBaseNode<EnOceanDataNode>
    {
        private string _serial;

        public EnOceanTypeNode(IDriverContext driverContext, ITeachInManager teachInManager) : base(driverContext, teachInManager)
        {
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            _serial = GetProperty("enocean-serialnumber").ValueString;
            TeachInManager.SetTeachedIn(_serial);
            return base.Init(token);
        }

        public override void TelegramReceived(RadioErp1Packet telegram)
        {
            if (telegram.SenderIdString == _serial)
            {
                base.TelegramReceived(telegram);
            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            if (ctx.NodeInstance.This2NodeTemplateNavigation.Key == "enocean-shortcut-LRN" || ctx.NodeInstance.This2NodeTemplateNavigation.Key == "enocean-shortcut-LRNB")
            {
                return new EnOceanLrnBit(ctx, TeachInManager);
            }
            if (ctx.NodeInstance.This2NodeTemplateNavigation.Key == "enocean-shortcut-CO")
            {
                return new EnOceanCoBit(ctx, TeachInManager);
            }

            return new EnOceanGenericData(ctx, TeachInManager);
        }
    }
}
