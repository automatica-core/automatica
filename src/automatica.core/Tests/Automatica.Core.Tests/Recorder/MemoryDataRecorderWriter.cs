using System;
using System.Threading.Tasks;
using Automatica.Core.Runtime.Recorder;

namespace Automatica.Core.Tests.Recorder
{
    public class MemoryDataRecorderWriter : IDataRecorderWriter
    {
        public Task Start()
        {
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            return Task.CompletedTask;
        }

        public Task AddTrend(Guid nodeInstance)
        {
            return Task.CompletedTask;
        }

        public Task RemoveTrend(Guid nodeInstance)
        {
            return Task.CompletedTask;
        }

        public Task RemoveAll()
        {
            return Task.CompletedTask;
        }
    }
}
