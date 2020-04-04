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
        public const string DeJson = "{\"Test\":\"Deutsch\",\"Deep1\":{\"Test\":\"Deutsch\"}}";
        public const string EnJson = "{\"Test\":\"English\",\"Deep1\":{\"Test\":\"English\"}}";

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

        [Theory]
        [InlineData("de", "Test", "Deutsch")]
        [InlineData("en", "Test", "English")]
        [InlineData("de", "Deep1.Test", "Deutsch")]
        [InlineData("en", "Deep1.Test", "English")]
        [InlineData("de", "Deep1.Test.X", "Deep1.Test.X")]
        [InlineData("en", "Deep1.Test.X", "Deep1.Test.X")]
        [InlineData("ru", "Test", "Test")]
        [InlineData("ru", "Deep1.Test", "Deep1.Test")]
        [InlineData("ru", "Deep1.Test.X", "Deep1.Test.X")]
        public void TestGetTranslation(string locale, string key, string text)
        {
            var localeProvider = ServiceProvider.GetService<ILocalizationProvider>();
            localeProvider.LoadFromAssembly(Assembly.GetAssembly(typeof(LocalizationControllerTests)));
            
            var translation = localeProvider.GetTranslation(locale, key);
            Assert.Equal(text, translation);
        }
    }
}
