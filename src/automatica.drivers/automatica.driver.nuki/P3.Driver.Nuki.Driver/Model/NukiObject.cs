using Newtonsoft.Json;

namespace P3.Driver.Nuki.Driver.Model
{
    internal class NukiObject
    {
        [JsonProperty("deviceType")]
        public int DeviceType { get; set; }

        [JsonProperty("nukiId")]
        public int NukiId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("firmwareVersion")]
        public string FirmwareVersion { get; set; }

        [JsonProperty("lastKnownState")]
        public LastKnownState LastKnownState { get; set; }
    }
}
