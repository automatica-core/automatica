namespace P3.Driver.VkingBms.Driver.Interfaces
{
    public interface IDataResponse
    {
        public byte Version { get; }
        public byte PackAddress { get; }
        public byte Rtn { get; }
    }
}
