using Docker.DotNet.Models;

namespace Automatica.Core.Satellite.Abstraction.Model
{
    public class RunningContainerInfo
    {
        public string DockerId { get; set; }
        public string RequestId { get; set; }
        public string Image { get; set; }

        public CreateContainerParameters CreateContainerParameters { get; set; }
    }
}
