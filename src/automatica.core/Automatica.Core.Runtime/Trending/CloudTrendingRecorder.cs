using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Runtime.Trending
{
    internal class CloudTrendingRecorder : BaseTrendingRecorder
    {
        public CloudTrendingRecorder(IConfiguration config) : base("CloudTrendingRecorder", config)
        {
        }
    }
}
