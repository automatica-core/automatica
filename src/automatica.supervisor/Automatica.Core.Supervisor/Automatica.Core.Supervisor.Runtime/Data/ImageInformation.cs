using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Automatica.Core.Supervisor.Runtime.Data
{
    public class ImageInformation
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("images")]
        public IList<Image> Images { get; set; }


        [JsonProperty("last_updated")]
        public DateTime LastUpdated { get; set; }
    }

    public class Image
    {

        [JsonProperty("architecture")]
        public string Architecture { get; set; }


        [JsonProperty("os")]
        public string OperatingSystem { get; set; }
    }
}
