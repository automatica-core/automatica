using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Http
{
    public class HostSmokeTests : IClassFixture<WebApiHostFixture>
    {
        private readonly WebApiHost _host;

        public HostSmokeTests(WebApiHostFixture fixture)
        {
            _host = fixture.Host;
        }

        [Fact]
        public async Task Version_IsAnonymous()
        {
            var response = await _host.CreateClient().GetAsync("webapi/version");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Ping_WithoutToken_Is401()
        {
            var response = await _host.CreateClient().GetAsync("webapi/Ping");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Ping_WithAdminToken_Is200()
        {
            var response = await _host.CreateClient(_host.CreateAdminToken()).GetAsync("webapi/Ping");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
