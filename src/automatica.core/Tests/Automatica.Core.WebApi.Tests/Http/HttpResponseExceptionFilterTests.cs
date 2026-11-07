using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Automatica.Core.Base.Exceptions;
using Automatica.Core.Base.License;
using Automatica.Core.Internals.Cloud.Exceptions;
using Automatica.Core.Runtime.Exceptions;
using Automatica.Core.WebApi.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Http
{
    /// <summary>
    /// Only part of the host when <see cref="WebApiHost"/> is created with test controllers.
    /// </summary>
    [Route("test/throw")]
    [AllowAnonymous]
    public class ThrowingTestController : ControllerBase
    {
        public static Exception Create(string kind) => kind switch
        {
            "license" => new LicenseInvalidException("LICENSE_INVALID"),
            "node" => new NodeNotFoundException(),
            "rule" => new RuleNotFoundException(),
            "apikey" => new NoApiKeyException(),
            "feature" => new FeatureNotLicensedException("satellites"),
            "webapi" => new WebApiException("SOMETHING_FAILED", ExceptionSeverity.Error),
            _ => new InvalidOperationException("unexpected")
        };

        [HttpGet("{kind}")]
        public IActionResult Throw(string kind) => throw Create(kind);
    }

    public class HttpResponseExceptionFilterUnitTests
    {
        private static ActionExecutedContext Execute(Exception exception)
        {
            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
            var context = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new object()) { Exception = exception };
            new HttpResponseExceptionFilter(NullLogger.Instance).OnActionExecuted(context);
            return context;
        }

        [Theory]
        [InlineData("license", StatusCodes.Status400BadRequest, "LICENSE_INVALID")]
        [InlineData("node", StatusCodes.Status404NotFound, "NODE_NOT_FOUND")]
        [InlineData("rule", StatusCodes.Status404NotFound, "RULE_NOT_FOUND")]
        [InlineData("apikey", StatusCodes.Status502BadGateway, "CLOUD_CONNECTION_INVALID")]
        [InlineData("feature", StatusCodes.Status403Forbidden, "FEATURE_NOT_LICENSED")]
        [InlineData("webapi", StatusCodes.Status500InternalServerError, "SOMETHING_FAILED")]
        public void KnownExceptions_AreMappedToStatusAndWebApiExceptionBody(string kind, int status, string errorText)
        {
            var context = Execute(ThrowingTestController.Create(kind));

            Assert.True(context.ExceptionHandled);
            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(status, result.StatusCode);
            var body = (IDictionary<string, object>)result.Value;
            Assert.Equal(errorText, body["errorText"]);
            Assert.Equal("WebApiException", body["TypeInfo"]);
        }

        [Fact]
        public void OtherExceptions_AreNotHandled()
        {
            var context = Execute(new InvalidOperationException("boom"));

            Assert.False(context.ExceptionHandled);
            Assert.Null(context.Result);
        }

        [Fact]
        public void NoException_LeavesTheResult()
        {
            var context = Execute(null);

            Assert.False(context.ExceptionHandled);
            Assert.Null(context.Result);
        }

        [Fact]
        public void Filter_RunsLast()
        {
            // after every other action filter, so their exceptions are mapped as well
            Assert.True(new HttpResponseExceptionFilter(NullLogger.Instance).Order > 1000);
        }
    }

    public class HttpResponseExceptionFilterHttpTests : IClassFixture<WebApiHostWithTestControllersFixture>
    {
        private readonly WebApiHost _host;

        public HttpResponseExceptionFilterHttpTests(WebApiHostWithTestControllersFixture fixture)
        {
            _host = fixture.Host;
        }

        [Theory]
        [InlineData("license", HttpStatusCode.BadRequest, "LICENSE_INVALID", ExceptionSeverity.Error)]
        [InlineData("node", HttpStatusCode.NotFound, "NODE_NOT_FOUND", ExceptionSeverity.Warning)]
        [InlineData("rule", HttpStatusCode.NotFound, "RULE_NOT_FOUND", ExceptionSeverity.Warning)]
        [InlineData("apikey", HttpStatusCode.BadGateway, "CLOUD_CONNECTION_INVALID", ExceptionSeverity.Warning)]
        [InlineData("feature", HttpStatusCode.Forbidden, "FEATURE_NOT_LICENSED", ExceptionSeverity.Warning)]
        [InlineData("webapi", HttpStatusCode.InternalServerError, "SOMETHING_FAILED", ExceptionSeverity.Error)]
        public async Task KnownExceptions_OverHttp_ReturnStatusAndJsonBody(string kind, HttpStatusCode status, string errorText, ExceptionSeverity severity)
        {
            var response = await _host.CreateClient().GetAsync($"test/throw/{kind}");

            Assert.Equal(status, response.StatusCode);
            Assert.StartsWith("application/json", response.Content.Headers.ContentType?.MediaType);
            var body = (JObject)await response.ReadJson();
            Assert.Equal(errorText, (string)body["errorText"]);
            Assert.Equal((int)severity, (int)body["severity"]);
            Assert.Equal("WebApiException", (string)(body["typeInfo"] ?? body["TypeInfo"]));
        }

        [Fact]
        public async Task UnexpectedException_IsNotTurnedIntoAWebApiExceptionBody()
        {
            // no exception handler in the pipeline: the test server surfaces it, in production it is a plain 500
            await Assert.ThrowsAsync<InvalidOperationException>(() => _host.CreateClient().GetAsync("test/throw/other"));
        }

        [Fact]
        public async Task LogicDataOfUnknownLogic_Returns404RuleNotFound()
        {
            var response = await _host.CreateClient(_host.CreateAdminToken()).GetAsync($"webapi/logics/data/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("RULE_NOT_FOUND", (string)(await response.ReadJson())["errorText"]);
        }
    }
}
