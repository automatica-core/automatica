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

        public virtual int SizeInBits { get; } = 8;


        protected KnxGroupAddress(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {

        }

        public override async Task<bool> Init(CancellationToken token = default)
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

            DptType = GetPropertyValueInt("knx-dpt");

            DriverContext.Logger.LogDebug($"GA {GroupAddress} - DptType {DptType}");



            Driver.AddAddressNotifier(GroupAddress, TelegramReceivedCallback);
            return true;
        }

        protected void ConvertFromBus(GroupEventArgs datagram)
        {
            var dpt = DptFactory.Default.Get(DptType, -1);
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

        public override async Task<bool> Read(CancellationToken token = default)
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

        protected GroupValue ConvertToBus(object value)
        {
            try
            {
                var dpt = DptFactory.Default.Get(DptType, -1);
                var decodedValue = dpt.ToGroupValue(value);
                return decodedValue;
            }
            catch (Exception e)
            { 
                DriverContext.Logger.LogError(e,$"Could not convert {value} to bus type");
                throw;
            }
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
