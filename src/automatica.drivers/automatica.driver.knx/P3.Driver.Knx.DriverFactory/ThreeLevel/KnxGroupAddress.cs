using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
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
            DptType = Convert.ToInt32(dptValueProp.This2PropertyTemplateNavigation.DefaultValue);

            DriverContext.Logger.LogDebug($"GA {GroupAddress} - DptType {DptType}");

            var readableFromBusProperty = DriverContext.NodeInstance.GetPropertyValue("readable_from_bus", false);

            if (readableFromBusProperty is bool bValue)
            {
                ReadableFromBus = bValue;
            }

            Driver.AddAddressNotifier(GroupAddress, this, TelegramReceivedCallback);
            return true;
        }

        public sealed override Task WriteValue(IDispatchable source, DispatchValue value, CancellationToken token = new CancellationToken())
        {
            var dptValue = ConvertToDptValue(value.Value);

            if (dptValue == null)
            {
                DriverContext.Logger.LogWarning($"Could not convert value correctly....ignore write!");
                return Task.CompletedTask;
            }

            if (DptType != ImplementationDptType)
            {
                DriverContext.Logger.LogWarning($"DptType {DptType} does not match implementation {ImplementationDptType}....we prefer the implementation one!");
            }

            var dpt = DptFactory.Default.Get(ImplementationDptType, -1);
            var decodedValue = dpt.ToGroupValue(dptValue);

            Driver.Write(this, GroupAddress, decodedValue);

            return Task.CompletedTask;
        }


        protected void ConvertFromBus(GroupEventArgs datagram)
        {
            if (DptType != ImplementationDptType)
            {
                DriverContext.Logger.LogWarning($"DptType {DptType} does not match implementation {ImplementationDptType}....we prefer the implementation one!");
            }

            var dpt = DptFactory.Default.Get(ImplementationDptType, -1);
            var value = dpt.ToValue(datagram.Value);

            if (ValueRead(value))
            {
                DispatchValue(value);
            }
        }

        protected void ConvertFromBus(GroupValue groupValue)
        {
            var dpt = DptFactory.Default.Get(DptType, -1);
            var value= dpt.ToValue(groupValue);

            if (ValueRead(value))
            {
                DispatchValue(value);
            }
        }

        protected virtual bool ValueRead(object value)
        {
            return true;
        }

        public sealed override async Task<bool> Read(CancellationToken token = default)
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
