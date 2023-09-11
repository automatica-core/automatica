using Automatica.Core.Base.Exceptions;
using Automatica.Core.Internals.Cloud.Exceptions;
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

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception is WebApiException webApiEx)
            {
                _logger.LogError(webApiEx, "An error occured while invoking an web api call");
                context.Result = new ObjectResult(webApiEx.ToJson())
                {
                    StatusCode = 500,
                };
                context.ExceptionHandled = true;
            }
            else if (context.Exception is NoApiKeyException)
            {
                var noApiKeyException = new WebApiException("CLOUD_CONNECTION_INVALID", ExceptionSeverity.Warning); 
                context.Result = new ObjectResult(noApiKeyException.ToJson())
                {
                    StatusCode = 500,
                };
                context.ExceptionHandled = true;
            }
        }
    }

}
