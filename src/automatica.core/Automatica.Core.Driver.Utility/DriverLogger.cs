using Microsoft.Extensions.Logging;
using System;

namespace Automatica.Core.Driver.Utility
{
    /// <summary>
    /// Utility class to log data from a driver
    /// </summary>
    public static class DriverLogger
    {
        public static void LogHexIn(this ILogger logger, byte[] data)
        {
            logger.LogTrace("<< {0}", Utils.ByteArrayToString(data));
        }

        public static void LogHexOut(this ILogger logger, byte[] data)
        {
            logger.LogTrace(">> {0}", Utils.ByteArrayToString(data));
        }

        public static void LogHexIn(this ILogger logger, ReadOnlyMemory<byte> data)
        {
            LogHexIn(logger, data.Span);
        }

        public static void LogHexOut(this ILogger logger, ReadOnlyMemory<byte> data)
        {
            LogHexOut(logger, data.Span);
        }

        public static void LogHexIn(this ILogger logger, ReadOnlySpan<byte> data)
        {
            logger.LogTrace("<< {0}", Utils.ByteArrayToString(data));
        }

        public static void LogHexOut(this ILogger logger, ReadOnlySpan<byte> data)
        {
            logger.LogTrace(">> {0}", Utils.ByteArrayToString(data));
        }
    }
}
