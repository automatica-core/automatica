using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using P3.Knx.Core.Ets;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Automatica.Core.Base.Exceptions;
using P3.Knx.Core.Abstractions;

namespace P3.Driver.Knx.DriverFactory.ThreeLevel
{
    public class Knx3Level: DriverBase
    {
        private readonly IKnxDriver _driver;
        private readonly IList<KnxMainGroup> _mainGroups;

        internal const string EtsImportFeauterName = "knx-ets-import";

        public Knx3Level(IDriverContext driverContext, IKnxDriver driver) : base(driverContext)
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

        public override async Task<IList<NodeInstance>> Import(string fileName)
        {
            if (!DriverContext.LicenseContract.IsFeatureLicensed(EtsImportFeauterName))
            {
                throw new LicenseInvalidException($"KNX.LICENSE.{EtsImportFeauterName.ToUpper()}");
            }

            var file = Path.Combine(Path.GetTempPath(), fileName);
            if (!File.Exists(file))
            {
                throw new FileNotFoundException();
            }

            var project = new EtsProjectParser().ParseEtsFile(file, GroupAddressStyle.ThreeLevel);
    
            return await EtsProjectToNodeConverter.ConvertToNodeInstances(DriverContext.NodeTemplateFactory, project, DriverContext.NodeInstance);
        }
    }
}
