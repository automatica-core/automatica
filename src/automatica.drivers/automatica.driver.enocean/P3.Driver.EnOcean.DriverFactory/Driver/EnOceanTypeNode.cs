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

        /// <summary>
        /// Set if the device expects an answer to each status telegram (A5-20 HVAC components): the command case.
        /// </summary>
        private Guid? _replyCommand;

        /// <summary>
        /// True for the command case of a bidirectional device (e.g. A5-20-01 controller to actuator): its values are
        /// sent as answer to the next status telegram, telegrams of the device are not decoded with its layout.
        /// </summary>
        internal bool IsReplyCommand { get; private set; }

        /// <summary>
        /// True for a two-way device template (status and command case in one node, A5-20-01..06): telegrams are decoded
        /// with the status fields, every status telegram is answered with the values written to the command fields.
        /// </summary>
        internal bool IsTwoWay { get; private set; }

        /// <summary>
        /// The case constants of what Automatica sends: the command case of a two-way device, otherwise the own case.
        /// </summary>
        private IReadOnlyList<EepCondition> _commandConditions = Array.Empty<EepCondition>();

        /// <summary>
        /// Tests: the last teach-in response or answer to a status telegram.
        /// </summary>
        internal Task<bool> LastReply { get; private set; } = Task.FromResult(false);

        public EnOceanTypeNode(IDriverContext driverContext, ITeachInManager teachInManager) : base(driverContext, teachInManager)
        {
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            _serial = GetProperty("enocean-serialnumber").ValueString;
            _conditions = EnOceanEepMetadata.GetConditions(DriverContext.NodeInstance.This2NodeTemplate ?? Guid.Empty);
            Outgoing = EnOceanEepMetadata.GetOutgoing(DriverContext.NodeInstance.This2NodeTemplate ?? Guid.Empty);
            SenderOffset = ReadSenderOffset();
            var template = DriverContext.NodeInstance.This2NodeTemplate ?? Guid.Empty;
            var twoWay = EnOceanEepMetadata.GetTwoWay(template);
            IsTwoWay = twoWay.HasValue;
            _commandConditions = twoWay.HasValue ? EnOceanEepMetadata.GetConditions(twoWay.Value.Command) : _conditions;
            _replyCommand = IsTwoWay ? template : EnOceanEepMetadata.GetReplyCommand(template);
            IsReplyCommand = EnOceanEepMetadata.IsReplyCommand(template);
            if (IsReplyCommand)
            {
                TeachInManager.SetCommandNode(_serial, this);
            }
            TeachInManager.SetTeachedIn(_serial, DriverContext.NodeInstance.Name);
            return base.Init(token);
        }

        protected override bool CreateCustomLogger()
        {
            return true;
        }

        public override void TelegramReceived(RadioErp1Packet telegram)
        {
            if (String.Equals(telegram.SenderIdString, _serial, StringComparison.OrdinalIgnoreCase))
            {
                DriverContext.Logger.LogDebug($"Telegram received for {_serial}");

                if (IsReplyCommand)
                {
                    // the layout of what Automatica sends, the status case of the device decodes the telegram
                    return;
                }

                // universal teach-in (VLD devices): never data, answered once per device if it waits for it
                if (telegram.Rorg == Rorg.Ute)
                {
                    var teachIn = TeachIn.Parse(telegram);
                    if (teachIn != null && teachIn.ResponseExpected && !teachIn.IsDeletion && TeachInManager.ClaimTeachInResponse(_serial))
                    {
                        DriverContext.Logger.LogInformation($"{_serial}: answering the UTE teach-in query of the device");
                        LastReply = Send(Rorg.Ute, TeachIn.UteResponse(telegram.Data.Span), DeviceId());
                    }
                    return;
                }

                // 1BS/4BS teach-in telegrams (LRN bit cleared) carry EEP/manufacturer info instead of values,
                // so only the LRN node gets them. RPS has no LRN bit and is always decoded.
                var isTeachIn = RadioErp1Packet.IsLrnTeachIn(telegram);
                if (isTeachIn)
                {
                    DriverContext.Logger.LogDebug($"Teach-in telegram received for {_serial}, values are not dispatched");
                    if (IsBidirectionalTeachInQuery(telegram) && TeachInManager.ClaimTeachInResponse(_serial))
                    {
                        LastReply = SendTeachInResponse(telegram);
                    }
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
                    // what Automatica sends, not part of the telegrams of the device
                    if (child.IsCommandField)
                    {
                        continue;
                    }
                    child.TelegramReceived(telegram);
                }

                if (!isTeachIn && _replyCommand.HasValue)
                {
                    LastReply = SendReply();
                }
            }
        }

        /// <returns>true if the node is a device (EEP case) node: a type template or a node with a serial number</returns>
        internal static bool IsTypeNode(NodeInstance node)
        {
            if (node == null)
            {
                return false;
            }

            var key = node.This2NodeTemplateNavigation?.Key;
            if (key != null && key.StartsWith("enocean-type-", StringComparison.Ordinal))
            {
                return true;
            }

            return node.PropertyInstance?.Any(p => p.This2PropertyTemplateNavigation?.Key == "enocean-serialnumber") == true;
        }

        /// <summary>
        /// A 4BS teach-in query of an A5-20 HVAC component: LRN type = with EEP (DB0.7), LRN status = query (DB0.4 = 0).
        /// The device repeats it and sends no values until the controller answers.
        /// </summary>
        internal static bool IsBidirectionalTeachInQuery(RadioErp1Packet telegram)
        {
            if (telegram.Rorg != Rorg.FourBs || telegram.Data.Length < 4)
            {
                return false;
            }

            var data = telegram.Data.Span;
            var withEep = (data[3] & 0x80) != 0;
            var query = (data[3] & 0x10) == 0;
            var func = (byte)(data[0] >> 2);
            return withEep && query && func == 0x20;
        }

        /// <summary>
        /// 4BS teach-in response: the EEP of the query, multi user manufacturer id, DB0 = 0xF0 (with EEP, EEP supported,
        /// teach-in successful, response), addressed to the device. The device learns the sender id (id base + offset).
        /// </summary>
        internal static byte[] FourBsTeachInResponse(ReadOnlySpan<byte> query)
        {
            var func = (byte)(query[0] >> 2);
            var type = (byte)(((query[0] & 0x03) << 5) | (query[1] >> 3));
            var data = FourBsTeachIn(func, type, ManufacturerId);
            data[3] = 0xF0;
            return data;
        }

        private async Task<bool> SendTeachInResponse(RadioErp1Packet query)
        {
            DriverContext.Logger.LogInformation($"{_serial}: answering the teach-in query of the device");
            return await Send(Rorg.FourBs, FourBsTeachInResponse(query.Data.Span), DeviceId());
        }

        /// <summary>
        /// Answers a status telegram with the values written to the command case node of the device.
        /// </summary>
        private async Task<bool> SendReply()
        {
            var command = IsTwoWay ? this : TeachInManager.GetCommandNode(_serial);
            if (command?.Outgoing == null)
            {
                DriverContext.Logger.LogDebug($"{_serial}: no command node with this serial number, status telegram not answered");
                return false;
            }

            // without a written value the case defaults would go out, e.g. valve position 0 % closes the valve
            if (!command.HasWrittenValues)
            {
                DriverContext.Logger.LogDebug($"{_serial}: nothing written to the command node yet, status telegram not answered");
                return false;
            }

            return await Send((Rorg)command.Outgoing.Rorg, command.BuildCommand(null, 0), DeviceId());
        }

        internal bool HasWrittenValues => ChildrenT.Any(c => c.WrittenRaw.HasValue);

        private byte[] DeviceId()
        {
            try
            {
                var id = Convert.FromHexString(_serial ?? "");
                return id.Length == 4 ? id : null;
            }
            catch (FormatException)
            {
                return null;
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

            if (IsReplyCommand || IsTwoWay)
            {
                // the device sleeps between its telegrams, the value goes out with the answer to its next one
                DriverContext.Logger.LogDebug($"{_serial}: {written.Name} is sent with the answer to the next status telegram");
                return true;
            }

            return await Send((Rorg)Outgoing.Rorg, BuildCommand(written, raw));
        }

        /// <summary>
        /// The command of this case: the case defaults, the last written values of the data points and
        /// <paramref name="raw"/> for <paramref name="written"/> (null: only the written values).
        /// </summary>
        internal byte[] BuildCommand(EnOceanDataNode written, long raw)
        {
            var data = new byte[Outgoing.Size];
            foreach (var value in Outgoing.Defaults)
            {
                EnOceanEepMetadata.SetBits(data, value.Offset, value.Length, value.Value);
            }

            foreach (var child in ChildrenT)
            {
                if (IsTwoWay && !child.IsCommandField)
                {
                    continue;
                }
                if (!ReferenceEquals(child, written) && child.WrittenRaw.HasValue)
                {
                    EnOceanEepMetadata.SetBits(data, child.BitOffset, child.FieldLength, child.WrittenRaw.Value);
                }
            }
            if (written != null)
            {
                EnOceanEepMetadata.SetBits(data, written.BitOffset, written.FieldLength, raw);
            }

            // the case constants (CMD, COM) and the LRN bit of a data telegram are not values a data point may change
            foreach (var condition in _commandConditions)
            {
                EnOceanEepMetadata.SetBits(data, condition.Offset, condition.Length, condition.Value);
            }
            if (Outgoing.Rorg == (byte)Rorg.FourBs)
            {
                EnOceanEepMetadata.SetBits(data, 28, 1, 1);
            }

            return data;
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

        private async Task<bool> Send(Rorg rorg, byte[] data, byte[] destination = null)
        {
            var telegram = new RadioErp1Packet(rorg, data, 0x00) { SenderOffset = SenderOffset, DestinationId = destination };
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
