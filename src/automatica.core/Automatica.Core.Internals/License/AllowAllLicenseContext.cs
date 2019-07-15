using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Base.License;
using Standard.Licensing.Validation;

namespace Automatica.Core.Internals.License
{
    public class AllowAllLicenseContext : ILicenseContext
    {
        public bool IsFeatureLicensed(string featureName)
        {
            return true;
        }

        public Task<bool> Init()
        {
            return Task.FromResult(true);
        }

        public IList<IValidationFailure> ValidationErrors => new List<IValidationFailure>();
        public bool IsLicensed => true;
        public int MaxDataPoints => int.MaxValue;
        public int MaxUsers => int.MaxValue;
        public Task<string> GetLicense()
        {
            return Task.FromResult("All Allowed");
        }

        public Task SaveLicense(string license)
        {
            return Task.CompletedTask;
        }

        public Task<bool> CheckIfValid(string license)
        {
            return Task.FromResult(true);
        }

        public ILicenseState GetLicenseState()
        {
            return new LicenseState(this);
        }
    }
}
