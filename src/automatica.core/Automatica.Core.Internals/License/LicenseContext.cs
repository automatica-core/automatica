using Automatica.Core.Base.License;
using Standard.Licensing;
using Standard.Licensing.Validation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.Internals.Cloud;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using AsyncKeyedLock;

namespace Automatica.Core.Internals.License
{
    public sealed class LicenseContext : ILicenseContext
    {
        private readonly ICloudApi _cloudApi;
        private readonly ILogger<LicenseContext> _logger;
        private int _dataPointsInUse;

        public const string LicenseFileName = ".automatica.core.lic";
        public string LicensePath { get; }

        public IList<IValidationFailure> ValidationErrors { get; private set; }

        public bool IsLicensed { get; private set; }

        public int MaxDataPoints { get; private set; }

        public int MaxUsers { get; private set; }

        public bool AllowRemoteControl { get; private set; }
        public int MaxRemoteTunnels { get; private set; }
        public long MaxRecordingDataPoints { get; private set; }
        public int MaxSatellites { get; private set; }
        public bool AllowTextToSpeech { get; private set; }

        private readonly AsyncNonKeyedLocker _semaphore = new(1);

        public bool DriverLicenseCountExceeded()
        {
            return _dataPointsInUse > MaxDataPoints;
        }

        public void IncrementDriverCount()
        {
            ++_dataPointsInUse;
        }

        public void DecrementDriverCount(int count)
        {
            _dataPointsInUse -= count;
        }

        public List<string> LicensedFeatures { get; set; }

        private Standard.Licensing.License _license;

        public LicenseContext(ICloudApi cloudApi, ILogger<LicenseContext> logger)
        {
            _cloudApi = cloudApi;
            _logger = logger;
            var path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            var fileName = Path.Combine(path, LicenseFileName);

            LicensePath = fileName;
        }

        public async Task<bool> Init()
        {
            using (await _semaphore.LockAsync())
            {
                _dataPointsInUse = 0;
                string pubKey = "";
                using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Automatica.Core.Internals.pub.txt") ?? throw new InvalidOperationException()))
                {
                    pubKey = await reader.ReadToEndAsync();
                }

                try
                {

                    var licenseString = await _cloudApi.GetLicense();

                    if (!String.IsNullOrEmpty(licenseString))
                    {
                        await using var file = new StreamWriter(LicensePath);
                        await file.WriteAsync(licenseString);
                        await file.FlushAsync();
                        file.Close();
                    }


                    if (File.Exists(LicensePath))
                    {
                        await using (var file = File.OpenRead(LicensePath))
                        {

                            var license = Standard.Licensing.License.Load(file);
                            if (license.Id != ServerInfo.ServerUid)
                            {
                                await file.DisposeAsync();
                                File.Delete(LicensePath);
                                return false;
                            }

                            var validationFailures = license.Validate().ExpirationDate().And()
                                .Signature(pubKey).AssertValidLicense();
                            ValidationErrors = validationFailures.ToList();
                            _license = license;
                        }

                        IsLicensed = ValidationErrors.Count == 0;

                        if (ValidationErrors.Count > 0)
                        {
                            _logger.LogError("License validation failed");

                            foreach (var validationError in ValidationErrors)
                            {
                                _logger.LogError(validationError.Message);
                            }
                        }
                    }
                }
                catch (System.Xml.XmlException)
                {
                    File.Delete(LicensePath);
                    IsLicensed = false;
                }
                catch (Exception e)
                {
                    IsLicensed = false;
                    _logger.LogError(e, "License validation failed");
                }

                if (IsLicensed && _license is { ProductFeatures: not null })
                {
                    MaxDataPoints = Convert.ToInt32(_license.ProductFeatures.Get("MaxDatapoints"));
                    MaxUsers = Convert.ToInt32(_license.ProductFeatures.Get("MaxUsers"));
                    if (_license.ProductFeatures.Contains("AllowRemoteControl"))
                    {
                        AllowRemoteControl = Convert.ToBoolean(_license.ProductFeatures.Get("AllowRemoteControl"));
                    }

                    if (_license.ProductFeatures.Contains("MaxRemoteTunnels"))
                    {
                        MaxRemoteTunnels = Convert.ToInt32(_license.ProductFeatures.Get("MaxRemoteTunnels"));
                    }

                    if (_license.ProductFeatures.Contains("MaxRecordingDataPoints"))
                    {
                        MaxRecordingDataPoints = Convert.ToInt64(_license.ProductFeatures.Get("MaxRecordingDataPoints"));
                    }
                    if (_license.ProductFeatures.Contains("MaxSatellites"))
                    {
                        MaxSatellites = Convert.ToInt32(_license.ProductFeatures.Get("MaxSatellites"));
                    }
                    if (_license.ProductFeatures.Contains("AllowTextToSpeech"))
                    {
                        AllowTextToSpeech = Convert.ToBoolean(_license.ProductFeatures.Get("AllowTextToSpeech"));
                    }


                    if (!AllowRemoteControl)
                    {
                        MaxRemoteTunnels = 0;
                    }
                }
                else
                {
                    MaxSatellites = 1;
                    MaxDataPoints = 100;
                    MaxUsers = 5;
                    MaxRemoteTunnels = 0;
                    AllowRemoteControl = false;
                    IsLicensed = true;
                    MaxRecordingDataPoints = 50;
                    AllowTextToSpeech = false;
                }

                _logger.LogInformation($"System is licensed to {_license?.Customer?.Email}:");
                _logger.LogInformation($"{nameof(MaxDataPoints)}: {MaxDataPoints}");
                _logger.LogInformation($"{nameof(MaxUsers)}: {MaxUsers}");
                _logger.LogInformation($"{nameof(MaxRemoteTunnels)}: {MaxRemoteTunnels}");
                _logger.LogInformation($"{nameof(AllowRemoteControl)}: {AllowRemoteControl}");
                _logger.LogInformation($"{nameof(MaxRecordingDataPoints)}: {MaxRecordingDataPoints}");
                _logger.LogInformation($"{nameof(MaxSatellites)}: {MaxSatellites}");
                _logger.LogInformation($"{nameof(AllowTextToSpeech)}: {AllowTextToSpeech}");
                _logger.LogInformation($"{nameof(IsLicensed)}: {IsLicensed}");
                _logger.LogInformation($"Features: {JsonConvert.SerializeObject(_license?.ProductFeatures)}");

                return IsLicensed;
            }
        }

        private async Task<bool> Validate(string license)
        {
            string pubKey;
            using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Automatica.Core.Internals.pub.txt") ?? throw new InvalidOperationException()))
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
            if (File.Exists(LicensePath))
            {
                using var reader = new StreamReader(LicensePath);
                var license = await reader.ReadToEndAsync();
                return license;
            }

            return null;
        }

        public async Task SaveLicense(string license)
        {
            await using var writer = new StreamWriter(LicensePath, false);
            await writer.WriteAsync(license);
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
