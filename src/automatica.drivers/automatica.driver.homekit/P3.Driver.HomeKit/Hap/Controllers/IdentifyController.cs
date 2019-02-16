namespace P3.Driver.HomeKit.Hap.Controllers
{
    internal sealed class IdentifyController : BaseController
    {
        public StateReturn Post(byte[] body)
        {
            return new StateReturn
            {
                Status = 0
            };
        }
    }
}
