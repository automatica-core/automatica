using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;

namespace Automatica.Core.Plugin.Standalone.Factories
{
    internal class RemoteServerCloudApi : IServerCloudApi
    {
        public Task<bool> SendEmail(IList<string> to, string subject, string message)
        {
            return Task.FromResult(false);
        }
    }
}
