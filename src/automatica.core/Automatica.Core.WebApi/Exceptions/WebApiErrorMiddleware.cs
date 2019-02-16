using Automatica.Core.Base.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Automatica.Core.Internals;

namespace Automatica.Core.WebApi.Exceptions
{
    public class WebApiErrorMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public WebApiErrorMiddleware(RequestDelegate next)
        {
            _next = next;
            _logger = SystemLogger.Instance;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch(WebApiException webApiEx)
            {
                _logger.LogError(webApiEx, "An error occured while invoking an webapi call");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                if (!context.Response.HasStarted)
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(webApiEx.ToJson());
                }
            }

        }
    }
}
