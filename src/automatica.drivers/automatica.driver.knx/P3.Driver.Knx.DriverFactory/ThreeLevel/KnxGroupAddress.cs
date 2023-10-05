using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Knx.Falcon;
using Knx.Falcon.ApplicationData.DatapointTypes;
using Microsoft.Extensions.Logging;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;

namespace P3.Driver.Knx.DriverFactory.ThreeLevel
{
    public abstract class KnxGroupAddress : KnxLevelBase
    {
        public string GroupAddress { get; private set; }
        public int DptType { get; private set; }
        public int DptSubType { get; private set; }

        public bool ReadableFromBus { get; set; }

        public abstract int ImplementationDptType { get; }



        protected KnxGroupAddress(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {

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

            var dptValueProp = GetProperty("knx-dpt");
            DptType = ImplementationDptType;
            DptSubType = Convert.ToInt32(dptValueProp.This2PropertyTemplateNavigation.DefaultValue);

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

                var dpt = DptFactory.Default.Get(ImplementationDptType, DptSubType);
                var decodedValue = dpt.ToGroupValue(dptValue);

                var result = await Driver.Write(this, GroupAddress, decodedValue).ConfigureAwait(false);
                await writeContext.DispatchValue(value, token);

                if (!result)
                {
                    DriverContext.Logger.LogWarning("Failed to write to Write datagram");
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
            if (DptType != ImplementationDptType)
            {
                DriverContext.Logger.LogWarning($"DptType {DptType} does not match implementation {ImplementationDptType}....we prefer the implementation one!");
            }

            var dpt = DptFactory.Default.Get(ImplementationDptType, DptSubType);
            var value = dpt.ToValue(datagram.Value);

            if (ValueRead(value))
            {
                DispatchRead(value);
            }
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
            if (data is GroupEventArgs knxDatagram)
            {
                ConvertFromBus(knxDatagram);
            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }

       
    }
}
