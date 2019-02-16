using System;
using System.Collections.Generic;
using Automatica.Core.EF;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Automatica.Core.WebApi.Converter.MessagePack
{
    public static class MessagePackFormatterMvcBuilderExtensions
    {
        public static IMvcBuilder AddMessagePackFormatters(this IMvcBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return AddMessagePackFormatters(builder, messagePackFormatterOptionsConfiguration: null);
        }

        public static IMvcCoreBuilder AddMessagePackFormatters(this IMvcCoreBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return AddMessagePackFormatters(builder, messagePackFormatterOptionsConfiguration: null);
        }

        public static IMvcBuilder AddMessagePackFormatters(this IMvcBuilder builder, Action<MessagePackFormatterOptions> messagePackFormatterOptionsConfiguration)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var messagePackFormatterOptions = new MessagePackFormatterOptions();
            messagePackFormatterOptionsConfiguration?.Invoke(messagePackFormatterOptions);

            foreach (var extension in messagePackFormatterOptions.SupportedExtensions)
            {
                foreach (var contentType in messagePackFormatterOptions.SupportedContentTypes)
                {
                    builder.AddFormatterMappings(m => m.SetMediaTypeMappingForFormat(extension, new MediaTypeHeaderValue(contentType)));
                }
            }




            builder.AddMvcOptions(options =>
            {
                options.ModelMetadataDetailsProviders.Add(new DefaultBindingMetadataProvider());
                options.ModelMetadataDetailsProviders.Add(new DefaultValidationMetadataProvider());
            });
            builder.AddMvcOptions(options => options.OutputFormatters.Add(new MessagePackOutputFormatter(messagePackFormatterOptions)));

            return builder;
        }

        public static IMvcCoreBuilder AddMessagePackFormatters(this IMvcCoreBuilder builder, Action<MessagePackFormatterOptions> messagePackFormatterOptionsConfiguration)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var messagePackFormatterOptions = new MessagePackFormatterOptions();
            messagePackFormatterOptionsConfiguration?.Invoke(messagePackFormatterOptions);

            foreach (var extension in messagePackFormatterOptions.SupportedExtensions)
            {
                foreach (var contentType in messagePackFormatterOptions.SupportedContentTypes)
                {
                    builder.AddFormatterMappings(m => m.SetMediaTypeMappingForFormat(extension, new MediaTypeHeaderValue(contentType)));
                }
            }

            builder.AddMvcOptions(options =>
            {
                options.InputFormatters.Add(new MessagePackInputFormatter(messagePackFormatterOptions));

                options.ModelMetadataDetailsProviders.Add(new DefaultBindingMetadataProvider());
                options.ModelMetadataDetailsProviders.Add(new DefaultValidationMetadataProvider());

            });
            builder.AddMvcOptions(options => options.OutputFormatters.Add(new MessagePackOutputFormatter(messagePackFormatterOptions)));

            return builder;
        }
    }
}
