using Automatica.Core.Runtime.Core.Plugins;
using Xunit;

namespace Automatica.Core.Tests.Update
{
    public class PluginInstallerTests
    {
        [Theory]
        [InlineData("P3.Driver.Knx", true)]
        [InlineData("P3.Logic.Time-1.0", true)]
        [InlineData("", false)]
        [InlineData(" ", false)]
        [InlineData(null, false)]
        [InlineData(".", false)]
        [InlineData("..", false)]
        [InlineData("../../..", false)]
        [InlineData("..\\..", false)]
        [InlineData("plugins/evil", false)]
        [InlineData("C:\\temp", false)]
        [InlineData("/etc", false)]
        public void TestComponentNameValidation(string componentName, bool valid)
        {
            Assert.Equal(valid, PluginInstaller.IsValidComponentName(componentName));
        }
    }
}
