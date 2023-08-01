using Automatica.Core.Base.License;
using Standard.Licensing.Validation;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Automatica.Core.Internals.License
{
    public interface ILicenseContext : ILicenseContract
    {
        Task<bool> Init();

        IList<IValidationFailure> ValidationErrors { get; }

        bool IsLicensed { get; }
        int MaxDataPoints { get; }
        int MaxUsers { get; }
        bool AllowRemoteControl { get; }

        int MaxRemoteTunnels { get; }

        Task<string> GetLicense();
        Task SaveLicense(string license);

        Task<bool> CheckIfValid(string license);

        ILicenseState GetLicenseState();
    }
}
