using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using P3.Knx.Core.Ets;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.License;
using Microsoft.Extensions.Logging;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;

namespace P3.Driver.Knx.DriverFactory.ThreeLevel
{
    public class Knx3Level: DriverNoneAttributeBase
    {
        private readonly KnxDriver _driver;
        private readonly IList<KnxMainGroup> _mainGroups;

        internal const string EtsImportFeatureName = "knx-ets-import";

        public Knx3Level(IDriverContext driverContext, KnxDriver driver) : base(driverContext)
        {
            _driver = driver;
            _mainGroups = new List<KnxMainGroup>();
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var m = new KnxMainGroup(ctx,  _driver);

            _mainGroups.Add(m);
            return m;
        }

        internal void ConnectionEstablished()
        {
            foreach(var m in _mainGroups)
            {
                m.ConnectionEstablished();
            }
        }

        public override async Task<IList<NodeInstance>> Import(ImportConfig config, CancellationToken token = new())
        {
            if (!DriverContext.LicenseContract.IsFeatureLicensed(EtsImportFeatureName))
            {
                DriverContext.Logger.LogError($"Import feature is not licensed....");
                throw new FeatureNotLicensedException(EtsImportFeatureName);
            }

            var file = ResolveImportFile(Path.GetTempPath(), config.FileName);
            if (file == null || !File.Exists(file))
            {
                throw new FileNotFoundException();
            }

            var project = new EtsProjectParser().ParseEtsFile(file, config.Password, GroupAddressStyle.ThreeLevel);

            return await EtsProjectToNodeConverter.ConvertToNodeInstances(DriverContext.NodeTemplateFactory, project, DriverContext.NodeInstance, token);
        }

        /// <summary>
        /// The uploaded project must be a file directly in the temp directory. Older servers pass the bare file name,
        /// newer ones the full path of the upload.
        /// </summary>
        /// <returns>the full path, or null if the name points somewhere else</returns>
        internal static string ResolveImportFile(string tempDirectory, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOf('\0') >= 0)
            {
                return null;
            }

            var root = Path.GetFullPath(tempDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            string candidate;
            try
            {
                candidate = Path.GetFullPath(Path.IsPathRooted(fileName) ? fileName : Path.Combine(root, fileName));
            }
            catch (System.Exception e) when (e is System.ArgumentException || e is System.NotSupportedException || e is PathTooLongException)
            {
                return null;
            }

            var directory = Path.GetDirectoryName(candidate);
            var comparison = System.OperatingSystem.IsWindows() ? System.StringComparison.OrdinalIgnoreCase : System.StringComparison.Ordinal;

            if (directory == null || !string.Equals(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), root, comparison))
            {
                return null;
            }

            return candidate;
        }
    }
}
