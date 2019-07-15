using Automatica.Core.Base.License;
using System.Collections.Generic;

namespace Automatica.Core.UnitTests.Base.Drivers
{
    public class LicenseStateMock : ILicenseState
    {
        public bool IsLicensed => true;

        public int MaxDataPoints => int.MaxValue;

        public int MaxUsers => int.MaxValue;

        public IList<(string message, string howToSolve)> ValidationErrors => new List<(string, string)>();
    }
}
