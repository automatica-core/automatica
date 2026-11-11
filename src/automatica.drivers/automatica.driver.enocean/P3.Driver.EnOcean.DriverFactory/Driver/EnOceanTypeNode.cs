using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Automatica.Core.EF.Exceptions;
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

        /// <summary>
        /// Manufacturer id of the teach-in telegrams Automatica sends: 0x7FF = multi user manufacturer id.
        /// </summary>
        internal const int ManufacturerId = 0x7FF;

        /// <summary>
        /// Set if the type is a command Automatica sends (D2-01 actuator set output, D2-05 go to position, A5-38-08).
        /// </summary>
        internal EepOutgoing Outgoing { get; private set; }

        /// <summary>
        /// Offset of the sender id to the id base of the stick, the actuator learns id base + offset.
        /// </summary>
        internal byte SenderOffset { get; private set; }

        public EnOceanTypeNode(IDriverContext driverContext, ITeachInManager teachInManager) : base(driverContext, teachInManager)
        {
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            _serial = GetProperty("enocean-serialnumber").ValueString;
            _conditions = EnOceanEepMetadata.GetConditions(DriverContext.NodeInstance.This2NodeTemplate ?? Guid.Empty);
            Outgoing = EnOceanEepMetadata.GetOutgoing(DriverContext.NodeInstance.This2NodeTemplate ?? Guid.Empty);
            SenderOffset = ReadSenderOffset();
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
            var node = CreateDataNode(ctx);
            node.Device = this;
            return node;
        }

        private EnOceanDataNode CreateDataNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation.Key;
            if (key == "enocean-teach-in")
            {
                return new EnOceanTeachIn(ctx, TeachInManager);
            }
            if (key == "enocean-shortcut-LRN" || key == "enocean-shortcut-LRNB")
            {
                return new EnOceanLrnBit(ctx, TeachInManager);
            }
            // D5-00-01 contact; CO in D2-14-31 is a gas concentration and decoded generically
            if (key == "enocean-shortcut-CO" && IsSingleBit(ctx.NodeInstance))
            {
                return new EnOceanCoBit(ctx, TeachInManager);
            }
            if (key == "enocean-shortcut-WIN")
            {
                return new EnOceanWinData(ctx, TeachInManager);
            }
            return new EnOceanGenericData(ctx, TeachInManager);
        }

        private byte ReadSenderOffset()
        {
            long? configured = null;
            try
            {
                var property = GetProperty("enocean-sender-offset");
                if (property?.Value != null)
                {
                    configured = Convert.ToInt64(property.Value, CultureInfo.InvariantCulture);
                }
            }
            catch (PropertyNotFoundException)
            {
                // node created by a driver version without write support
            }
            catch (FormatException)
            {
                // empty or invalid value, use the derived offset
            }

            if (configured >= 0 && configured <= 127)
            {
                return (byte)configured.Value;
            }
            if (configured > 127)
            {
                DriverContext.Logger.LogWarning($"Sender offset {configured} of {_serial} is out of range 0..127, using the derived offset");
            }

            return DerivedSenderOffset(_serial);
        }

        /// <summary>
        /// 1..127, stable per device id, so actuators get different sender ids without configuration.
        /// </summary>
        internal static byte DerivedSenderOffset(string serial)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var c in (serial ?? "").ToUpperInvariant())
                {
                    hash = (hash ^ c) * 16777619u;
                }

                return (byte)(1 + hash % 127);
            }
        }

        /// <summary>
        /// Sends the command of this case: the case defaults, the last written values of the other data points and
        /// <paramref name="raw"/> for <paramref name="written"/>.
        /// </summary>
        internal async Task<bool> SendCommand(EnOceanDataNode written, long raw)
        {
            if (Outgoing == null)
            {
                DriverContext.Logger.LogWarning($"{_serial}: the profile case is not a command, nothing sent");
                return false;
            }

            var data = new byte[Outgoing.Size];
            foreach (var value in Outgoing.Defaults)
            {
                EnOceanEepMetadata.SetBits(data, value.Offset, value.Length, value.Value);
            }

            foreach (var child in ChildrenT)
            {
                if (!ReferenceEquals(child, written) && child.WrittenRaw.HasValue)
                {
                    EnOceanEepMetadata.SetBits(data, child.BitOffset, child.FieldLength, child.WrittenRaw.Value);
                }
            }
            EnOceanEepMetadata.SetBits(data, written.BitOffset, written.FieldLength, raw);

            // the case constants (CMD, COM) and the LRN bit of a data telegram are not values a data point may change
            foreach (var condition in _conditions)
            {
                EnOceanEepMetadata.SetBits(data, condition.Offset, condition.Length, condition.Value);
            }
            if (Outgoing.Rorg == (byte)Rorg.FourBs)
            {
                EnOceanEepMetadata.SetBits(data, 28, 1, 1);
            }

            return await Send((Rorg)Outgoing.Rorg, data);
        }

        /// <summary>
        /// Sends the teach-in telegram of the profile: 4BS teach-in with EEP for A5, UTE teach-in query for VLD.
        /// </summary>
        internal async Task<bool> SendTeachIn()
        {
            if (Outgoing == null)
            {
                DriverContext.Logger.LogWarning($"{_serial}: the profile case is not a command, no teach-in sent");
                return false;
            }

            return Outgoing.Rorg == (byte)Rorg.FourBs
                ? await Send(Rorg.FourBs, FourBsTeachIn(Outgoing.Func, Outgoing.Type, ManufacturerId))
                : await Send(Rorg.Ute, UteTeachInQuery(Outgoing.Rorg, Outgoing.Func, Outgoing.Type, ManufacturerId));
        }

        /// <summary>
        /// 4BS teach-in variation 2: FUNC (6 bit), TYPE (7 bit), manufacturer (11 bit), LRN type = with EEP, LRN bit = 0.
        /// </summary>
        internal static byte[] FourBsTeachIn(byte func, byte type, int manufacturer)
        {
            return new[]
            {
                (byte)((func << 2) | (type >> 5)),
                (byte)(((type << 3) & 0xF8) | ((manufacturer >> 8) & 0x07)),
                (byte)(manufacturer & 0xFF),
                (byte)0x80
            };
        }

        /// <summary>
        /// UTE teach-in query (DB6 first): bidirectional, response expected, teach-in request, all channels, then
        /// manufacturer id, TYPE, FUNC and RORG of the profile.
        /// </summary>
        internal static byte[] UteTeachInQuery(byte rorg, byte func, byte type, int manufacturer)
        {
            return new[]
            {
                (byte)0x80,
                (byte)0xFF,
                (byte)(manufacturer & 0xFF),
                (byte)((manufacturer >> 8) & 0x07),
                type,
                func,
                rorg
            };
        }

        private async Task<bool> Send(Rorg rorg, byte[] data)
        {
            var telegram = new RadioErp1Packet(rorg, data, 0x00) { SenderOffset = SenderOffset };
            DriverContext.Logger.LogDebug($"{_serial}: sending {rorg} {BitConverter.ToString(data)} with sender offset {SenderOffset}");
            return await TeachInManager.Send(telegram);
        }

        private static bool IsSingleBit(NodeInstance node)
        {
            var length = node.PropertyInstance.FirstOrDefault(p => p.This2PropertyTemplateNavigation?.Key == "enocean-length");
            return length?.Value != null && Convert.ToInt32(length.Value) == 1;
        }
    }
}
