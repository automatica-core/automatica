using Automatica.Core.Base.Retry;
using Polly;

namespace Automatica.Core.UnitTests.Base.Drivers
{
    internal class RetryContextMock : IRetryContext
    {
        public ResiliencePipeline GetPipeline()
        {
            return new ResiliencePipelineBuilder()
                .Build();
        }
    }
}
