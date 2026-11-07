using System;
using System.IO;
using System.Linq;
using System.Xml;
using P3.Knx.Core.Ets;
using Xunit;
using static P3.Knx.Core.Ets.Internal.Tests.EtsTestArchives;

namespace P3.Knx.Core.Ets.Internal.Tests
{
    /// <summary>
    /// Parses ETS project archives built in the test (structure, group addresses, datapoint types and the error cases).
    /// </summary>
    public sealed class EtsProjectParserTests : IDisposable
    {
        private readonly EtsTestArchives _archives = new();

        public void Dispose() => _archives.Dispose();

        private string MinimalProject(string style = "ThreeLevel") => _archives.Zip("mini.knxproj", MinimalProjectEntries(style: style).ToArray());

        private static EtsDatapoint[] AllDatapoints(EtsGroup group) =>
            group.Datapoints.Concat(group.SubGroups.SelectMany(AllDatapoints)).ToArray();

        [Fact]
        public void ParseEtsFile_MinimalThreeLevelProject_ReturnsTheExpectedGroupAddresses()
        {
            var project = new EtsProjectParser().ParseEtsFile(MinimalProject());

            var addresses = AllDatapoints(project).Select(a => a.GetGroupAddress()).ToArray();

            Assert.Equal(new[] { "1/0/1", "1/0/2", "1/1/10", "2/7/255" }, addresses);
        }

        [Fact]
        public void ParseEtsFile_MinimalThreeLevelProject_ReadsNameStyleAndStructure()
        {
            var project = new EtsProjectParser().ParseEtsFile(MinimalProject());

            Assert.Equal("Mini", project.Name);
            Assert.Equal(GroupAddressStyle.ThreeLevel, project.GroupAddressStyle);
            Assert.Equal(2, project.Children.Count);

            var lighting = Assert.IsType<EtsGroup>(project.Children[0]);
            Assert.Equal("Lighting", lighting.Name);
            Assert.Equal("all lights", lighting.Description);
            Assert.Equal(1, lighting.GroupIndex);
            Assert.Equal(1, lighting.GetMainGroup());
            Assert.Equal(new[] { "Ground floor", "First floor" }, lighting.SubGroups.Select(a => a.Name));
            Assert.Equal(new[] { 0, 1 }, lighting.SubGroups.Select(a => a.GroupIndex));

            var blinds = Assert.IsType<EtsGroup>(project.Children[1]);
            Assert.Equal(2, blinds.GroupIndex);
            var roof = blinds.SubGroups.Single();
            Assert.Equal(7, roof.GroupIndex);
            Assert.Equal(7, roof.GetMiddleGroup());
            Assert.Equal(255, roof.Datapoints.Single().GroupIndex);
        }

        [Fact]
        public void ParseEtsFile_MinimalThreeLevelProject_KeepsTheDatapointTypes()
        {
            var project = new EtsProjectParser().ParseEtsFile(MinimalProject());
            var dps = AllDatapoints(project).ToDictionary(a => a.GetGroupAddress());

            Assert.Equal(new[] { "DPST-1-1" }, dps["1/0/1"].DatapointTypes);
            Assert.Equal(Tuple.Create(5, 1), dps["1/0/2"].DatapointTypesSplitted.Single());
            Assert.Equal(Tuple.Create(9, 1), dps["1/1/10"].DatapointTypesSplitted.Single());
            Assert.Equal(Tuple.Create(1, 8), dps["2/7/255"].DatapointTypesSplitted.Single());
            Assert.Equal("Bath temperature", dps["1/1/10"].Name);
            Assert.Equal(2314, dps["1/1/10"].Address);
            Assert.Equal(10, dps["1/1/10"].GetAddress());
        }

        [Fact]
        public void ParseEtsFile_GroupAddressConnectedToADevice_HasDeviceLinked()
        {
            var project = new EtsProjectParser().ParseEtsFile(MinimalProject());
            var dps = AllDatapoints(project).ToDictionary(a => a.GetGroupAddress());

            Assert.True(dps["1/0/1"].HasDeviceLinked);
            Assert.False(dps["1/0/2"].HasDeviceLinked);
            Assert.False(dps["2/7/255"].HasDeviceLinked);
        }

        [Fact]
        public void ParseEtsFile_TypeOnlyOnTheComObjectInstanceRef_IsTakenFromThere()
        {
            var ranges = Range("P-0001-0_GR-1", 0, 2047, "Main", Range("P-0001-0_GR-2", 0, 255, "Middle", Ga("P-0001-0_GA-1", 1, "no type")));
            var devices = Device("P-0001-0_DI-1", ("M-0083_A-0001_O-1_R-1", "DPST-14-56", "P-0001-0_GA-1", false));
            var file = _archives.Zip("coir.knxproj",
                Text("P-0001/project.xml", ProjectXml("P-0001", "Coir", "ThreeLevel")),
                Text("P-0001/0.xml", InstallationXml("P-0001", ranges, devices)));

            var dp = AllDatapoints(new EtsProjectParser().ParseEtsFile(file)).Single();

            Assert.Equal(new[] { "DPST-14-56" }, dp.DatapointTypes);
            Assert.True(dp.HasDeviceLinked); // a receive connector also links the device
        }

        [Fact]
        public void ParseEtsFile_TypeOnlyInTheManufacturerData_IsResolvedFromTheComObjectRef()
        {
            var ranges = Range("P-0001-0_GR-1", 0, 2047, "Main", Range("P-0001-0_GR-2", 0, 255, "Middle", Ga("P-0001-0_GA-1", 1, "from manufacturer")));
            var devices = Device("P-0001-0_DI-1", ("M-0083_A-0001_O-2_R-2", null, "P-0001-0_GA-1", true));
            var manufacturer = $"<KNX xmlns=\"{Ns}\"><ManufacturerData><Manufacturer RefId=\"M-0083\"><ApplicationPrograms><ApplicationProgram Id=\"M-0083_A-0001\"><Static><ComObjectRefs>" +
                               "<ComObjectRef Id=\"M-0083_A-0001_O-2_R-2\" DatapointType=\"DPST-9-1\" />" +
                               "</ComObjectRefs></Static></ApplicationProgram></ApplicationPrograms></Manufacturer></ManufacturerData></KNX>";
            var file = _archives.Zip("manufacturer.knxproj",
                Text("P-0001/project.xml", ProjectXml("P-0001", "Manufacturer", "ThreeLevel")),
                Text("P-0001/0.xml", InstallationXml("P-0001", ranges, devices)),
                Text("M-0083/M-0083_A-0001.xml", manufacturer));

            var dp = AllDatapoints(new EtsProjectParser().ParseEtsFile(file)).Single();

            Assert.Equal(new[] { "DPST-9-1" }, dp.DatapointTypes);
        }

        [Fact]
        public void ParseEtsFile_OnlyAnObjectSizeOnTheComObject_SetsTheDatapointSize()
        {
            var ranges = Range("P-0001-0_GR-1", 0, 2047, "Main", Range("P-0001-0_GR-2", 0, 255, "Middle", Ga("P-0001-0_GA-1", 1, "size only")));
            var devices = Device("P-0001-0_DI-1", ("M-0083_A-0001_O-3_R-7", null, "P-0001-0_GA-1", true));
            var manufacturer = $"<KNX xmlns=\"{Ns}\"><ManufacturerData><Manufacturer RefId=\"M-0083\"><ApplicationPrograms><ApplicationProgram Id=\"M-0083_A-0001\"><Static>" +
                               "<ComObjectTable><ComObject Id=\"M-0083_A-0001_O-3\" ObjectSize=\"2 Bytes\" /></ComObjectTable>" +
                               "<ComObjectRefs><ComObjectRef Id=\"M-0083_A-0001_O-3_R-7\" /></ComObjectRefs>" +
                               "</Static></ApplicationProgram></ApplicationPrograms></Manufacturer></ManufacturerData></KNX>";
            var file = _archives.Zip("size.knxproj",
                Text("P-0001/project.xml", ProjectXml("P-0001", "Size", "ThreeLevel")),
                Text("P-0001/0.xml", InstallationXml("P-0001", ranges, devices)),
                Text("M-0083/M-0083_A-0001.xml", manufacturer));

            var dp = AllDatapoints(new EtsProjectParser().ParseEtsFile(file)).Single();

            Assert.Empty(dp.DatapointTypes);
            Assert.Equal(16, dp.DatapointSizeBits);
        }

        [Theory]
        [InlineData("1 Bit", 1)]
        [InlineData("4 Bit", 4)]
        [InlineData("1 Byte", 8)]
        [InlineData("14 Bytes", 112)]
        public void AddDatapointTypeOrSize_ObjectSize_IsConvertedToBits(string size, int expectedBits)
        {
            var main = new EtsGroup(new System.Xml.Linq.XElement("GroupRange", new System.Xml.Linq.XAttribute("RangeStart", "0")), null);
            var middle = new EtsGroup(new System.Xml.Linq.XElement("GroupRange", new System.Xml.Linq.XAttribute("RangeStart", "0")), main);
            var dp = new EtsDatapoint(new System.Xml.Linq.XElement("GroupAddress", new System.Xml.Linq.XAttribute("Address", "1")), middle);

            dp.AddDatapointTypeOrSize(size);
            dp.AddDatapointTypeOrSize("16 Bytes"); // the smallest size wins

            Assert.Equal(expectedBits, dp.DatapointSizeBits);
        }

        [Fact]
        public void ParseEtsFile_MultipleTypesOnOneGroupAddress_AreSplit()
        {
            var ranges = Range("P-0001-0_GR-1", 0, 2047, "Main", Range("P-0001-0_GR-2", 0, 255, "Middle", Ga("P-0001-0_GA-1", 1, "multi", "DPST-1-1 DPST-1-2")));
            var file = _archives.Zip("multi.knxproj",
                Text("P-0001/project.xml", ProjectXml("P-0001", "Multi", "ThreeLevel")),
                Text("P-0001/0.xml", InstallationXml("P-0001", ranges)));

            var dp = AllDatapoints(new EtsProjectParser().ParseEtsFile(file)).Single();

            Assert.Equal(new[] { Tuple.Create(1, 1), Tuple.Create(1, 2) }, dp.DatapointTypesSplitted);
        }

        [Fact]
        public void ParseEtsFile_TwoLevelProject_UsesTheSubAddressAsGroupIndex()
        {
            var ranges = Range("P-0001-0_GR-1", 2048, 4095, "Main 1", Ga("P-0001-0_GA-1", 2048 + 1500, "two level"));
            var file = _archives.Zip("twolevel.knxproj",
                Text("P-0001/project.xml", ProjectXml("P-0001", "Two", "TwoLevel")),
                Text("P-0001/0.xml", InstallationXml("P-0001", ranges)));

            var project = new EtsProjectParser().ParseEtsFile(file);

            Assert.Equal(GroupAddressStyle.TwoLevel, project.GroupAddressStyle);
            var main = (EtsGroup)project.Children.Single();
            Assert.Equal(1, main.GroupIndex);
            Assert.Equal(1500, main.Datapoints.Single().GroupIndex);
            Assert.Equal(string.Empty, main.Datapoints.Single().GetGroupAddress()); // no three level address
        }

        [Theory]
        [InlineData("Free", GroupAddressStyle.Free)]
        [InlineData("SomethingNew", GroupAddressStyle.Free)]
        public void ParseEtsFile_OtherGroupAddressStyles_AreFree(string style, GroupAddressStyle expected)
        {
            var file = _archives.Zip("free.knxproj",
                Text("P-0001/project.xml", ProjectXml("P-0001", "Free", style)),
                Text("P-0001/0.xml", InstallationXml("P-0001", "")));

            Assert.Equal(expected, new EtsProjectParser().ParseEtsFile(file).GroupAddressStyle);
        }

        [Fact]
        public void ParseEtsFile_UnexpectedGroupAddressStyle_ThrowsWrongGroupAddressStyle()
        {
            var ex = Assert.Throws<EtsProjectParserWrongGroupAddressStyleException>(() =>
                new EtsProjectParser().ParseEtsFile(MinimalProject("TwoLevel"), null, GroupAddressStyle.ThreeLevel));

            Assert.Equal(GroupAddressStyle.ThreeLevel, ex.Expected);
            Assert.Equal(GroupAddressStyle.TwoLevel, ex.Actual);
            Assert.Equal("KNX.ETS.WRONG_GROUP_STYLE", ex.Message);
        }

        [Fact]
        public void ParseEtsFile_ExpectedGroupAddressStyle_IsAccepted()
        {
            var project = new EtsProjectParser().ParseEtsFile(MinimalProject(), null, GroupAddressStyle.ThreeLevel);

            Assert.Equal(GroupAddressStyle.ThreeLevel, project.GroupAddressStyle);
        }

        [Fact]
        public void ParseEtsFile_ArchiveWithoutProject_ThrowsEmptyProject()
        {
            var file = _archives.Zip("noproject.knxproj", Text("knx_master.xml", $"<KNX xmlns=\"{Ns}\" />"), Text("M-0083/M-0083_A-0001.xml", $"<KNX xmlns=\"{Ns}\" />"));

            var ex = Assert.Throws<EtsProjectParserEmptyProjectFileException>(() => new EtsProjectParser().ParseEtsFile(file));
            Assert.Equal("KNX.ETS.EMPTY_PROJECT", ex.Message);
        }

        [Fact]
        public void ParseEtsFile_TwoProjects_ThrowsTooManyProjects()
        {
            var entries = MinimalProjectEntries("P-0001").Concat(MinimalProjectEntries("P-0002").Skip(1)).ToArray();
            var file = _archives.Zip("two.knxproj", entries);

            Assert.Throws<EtsProjectParserTooManyProjectsInFileException>(() => new EtsProjectParser().ParseEtsFile(file));
        }

        [Fact]
        public void ParseEtsFile_XmlOutsideAProjectDirectory_IsIgnored()
        {
            // only files directly in P-* directories are project files, UserFiles are never parsed
            var entries = MinimalProjectEntries().Concat(new[]
            {
                Text("P-0001/UserFiles/other.xml", ProjectXml("P-0009", "Other", "TwoLevel")),
                Text("other.xml", ProjectXml("P-0008", "Other", "TwoLevel")),
                Text("X-0001/project.xml", ProjectXml("P-0007", "Other", "TwoLevel")),
            }).ToArray();
            var file = _archives.Zip("ignored.knxproj", entries);

            var project = new EtsProjectParser().ParseEtsFile(file);

            Assert.Equal("Mini", project.Name);
        }

        [Fact]
        public void ParseEtsFile_EmptyArchive_ThrowsInvalidZipFile()
        {
            var file = _archives.Zip("empty.knxproj");

            var ex = Assert.Throws<EtsProjectParserInvalidZipFileException>(() => new EtsProjectParser().ParseEtsFile(file));
            Assert.Equal("KNX.ETS.ZIPFILE_ERROR", ex.Message);
        }

        [Fact]
        public void ParseEtsFile_ZeroByteFile_ThrowsInvalidZipFile()
        {
            var file = Path.Combine(_archives.WorkDir, "zero.knxproj");
            File.WriteAllBytes(file, Array.Empty<byte>());

            Assert.Throws<EtsProjectParserInvalidZipFileException>(() => new EtsProjectParser().ParseEtsFile(file));
        }

        [Fact]
        public void ParseEtsFile_NotAZipFile_ThrowsInvalidZipFile()
        {
            var file = Path.Combine(_archives.WorkDir, "text.knxproj");
            File.WriteAllText(file, "this is not a zip file, but long enough to look for a central directory .......");

            Assert.Throws<EtsProjectParserInvalidZipFileException>(() => new EtsProjectParser().ParseEtsFile(file));
        }

        [Fact]
        public void ParseEtsFile_MissingFile_ThrowsParserException()
        {
            var ex = Assert.ThrowsAny<EtsProjectParserException>(() => new EtsProjectParser().ParseEtsFile(Path.Combine(_archives.WorkDir, "missing.knxproj")));

            Assert.IsAssignableFrom<IOException>(ex.InnerException);
        }

        [Fact]
        public void ParseEtsFile_InvalidXml_ThrowsParserException()
        {
            var file = _archives.Zip("broken.knxproj", Text("P-0001/0.xml", "<KNX><Project Id=\"P-0001\">"));

            var ex = Assert.ThrowsAny<EtsProjectParserException>(() => new EtsProjectParser().ParseEtsFile(file));
            Assert.IsType<XmlException>(ex.InnerException);
        }

        [Fact]
        public void ParseEtsFile_ExternalEntity_IsNotResolved()
        {
            var secret = Path.Combine(_archives.WorkDir, "secret.txt");
            File.WriteAllText(secret, "TOP-SECRET");
            var xml = $"<?xml version=\"1.0\"?><!DOCTYPE KNX [<!ENTITY xxe SYSTEM \"{new Uri(secret).AbsoluteUri}\">]>" +
                      $"<KNX xmlns=\"{Ns}\"><Project Id=\"P-0001\"><ProjectInformation Name=\"&xxe;\" GroupAddressStyle=\"ThreeLevel\" /></Project></KNX>";
            var file = _archives.Zip("xxe.knxproj", Text("P-0001/project.xml", xml));

            var ex = Assert.ThrowsAny<EtsProjectParserException>(() => new EtsProjectParser().ParseEtsFile(file));

            Assert.IsType<XmlException>(ex.InnerException);
            Assert.DoesNotContain("TOP-SECRET", ex.ToString());
        }

        [Fact]
        public void ParseEtsFile_EntityExpansion_IsRejected()
        {
            var xml = "<?xml version=\"1.0\"?><!DOCTYPE KNX [<!ENTITY a \"aaaaaaaaaa\"><!ENTITY b \"&a;&a;&a;&a;&a;&a;&a;&a;&a;&a;\">]>" +
                      $"<KNX xmlns=\"{Ns}\"><Project Id=\"P-0001\"><ProjectInformation Name=\"&b;\" GroupAddressStyle=\"ThreeLevel\" /></Project></KNX>";
            var file = _archives.Zip("billion.knxproj", Text("P-0001/project.xml", xml));

            var ex = Assert.ThrowsAny<EtsProjectParserException>(() => new EtsProjectParser().ParseEtsFile(file));
            Assert.IsType<XmlException>(ex.InnerException);
        }

        [Fact]
        public void ParseEtsFile_SameParserInstance_CanBeReused()
        {
            var parser = new EtsProjectParser();
            var file = MinimalProject();

            Assert.Throws<EtsProjectParserInvalidZipFileException>(() => parser.ParseEtsFile(_archives.Zip("empty.knxproj")));
            var first = parser.ParseEtsFile(file);
            var second = parser.ParseEtsFile(file);

            Assert.Equal(4, AllDatapoints(first).Length);
            Assert.Equal(4, AllDatapoints(second).Length); // no state of the first run leaks into the second
        }

        [Fact(Skip = "BUG: EtsEntity.GetMainGroup masks the range start with 0x7800 (4 bits), three level main groups 16-31 come out as 0-15")]
        public void ParseEtsFile_MainGroupAbove15_KeepsTheMainGroup()
        {
            // main group 20 = range start 20 * 2048, middle group 20/3, group address 20/3/4
            var ranges = Range("P-0001-0_GR-1", 40960, 43007, "Main 20", Range("P-0001-0_GR-2", 40960 + 768, 40960 + 1023, "Middle 3", Ga("P-0001-0_GA-1", 40960 + 768 + 4, "high")));
            var file = _archives.Zip("main20.knxproj",
                Text("P-0001/project.xml", ProjectXml("P-0001", "High", "ThreeLevel")),
                Text("P-0001/0.xml", InstallationXml("P-0001", ranges)));

            var project = new EtsProjectParser().ParseEtsFile(file);
            var main = (EtsGroup)project.Children.Single();

            Assert.Equal(20, main.GroupIndex);
            Assert.Equal(20, main.GetMainGroup());
            Assert.Equal("20/3/4", AllDatapoints(project).Single().GetGroupAddress());
        }
    }
}
