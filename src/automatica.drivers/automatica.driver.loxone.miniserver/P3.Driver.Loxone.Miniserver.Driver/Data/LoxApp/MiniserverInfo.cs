using Newtonsoft.Json;

namespace P3.Driver.Loxone.Miniserver.Driver.Data.LoxApp
{
    public class LoxoneUser
    {
        public string Name { get; set; }
        public string Uuid { get; set; }
        public bool isAdmin { get; set; }
        public bool changePassword { get; set; }
        public long UserRights { get; set; }
    }
    public class MiniserverInfo
    {
        [JsonProperty("serialNr")]
        public string SerialNumber { get; set; }

        [JsonProperty("msName")]
        public string Name { get; set; }

        public string ProjectName { get; set; }
        public string LocalUrl { get; set; }

        [JsonProperty("tempUnit")]
        public int TemperatureUnit { get; set; }

        public string Currency { get; set; }

        public string Location { get; set; }
        public string LanguageCode { get; set; }
        public string HeatPeriodStart { get; set; }
        public string HeatPeriodEnd { get; set; }
        public string CoolPeriodStart { get; set; }
        public string CoolPeriodEnd { get; set; }

        [JsonProperty("catTitle")]
        public string CategoryTitle { get; set; }

        public string RoomTitle { get; set; }

        public int MiniserverType { get; set; }

        public LoxoneUser CurrentUser { get; set; }

    }
}
