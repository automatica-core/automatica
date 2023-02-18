using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using P3.Knx.Core.Ets;
using Xunit;

namespace P3.Driver.Knx.Tests.ETS
{
    //Test
    public class ETSProjectParserTest
    {

        [Fact]
        public void TestBuildingStructureAndFunctions()
        {
            var path = GetFile("ETS5_ImportTest");
            EtsProject p = new EtsProjectParser().ParseEtsFile(path);

            Assert.True(p.Buildings.Count == 1);
            Assert.True(p.Buildings[0].Name == "Haus");
            Assert.True(p.Buildings[0].Children[0].Name == "Haus");

            Assert.True(p.Buildings[0].Children[0].Children.Count == 2);
            Assert.True(p.Buildings[0].Children[0].Children[0].Name == "EG");
            Assert.True(p.Buildings[0].Children[0].Children[1].Name == "UG");

            Assert.True(p.Buildings[0].Children[0].Children[0].Children.Count == 1);
            Assert.True(p.Buildings[0].Children[0].Children[0].Children[0].Name == "Raum1");
            Assert.True(p.Buildings[0].Children[0].Children[0].Children[0].Functions.Count == 2);
            Assert.True(p.Buildings[0].Children[0].Children[0].Children[0].Functions[0].EtsFunctionType == EtsFunctionType.LightOnOff);

            Assert.True(p.Buildings[0].Children[0].Children[0].Children[0].Functions[0].GroupAddressReferences[0].Datapoint.GetGroupAddress() == "0/0/1");
            Assert.True(p.Buildings[0].Children[0].Children[0].Children[0].Functions[0].GroupAddressReferences[1].Datapoint.GetGroupAddress() == "0/0/2");

            Assert.True(p.Buildings[0].Children[0].Children[0].Children[0].Functions[1].EtsFunctionType == EtsFunctionType.Blinds);
            Assert.True(p.Buildings[0].Children[0].Children[0].Children[0].Functions[1].GroupAddressReferences[0].Datapoint.GetGroupAddress() == "0/0/3");
            Assert.True(p.Buildings[0].Children[0].Children[0].Children[0].Functions[1].GroupAddressReferences[1].Datapoint.GetGroupAddress() == "0/0/4");
            Assert.True(p.Buildings[0].Children[0].Children[0].Children[0].Functions[1].GroupAddressReferences[2].Datapoint.GetGroupAddress() == "0/0/5");
            Assert.True(p.Buildings[0].Children[0].Children[0].Children[0].Functions[1].GroupAddressReferences[3].Datapoint.GetGroupAddress() == "0/0/6");
            Assert.True(p.Buildings[0].Children[0].Children[0].Children[0].Functions[1].GroupAddressReferences[4].Datapoint.GetGroupAddress() == "0/0/7");
            Assert.True(p.Buildings[0].Children[0].Children[0].Children[0].Functions[1].GroupAddressReferences[5].Datapoint.GetGroupAddress() == "0/0/8");


            Assert.True(p.Buildings[0].Children[0].Children[1].Children.Count == 2);
            Assert.True(p.Buildings[0].Children[0].Children[1].Children[0].Name == "Technik");
            Assert.True(p.Buildings[0].Children[0].Children[1].Children[0].Functions.Count == 1);
            Assert.True(p.Buildings[0].Children[0].Children[1].Children[0].Functions[0].EtsFunctionType == EtsFunctionType.Dimm);

            Assert.True(p.Buildings[0].Children[0].Children[1].Children[1].Name == "Waschraum");
            Assert.True(p.Buildings[0].Children[0].Children[1].Children[1].Functions[0].EtsFunctionType == EtsFunctionType.LightOnOff);

        }

        [Fact]
        public void TestSimpleTwoLevel()
        {
            EtsProject p = new EtsProjectParser().ParseEtsFile(GetFile("ETS5_Simple_TwoLevel"));
            Assert.Equal("ETS5_Simple_TwoLevel", p.Name);
            Assert.Equal(GroupAddressStyle.TwoLevel, p.GroupAddressStyle);
            Assert.Equal(32, p.Children.Count);

            EtsGroup g = p.Children[0] as EtsGroup;
            Assert.Equal("New main group", g.Name);
            Assert.Equal(0, g.GroupIndex);
            Assert.Equal(2047, g.Children.Count);
            Assert.Equal("", g.Description);
            for (int j = 0; j < g.Children.Count; j++)
                Assert.Equal(j + 1, (g.Children[j] as EtsDatapoint).GroupIndex);

            for (int i = 1; i < p.Children.Count; i++)
            {
                g = p.Children[i] as EtsGroup;
                Assert.Equal("New main group", g.Name);
                Assert.Equal(i, g.GroupIndex);
                Assert.Equal(2048, g.Children.Count);
                Assert.Equal("", g.Description);
                for (int j = 0; j < g.Children.Count; j++)
                    Assert.Equal(j, (g.Children[j] as EtsDatapoint).GroupIndex);
            }
        }
        [Fact]
        public void TestSimpleThreeLevel()
        {
            EtsProject p = new EtsProjectParser().ParseEtsFile(GetFile("ETS5_Simple_ThreeLevel"));
            Assert.Equal("ETS5_Simple_ThreeLevel", p.Name);
            Assert.Equal(GroupAddressStyle.ThreeLevel, p.GroupAddressStyle);
            Assert.Equal(4, p.Children.Count);

            EtsGroup g0 = p.Children[0] as EtsGroup;
            Assert.Equal("Main Group 0", g0.Name);
            Assert.Equal(0, g0.GroupIndex);
            Assert.Equal(2, g0.Children.Count);
            Assert.Equal("Description main group\r\n0", g0.Description);

            EtsGroup g0_0 = g0.Children[0] as EtsGroup;
            Assert.Equal("Middle Group 0.0", g0_0.Name);
            Assert.Equal(0, g0_0.GroupIndex);
            Assert.Equal(5, g0_0.Children.Count);
            Assert.Equal("Description middle group\r\n0", g0_0.Description);

            EtsDatapoint dp0_0_1 = g0_0.Children[0] as EtsDatapoint;
            Assert.Equal("Group Address 0.0.1 LED 1 Switch", dp0_0_1.Name);
            Assert.Equal(1, dp0_0_1.GroupIndex);
            AssertDatapointType(dp0_0_1, "DPST-1-1");
            Assert.Equal("Description group address\r\n1 'test'", dp0_0_1.Description);
            Assert.True(dp0_0_1.HasDeviceLinked);

            EtsDatapoint dp0_0_2 = g0_0.Children[1] as EtsDatapoint;
            Assert.Equal("Group Address 0.0.2 Temperature", dp0_0_2.Name);
            Assert.Equal(2, dp0_0_2.GroupIndex);
            AssertDatapointType(dp0_0_2, "DPST-9-1");
            Assert.Equal("", dp0_0_2.Description);
            Assert.True(dp0_0_2.HasDeviceLinked);

            EtsDatapoint dp0_0_3 = g0_0.Children[2] as EtsDatapoint;
            Assert.Equal("Group Address 0.0.3 Min/Max Temperature-State", dp0_0_3.Name);
            Assert.Equal(3, dp0_0_3.GroupIndex);
            AssertDatapointType(dp0_0_3, "DPST-1-1");
            Assert.Equal("", dp0_0_3.Description);
            Assert.True(dp0_0_3.HasDeviceLinked);

            EtsDatapoint dp0_0_4 = g0_0.Children[3] as EtsDatapoint;
            Assert.Equal("Group Address 0.0.4 Boolean Control", dp0_0_4.Name);
            Assert.Equal(4, dp0_0_4.GroupIndex);
            AssertDatapointType(dp0_0_4, "DPST-2-2");
            Assert.Equal("", dp0_0_4.Description);
            Assert.False(dp0_0_4.HasDeviceLinked);

            EtsDatapoint dp0_0_5 = g0_0.Children[4] as EtsDatapoint;
            Assert.Equal("Group Address 0.0.5 No Datatype", dp0_0_5.Name);
            Assert.Equal(5, dp0_0_5.GroupIndex);
            AssertDatapointType(dp0_0_5, "");
            Assert.Equal("", dp0_0_5.Description);
            Assert.False(dp0_0_5.HasDeviceLinked);

            EtsGroup g1 = p.Children[1] as EtsGroup;
            Assert.Equal("Main Group 1", g1.Name);
            Assert.Equal(1, g1.GroupIndex);
            Assert.True(g1.Children.Count == 0);
            Assert.Equal("", g1.Description);

            EtsGroup g2 = p.Children[2] as EtsGroup;
            Assert.Equal("Main Group 2", g2.Name);
            Assert.Equal(2, g2.GroupIndex);
            Assert.Equal(2, g2.Children.Count);
            Assert.Equal("", g2.Description);

            EtsGroup g2_0 = g2.Children[0] as EtsGroup;
            Assert.Equal("Middle Group 2.0", g2_0.Name);
            Assert.Equal(0, g2_0.GroupIndex);
            Assert.True(g2_0.Children.Count == 0);
            Assert.Equal("", g2_0.Description);

            EtsGroup g2_1 = g2.Children[1] as EtsGroup;
            Assert.Equal("Middle Group 2.1", g2_1.Name);
            Assert.Equal(1, g2_1.GroupIndex);
            Assert.True(g2_1.Children.Count == 0);
            Assert.Equal("", g2_1.Description);

            EtsGroup g31 = p.Children[3] as EtsGroup;
            Assert.Equal(31, g31.GroupIndex);
            Assert.True(g31.Children.Count == 1);
            Assert.Equal("", g31.Description);

            EtsGroup g31_7 = g31.Children[0] as EtsGroup;
            Assert.Equal("Middle Group 31.7 Filled Up", g31_7.Name);
            Assert.Equal(7, g31_7.GroupIndex);
            Assert.Equal(256, g31_7.Children.Count);
            Assert.Equal("", g31_7.Description);

            for (int i = 0; i < g31_7.Children.Count; i++)
            {
                Assert.Equal(i, (g31_7.Children[i] as EtsDatapoint).GroupIndex);
                Assert.False((g31_7.Children[i] as EtsDatapoint).HasDeviceLinked);
            }
        }

        [Fact]
        public void TestFailOnPasswordProtectedFile()
        {
            Assert.Throws<EtsProjectParserPasswordRequiredException>(() => new EtsProjectParser().ParseEtsFile(GetFile("ETS5_Password_Protected")));
        }

        [Fact]
        public void TestFailOnInvalidZipFile()
        {
            Assert.Throws<EtsProjectParserInvalidZipFileException>(() => new EtsProjectParser().ParseEtsFile(GetFile("NotAZipFile")));
        }
        private String GetFile(string fileName)
        {
            return Path.Combine("ETS", fileName + ".knxproj");
        }

        private void AssertDatapointType(EtsDatapoint dp, string dpt)
        {
            List<string> dpts = dpt.Any() ? dpt.Split(' ').ToList() : new List<string>();

            Assert.Equal(dpts.Count, dp.DatapointTypes.Count());
            dpts.ForEach(i => Assert.Contains(i, dp.DatapointTypes));
        }
    }
}
