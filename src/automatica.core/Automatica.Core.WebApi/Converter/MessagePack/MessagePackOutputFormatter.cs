using System;
using System.Threading.Tasks;
using Automatica.Core.Internals;
using MessagePack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Automatica.Core.WebApi.Converter.MessagePack
{
    public class MessagePackOutputFormatter : OutputFormatter
    {
        private readonly MessagePackFormatterOptions _options;

        public MessagePackOutputFormatter(MessagePackFormatterOptions messagePackFormatterOptions)
        {
            _options = messagePackFormatterOptions ?? throw new ArgumentNullException(nameof(messagePackFormatterOptions));
            foreach (var contentType in messagePackFormatterOptions.SupportedContentTypes)
            {
                SupportedMediaTypes.Add(new MediaTypeHeaderValue(contentType));
            }
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            
            SystemLogger.Instance.LogDebug($"Serialize {context.Object}...");
            MessagePackSerializer.NonGeneric.Serialize(context.ObjectType, context.HttpContext.Response.Body, context.Object, _options.FormatterResolver);
            SystemLogger.Instance.LogDebug($"Serialize {context.Object}...done");
            //SystemLogger.Instance.LogDebug($"Serialize ToBase64 {context.Object}...done");

            //await context.HttpContext.Response.WriteAsync(body);
            return Task.CompletedTask;
        }
    }
}
