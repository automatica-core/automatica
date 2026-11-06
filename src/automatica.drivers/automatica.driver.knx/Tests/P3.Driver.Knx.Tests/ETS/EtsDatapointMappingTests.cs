using System;
using System.Xml.Linq;
using Automatica.Core.UnitTests.Base.Drivers;
using P3.Driver.Knx.DriverFactory;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Knx.Core.Ets;
using Xunit;

namespace P3.Driver.Knx.Tests.ETS
{
    /// <summary>
    /// Node template and DPT properties created for an ETS group address.
    /// </summary>
    public class EtsDatapointMappingTests : DriverFactoryTestBase<KnxIpDriverFactory>
    {
        private static EtsDatapoint Datapoint(string datapointType)
        {
            var main = new EtsGroup(new XElement("GroupRange", new XAttribute("RangeStart", "2048"), new XAttribute("RangeEnd", "4095")), null);
            var middle = new EtsGroup(new XElement("GroupRange", new XAttribute("RangeStart", "2560"), new XAttribute("RangeEnd", "2815")), main);
            return new EtsDatapoint(new XElement("GroupAddress", new XAttribute("Address", "2563"), new XAttribute("Name", "ga"),
                new XAttribute("DatapointType", datapointType)), middle);
        }

        [Theory]
        [InlineData("DPST-6-20", "knx-dpt6.020")]
        [InlineData("DPST-6-10", "knx-dpt6")]
        [InlineData("DPST-6-1", "knx-dpt6")]
        [InlineData("DPT-6", "knx-dpt6")]
        public void Dpt6StatusWithModeGetsItsOwnNode(string datapointType, string expectedKey)
        {
            var node = EtsProjectToNodeConverter.CreateNodeTemplateFromEtsDataPoint(Datapoint(datapointType), Factory);

            Assert.Equal(expectedKey, node.This2NodeTemplateNavigation.Key);
            Assert.Equal(6, Convert.ToInt32(node.GetProperty("knx-dpt").Value));
        }

        [Theory]
        [InlineData("DPST-5-10", 10)]
        [InlineData("DPST-5-4", 4)]
        [InlineData("DPST-5-1", 1)]
        public void Dpt5SubTypeIsStoredInTheSubTypeProperty(string datapointType, int expectedSubType)
        {
            var node = EtsProjectToNodeConverter.CreateNodeTemplateFromEtsDataPoint(Datapoint(datapointType), Factory);

            Assert.Equal(5, Convert.ToInt32(node.GetProperty("knx-dpt").Value));
            Assert.Equal(expectedSubType, Convert.ToInt32(node.GetProperty("knx-dpt-sub").Value));
        }

        [Theory]
        [InlineData("DPST-7-7", 7)]
        [InlineData("DPST-8-10", 8)]
        [InlineData("DPST-9-1", 9)]
        [InlineData("DPST-10-1", 10)]
        [InlineData("DPST-11-1", 11)]
        public void KnxDptHoldsTheMainType(string datapointType, int expectedMainType)
        {
            var node = EtsProjectToNodeConverter.CreateNodeTemplateFromEtsDataPoint(Datapoint(datapointType), Factory);

            Assert.Equal(expectedMainType, Convert.ToInt32(node.GetProperty("knx-dpt").Value));
        }

        [Theory]
        [InlineData("DPST-16-0", 1)] // 16.000 ASCII
        [InlineData("DPST-16-1", 2)] // 16.001 ISO 8859-1
        [InlineData("DPT-16", 1)]
        public void Dpt16SubTypeIsStoredInTheSubTypeProperty(string datapointType, int expectedSubType)
        {
            var node = EtsProjectToNodeConverter.CreateNodeTemplateFromEtsDataPoint(Datapoint(datapointType), Factory);

            Assert.Equal("knx-dpt16", node.This2NodeTemplateNavigation.Key);
            Assert.Equal(expectedSubType, Convert.ToInt32(node.GetProperty("knx-dpt-sub").Value));
        }
    }
}
