using System.Collections.Specialized;
using P3.Driver.HomeKit.Hap.Model;

namespace P3.Driver.HomeKit.Hap.Controllers
{
    internal class AccesoriesController : BaseController
    {
        public AccessoryData Get(HomeKitServer homeKitServer, NameValueCollection queryString)
        {
            var ad = new AccessoryData();

            ad.Accessories = homeKitServer.GetAccessories();

            return ad;
        }
    }
}
