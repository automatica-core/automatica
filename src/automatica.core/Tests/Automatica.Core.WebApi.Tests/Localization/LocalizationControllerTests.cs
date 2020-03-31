using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Automatica.Core.Base.Localization;
using Automatica.Core.WebApi.Controllers;
using Automatica.Core.WebApi.Tests.Base;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Localization
{
    public class LocalizationControllerTests : BaseControllerTest<LocalizationController>
    {
        public const string DeJson = "{\"Test\":\"de\"}";
        public const string EnJson = "{\"Test\":\"en\"}";

        [Fact]
        public void TestGet()
        {
            var localeProvider = ServiceProvider.GetService<ILocalizationProvider>();
            localeProvider.LoadFromAssembly(Assembly.GetAssembly(typeof(LocalizationControllerTests)));

            var de = Controller.Get("de");
            Assert.Equal(DeJson, de);

            var en = Controller.Get("en");
            Assert.Equal(EnJson, en);
        }

        [Fact]
        public void TestLocaleNotFound()
        {
            var ru = Controller.Get("ru");

            Assert.NotNull(ru);
            Assert.Equal("[]", ru.ToString());
        }

        [Fact]
        public void TestDoubleAssemblyLoad()
        {
            var localeProvider = ServiceProvider.GetService<ILocalizationProvider>();
            localeProvider.LoadFromAssembly(Assembly.GetAssembly(typeof(LocalizationControllerTests)));
            localeProvider.LoadFromAssembly(Assembly.GetAssembly(typeof(LocalizationControllerTests)));

            var de = Controller.Get("de");
            Assert.Equal(DeJson, de);
        }
    }
}
