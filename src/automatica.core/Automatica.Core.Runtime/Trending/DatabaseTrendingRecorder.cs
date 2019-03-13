using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Runtime.Trending
{
    internal class DatabaseTrendingRecorder : BaseTrendingRecorder
    {
        public DatabaseTrendingRecorder(IConfiguration config) : base("DatabaseTrendingRecorder", config)
        {
        }
    }
}
