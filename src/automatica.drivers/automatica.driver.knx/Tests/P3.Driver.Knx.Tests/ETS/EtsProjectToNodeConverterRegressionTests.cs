using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using P3.Driver.Knx.DriverFactory;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Knx.Core.Ets;
using Xunit;
using static P3.Driver.Knx.Tests.ETS.EtsTestArchives;

namespace P3.Driver.Knx.Tests.ETS
{
    /// <summary>
    /// ETS import into the KNX node tree with projects built in the test.
    /// </summary>
    public sealed class EtsProjectToNodeConverterRegressionTests : DriverFactoryTestBase<KnxIpDriverFactory>, IDisposable
    {
        private readonly EtsTestArchives _archives = new();

        public void Dispose() => _archives.Dispose();

        private EtsProject Parse(string groupRanges, string style = "ThreeLevel")
        {
            var file = _archives.Zip(Guid.NewGuid().ToString("N") + ".knxproj",
                Text("P-0001/project.xml", ProjectXml("P-0001", "Import", style)),
                Text("P-0001/0.xml", InstallationXml("P-0001", groupRanges)));
            return new EtsProjectParser().ParseEtsFile(file);
        }

        private static int Address(NodeInstance node) => node.GetPropertyValueInt("knx-address");

        [Fact]
        public async Task ConvertToNodeInstances_MinimalProject_CreatesTheGroupTree()
        {
            var file = _archives.Zip("mini.knxproj", MinimalProjectEntries().ToArray());
            var project = new EtsProjectParser().ParseEtsFile(file);
            var knxInterface = CreateNodeInstance(KnxIpDriverFactory.KnxIpGateway3LevelInterface);

            var mains = await EtsProjectToNodeConverter.ConvertToNodeInstances(Factory, project, knxInterface);

            Assert.Equal(new[] { 1, 2 }, mains.Select(Address));
            Assert.Equal(new[] { "Lighting", "Blinds" }, mains.Select(a => a.Name));
            Assert.All(mains, a => Assert.Equal(knxInterface.ObjId, a.This2ParentNodeInstance));

            var lighting = mains[0].InverseThis2ParentNodeInstanceNavigation.ToList();
            Assert.Equal(new[] { 0, 1 }, lighting.Select(Address));
            Assert.Equal(new[] { "Ground floor", "First floor" }, lighting.Select(a => a.Name));

            var groundFloor = lighting[0].InverseThis2ParentNodeInstanceNavigation.ToList();
            Assert.Equal(new[] { 1, 2 }, groundFloor.Select(Address));
            Assert.Equal(new[] { "knx-dpt1", "knx-dpt5" }, groundFloor.Select(a => a.This2NodeTemplateNavigation.Key));
            Assert.Equal(new[] { "Kitchen switch", "Kitchen dim value" }, groundFloor.Select(a => a.Name));
            Assert.All(groundFloor, a => Assert.Equal(lighting[0].ObjId, a.This2ParentNodeInstance));

            var bath = lighting[1].InverseThis2ParentNodeInstanceNavigation.Single();
            Assert.Equal(10, Address(bath));
            Assert.Equal("knx-dpt9", bath.This2NodeTemplateNavigation.Key);

            var roof = mains[1].InverseThis2ParentNodeInstanceNavigation.Single();
            Assert.Equal(7, Address(roof));
            Assert.Equal(255, Address(roof.InverseThis2ParentNodeInstanceNavigation.Single()));
        }

        [Fact]
        public async Task ConvertToNodeInstances_UnsupportedOrMissingType_IsSkipped()
        {
            var project = Parse(Range("P-0001-0_GR-1", 0, 2047, "Main", Range("P-0001-0_GR-2", 0, 255, "Middle",
                Ga("P-0001-0_GA-1", 1, "no type") +
                Ga("P-0001-0_GA-2", 2, "unknown type", "DPST-999-1") +
                Ga("P-0001-0_GA-3", 3, "catalogue type", "DPST-12-1"))));
            var knxInterface = CreateNodeInstance(KnxIpDriverFactory.KnxIpGateway3LevelInterface);

            var mains = await EtsProjectToNodeConverter.ConvertToNodeInstances(Factory, project, knxInterface);

            var dp = mains.Single().InverseThis2ParentNodeInstanceNavigation.Single().InverseThis2ParentNodeInstanceNavigation.Single();
            Assert.Equal(3, Address(dp));
            Assert.Equal("knx-dpt12", dp.This2NodeTemplateNavigation.Key);
        }

        [Fact]
        public async Task ConvertToNodeInstances_TwoLevelProject_IsNotSupported()
        {
            var project = Parse(Range("P-0001-0_GR-1", 2048, 4095, "Main", Ga("P-0001-0_GA-1", 2049, "ga", "DPST-1-1")), "TwoLevel");

            await Assert.ThrowsAsync<NotImplementedException>(() =>
                EtsProjectToNodeConverter.ConvertToNodeInstances(Factory, project, CreateNodeInstance(KnxIpDriverFactory.KnxIpGateway3LevelInterface)));
        }

        [Fact]
        public async Task ConvertToNodeInstances_ExistingMainGroup_IsReused()
        {
            var project = Parse(Range("P-0001-0_GR-1", 2048, 4095, "Renamed", Range("P-0001-0_GR-2", 2048, 2303, "Middle", Ga("P-0001-0_GA-1", 2049, "ga", "DPST-1-1"))));
            var knxInterface = CreateNodeInstance(KnxIpDriverFactory.KnxIpGateway3LevelInterface);
            var existing = CreateNodeInstance(KnxIpDriverFactory.KnxIp3LevelMainAddress);
            existing.SetProperty("knx-address", 1);
            knxInterface.InverseThis2ParentNodeInstanceNavigation.Add(existing);

            var mains = await EtsProjectToNodeConverter.ConvertToNodeInstances(Factory, project, knxInterface);

            Assert.Same(existing, mains.Single());
            Assert.Equal("Renamed", existing.Name);
        }

        [Fact(Skip = "BUG: EtsProjectToNodeConverter looks up existing group address nodes by the full 16 bit address (dp.Address) but stores dp.GetAddress(), a second import duplicates every group address")]
        public async Task ConvertToNodeInstances_SecondImport_DoesNotDuplicateGroupAddresses()
        {
            var file = _archives.Zip("mini.knxproj", MinimalProjectEntries().ToArray());
            var knxInterface = CreateNodeInstance(KnxIpDriverFactory.KnxIpGateway3LevelInterface);
            var first = await EtsProjectToNodeConverter.ConvertToNodeInstances(Factory, new EtsProjectParser().ParseEtsFile(file), knxInterface);
            foreach (var main in first)
            {
                knxInterface.InverseThis2ParentNodeInstanceNavigation.Add(main);
            }

            await EtsProjectToNodeConverter.ConvertToNodeInstances(Factory, new EtsProjectParser().ParseEtsFile(file), knxInterface);

            var lighting = knxInterface.InverseThis2ParentNodeInstanceNavigation.Single(a => Address(a) == 1);
            Assert.Equal(2, lighting.InverseThis2ParentNodeInstanceNavigation.Count);
            var groundFloor = lighting.InverseThis2ParentNodeInstanceNavigation.Single(a => Address(a) == 0);
            Assert.Equal(new[] { 1, 2 }, groundFloor.InverseThis2ParentNodeInstanceNavigation.Select(Address).OrderBy(a => a));
        }

        [Fact(Skip = "BUG: EtsProjectToNodeConverter sets knx-address from EtsEntity.GetMainGroup, which masks with 0x7800, main groups 16-31 are imported as 0-15")]
        public async Task ConvertToNodeInstances_MainGroupAbove15_KeepsTheMainGroupAddress()
        {
            var project = Parse(Range("P-0001-0_GR-1", 20 * 2048, 21 * 2048 - 1, "Main 20",
                Range("P-0001-0_GR-2", 20 * 2048, 20 * 2048 + 255, "Middle 0", Ga("P-0001-0_GA-1", 20 * 2048 + 1, "ga", "DPST-1-1"))));

            var mains = await EtsProjectToNodeConverter.ConvertToNodeInstances(Factory, project, CreateNodeInstance(KnxIpDriverFactory.KnxIpGateway3LevelInterface));

            Assert.Equal(20, Address(mains.Single()));
        }
    }
}
