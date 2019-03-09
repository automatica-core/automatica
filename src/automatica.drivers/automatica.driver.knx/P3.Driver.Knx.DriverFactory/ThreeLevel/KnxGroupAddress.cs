using System.Threading.Tasks;
using Automatica.Core.Driver;
using P3.Knx.Core.Abstractions;
using P3.Knx.Core.DPT;
using P3.Knx.Core.Driver;

namespace P3.Driver.Knx.DriverFactory.ThreeLevel
{
    public abstract class KnxGroupAddress : KnxLevelBase
    {
        public string GroupAddress { get; private set; }
        public int DptType { get; private set; }
        public string DptTypeString { get; private set; }
        protected KnxGroupAddress(IDriverContext driverContext, IKnxDriver knxDriver) : base(driverContext, knxDriver)
        {

        }

        public override bool Init()
        {
            base.Init();

            var mainAddress = ((KnxLevelBase)Parent.Parent).Address;
            var middleAddress = ((KnxLevelBase)Parent).Address;
            var group = Address;

            GroupAddress = $"{mainAddress}/{middleAddress}/{group}";

            DptType = GetPropertyValueInt("knx-dpt");

            DptTypeString = GetDptString(DptType);

            Driver.AddAddressNotifier(GroupAddress, TelegramReceivedCallback);
            return true;
        }

        protected abstract string GetDptString(int dpt);

        protected virtual void ConvertFromBus(KnxDatagram datagram)
        {
            var value = DptTranslator.Instance.FromDataPoint(DptTypeString, datagram.Data);

            if (ValueRead(value))
            {
                DispatchValue(value);
            }
        }

        protected virtual bool ValueRead(object value)
        {
            return true;
        }

        public override async Task<bool> Read()
        {
            if (DriverContext.NodeInstance.IsReadable)
            {
                return await Driver.Read(GroupAddress);
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

        protected virtual byte[] ConvertToBus(object value)
        {
            return DptTranslator.Instance.ToDataPoint(DptTypeString, value);
        }

        private void TelegramReceivedCallback(object data)
        {
            if (data is KnxDatagram knxDatagram)
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
