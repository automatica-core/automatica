namespace P3.Driver.VkingBms.Driver.Interfaces
{
    public interface IAnalogDataResponse : IDataResponse
    {
        public byte Address { get; }
        public ushort[] CellVoltages { get; }

        public ushort[] Temperatures { get; }

        public short Current { get; }
        public ushort Voltage { get; }
        public ushort RemainingCapacity { get; }
        public ushort FullCapacity { get; }
        public ushort CycleNumber { get; }

        public byte Soh { get; }
        public byte Soc { get; }
        public ushort BalanceState1 { get; }
        public ushort Humidity { get; }
        public ushort BalanceState2 { get; }
    }
}
