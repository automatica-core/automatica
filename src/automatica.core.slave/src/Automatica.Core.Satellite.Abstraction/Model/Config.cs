namespace Automatica.Core.Satellite.Abstraction.Model
{
    public class Config
    {
        public string ServerUrl { get; set; }
        public string ClientId { get; set; }
        public string ClientKey { get; set; }

        public int Port { get; set; } = 5010;

        public string DockerTag { get; set; }
    }
}
