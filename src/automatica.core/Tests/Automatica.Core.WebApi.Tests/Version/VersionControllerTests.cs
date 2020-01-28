using Automatica.Core.WebApi.Controllers;
using Automatica.Core.WebApi.Tests.Base;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Version
{
    public class VersionControllerTests : BaseControllerTest<VersionController>
    {
        [Fact]
        public void TestGetVersion()
        {
            Assert.NotNull(Controller.Get());
        }
    }
}
