using System;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.EnOceanFactory.WindowHandle;
using Xunit;

namespace P3.Logic.EnOcean.Tests
{
    /// <summary>
    /// EnOcean F6-10-00 window handle: 13 = closed (0), 12/14 = tilted (1), 15 = open (2), everything else is ignored.
    /// </summary>
    public class EnOceanWindowHandleLogicTests : LogicTest<EnOceanWindowHandleLogicFactory>
    {
        private System.Collections.Generic.IList<Automatica.Core.Logic.ILogicOutputChanged> Input(object value) =>
            Logic.ValueChanged(GetLogicInterfaceByTemplate(EnOceanWindowHandleLogicFactory.RuleInput), Dispatchable, value);

        private void AssertSingleOutput(object value, int expected)
        {
            var result = Input(value);

            var output = Assert.Single(result);
            Assert.Equal(EnOceanWindowHandleLogicFactory.RuleOutput, output.Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate);
            Assert.IsType<int>(output.Value);
            Assert.Equal(expected, output.Value);
        }

        [Theory]
        [InlineData(13, 0)]
        [InlineData(12, 1)]
        [InlineData(14, 1)]
        [InlineData(15, 2)]
        public void Input_KnownHandleCode_MapsToPosition(int code, int expected)
        {
            AssertSingleOutput(code, expected);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(11)]
        [InlineData(16)]
        [InlineData(-13)]
        [InlineData(255)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public void Input_UnknownHandleCode_ProducesNoOutput(int code)
        {
            Assert.Empty(Input(code));
        }

        [Theory]
        [InlineData("13", 0)]
        [InlineData("12", 1)]
        [InlineData("14", 1)]
        [InlineData("15", 2)]
        public void Input_NumericString_IsConverted(string code, int expected)
        {
            AssertSingleOutput(code, expected);
        }

        [Theory]
        [InlineData(13d, 0)]
        [InlineData(15d, 2)]
        [InlineData(14.4d, 1)]
        // Convert.ToInt32 rounds half to even
        [InlineData(12.5d, 1)]
        [InlineData(13.5d, 1)]
        [InlineData(14.5d, 1)]
        [InlineData(15.49d, 2)]
        [InlineData(12.6d, 0)]
        public void Input_Double_IsRoundedToNearestCode(double code, int expected)
        {
            AssertSingleOutput(code, expected);
        }

        [Theory]
        [InlineData((byte)13, 0)]
        [InlineData((short)12, 1)]
        [InlineData(14L, 1)]
        [InlineData((uint)15, 2)]
        public void Input_OtherIntegerTypes_AreConverted(object code, int expected)
        {
            AssertSingleOutput(code, expected);
        }

        [Fact]
        public void Input_Decimal_IsConverted()
        {
            AssertSingleOutput(15m, 2);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Input_Bool_IsNotAHandleCode(bool value)
        {
            // true -> 1, false -> 0: no handle position
            Assert.Empty(Input(value));
        }

        [Fact]
        public void Input_Null_ProducesNoOutput()
        {
            // Convert.ToInt32(null) == 0
            Assert.Empty(Input(null));
        }

        [Theory]
        [InlineData("open")]
        [InlineData("")]
        [InlineData("13.0")]
        [InlineData("0x0D")]
        public void Input_NonIntegerString_IsSwallowedWithoutOutput(string value)
        {
            // the FormatException is caught (and logged) by the logic base class
            var ex = Record.Exception(() => Input(value));

            Assert.Null(ex);
            Assert.Empty(Input(value));
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(1e20)]
        public void Input_NotRepresentableDouble_IsSwallowedWithoutOutput(double value)
        {
            var ex = Record.Exception(() => Input(value));

            Assert.Null(ex);
            Assert.Empty(Input(value));
        }

        [Fact]
        public void Input_SameValueTwice_EmitsEachTime()
        {
            // duplicate values are not filtered, the handle position is sent again
            AssertSingleOutput(15, 2);
            AssertSingleOutput(15, 2);
        }

        [Fact]
        public void Input_Sequence_FollowsTheHandle()
        {
            var positions = new[] { 13, 14, 15, 12, 13 }
                .Select(code => (int)Assert.Single(Input(code)).Value)
                .ToArray();

            Assert.Equal(new[] { 0, 1, 2, 1, 0 }, positions);
        }

        [Fact]
        public void Input_UnknownCodeBetweenKnownCodes_DoesNotResetThePosition()
        {
            AssertSingleOutput(15, 2);
            Assert.Empty(Input(99));
            AssertSingleOutput(13, 0);
        }

        [Fact]
        public void Input_DispatchValueOverload_UsesTheValue()
        {
            var value = new DispatchValue(Guid.NewGuid(), DispatchableType.NodeInstance, 15, DateTime.UnixEpoch, DispatchValueSource.Read);

            var result = Logic.ValueChanged(GetLogicInterfaceByTemplate(EnOceanWindowHandleLogicFactory.RuleInput), Dispatchable, value);

            Assert.Equal(2, Assert.Single(result).Value);
        }

        [Fact]
        public void OutputInterface_IsNotInputHandled()
        {
            // a value on the output interface itself is not interpreted as handle code
            var result = Logic.ValueChanged(GetLogicInterfaceByTemplate(EnOceanWindowHandleLogicFactory.RuleOutput), Dispatchable, 15);

            Assert.Empty(result);
        }
    }

    public class EnOceanWindowHandleLogicFactoryTests
    {
        [Fact]
        public void Metadata_IsStable()
        {
            var factory = new EnOceanWindowHandleLogicFactory();

            Assert.Equal("EnOcean.WindowHandle", factory.LogicName);
            Assert.Equal(new Guid("5ef3001b-0fbf-40e2-86b9-c6e672daef1c"), factory.LogicGuid);
            Assert.Equal(new Version(0, 0, 0, 1), factory.LogicVersion);
        }

        [Fact]
        public void InitTemplates_CreatesOneInputAndOneOutput()
        {
            var factory = new EnOceanWindowHandleLogicFactory();
            var templates = new LogicTemplateFactoryMock();

            factory.InitTemplates(templates);

            var template = templates.GetById(factory.LogicGuid);
            Assert.Equal("enocean-window-handle", template.Key);
            Assert.Equal(2, template.RuleInterfaceTemplate.Count);

            var input = template.RuleInterfaceTemplate.Single(a => a.ObjId == EnOceanWindowHandleLogicFactory.RuleInput);
            Assert.Equal((long)LogicInterfaceDirection.Input, input.This2RuleInterfaceDirection);
            Assert.Equal(1, input.MaxLinks);

            var output = template.RuleInterfaceTemplate.Single(a => a.ObjId == EnOceanWindowHandleLogicFactory.RuleOutput);
            Assert.Equal((long)LogicInterfaceDirection.Output, output.This2RuleInterfaceDirection);
        }

        [Fact]
        public void CreateLogicInstance_ReturnsWindowHandleLogic()
        {
            var test = new EnOceanWindowHandleLogicTests();

            Assert.IsType<EnOceanWindowHandleLogic>(test.Instance.CreateLogicInstance(test.Context));
        }
    }
}
