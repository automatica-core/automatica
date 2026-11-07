using System.Net.Http;
using System.Threading.Tasks;
using P3.Driver.FroniusSolar.Model.Data;
using Xunit;

namespace P3.Driver.FroniusSolar.Tests
{
    /// <summary>
    /// SolarApi (own Fronius client) against a loopback HTTP server
    /// </summary>
    public class SolarApiTests
    {
        [Fact]
        public async Task GetInverterInfo_ParsesAllInverters()
        {
            using var server = new LoopbackHttpServer(_ => new LoopbackHttpServer.Response(200, FroniusJson.InverterInfo()));
            using var api = new SolarApi($"127.0.0.1:{server.Port}");

            var info = await api.GetInverterInfo();

            Assert.Equal(0, info.Head.Status.Code);
            Assert.Equal("Inverter", info.Head.RequestArguments["DeviceClass"]!.ToString());
            Assert.Equal(2, info.Body.Data.Count);

            var roof = info.Body.Data[1];
            Assert.Equal(102, roof.DeviceType);
            Assert.Equal(5000, roof.PvPower);
            Assert.Equal("Roof", roof.Name);
            Assert.Equal("38183", roof.UniqueId);
            Assert.Equal(StatusCode.Running, roof.StatusCode);

            var garage = info.Body.Data[2];
            Assert.Equal(StatusCode.Error, garage.StatusCode);
            Assert.Equal(102, garage.ErrorCode);
        }

        [Fact]
        public async Task GetInverterInfo_RequestsTheSolarApiV1Path()
        {
            using var server = new LoopbackHttpServer(_ => new LoopbackHttpServer.Response(200, FroniusJson.InverterInfo()));
            using var api = new SolarApi($"127.0.0.1:{server.Port}");

            await api.GetInverterInfo();

            var request = Assert.Single(server.Requests);
            Assert.Equal("GET", request.Method);
            Assert.Equal("/solar_api/v1/GetInverterInfo.cgi", request.Path);
        }

        [Fact]
        public async Task GetRequest_ServerError_Throws()
        {
            using var server = new LoopbackHttpServer(_ => new LoopbackHttpServer.Response(500, "{}"));
            using var api = new SolarApi($"127.0.0.1:{server.Port}");

            await Assert.ThrowsAsync<HttpRequestException>(() => api.GetInverterInfo());
        }

        [Fact]
        public async Task GetRequest_EmptyJsonObject_ReturnsEmptyModel()
        {
            using var server = new LoopbackHttpServer(_ => new LoopbackHttpServer.Response(200, "{}"));
            using var api = new SolarApi($"127.0.0.1:{server.Port}");

            var info = await api.GetInverterInfo();

            Assert.NotNull(info);
            Assert.Null(info.Body);
        }

        [Fact]
        public void SolarApiUri_IsVersion1()
        {
            Assert.Equal("/solar_api/v1", SolarApi.SolarApiUri);
        }
    }
}
