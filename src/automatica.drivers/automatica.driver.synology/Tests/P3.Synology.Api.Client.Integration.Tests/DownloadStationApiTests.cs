using FluentAssertions;
using P3.Synology.Api.Client.Integration.Tests.Fixtures;
using Xunit;

namespace P3.Synology.Api.Client.Integration.Tests
{
    public class DownloadStationApiTests : IClassFixture<SynologyFixture>
    {
        private readonly SynologyFixture _fixture;

        public DownloadStationApiTests(SynologyFixture fixture)
        {
            _fixture = fixture;

            _fixture.LoginAsync().Wait();
        }

        [Fact]
        public async void DownloadStationApi_DownloadStation_ListTasks()
        {
            // arrange && act
            var listResponse = await _fixture
                .Client
                .DownloadStationApi()
                .TaskEndpoint()
                .ListAsync();

            // assert
            listResponse.Should().NotBeNull();
        }
    }
}
