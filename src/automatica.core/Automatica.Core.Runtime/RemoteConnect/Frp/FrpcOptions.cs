using System.Runtime.InteropServices;

namespace Automatica.Core.Runtime.RemoteConnect.Frp
{
    public class FrpcOptions
    {
        private bool _useSsh;
        public string ServerAddress { get; set; }
        public int ServerPort { get; set; }

        public int AdminPort { get; set; } = 7400;

        public string LogFile { get; set; } = "console";
        public string LogLevel { get; set; } = "trace";

        public bool UseWeb { get; set; }
        public string LocalIp { get; set; }
        public int LocalPort { get; set; }
        public string SubDomain { get; set; }

        public bool UseSsh
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return false;
                }
                return _useSsh;
            }
            set => _useSsh = value;
        }
    }
}
