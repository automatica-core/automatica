using System;
using System.Collections.Generic;
using System.Text;
using Automatica.Core.Base.License;

namespace Automatica.Core.UnitTests.Base.Common
{
    internal class LicenseContractMock : ILicenseContract
    {
        public bool IsFeatureLicensed(string featureName)
        {
            return true;
        }

        public bool DriverLicenseCountExceeded()
        {
            return false;
        }

        public void IncrementDriverCount()
        {
            
        }
    }
}
