using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.Driver;
using P3.Driver.EnOcean.Data.Packets;
using P3.Driver.EnOcean.DriverFactory.Driver.Data;
using P3.Driver.EnOcean.DriverFactory.Driver.Learned;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace P3.Driver.EnOcean.DriverFactory.Driver
{
    public class EnOceanTypeNode : EnOceanBaseNode<EnOceanDataNode>
    {
        private string _serial;
        private IReadOnlyList<EepCondition> _conditions = Array.Empty<EepCondition>();

        public EnOceanTypeNode(IDriverContext driverContext, ITeachInManager teachInManager) : base(driverContext, teachInManager)
        {
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            _serial = GetProperty("enocean-serialnumber").ValueString;
            _conditions = EnOceanEepMetadata.GetConditions(DriverContext.NodeInstance.This2NodeTemplate ?? Guid.Empty);
            TeachInManager.SetTeachedIn(_serial);
            return base.Init(token);
        }

        protected override bool CreateCustomLogger()
        {
            return true;
        }

        public override void TelegramReceived(RadioErp1Packet telegram)
        {
            if (telegram.SenderIdString == _serial)
            {
                DriverContext.Logger.LogDebug($"Telegram received for {_serial}");

                // 1BS/4BS teach-in telegrams (LRN bit cleared) carry EEP/manufacturer info instead of values,
                // so only the LRN node gets them. RPS has no LRN bit and is always decoded.
                var isTeachIn = RadioErp1Packet.IsLrnTeachIn(telegram);
                if (isTeachIn)
                {
                    DriverContext.Logger.LogDebug($"Teach-in telegram received for {_serial}, values are not dispatched");
                }
                else if (!EnOceanEepMetadata.Matches(_conditions, telegram.Data.Span))
                {
                    // a multi-message profile (e.g. D2-01 CMD 0x4 status vs. CMD 0x7 measurement): the telegram
                    // belongs to another case of the profile and would be decoded with the wrong layout
                    DriverContext.Logger.LogDebug($"Telegram for {_serial} belongs to another case of the profile, ignored");
                    return;
                }

                foreach (var child in ChildrenT)
                {
                    if (isTeachIn && !(child is EnOceanLrnBit))
                    {
                        continue;
                    }
                    child.TelegramReceived(telegram);
                }
            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            if (ctx.NodeInstance.This2NodeTemplateNavigation.Key == "enocean-shortcut-LRN" || ctx.NodeInstance.This2NodeTemplateNavigation.Key == "enocean-shortcut-LRNB")
            {
                return new EnOceanLrnBit(ctx, TeachInManager);
            }
            // D5-00-01 contact; CO in D2-14-31 is a gas concentration and decoded generically
            if (ctx.NodeInstance.This2NodeTemplateNavigation.Key == "enocean-shortcut-CO" && IsSingleBit(ctx.NodeInstance))
            {
                return new EnOceanCoBit(ctx, TeachInManager);
            }
            if (ctx.NodeInstance.This2NodeTemplateNavigation.Key == "enocean-shortcut-WIN")
            {
                return new EnOceanWinData(ctx, TeachInManager);
            }
            return new EnOceanGenericData(ctx, TeachInManager);
        }

        private static bool IsSingleBit(NodeInstance node)
        {
            var length = node.PropertyInstance.FirstOrDefault(p => p.This2PropertyTemplateNavigation?.Key == "enocean-length");
            return length?.Value != null && Convert.ToInt32(length.Value) == 1;
        }
    }
}
