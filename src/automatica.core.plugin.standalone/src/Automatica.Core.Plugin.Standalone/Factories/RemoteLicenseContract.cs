using Automatica.Core.Base.License;

namespace Automatica.Core.Plugin.Standalone.Factories
{
    internal class RemoteLicenseContract : ILicenseContract
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
