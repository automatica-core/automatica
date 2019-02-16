using Microsoft.Extensions.Logging;
using Rssdp;
using Rssdp.Infrastructure;

namespace P3.Driver.HueBridge.Api
{
    public class Discovery : PublisherBase
    {
        private const string OsName = "Linux";
        private const string OsVersion = "3.14.0";

        public Discovery(ILogger logger)
            : this(new CommunicationsServer(new SocketFactory(null)), logger)
        {


        }

        public Discovery(ISsdpCommunicationsServer communicationsServer, ILogger logger)
            : base(communicationsServer, OsName, OsVersion, new SsdpTraceLogger(logger))
        {

        }
        public Discovery(ISsdpCommunicationsServer communicationsServer, string osName, string osVersion) : base(communicationsServer, osName, osVersion)
        {
        }

        public Discovery(ISsdpCommunicationsServer communicationsServer, string osName, string osVersion, ISsdpLogger log) : base(communicationsServer, osName, osVersion, log)
        {
        }

    }
}
