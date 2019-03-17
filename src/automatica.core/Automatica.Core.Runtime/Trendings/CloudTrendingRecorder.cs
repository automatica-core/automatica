using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models.Trendings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Trendings
{
    internal class CloudTrendingRecorder : BaseTrendingRecorder
    {
        public CloudTrendingRecorder(IConfiguration config, IDispatcher dispatcher) : base("CloudTrendingRecorder", config, dispatcher)
        {
        }

        internal override void Save(Trending trend)
        {
            Logger.LogInformation($"CloudLogger save is not implemented...");
        }
    }
}
