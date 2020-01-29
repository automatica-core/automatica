using Newtonsoft.Json;

namespace P3.Driver.FroniusSolar.Model.Data
{
    public enum StatusCode
    {
        Startup0 = 0,
        Startup1 = 1,
        Startup2 = 2,
        Startup3 = 3,
        Startup4 = 4,
        Startup5 = 5,
        Startup6 = 6,
        Running = 7,
        Standby = 8,
        Starting = 9,
        Error = 10
    }
    public sealed class Device
    {
        [JsonProperty("DT")]
        public int DeviceType { get; set; }

        [JsonProperty("PVPower")]
        public int PvPower { get; set; }

        [JsonProperty("CustomName")]
        public string Name { get; set; }

        [JsonProperty("UniqueID")]
        public string UniqueId { get; set; }

        [JsonProperty("ErrorCode")]
        public int ErrorCode { get; set; }

        [JsonProperty("StatusCode")] 
        public StatusCode StatusCode { get; set; }
    }
}
