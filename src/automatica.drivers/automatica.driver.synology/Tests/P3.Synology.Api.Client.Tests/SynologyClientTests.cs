using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using System.Web;
using P3.Synology.Api.Client.Exceptions;
using Xunit;

namespace P3.Synology.Api.Client.Tests
{
    public class SynologyClientTests
    {
        private const string InfoResponse = "{\"success\":true,\"data\":{\"SYNO.API.Auth\":{\"path\":\"auth.cgi\",\"minVersion\":1,\"maxVersion\":7},\"SYNO.FileStation.List\":{\"path\":\"FileStation/list.cgi\",\"minVersion\":1,\"maxVersion\":2}}}";

        private static RecordingHttpHandler Dsm(string loginResponse, string infoResponse = InfoResponse)
        {
            return new RecordingHttpHandler(request =>
            {
                var path = request.RequestUri.AbsolutePath;
                if (path == "/webapi/query.cgi")
                {
                    return RecordingHttpHandler.JsonResponse(infoResponse);
                }
                if (request.Method == HttpMethod.Post)
                {
                    return RecordingHttpHandler.JsonResponse(loginResponse);
                }
                return RecordingHttpHandler.JsonResponse("{\"success\":true}");
            });
        }

        private static SynologyClient CreateClient(RecordingHttpHandler handler, string url = "http://diskstation:5000")
        {
            return new SynologyClient(url, new HttpClient(handler));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Constructor_NoUrl_ThrowsArgumentNullException(string url)
        {
            Assert.Throws<ArgumentNullException>(() => new SynologyClient(url, new HttpClient(Dsm("{}"))));
        }

        [Theory]
        [InlineData("http://diskstation:5000")]
        [InlineData("http://diskstation:5000/")]
        public void Constructor_QueriesTheApiInfo(string url)
        {
            var handler = Dsm("{}");

            CreateClient(handler, url);

            var request = handler.Requests.Single();
            Assert.Equal("http://diskstation:5000/webapi/query.cgi", request.Uri.GetLeftPart(UriPartial.Path));
            var query = HttpUtility.ParseQueryString(request.Uri.Query);
            Assert.Equal("SYNO.API.Info", query["api"]);
            Assert.Equal("query", query["method"]);
            Assert.Equal("all", query["query"]);
        }

        [Fact]
        public void Constructor_UsesThePathsReportedByTheDsm()
        {
            var client = CreateClient(Dsm("{}"));

            Assert.Equal("auth.cgi", client.ApisInfo.AuthApi.Path);
            Assert.Equal("FileStation/list.cgi", client.ApisInfo.FileStationListApi.Path);
            // not reported, the default stays
            Assert.Equal("entry.cgi", client.ApisInfo.FileStationUploadApi.Path);
        }

        [Fact]
        public void Constructor_InfoQueryFails_Throws()
        {
            var handler = new RecordingHttpHandler(_ => RecordingHttpHandler.JsonResponse("", HttpStatusCode.InternalServerError));

            var exception = Assert.Throws<AggregateException>(() => CreateClient(handler));

            Assert.IsType<UnexpectedResponseStatusException>(exception.InnerException);
        }

        [Fact]
        public async Task LoginAsync_Success_CreatesSession_AndPostsToTheReportedAuthPath()
        {
            var handler = Dsm("{\"success\":true,\"data\":{\"sid\":\"made-up-sid\"}}");
            var client = CreateClient(handler);

            await client.LoginAsync("automatica", "made-up-password");

            Assert.True(client.IsLoggedIn);
            Assert.Equal("made-up-sid", client.Session.Sid);

            var login = handler.Requests.Single(a => a.Method == HttpMethod.Post);
            Assert.Equal("http://diskstation:5000/webapi/auth.cgi", login.Uri.ToString());
            Assert.Equal("made-up-password", HttpUtility.ParseQueryString(login.Body)["passwd"]);
            Assert.All(handler.Requests, r => Assert.DoesNotContain("made-up-password", r.Uri.ToString()));
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ThrowsAndStaysLoggedOut()
        {
            var client = CreateClient(Dsm("{\"success\":false,\"error\":{\"code\":400}}"));

            var exception = await Assert.ThrowsAsync<SynologyApiException>(() => client.LoginAsync("automatica", "wrong-made-up"));

            Assert.Equal(400, exception.ErrorCode);
            Assert.False(client.IsLoggedIn);
            Assert.Null(client.Session);
        }

        [Fact]
        public async Task LoginAsync_FailureWithoutErrorObject_ThrowsAndStaysLoggedOut()
        {
            var client = CreateClient(Dsm("{\"success\":false}"));

            var exception = await Assert.ThrowsAsync<SynologyApiException>(() => client.LoginAsync("automatica", "made-up"));

            Assert.Equal(0, exception.ErrorCode);
            Assert.False(client.IsLoggedIn);
        }

        [Theory]
        [InlineData("{\"success\":true,\"data\":{\"sid\":\"\"}}")]
        [InlineData("{\"success\":true,\"data\":{\"sid\":\"  \"}}")]
        [InlineData("{\"success\":true,\"data\":{}}")]
        public async Task LoginAsync_NoSid_ThrowsSecurityException(string response)
        {
            var client = CreateClient(Dsm(response));

            await Assert.ThrowsAsync<SecurityException>(() => client.LoginAsync("automatica", "made-up"));

            Assert.False(client.IsLoggedIn);
        }

        [Fact]
        public async Task LoginAsync_SuccessWithoutData_ThrowsInsteadOfLoggingIn()
        {
            var client = CreateClient(Dsm("{\"success\":true}"));

            await Assert.ThrowsAnyAsync<Exception>(() => client.LoginAsync("automatica", "made-up"));

            Assert.False(client.IsLoggedIn);
        }

        [Fact(Skip = "BUG: a successful login response without data makes LoginAsync throw NullReferenceException (loginResult.Sid on null) instead of the SecurityException used for a missing sid - SynologyClient.cs:82")]
        public async Task LoginAsync_SuccessWithoutData_ThrowsSecurityException()
        {
            var client = CreateClient(Dsm("{\"success\":true}"));

            await Assert.ThrowsAsync<SecurityException>(() => client.LoginAsync("automatica", "made-up"));
        }

        [Fact]
        public void FileStationApi_NotLoggedIn_ThrowsSecurityException()
        {
            var client = CreateClient(Dsm("{}"));

            Assert.Throws<SecurityException>(() => client.FileStationApi());
            Assert.Throws<SecurityException>(() => client.DownloadStationApi());
        }

        [Fact]
        public async Task LogoutAsync_NotLoggedIn_SendsNothing()
        {
            var handler = Dsm("{}");
            var client = CreateClient(handler);

            await client.LogoutAsync();

            Assert.Single(handler.Requests);
        }

        [Fact]
        public async Task LogoutAsync_LoggedIn_SendsSidAndClearsSession()
        {
            var handler = Dsm("{\"success\":true,\"data\":{\"sid\":\"made-up-sid\"}}");
            var client = CreateClient(handler);
            await client.LoginAsync("automatica", "made-up");

            await client.LogoutAsync();

            Assert.False(client.IsLoggedIn);
            var logout = handler.Requests.Last();
            Assert.Equal(HttpMethod.Get, logout.Method);
            var query = HttpUtility.ParseQueryString(logout.Uri.Query);
            Assert.Equal("logout", query["method"]);
            Assert.Equal("made-up-sid", query["_sid"]);
        }
    }
}
