using Polly;

namespace Automatica.Core.Base.Retry
{
    public interface IRetryContext
    {
        ResiliencePipeline GetPipeline();
    }
}
