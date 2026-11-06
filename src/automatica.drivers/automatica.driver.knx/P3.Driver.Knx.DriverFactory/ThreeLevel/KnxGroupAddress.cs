using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Cryptography;
using Automatica.Core.Driver;
using Knx.Falcon;
using Knx.Falcon.ApplicationData.DatapointTypes;
using Microsoft.Extensions.Logging;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;

namespace P3.Driver.Knx.DriverFactory.ThreeLevel
{
    public abstract class KnxGroupAddress(IDriverContext driverContext, KnxDriver knxDriver)
        : KnxLevelBase(driverContext, knxDriver)
    {
        public string GroupAddress { get; private set; }
        public int DptType { get; private set; }
        public int DptSubType { get; private set; }

        public bool ReadableFromBus { get; set; }

        public abstract int ImplementationDptType { get; }

        /// <summary>
        /// Sub type used when the node has no sub type selection (knx-dpt-sub), x.001 is the plain value of every main type.
        /// </summary>
        internal const int DefaultDptSubType = 1;

        /// <summary>
        /// Falcon sub type used to encode and decode telegrams. It is the stored sub type unless a node numbers its sub type selection differently.
        /// </summary>
        protected internal virtual int CodecSubType => DptSubType;


        protected override Task<bool> StartedInternal(CancellationToken token = new CancellationToken())
        {
            if (ReadableFromBus)
            {
                DriverContext.Logger.LogInformation($"Start initial read for {GroupAddress}");
                ThreadPool.QueueUserWorkItem(async a => { await Driver.Read(GroupAddress); });
            }

            return base.StartedInternal(token);
        }

        public sealed override async Task<bool> Init(CancellationToken token = default)
        {
            await base.Init(token);

            if (Parent is KnxLevelBase parentLevel)
            {
                var mainAddress = ((KnxLevelBase)Parent.Parent).Address;
                var middleAddress = ((KnxLevelBase)Parent).Address;
                var group = Address;
                GroupAddress = $"{mainAddress}/{middleAddress}/{group}";
            }
            else
            {
                GroupAddress = $"{Address}";
            }

            DptType = ImplementationDptType;
            // the knx-dpt template default is the main type (e.g. 7), as a sub type that would be 7.007 (time period in hours)
            DptSubType = DefaultDptSubType;

            var dptSubType = GetPropertyValue("knx-dpt-sub", DptSubType);
            
            if (dptSubType != null)
            {
                DptSubType = Convert.ToInt32(dptSubType);
            }

            DriverContext.Logger.LogDebug($"GA {GroupAddress} - DptType {DptType}");

            var readableFromBusProperty = DriverContext.NodeInstance.GetPropertyValue("readable_from_bus", false);

            if (readableFromBusProperty is bool bValue)
            {
                ReadableFromBus = bValue;
            }

            Driver.AddAddressNotifier(GroupAddress, this, TelegramReceivedCallback);
            return true;
        }

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            try
            {
                var dptValue = ConvertToDptValue(value);

                if (dptValue == null) //value did not change
                {
                    DriverContext.Logger.LogDebug($"{GroupAddress} Value did not change, we will not write it WriteOnlyIfChanged is: {WriteOnlyIfChanged} (NewValue: {value} OldValue: {GetCurrentValue()})");
                    return;
                }

                if (DptType != ImplementationDptType)
                {
                    DriverContext.Logger.LogWarning(
                        $"{GroupAddress} DptType {DptType} does not match implementation {ImplementationDptType}....we prefer the implementation one!");
                }

                var decodedValue = ToGroupValue(dptValue);

                var result = await Driver.Write(this, GroupAddress, decodedValue, token).ConfigureAwait(false);

                if (result)
                {
                    await writeContext.DispatchValue(value, token);
                    DriverContext.Logger.LogDebug($"Successfully write value {value} {decodedValue} on {GroupAddress}");
                }
                else
                {
                    DriverContext.Logger.LogWarning($"{GroupAddress}: Failed to write to Write datagram {value} {decodedValue}");
                    throw new ArgumentException(
                        $"{GroupAddress}: Failed to write to Write datagram {value} {decodedValue}");
                }
            }
            catch (NotImplementedException)
            {
                DriverContext.Logger.LogWarning(
                    $"{DriverContext.NodeInstance.Name} {value} Could not convert value correctly....ignore write!");
            }

        }

        protected void ConvertFromBus(GroupEventArgs datagram)
        {
            ConvertFromBus(datagram.Value);
        }

        protected void ConvertFromBus(GroupValue groupValue)
        {
            if (DptType != ImplementationDptType)
            {
                DriverContext.Logger.LogWarning($"DptType {DptType} does not match implementation {ImplementationDptType}....we prefer the implementation one!");
            }

            try
            {
                var value = FromGroupValue(groupValue);

                if (ValueRead(value))
                {
                    DispatchRead(value);
                }
            }
            catch (FormatException fe)
            {
                DriverContext.Logger.LogError(fe, $"Could not parse DPT correctly from {GroupAddress} (Target DPT is {ImplementationDptType}.{CodecSubType:000} Value is {groupValue.Value.ToHex(true)} {fe}");
            }
        }

        /// <summary>
        /// Encodes the value returned by <see cref="ConvertToDptValue"/> to the telegram payload.
        /// </summary>
        protected virtual GroupValue ToGroupValue(object dptValue)
        {
            var dpt = DptFactory.Default.Get(ImplementationDptType, CodecSubType);
            return dpt.ToGroupValue(dptValue);
        }

        /// <summary>
        /// Decodes a received telegram payload to the value passed to <see cref="ValueRead"/>.
        /// </summary>
        protected virtual object FromGroupValue(GroupValue groupValue)
        {
            var dpt = DptFactory.Default.Get(ImplementationDptType, CodecSubType);
            return dpt.ToValue(groupValue);
        }

        /// <summary>
        /// Creates the telegram payload from a raw datapoint value (BAOS), values up to 6 bit are sent in the APCI byte.
        /// </summary>
        protected virtual GroupValue CreateGroupValue(byte[] data)
        {
            var dpt = DptFactory.Default.Get(ImplementationDptType, CodecSubType);
            var sizeInBit = (int)dpt.SizeInBit;
            if (sizeInBit is > 0 and <= 6 && data.Length == 1)
            {
                return new GroupValue((byte)(data[0] & ((1 << sizeInBit) - 1)), sizeInBit);
            }

            return new GroupValue(data);
        }

        protected virtual bool ValueRead(object value)
        {
            return true;
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            if (DriverContext.NodeInstance.IsReadable)
            {
                var readGood = await Driver.Read(GroupAddress);
                DriverContext.Logger.LogDebug($"Read {GroupAddress}, response was {readGood}");
            }
            return false;
        }

        internal override void ConnectionEstablished()
        {
            if (DriverContext.NodeInstance.IsReadable)
            {
             //   Driver.Read(this);
            }
        }

        protected abstract object ConvertToDptValue(object value);

        protected virtual object GetCurrentValue()
        {
            return null;
        }

        private void TelegramReceivedCallback(object data)
        {
            switch (data)
            {
                case GroupEventArgs knxDatagram:
                    ConvertFromBus(knxDatagram);
                    break;
                case GroupValue groupValue:
                    ConvertFromBus(groupValue);
                    break;
                case byte[] raw:
                    ConvertFromBus(CreateGroupValue(raw));
                    break;
                case ReadOnlyMemory<byte> memory:
                    ConvertFromBus(CreateGroupValue(memory.ToArray()));
                    break;
            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }

       
    }
}
