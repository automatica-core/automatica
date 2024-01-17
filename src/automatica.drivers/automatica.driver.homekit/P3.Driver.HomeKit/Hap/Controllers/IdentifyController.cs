using Microsoft.Extensions.Logging;

namespace P3.Driver.HomeKit.Hap.Controllers
{
    internal sealed class IdentifyController : BaseController
    {
        public IdentifyController(ILogger logger) : base(logger)
        {
            
        }
        public StateReturn Post(byte[] body)
        {
            return new StateReturn
            {
                Status = 0
            };
        }
    }
}
