using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.WebApi.Controllers;
using Automatica.Core.WebApi.Tests.Base;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Settings
{
    public class SettingsControllerTests : BaseControllerTest<SettingsController>
    {
        [Fact]
        public void TestGetSettings()
        {
            var settings = Controller.LoadSettings();
            Assert.NotEmpty(settings);
        }

        [Fact]
        public async Task SaveSettings()
        {
            var settings = Controller.LoadSettings().ToList();

            var cloudUrlSetting = settings.First(a => a.ValueKey == "cloudUrl");
            Assert.NotNull(cloudUrlSetting);

            cloudUrlSetting.ValueText = "my-new-test-url";

            var savedSettings = await Controller.SaveSettings(settings);

            Assert.Equal(cloudUrlSetting.ValueText, savedSettings.First(a => a.ValueKey == "cloudUrl").ValueText);
        }

        private Setting CreateSetting(object value, PropertyTemplateType type)
        {
            var settings = new Setting
            {
                Type = (long) type,
                ValueKey = "test",
                Value = value
            };

            return settings;
        }

        [Fact]
        public void TestSettingsValue()
        {
            const string testStringValue = "mytest";
            Assert.Equal(testStringValue, CreateSetting(testStringValue, PropertyTemplateType.Text).Value);
            Assert.Equal(testStringValue, CreateSetting(testStringValue, PropertyTemplateType.Parity).Value);
            Assert.Equal(testStringValue, CreateSetting(testStringValue, PropertyTemplateType.Interface).Value);
            Assert.Equal(testStringValue, CreateSetting(testStringValue, PropertyTemplateType.Ip).Value);
            Assert.Equal(testStringValue, CreateSetting(testStringValue, PropertyTemplateType.Color).Value);

            const int testIntValue = 42;
            Assert.Equal(testIntValue, CreateSetting(testIntValue, PropertyTemplateType.DropDown).Value);
            Assert.Equal(testIntValue, CreateSetting(testIntValue, PropertyTemplateType.Baudrate).Value);
            Assert.Equal(testIntValue, CreateSetting(testIntValue, PropertyTemplateType.Databits).Value);
            Assert.Equal(testIntValue, CreateSetting(testIntValue, PropertyTemplateType.Integer).Value);
            Assert.Equal(testIntValue, CreateSetting(testIntValue, PropertyTemplateType.Enum).Value);
            Assert.Equal(testIntValue, CreateSetting(testIntValue, PropertyTemplateType.Range).Value);

            const double testDoubleValue = 42.0;
            Assert.Equal(testDoubleValue, CreateSetting(testDoubleValue, PropertyTemplateType.Numeric).Value);
            Assert.Equal(testDoubleValue, CreateSetting(testDoubleValue, PropertyTemplateType.Stopbits).Value);

            Assert.Equal(true, CreateSetting(true, PropertyTemplateType.Bool).Value);

            var testDateTimeValue = DateTime.Now;
            Assert.Equal(testDateTimeValue, CreateSetting(testDateTimeValue, PropertyTemplateType.Time).Value);


            Assert.Throws<NotImplementedException>(() =>
                CreateSetting("NotImpl", PropertyTemplateType.CategoryInstance).Value);
            Assert.Throws<NotImplementedException>(() =>
                CreateSetting("NotImpl", PropertyTemplateType.NodeInstance).Value);

        }
    }
}
