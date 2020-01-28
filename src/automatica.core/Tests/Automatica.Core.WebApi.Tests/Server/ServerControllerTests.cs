using Automatica.Core.WebApi.Controllers;
using Automatica.Core.WebApi.Tests.Base;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Server
{
    public class ServerControllerTests : BaseControllerTest<ServerController>
    {
        [Fact]
        public void TestServerState()
        {
            Assert.NotNull(Controller.GetServerStatus());
        }
    }
}
