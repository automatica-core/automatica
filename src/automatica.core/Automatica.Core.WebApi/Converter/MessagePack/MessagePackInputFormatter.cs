using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Internals;
using Automatica.Core.WebApi.Controllers;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace Automatica.Core.WebApi.Converter.MessagePack
{
    public class MessagePackInputFormatter : InputFormatter
    {
        private readonly MessagePackFormatterOptions _options;

        public MessagePackInputFormatter(MessagePackFormatterOptions messagePackFormatterOptions)
        {
            _options = messagePackFormatterOptions ?? throw new ArgumentNullException(nameof(messagePackFormatterOptions));
            foreach (var contentType in messagePackFormatterOptions.SupportedContentTypes)
            {
                SupportedMediaTypes.Add(new MediaTypeHeaderValue(contentType));
            }
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var request = context.HttpContext.Request;

            if (!request.Body.CanSeek && !_options.SuppressReadBuffering)
            {
                BufferingHelper.EnableRewind(request);

                await request.Body.DrainAsync(CancellationToken.None);
                request.Body.Seek(0L, SeekOrigin.Begin);
            }

            try
            {
                var result =
                    MessagePackSerializer.NonGeneric.Deserialize(context.ModelType, request.Body, _options.FormatterResolver);
                var formatterResult = await InputFormatterResult.SuccessAsync(result);



                return formatterResult;
            }
            catch (Exception e)
            {
                SystemLogger.Instance.LogDebug($"Could not deserialize object {e}");
                throw;
            }
        }


        protected override bool CanReadType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentException("Type cannot be null");
            }

            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsInterface && typeInfo.ImplementedInterfaces.Contains(typeof(IEnumerable)))
            {
                return true;
            }

            return !typeInfo.IsAbstract && !typeInfo.IsInterface && typeInfo.IsPublic;
        }
    }
}
