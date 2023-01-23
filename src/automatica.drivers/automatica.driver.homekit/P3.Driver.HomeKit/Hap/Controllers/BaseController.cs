using System;
using Newtonsoft.Json;

namespace P3.Driver.HomeKit.Hap.Controllers
{
    internal class StateReturn
    {
        [JsonProperty("status")]
        public int Status { get; set; }

        public string ContentType => "application/hap+json";
    }
    internal class BaseController
    {
        public static byte[] StringToByteArray(String hex)
        {
            return Automatica.Core.Driver.Utility.Utils.StringToByteArray(hex);
        }

        public static string ByteArrayToString(byte[] ba)
        {
            return Automatica.Core.Driver.Utility.Utils.ByteArrayToString(in ba).Replace(" ", "");
        }
    }
}
