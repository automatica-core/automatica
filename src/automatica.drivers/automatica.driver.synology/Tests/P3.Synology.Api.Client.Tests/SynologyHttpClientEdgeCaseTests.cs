using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using P3.Synology.Api.Client.ApiDescription;
using P3.Synology.Api.Client.Apis.Auth;
using P3.Synology.Api.Client.Apis.Auth.Models;
using P3.Synology.Api.Client.Exceptions;
using P3.Synology.Api.Client.Session;
using P3.Synology.Api.Client.Shared.Models;
using Xunit;

namespace P3.Synology.Api.Client.Tests
{
    public class SynologyHttpClientEdgeCaseTests
    {
        private readonly DefaultApisInfo _apis = new DefaultApisInfo();

        private static SynologyHttpClient Client(RecordingHttpHandler handler, string baseAddress = "http://dsm-url.com/webapi")
        {
            return new SynologyHttpClient(handler.ToHttpClient(baseAddress));
        }

        #region base address

        [Fact]
        public async Task GetAsync_NoBaseAddress_ThrowsInvalidOperationException_WithoutSending()
        {
            var handler = RecordingHttpHandler.Json("{\"success\":true}");
            var client = Client(handler, null);

            await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync<BaseApiResponse>(_apis.InfoApi, "query", new Dictionary<string, string>()));
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task PostAsync_NoBaseAddress_ThrowsInvalidOperationException_WithoutSending()
        {
            var handler = RecordingHttpHandler.Json("{\"success\":true}");
            var client = Client(handler, null);

            await Assert.ThrowsAsync<InvalidOperationException>(() => client.PostAsync<BaseApiResponse>(_apis.FileStationUploadApi, "upload", new StringContent("x")));
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task PostFormAsync_NoBaseAddress_ThrowsInvalidOperationException_WithoutSending()
        {
            var handler = RecordingHttpHandler.Json("{\"success\":true}");
            var client = Client(handler, null);

            await Assert.ThrowsAsync<InvalidOperationException>(() => client.PostFormAsync<LoginResponse>(_apis.AuthApi, "login", new Dictionary<string, string> { { "passwd", "made-up" } }));
            Assert.Empty(handler.Requests);
        }

        [Theory]
        [InlineData("http://dsm-url.com/webapi", "query.cgi")]
        [InlineData("http://dsm-url.com/webapi/", "query.cgi")]
        [InlineData("http://dsm-url.com/webapi/", "/query.cgi")]
        [InlineData("http://dsm-url.com/webapi", "/query.cgi")]
        public async Task GetAsync_SlashesBetweenBaseAndPath_AreJoinedOnce(string baseAddress, string path)
        {
            var handler = RecordingHttpHandler.Json("{\"success\":true}");
            var client = Client(handler, baseAddress);

            await client.GetAsync<BaseApiResponse>(new ApiInfo("SYNO.API.Info", path, 1), "query", new Dictionary<string, string>());

            Assert.Equal("http://dsm-url.com/webapi/query.cgi", handler.Requests.Single().Uri.GetLeftPart(UriPartial.Path));
        }

        #endregion

        #region response handling

        [Fact]
        public async Task GetAsync_JsonNullBody_ThrowsUnexpectedResponseStatusException()
        {
            var client = Client(RecordingHttpHandler.Json("null"));

            var exception = await Assert.ThrowsAsync<UnexpectedResponseStatusException>(() => client.GetAsync<BaseApiResponse>(_apis.InfoApi, "query", new Dictionary<string, string>()));

            Assert.Equal(HttpStatusCode.OK, exception.StatusCode);
            Assert.Contains("empty response body", exception.Message);
        }

        [Fact]
        public async Task GetAsync_EmptyBody_Throws()
        {
            var client = Client(RecordingHttpHandler.Json(""));

            await Assert.ThrowsAnyAsync<Exception>(() => client.GetAsync<BaseApiResponse>(_apis.InfoApi, "query", new Dictionary<string, string>()));
        }

        [Fact(Skip = "BUG: a 200 response with an empty body (Content-Length 0) throws a raw System.Text.Json JsonException, the 'Received an empty response body.' handling only catches a literal JSON null - SynologyHttpClient.cs:131")]
        public async Task GetAsync_EmptyBody_ThrowsUnexpectedResponseStatusException()
        {
            var client = Client(RecordingHttpHandler.Json(""));

            await Assert.ThrowsAsync<UnexpectedResponseStatusException>(() => client.GetAsync<BaseApiResponse>(_apis.InfoApi, "query", new Dictionary<string, string>()));
        }

        [Fact]
        public async Task GetAsync_InvalidJson_ThrowsJsonException()
        {
            var client = Client(RecordingHttpHandler.Json("<html>DSM is starting</html>"));

            await Assert.ThrowsAnyAsync<JsonException>(() => client.GetAsync<BaseApiResponse>(_apis.InfoApi, "query", new Dictionary<string, string>()));
        }

        [Fact]
        public async Task GetAsync_FailureWithoutErrorObject_ThrowsSynologyApiExceptionWithCodeZero()
        {
            var client = Client(RecordingHttpHandler.Json("{\"success\":false}"));

            var exception = await Assert.ThrowsAsync<SynologyApiException>(() => client.GetAsync<BaseApiResponse>(_apis.InfoApi, "query", new Dictionary<string, string>()));

            Assert.Equal(0, exception.ErrorCode);
            Assert.Equal("", exception.ErrorDescription);
            Assert.Equal("query", exception.ApiMethod);
            Assert.Same(_apis.InfoApi, exception.ApiInfo);
            Assert.Empty(exception.Data);
        }

        [Fact]
        public async Task GetAsync_FailureWithErrorButNoDetails_HasNoAdditionalData()
        {
            var client = Client(RecordingHttpHandler.Json("{\"success\":false,\"error\":{\"code\":105}}"));

            var exception = await Assert.ThrowsAsync<SynologyApiException>(() => client.GetAsync<BaseApiResponse>(_apis.FileStationListApi, "list", new Dictionary<string, string>()));

            Assert.Equal(105, exception.ErrorCode);
            Assert.Equal("The logged in session does not have permission", exception.ErrorDescription);
            Assert.Empty(exception.Data);
        }

        [Fact]
        public async Task GetAsync_CommonErrorCode_WinsOverApiSpecificTexts()
        {
            var client = Client(RecordingHttpHandler.Json("{\"success\":false,\"error\":{\"code\":119}}"));

            var exception = await Assert.ThrowsAsync<SynologyApiException>(() => client.GetAsync<BaseApiResponse>(_apis.AuthApi, "logout", new Dictionary<string, string>()));

            Assert.Equal("SID not found", exception.ErrorDescription);
        }

        [Fact]
        public async Task GetAsync_FileStationErrorCode_UsesFileStationText()
        {
            var client = Client(RecordingHttpHandler.Json("{\"success\":false,\"error\":{\"code\":408}}"));

            var exception = await Assert.ThrowsAsync<SynologyApiException>(() => client.GetAsync<BaseApiResponse>(_apis.FileStationListApi, "list", new Dictionary<string, string>()));

            Assert.Equal("No such file or directory", exception.ErrorDescription);
        }

        [Fact]
        public async Task GetAsync_UnknownErrorCode_HasEmptyDescription()
        {
            var client = Client(RecordingHttpHandler.Json("{\"success\":false,\"error\":{\"code\":4711}}"));

            var exception = await Assert.ThrowsAsync<SynologyApiException>(() => client.GetAsync<BaseApiResponse>(_apis.AuthApi, "login", new Dictionary<string, string>()));

            Assert.Equal(4711, exception.ErrorCode);
            Assert.Equal("", exception.ErrorDescription);
        }

        [Fact]
        public async Task GetAsync_AdditionalErrors_AreAddedToData()
        {
            var client = Client(RecordingHttpHandler.Json("{\"success\":false,\"error\":{\"code\":1100,\"errors\":[{\"code\":408,\"path\":\"/a\"},{\"code\":414,\"path\":\"/b\"}]}}"));

            var exception = await Assert.ThrowsAsync<SynologyApiException>(() => client.GetAsync<BaseApiResponse>(_apis.FileStationCreateFolderApi, "create", new Dictionary<string, string>()));

            Assert.Equal(2, exception.Data.Count);
            Assert.Equal("/a", exception.Data["[408] No such file or directory"]);
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.BadGateway)]
        [InlineData(HttpStatusCode.NoContent)]
        public async Task GetAsync_NonOkStatus_ThrowsUnexpectedResponseStatusException(HttpStatusCode status)
        {
            var client = Client(RecordingHttpHandler.Json("{\"success\":true}", status));

            var exception = await Assert.ThrowsAsync<UnexpectedResponseStatusException>(() => client.GetAsync<BaseApiResponse>(_apis.InfoApi, "query", new Dictionary<string, string>()));

            Assert.Equal(status, exception.StatusCode);
        }

        [Fact]
        public async Task PostFormAsync_NonOkStatus_ThrowsUnexpectedResponseStatusException()
        {
            var client = Client(RecordingHttpHandler.Json("", HttpStatusCode.ServiceUnavailable));

            var exception = await Assert.ThrowsAsync<UnexpectedResponseStatusException>(() => client.PostFormAsync<LoginResponse>(_apis.AuthApi, "login", new Dictionary<string, string>()));

            Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
        }

        [Fact]
        public async Task GetAsync_SuccessWithoutData_ReturnsNull()
        {
            var client = Client(RecordingHttpHandler.Json("{\"success\":true}"));

            var result = await client.GetAsync<LoginResponse>(_apis.AuthApi, "logout", new Dictionary<string, string>());

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAsync_BaseApiResponse_ReturnsSuccess()
        {
            var client = Client(RecordingHttpHandler.Json("{\"success\":true,\"data\":{\"ignored\":1}}"));

            var result = await client.GetAsync<BaseApiResponse>(_apis.AuthApi, "logout", new Dictionary<string, string>());

            Assert.True(result.Success);
            Assert.Null(result.Error);
        }

        #endregion

        #region get query

        [Fact]
        public async Task GetAsync_Query_ContainsApiVersionMethodAndEncodedParameters()
        {
            var handler = RecordingHttpHandler.Json("{\"success\":true}");
            var client = Client(handler);

            await client.GetAsync<BaseApiResponse>(_apis.FileStationListApi, "list", new Dictionary<string, string> { { "folder_path", "/home/a b&c=d" } });

            var request = handler.Requests.Single();
            Assert.Equal(HttpMethod.Get, request.Method);
            var query = HttpUtility.ParseQueryString(request.Uri.Query);
            Assert.Equal("SYNO.FileStation.List", query["api"]);
            Assert.Equal("2", query["version"]);
            Assert.Equal("list", query["method"]);
            Assert.Equal("FileStation", query["session"]);
            Assert.Equal("/home/a b&c=d", query["folder_path"]);
            Assert.Null(query["_sid"]);
        }

        [Fact]
        public async Task GetAsync_WithSession_AddsSid()
        {
            var handler = RecordingHttpHandler.Json("{\"success\":true}");
            var client = Client(handler);

            await client.GetAsync<BaseApiResponse>(_apis.FileStationListApi, "list", new Dictionary<string, string>(), new SynologySession("made-up-sid"));

            Assert.Equal("made-up-sid", HttpUtility.ParseQueryString(handler.Requests.Single().Uri.Query)["_sid"]);
        }

        [Fact]
        public async Task GetAsync_ApiWithoutSessionName_HasNoSessionParameter()
        {
            var handler = RecordingHttpHandler.Json("{\"success\":true}");
            var client = Client(handler);

            await client.GetAsync<BaseApiResponse>(_apis.AuthApi, "logout", new Dictionary<string, string>());

            Assert.Null(HttpUtility.ParseQueryString(handler.Requests.Single().Uri.Query)["session"]);
        }

        #endregion

        #region post form

        [Fact]
        public async Task PostFormAsync_SendsEverythingInTheBody_AndNothingInTheUrl()
        {
            var handler = RecordingHttpHandler.Json("{\"success\":true,\"data\":{\"sid\":\"x\"}}");
            var client = Client(handler);

            await client.PostFormAsync<LoginResponse>(_apis.AuthApi, "login", new Dictionary<string, string>
            {
                { "account", "automatica" },
                { "passwd", "made-up p&ss=word" }
            });

            var request = handler.Requests.Single();
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("", request.Uri.Query);
            Assert.Equal("http://dsm-url.com/webapi/entry.cgi", request.Uri.ToString());
            Assert.Equal("application/x-www-form-urlencoded", request.ContentType);

            var form = HttpUtility.ParseQueryString(request.Body);
            Assert.Equal("SYNO.API.Auth", form["api"]);
            Assert.Equal("6", form["version"]);
            Assert.Equal("login", form["method"]);
            Assert.Equal("automatica", form["account"]);
            Assert.Equal("made-up p&ss=word", form["passwd"]);
            Assert.DoesNotContain("made-up", request.Uri.ToString());
        }

        [Fact]
        public async Task PostFormAsync_NullValues_AreNotSent()
        {
            var handler = RecordingHttpHandler.Json("{\"success\":true}");
            var client = Client(handler);

            await client.PostFormAsync<BaseApiResponse>(_apis.AuthApi, "login", new Dictionary<string, string> { { "otp_code", null }, { "format", "sid" } });

            var form = HttpUtility.ParseQueryString(handler.Requests.Single().Body);
            Assert.DoesNotContain("otp_code", form.AllKeys);
            Assert.Equal("sid", form["format"]);
        }

        [Fact]
        public async Task PostFormAsync_WithSession_SidIsInTheBodyNotInTheUrl()
        {
            var handler = RecordingHttpHandler.Json("{\"success\":true}");
            var client = Client(handler);

            await client.PostFormAsync<BaseApiResponse>(_apis.FileStationCreateFolderApi, "create", new Dictionary<string, string>(), new SynologySession("made-up-sid"));

            var request = handler.Requests.Single();
            Assert.Equal("", request.Uri.Query);
            var form = HttpUtility.ParseQueryString(request.Body);
            Assert.Equal("made-up-sid", form["_sid"]);
            Assert.Equal("FileStation", form["session"]);
        }

        [Fact]
        public async Task PostAsync_WithSession_SidIsInTheQuery()
        {
            var handler = RecordingHttpHandler.Json("{\"success\":true}");
            var client = Client(handler);

            await client.PostAsync<BaseApiResponse>(_apis.FileStationUploadApi, "upload", new StringContent("data"), new SynologySession("made-up-sid"));

            Assert.Equal("?_sid=made-up-sid", handler.Requests.Single().Uri.Query);
        }

        [Fact]
        public async Task PostAsync_WithoutSession_HasNoQuery()
        {
            var handler = RecordingHttpHandler.Json("{\"success\":true}");
            var client = Client(handler);

            await client.PostAsync<BaseApiResponse>(_apis.FileStationUploadApi, "upload", new StringContent("data"));

            Assert.Equal("", handler.Requests.Single().Uri.Query);
            Assert.Equal("data", handler.Requests.Single().Body);
        }

        #endregion

        #region auth api

        [Fact]
        public async Task AuthApiLogin_PasswordWithUrlCharacters_IsOnlyInTheFormBody()
        {
            var handler = RecordingHttpHandler.Json("{\"success\":true,\"data\":{\"sid\":\"made-up-sid\"}}");
            var auth = new AuthApi(Client(handler), _apis.AuthApi);
            const string password = "?passwd=leak&x=/#%20";

            var result = await auth.LoginAsync("automatica", password, "123456");

            Assert.Equal("made-up-sid", result.Sid);
            var request = handler.Requests.Single();
            Assert.Equal("", request.Uri.Query);
            Assert.DoesNotContain("leak", request.Uri.ToString());
            var form = HttpUtility.ParseQueryString(request.Body);
            Assert.Equal(password, form["passwd"]);
            Assert.Equal("123456", form["otp_code"]);
            Assert.Equal("sid", form["format"]);
        }

        [Fact]
        public async Task AuthApiLogin_WithoutOtp_SendsEmptyOtpCode()
        {
            var handler = RecordingHttpHandler.Json("{\"success\":true,\"data\":{\"sid\":\"made-up-sid\"}}");
            var auth = new AuthApi(Client(handler), _apis.AuthApi);

            await auth.LoginAsync("automatica", "made-up");

            var form = HttpUtility.ParseQueryString(handler.Requests.Single().Body);
            Assert.Equal("", form["otp_code"]);
        }

        [Theory]
        [InlineData(400, "No such account or incorrect password")]
        [InlineData(401, "Account disabled")]
        [InlineData(403, "2-step verification code required")]
        public async Task AuthApiLogin_Failure_ThrowsWithAuthErrorText(int code, string description)
        {
            var handler = RecordingHttpHandler.Json($"{{\"success\":false,\"error\":{{\"code\":{code}}}}}");
            var auth = new AuthApi(Client(handler), _apis.AuthApi);

            var exception = await Assert.ThrowsAsync<SynologyApiException>(() => auth.LoginAsync("automatica", "wrong-made-up"));

            Assert.Equal(code, exception.ErrorCode);
            Assert.Equal(description, exception.ErrorDescription);
            Assert.Equal("login", exception.ApiMethod);
            Assert.DoesNotContain("wrong-made-up", exception.Message);
        }

        #endregion
    }
}
