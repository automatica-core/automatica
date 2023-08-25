using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using P3.Knx.Core.Ets;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Automatica.Core.Base.IO;
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
                return new List<NodeInstance>();
            }

            var file = Path.Combine(Path.GetTempPath(), config.FileName);
            if (!File.Exists(file))
            {
                throw new FileNotFoundException();
            }

            var project = new EtsProjectParser().ParseEtsFile(file, config.Password, GroupAddressStyle.ThreeLevel);

            return await EtsProjectToNodeConverter.ConvertToNodeInstances(DriverContext.NodeTemplateFactory, project, DriverContext.NodeInstance, token);
        }
    }
}
