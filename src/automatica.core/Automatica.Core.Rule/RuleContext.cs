using Automatica.Core.Base.IO;
using Automatica.Core.Base.License;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Rule
{
    /// <summary>
    /// Implementation of the <see cref="IRuleContext"/>
    /// </summary>
    public class RuleContext : IRuleContext
    {
        public RuleContext(RuleInstance ruleInstance, IDispatcher dispatcher, IRuleInstanceVisuNotify notify, ILogger logger, IServerCloudApi api, ILicenseContract licenseContract)
        {
            RuleInstance = ruleInstance;
            Dispatcher = dispatcher;
            Notify = notify;
            Logger = logger;
            CloudApi = api;
            LicenseContract = licenseContract;
        }

        public RuleInstance RuleInstance { get; }
        public IDispatcher Dispatcher { get; }
        public IRuleInstanceVisuNotify Notify { get; }

        public ILogger Logger { get; }
        public IServerCloudApi CloudApi { get; }

        public ILicenseContract LicenseContract { get; }
    }
}
