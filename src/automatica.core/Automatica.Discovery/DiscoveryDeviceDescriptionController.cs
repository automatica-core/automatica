using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.WebApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rssdp;

namespace Automatica.Discovery
{
    [Route("webapi/discovery")]
    [AllowAnonymous]
    public class DiscoveryDeviceDescriptionController : BaseController
    {
        private readonly DiscoveryService _discoveryService;

        public DiscoveryDeviceDescriptionController(AutomaticaContext dbContext, DiscoveryService discoveryService) : base(dbContext)
        {
            _discoveryService = discoveryService;
        }

        [HttpGet]
        [Produces("application/xml")]
        public ContentResult GetDeviceDescription()
        {
            string xml = _discoveryService.Device.ToDescriptionDocument();
            return new ContentResult
            {
                ContentType = "application/xml",
                Content = xml,
                StatusCode = 200
            };
        }

        [HttpGet]
        [Route("scan")]
        public async Task<IEnumerable<DiscoveredSsdpDevice>> StartScan()
        {
            var data = await _discoveryService.Scan();

            return data;
        }
    }
}
