using System.Collections.Specialized;
using Microsoft.Extensions.Logging;
using P3.Driver.HomeKit.Hap.Model;

namespace P3.Driver.HomeKit.Hap.Controllers
{
    internal class AccesoriesController : BaseController
    {
        private readonly ILogger _logger;

        public AccesoriesController(ILogger logger)
        {
            _logger = logger;
        }
        public AccessoryData Get(HomeKitServer homeKitServer, NameValueCollection queryString)
        {
            _logger.LogDebug($"Working on queryString {queryString}");
            var ad = new AccessoryData { Accessories = homeKitServer.GetAccessories()};

            return ad;
        }
    }
}
