using Automatica.Core.Base.License;
using Standard.Licensing;
using Standard.Licensing.Validation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Automatica.Core.Internals.License
{
    public class LicenseContext : ILicenseContext
    {
        private int _dataPointsInUse = 0;

        public const string LicenseFileName = ".automatica.core.lic";
        public string LicensePath { get; }

        public IList<IValidationFailure> ValidationErrors { get; private set; }

        public bool IsLicensed { get; private set; }

        public int MaxDataPoints { get; private set; }

        public int MaxUsers { get; private set; }
        public bool DriverLicenseCountExceeded()
        {
            return _dataPointsInUse > MaxDataPoints;
        }

        public void IncrementDriverCount()
        {
            ++_dataPointsInUse;
        }

        public List<string> LicensedFeatures { get; set; }

        private Standard.Licensing.License _license;

        public LicenseContext()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            var fileName = Path.Combine(path, LicenseFileName);

            LicensePath = fileName;
        }

        public async Task<bool> Init()
        {
            string pubKey = "";
            using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Automatica.Core.Internals.pub.txt")))
            {
                pubKey = await reader.ReadToEndAsync();
            }

            if (File.Exists(LicensePath))
            {

                using (var file = File.OpenRead(LicensePath))
                {
                    var license = Standard.Licensing.License.Load(file);

                    var validationFailures = license.Validate().
                                                ExpirationDate().
                                                When(lic => lic.Type == LicenseType.Trial).
                                                And().
                                                Signature(pubKey).
                                                AssertValidLicense();
                    ValidationErrors = validationFailures.ToList();
                    _license = license;
                }

                IsLicensed = ValidationErrors.Count == 0;
            }

            if(IsLicensed)
            {
                MaxDataPoints = Convert.ToInt32(_license.ProductFeatures.Get("MaxDatapoints"));
                MaxUsers = Convert.ToInt32(_license.ProductFeatures.Get("MaxUsers"));
            }
            else
            {
                MaxDataPoints = int.MaxValue;
                MaxUsers = int.MaxValue;
            }
            return IsLicensed;

        }

        private async Task<bool> Validate(string license)
        {
            string pubKey = "";
            using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Automatica.Core.Internals.pub.txt")))
            {
                pubKey = await reader.ReadToEndAsync();
            }

            var lic = Standard.Licensing.License.Load(license);

            var validationFailures = lic.Validate().
                                        ExpirationDate().
                                        When(lict => lict.Type == LicenseType.Trial).
                                        And().
                                        Signature(pubKey).
                                        AssertValidLicense();
            ValidationErrors = validationFailures.ToList();


            return ValidationErrors.Count == 0;

        }

        public ILicenseState GetLicenseState()
        {
            return new LicenseState(this);
        }

        public async Task<string> GetLicense()
        {
            string license = "";
            using (var reader = new StreamReader(LicensePath))
            {
                license = await reader.ReadToEndAsync();
            }
            return license;
        }

        public async Task SaveLicense(string license)
        {
            using (var writer = new StreamWriter(LicensePath, false))
            {
                await writer.WriteAsync(license);
            }
        }

        public Task<bool> CheckIfValid(string license)
        {
            return Validate(license);
        }

        public bool IsFeatureLicensed(string featureName)
        {
            if (_license == null)
            {
                return false;
            }

            return _license.ProductFeatures.Contains(featureName);
        }
    }
}
