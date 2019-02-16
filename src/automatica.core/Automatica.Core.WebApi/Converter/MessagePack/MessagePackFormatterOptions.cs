using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Automatica.Core.EF;
using Automatica.Core.WebApi.Converter.MessagePack.GuidFormatters;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace Automatica.Core.WebApi.Converter.MessagePack
{
    public class MessagePackFormatterOptions
    {
        public IFormatterResolver FormatterResolver { get; set; } = DefaultFormatResolver.Instance;

        public HashSet<string> SupportedContentTypes { get; set; } = new HashSet<string> { "application/x-msgpack", "application/msgpack" };

        public HashSet<string> SupportedExtensions { get; set; } = new HashSet<string> { "mp" };

        public bool SuppressReadBuffering { get; set; } = false;
    }

    public class DefaultFormatResolver : IFormatterResolver {
        public static readonly IFormatterResolver Instance = new DefaultFormatResolver();

        private DefaultFormatResolver()
        {

        }

        public static readonly IList<IFormatterResolver> Resolvers = new[]
        {
            NativeDateTimeResolver.Instance, // Native c# DateTime format, preserving timezone
            BuiltinResolver.Instance, // Try Builtin
            AttributeFormatterResolver.Instance, // Try use [MessagePackFormatter]
            DynamicEnumResolver.Instance, // Try Enum
            DynamicGenericResolver.Instance, // Try Array, Tuple, Collection

            // remove Union, Object, ContractleessObject resolvers

            // All object should resolve by TypelessObjectResolver(needs to additional TypelessObjectFormatter<T>)
            TypelessObjectResolver.Instance,
            ContractlessStandardResolver.Instance
        };

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            if (typeof(T) == typeof(Guid?))
            {
                return (IMessagePackFormatter<T>)NullableGuidFormatter.Instance;
            }
            return Cache<T>.Formatter;
        }

        private static class Cache<T>
        {
            public static readonly IMessagePackFormatter<T> Formatter;

            static Cache()
            {
                foreach (var resolver in Resolvers)
                {
                    Formatter = resolver.GetFormatter<T>();
                    if (Formatter != null)
                    {
                        return;
                    }
                }
            }
        }
    }
}
