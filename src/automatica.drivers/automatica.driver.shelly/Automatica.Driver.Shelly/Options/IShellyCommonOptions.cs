using System;

namespace Automatica.Driver.Shelly.Options
{
    /// <summary>
    /// Common options across all shelly devices
    /// </summary>
    public interface IShellyCommonOptions : IShellyAuthOptions
    {
        /// <summary>
        /// URI for the Shelly device
        /// </summary>
        Uri ServerUri { get; }
        /// <summary>
        /// Default timeout for HTTP requests if a specific timeout is not supplied
        /// </summary>
        TimeSpan? DefaultTimeout { get; }
    }
}