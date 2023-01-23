namespace P3.Driver.VkingBms.Driver.Interfaces
{
    public interface IBmsInfoResponse : IDataResponse
    {
        public string CompanyName { get;  }
        public string ManufacturedTime { get; }
        public string ModuleName { get;  }
        public string VersionNumber { get; }
        public string BatteryType { get; }
        public string SerialNumber1 { get;}
        public string SerialNumber2 { get; }
    }
}
