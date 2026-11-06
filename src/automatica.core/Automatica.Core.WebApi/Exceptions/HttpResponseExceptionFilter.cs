using Automatica.Core.Base.Exceptions;
using Automatica.Core.Base.License;
using Automatica.Core.Internals.Cloud.Exceptions;
using Automatica.Core.Runtime.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.WebApi.Exceptions
{
    public class HttpResponseExceptionFilter : IActionFilter, IOrderedFilter
    {
        private readonly ILogger _logger;
        public int Order { get; set; } = int.MaxValue - 10;

        public HttpResponseExceptionFilter(ILogger logger)
        {
            _logger = logger;
        }
        
        public void OnActionExecuting(ActionExecutingContext context) { }

        /// <summary>
        /// Status code of a known error: only unexpected errors are a 500 (the client reads the error from the body
        /// for every status, only 401 is handled differently).
        /// </summary>
        private static int GetStatusCode(WebApiException exception)
        {
            switch (exception)
            {
                case LicenseInvalidException:
                    return StatusCodes.Status400BadRequest;
                case NodeNotFoundException:
                case RuleNotFoundException:
                    return StatusCodes.Status404NotFound;
                default:
                    return StatusCodes.Status500InternalServerError;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception is WebApiException webApiEx)
            {
                var statusCode = GetStatusCode(webApiEx);
                if (statusCode >= 500)
                {
                    _logger.LogError(webApiEx, "An error occured while invoking an web api call");
                }
                else
                {
                    _logger.LogWarning($"Web api call failed with {statusCode}: {webApiEx.ErrorText}");
                }
                context.Result = new ObjectResult(webApiEx.ToJson())
                {
                    StatusCode = statusCode,
                };
                context.ExceptionHandled = true;
            }
            else if (context.Exception is NoApiKeyException)
            {
                // the cloud (an upstream service) refused the api key of this server
                var noApiKeyException = new WebApiException("CLOUD_CONNECTION_INVALID", ExceptionSeverity.Warning);
                context.Result = new ObjectResult(noApiKeyException.ToJson())
                {
                    StatusCode = StatusCodes.Status502BadGateway,
                };
                context.ExceptionHandled = true;
            }
            else if (context.Exception is FeatureNotLicensedException)
            {
                var noApiKeyException = new WebApiException("FEATURE_NOT_LICENSED", ExceptionSeverity.Warning);
                context.Result = new ObjectResult(noApiKeyException.ToJson())
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                };
                context.ExceptionHandled = true;
            }
        }
    }

}
