using Automatica.Core.Base.Exceptions;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.License;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Automatica.Core.Model.Models.User;

namespace Automatica.Core.WebApi.Controllers
{
    public class LicenseDto
    {
        public string License { get; set; }
    }

    [Route("license")]
    [Authorize(Roles = Role.AdminRole)]
    public class LicenseController : BaseController
    {
        public LicenseController(AutomaticaContext dbContext, ILicenseContext licenseContext) : base(dbContext)
        {
            LicenseContext = licenseContext;
        }

        public ILicenseContext LicenseContext { get; }

        [HttpGet, Route("")]
        public async Task<LicenseDto> GetLicense()
        {
            var x = new LicenseDto();
            x.License = await LicenseContext.GetLicense();
            return x;
        }

        [HttpPost, Route("")]
        public async Task<LicenseDto> GetLicense([FromBody]LicenseDto data)
        {
            var license = data.License;

            if(await LicenseContext.CheckIfValid(license))
            {
                await LicenseContext.SaveLicense(license);
                return await GetLicense();
            }

            throw new WebApiException("InvalidLicense", ExceptionSeverity.Error);
        }
    }
}
