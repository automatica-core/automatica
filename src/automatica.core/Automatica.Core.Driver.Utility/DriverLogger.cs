using Microsoft.Extensions.Logging;
using System;

namespace Automatica.Core.Driver.Utility
{
    /// <summary>
    /// Utility class to log data from a driver
    /// </summary>
    public static class DriverLogger
    {
        /// <summary>
        /// Log input data in Hex
        /// </summary>
        /// <param name="driverName">The name of the driver, printed in the log</param>
        /// <param name="data">The input data</param>
        public static void LogHexIn(this ILogger logger, byte[] data)
        {
            logger.LogTrace("<< {0}", Utils.ByteArrayToString(data));
        }

        /// <summary>
        /// Log output data in Hex
        /// </summary>
        /// <param name="driverName">The name of the driver, printed in the log</param>
        /// <param name="data">The output data</param>
        public static void LogHexOut(this ILogger logger, byte[] data)
        {
            logger.LogTrace(">> {0}", Utils.ByteArrayToString(data));
        }

        /// <summary>
        /// Log input data in Hex
        /// </summary>
        /// <param name="driverName">The name of the driver, printed in the log</param>
        /// <param name="data">The input data</param>
        public static void LogHexIn(this ILogger logger, ReadOnlyMemory<byte> data)
        {
            LogHexIn(logger, data.Span);
        }

        /// <summary>
        /// Log output data in Hex
        /// </summary>
        /// <param name="driverName">The name of the driver, printed in the log</param>
        /// <param name="data">The output data</param>
        public static void LogHexOut(this ILogger logger, ReadOnlyMemory<byte> data)
        {
            LogHexOut(logger, data.Span);
        }

        /// <summary>
        /// Log input data in Hex
        /// </summary>
        /// <param name="driverName">The name of the driver, printed in the log</param>
        /// <param name="data">The input data</param>
        public static void LogHexIn(this ILogger logger, ReadOnlySpan<byte> data)
        {
            logger.LogTrace("<< {0}", Utils.ByteArrayToString(data));
        }

        /// <summary>
        /// Log output data in Hex
        /// </summary>
        /// <param name="driverName">The name of the driver, printed in the log</param>
        /// <param name="data">The output data</param>
        public static void LogHexOut(this ILogger logger, ReadOnlySpan<byte> data)
        {
            logger.LogTrace(">> {0}", Utils.ByteArrayToString(data));
        }
    }
}
