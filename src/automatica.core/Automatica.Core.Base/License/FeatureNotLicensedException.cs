using System;

namespace Automatica.Core.Base.License
{
    public sealed class FeatureNotLicensedException : Exception
    {
        public string FeatureName { get; }

        public FeatureNotLicensedException(string featureName)
        {
            FeatureName = featureName;
        }
    }
}
