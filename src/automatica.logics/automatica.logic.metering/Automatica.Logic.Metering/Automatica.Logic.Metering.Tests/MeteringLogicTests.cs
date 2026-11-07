using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Logics;
using Xunit;

namespace Automatica.Logic.Metering.Tests
{
    /// <summary>
    /// The metering logic is a placeholder without interfaces: it must start/stop cleanly and never emit anything.
    /// </summary>
    public class MeteringLogicTests : LogicTest<MeteringFactory>
    {
        private RuleInterfaceInstance AdHocInterface(LogicInterfaceDirection direction, string key = "x")
        {
            var template = new RuleInterfaceTemplate
            {
                ObjId = Guid.NewGuid(),
                Key = key,
                Name = key,
                This2RuleInterfaceDirection = (long)direction,
                ParameterDataType = RuleInterfaceParameterDataType.Double
            };
            return new RuleInterfaceInstance
            {
                ObjId = Guid.NewGuid(),
                This2RuleInterfaceTemplate = template.ObjId,
                This2RuleInterfaceTemplateNavigation = template,
                This2RuleInstanceNavigation = RuleInstance
            };
        }

        [Fact]
        public async Task Start_ReturnsTrue()
        {
            Assert.True(await Logic.Start());
        }

        [Fact]
        public async Task Stop_ReturnsTrue_AndCanBeRestarted()
        {
            Assert.True(await Logic.Stop());
            Assert.True(await Logic.Start());
            Assert.True(await Logic.Restart(RuleInstance));
        }

        [Fact]
        public void RuleInstance_HasNoInterfaces()
        {
            Assert.Empty(RuleInstance.RuleInterfaceInstance);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(true)]
        [InlineData(1.5d)]
        [InlineData(double.NaN)]
        [InlineData("1234.5")]
        public void InputValue_ProducesNoOutput(object value)
        {
            var result = Logic.ValueChanged(AdHocInterface(LogicInterfaceDirection.Input), Dispatchable, value);

            Assert.Empty(result);
        }

        [Fact]
        public async Task InputValue_DispatchesNothing()
        {
            await Context.Dispatcher.ClearValues();

            LogicInputChanged(AdHocInterface(LogicInterfaceDirection.Input), 42d);

            Assert.Empty(Context.Dispatcher.GetValues(DispatchableType.RuleInstance));
        }

        [Fact]
        public void ParameterValue_IsStoredForTheVisu_AndProducesNoOutput()
        {
            var result = Logic.ValueChanged(AdHocInterface(LogicInterfaceDirection.Param, "price"), Dispatchable, 0.25d);

            Assert.Empty(result);
            var visu = Assert.IsType<Dictionary<string, object>>(Logic.GetDataForVisu());
            Assert.Equal(0.25d, visu["price"]);
        }

        [Fact]
        public void GetDataForVisu_WithoutValues_IsEmpty()
        {
            var visu = Assert.IsType<Dictionary<string, object>>(Logic.GetDataForVisu());

            Assert.Empty(visu);
        }
    }

    public class MeteringFactoryTests
    {
        [Fact]
        public void Metadata_IsStable()
        {
            var factory = new MeteringFactory();

            Assert.Equal("Metering", factory.LogicName);
            Assert.Equal(new Guid("6b395c46-ee29-4f06-b416-c9367c41fc85"), factory.LogicGuid);
            Assert.Equal(new Version(0, 3, 1, 1), factory.LogicVersion);
        }

        [Fact]
        public void InitTemplates_CreatesTemplateWithoutInterfaces()
        {
            var factory = new MeteringFactory();
            var templates = new LogicTemplateFactoryMock();

            factory.InitTemplates(templates);

            var template = templates.GetById(factory.LogicGuid);
            Assert.Equal("metering", template.Key);
            Assert.Equal("METERING.NAME", template.Name);
            Assert.Equal("METERING.NAME", template.Group);
            Assert.Empty(template.RuleInterfaceTemplate);
        }

        [Fact]
        public void CreateLogicInstance_ReturnsMeteringLogic()
        {
            var test = new MeteringLogicTests();

            Assert.IsType<MeteringLogic>(test.Instance.CreateLogicInstance(test.Context));
        }
    }
}
