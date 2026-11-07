using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Operations.Gauge.EasyCircular;
using P3.Logic.Operations.Gauge.EasyLinear;
using P3.Logic.Operations.Gauge.RangeCircular;
using Xunit;

namespace P3.Logic.Operations.Tests
{
    public abstract class GaugeTestsBase<T> : LogicTest<T> where T : LogicFactory
    {
        protected abstract string ExpectedGaugeType { get; }
        protected abstract IReadOnlyDictionary<string, object> ExpectedParameters { get; }

        private Automatica.Core.EF.Models.RuleInterfaceInstance ValueInterface =>
            RuleInstance.RuleInterfaceInstance.Single(a => a.This2RuleInterfaceTemplateNavigation.Key == "value");

        private IDictionary<string, object> VisuData => Assert.IsAssignableFrom<IDictionary<string, object>>(Logic.GetDataForVisu());

        [Fact]
        public void Factory_HasExactlyOneValueInput()
        {
            var inputs = RuleInstance.RuleInterfaceInstance
                .Where(a => a.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection == (long)LogicInterfaceDirection.Input)
                .ToList();

            Assert.Single(inputs);
            Assert.Equal("value", inputs[0].This2RuleInterfaceTemplateNavigation.Key);
            Assert.Equal(1, inputs[0].This2RuleInterfaceTemplateNavigation.MaxLinks);
        }

        [Fact]
        public void Factory_HasNoOutputs()
        {
            Assert.DoesNotContain(RuleInstance.RuleInterfaceInstance,
                a => a.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection == (long)LogicInterfaceDirection.Output);
        }

        [Fact]
        public void Factory_InterfaceTemplateIds_AreUnique()
        {
            var ids = RuleInstance.RuleInterfaceInstance.Select(a => a.This2RuleInterfaceTemplate).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }

        [Fact]
        public void GetDataForVisu_ContainsGaugeType()
        {
            Assert.Equal(ExpectedGaugeType, VisuData["gauge_type"]);
        }

        [Fact]
        public void GetDataForVisu_ContainsParameterDefaults()
        {
            var data = VisuData;
            foreach (var (key, expected) in ExpectedParameters)
            {
                Assert.True(data.ContainsKey(key), $"missing {key}");
                Assert.Equal(Convert.ToString(expected), Convert.ToString(data[key]));
            }
        }

        [Fact]
        public void GetDataForVisu_WithoutValue_HasOnlyParameters()
        {
            Assert.False(VisuData.ContainsKey(ValueInterface.ObjId.ToString()));
        }

        [Theory]
        [InlineData(42)]
        [InlineData(-3.5)]
        [InlineData("text")]
        [InlineData(true)]
        [InlineData(null)]
        public void ValueInput_IsForwardedUnchanged(object value)
        {
            var outputs = Logic.ValueChanged(ValueInterface, Dispatchable, value);

            var output = Assert.Single(outputs);
            Assert.Equal(value, output.Value);
            Assert.Equal(ValueInterface.ObjId, output.Instance.Id);
        }

        [Fact]
        public void ValueInput_NaN_IsForwarded()
        {
            var output = Assert.Single(Logic.ValueChanged(ValueInterface, Dispatchable, double.NaN));
            Assert.True(double.IsNaN((double)output.Value));
        }

        [Fact]
        public void ValueInput_LatestValue_IsInVisuData()
        {
            Logic.ValueChanged(ValueInterface, Dispatchable, 10);
            Logic.ValueChanged(ValueInterface, Dispatchable, 20);

            Assert.Equal(20, VisuData[ValueInterface.ObjId.ToString()]);
        }

        [Fact]
        public void ParameterInput_EmitsNothing()
        {
            var param = RuleInstance.RuleInterfaceInstance.First(a =>
                a.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection == (long)LogicInterfaceDirection.Param);

            Assert.Empty(Logic.ValueChanged(param, Dispatchable, 7));
        }
    }

    public class EasyCircularGaugeTests : GaugeTestsBase<EasyCircularControlFactory>
    {
        protected override string ExpectedGaugeType => "1";

        protected override IReadOnlyDictionary<string, object> ExpectedParameters => new Dictionary<string, object>
        {
            ["scale_start"] = 0, ["scale_end"] = 100, ["ticks"] = 1
        };
    }

    public class EasyLinearGaugeTests : GaugeTestsBase<EasyLinearControlFactory>
    {
        protected override string ExpectedGaugeType => "0";

        protected override IReadOnlyDictionary<string, object> ExpectedParameters => new Dictionary<string, object>
        {
            ["scale_start"] = 0, ["scale_end"] = 100, ["ticks"] = 1
        };
    }

    public class RangeCircularGaugeTests : GaugeTestsBase<RangeCircularControlFactory>
    {
        protected override string ExpectedGaugeType => "2";

        protected override IReadOnlyDictionary<string, object> ExpectedParameters => new Dictionary<string, object>
        {
            ["ticks"] = 5,
            ["first_scale_start"] = 0, ["first_scale_end"] = 33, ["first_color"] = "red",
            ["second_scale_start"] = 34, ["second_scale_end"] = 66, ["second_color"] = "orange",
            ["third_scale_start"] = 67, ["third_scale_end"] = 100, ["third_color"] = "green",
        };

        [Fact]
        public void Factory_HasElevenParameters()
        {
            var parameters = RuleInstance.RuleInterfaceInstance.Count(a =>
                a.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection == (long)LogicInterfaceDirection.Param);

            // ticks + 3 x (start, end, color) + gauge type
            Assert.Equal(11, parameters);
        }

        [Fact]
        public void Factory_RangeParameterIds_DifferOnlyInTheLastByte()
        {
            var starts = new[] { "first_scale_start", "second_scale_start", "third_scale_start" }
                .Select(k => RuleInstance.RuleInterfaceInstance.Single(a => a.This2RuleInterfaceTemplateNavigation.Key == k).This2RuleInterfaceTemplate.ToByteArray())
                .ToList();

            Assert.Equal(starts[0][..15], starts[1][..15]);
            Assert.Equal(starts[0][..15], starts[2][..15]);
            Assert.Equal(starts[0][15] + 1, starts[1][15]);
            Assert.Equal(starts[0][15] + 2, starts[2][15]);
            Assert.Equal(new Guid("01bc8d9c-167d-4877-8942-3a7f7bbc399d"), new Guid(starts[0]));
        }
    }

    public class OperationsFactoryTests
    {
        public static IEnumerable<object[]> Factories => new List<object[]>
        {
            new object[] { new Dimmer.DimmerLogicFactory() },
            new object[] { new Slider.SliderLogicFactory() },
            new object[] { new Shutter.ShutterLogicFactory() },
            new object[] { new Switch.SwitchLogicFactory() },
            new object[] { new Push.PushLogicFactory() },
            new object[] { new EasyCircularControlFactory() },
            new object[] { new EasyLinearControlFactory() },
            new object[] { new RangeCircularControlFactory() },
        };

        [Fact]
        public void AllFactories_HaveUniqueLogicGuidsAndNames()
        {
            var factories = Factories.Select(a => (LogicFactory)a[0]).ToList();

            Assert.Equal(factories.Count, factories.Select(a => a.LogicGuid).Distinct().Count());
            Assert.Equal(factories.Count, factories.Select(a => a.LogicName).Distinct().Count());
            Assert.All(factories, f => Assert.StartsWith("P3.Logic.Operations.", f.LogicName));
        }

        [Fact]
        public void AllFactories_InterfaceTemplateIds_AreUniqueAcrossLogics()
        {
            var mock = new LogicTemplateFactoryMock();
            var ids = new List<Guid>();
            foreach (var factory in Factories.Select(a => (LogicFactory)a[0]))
            {
                factory.InitTemplates(mock);
                ids.AddRange(mock.GetById(factory.LogicGuid).RuleInterfaceTemplate.Select(a => a.ObjId));
            }

            Assert.Equal(ids.Count, ids.Distinct().Count());
        }

        [Theory]
        [MemberData(nameof(Factories))]
        public void Factory_InitTemplates_CreatesTemplateWithInterfaces(LogicFactory factory)
        {
            var mock = new LogicTemplateFactoryMock();

            factory.InitTemplates(mock);

            var template = mock.GetById(factory.LogicGuid);
            Assert.NotEmpty(template.RuleInterfaceTemplate);
            Assert.Equal("OPERATIONS.NAME", template.Group);
            Assert.All(template.RuleInterfaceTemplate, t => Assert.Equal(factory.LogicGuid, t.This2RuleTemplate));
        }

        [Theory]
        [MemberData(nameof(Factories))]
        public void Factory_ParameterTemplates_HaveDefaultValues(LogicFactory factory)
        {
            var mock = new LogicTemplateFactoryMock();

            factory.InitTemplates(mock);

            var parameters = mock.GetById(factory.LogicGuid).RuleInterfaceTemplate
                .Where(t => t.This2RuleInterfaceDirection == (long)LogicInterfaceDirection.Param);
            Assert.All(parameters, p => Assert.False(string.IsNullOrEmpty(p.DefaultValue), $"{p.Key} has no default"));
        }
    }
}
