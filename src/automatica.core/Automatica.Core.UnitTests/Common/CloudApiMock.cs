using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;

namespace Automatica.Core.UnitTests.Base.Common
{
    public class CloudApiMock : IServerCloudApi
    {
        public Task<bool> SendEmail(IList<string> to, string subject, string message)
        {
            return Task.FromResult(true);
        }
    }
}
