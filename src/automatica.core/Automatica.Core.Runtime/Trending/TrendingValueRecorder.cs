using Automatica.Core.EF.Models;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.Trending
{
    internal class TrendingValueRecorder
    {
        public TrendingValueRecorder(NodeInstance instance)
        {
            Instance = instance;
        }

        internal Task ValueChanged(object value)
        {
            return Task.CompletedTask;
        }


        public Task Stop()
        {
            return Task.CompletedTask;
        }

        public NodeInstance Instance { get; }
    }
}
