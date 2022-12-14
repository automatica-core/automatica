using P3.Driver.VkingBms.Driver.Interfaces;

namespace P3.Driver.VkingBms.Driver.Data
{
    internal class AnalogDataResponse : BaseResponse, IAnalogDataResponse
    {
        public byte Address { get; private set; }
        public ushort[] CellVoltages { get; private set; } = Array.Empty<ushort>();
        public ushort[] Temperatures { get; private set; } = Array.Empty<ushort>();
        public short Current { get; private set; }
        public ushort Voltage { get; private set; }
        public ushort RemainingCapacity { get; private set; }
        public ushort FullCapacity { get; private set; }
        public ushort CycleNumber { get; private set; }
        public byte Soh { get; private set; }
        public byte Soc { get; private set; }
        public ushort BalanceState1 { get; private set; }
        public ushort Humidity { get; private set; }
        public ushort BalanceState2 { get; private set; }

        public AnalogDataResponse(byte[] response) : base(response)
        {
            GetData(response, 13);
        }

        private void GetData(byte[] data, int index)
        {
            
            Address = Util.GetByte(data, index);
            index += 2;
            CellVoltages = new ushort[Util.GetByte(data, index)];
            index += 2;
            for (int index4 = 0; index4 < CellVoltages.Length; ++index4)
            {
                CellVoltages[index4] = Util.GetUShort(data, index);
                index += 4;
            }
            Temperatures = new ushort[Util.GetByte(data, index)];
            index += 2;

            for (int index6 = 0; index6 < Temperatures.Length; ++index6)
            {
                Temperatures[index6] = (ushort)(Util.GetUShort(data, index) - 40U);
                index += 4;
            }

            Current = Util.GetShort(data, index);
            index += 4;

            Voltage = Util.GetUShort(data, index);
            index += 4;

            RemainingCapacity = Util.GetUShort(data, index);
            index += 4 + 2;

            FullCapacity = Util.GetUShort(data, index);
            index += 4;

            CycleNumber = Util.GetUShort(data, index);
            index += 4;

            Soc = Util.GetByte(data, index);
            index += 2;

            Soh = Util.GetByte(data, index);
            index += 2;

            BalanceState1 = Util.GetUShort(data, index);
            index += 4;

            Humidity = Util.GetUShort(data, index);
            index += 4;

            BalanceState2 = Util.GetUShort(data, index);
        }

    }
}
