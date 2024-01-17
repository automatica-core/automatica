using Polly;
using Polly.Registry;

namespace Automatica.Core.Base.Retry
{
    internal class RetryContext(ResiliencePipelineProvider<string> provider) : IRetryContext
    {
        public ResiliencePipeline GetPipeline()
        {
            return provider.GetPipeline("start-pipeline");
        }
    }
}
