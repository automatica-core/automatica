using System.Collections.Generic;

namespace Automatica.Core.Base.License
{
    public interface ILicenseState
    {
        bool IsLicensed { get; }
        int MaxDataPoints { get; }
        int MaxUsers { get; }

        IList<(string message, string howToSolve)> ValidationErrors { get; }
    }
}
