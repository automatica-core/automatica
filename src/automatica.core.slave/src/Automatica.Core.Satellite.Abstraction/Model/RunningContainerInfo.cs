using Docker.DotNet.Models;

namespace Automatica.Core.Satellite.Abstraction.Model
{
    public class RunningContainerInfo
    {
        public string DockerId { get; set; }
        public string RequestId { get; set; }
        public string Image { get; set; }

        // contains the broker credentials in the environment, not part of the status api
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public CreateContainerParameters CreateContainerParameters { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public ActionRequest Request { get; set; }
    }
}
