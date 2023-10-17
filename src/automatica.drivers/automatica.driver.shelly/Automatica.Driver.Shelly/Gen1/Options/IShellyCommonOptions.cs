using System;
using Automatica.Driver.Shelly.Common;

namespace Automatica.Driver.Shelly.Gen1.Options
{
    /// <summary>
    /// Common options across all shelly devices
    /// </summary>
    public interface IShellyCommonOptions : IShellyAuthOptions, IShellyAddress
    {
        /// <summary>
        /// URI for the Shelly device
        /// </summary>
        Uri ServerUri { get; }
        /// <summary>
        /// Default timeout for HTTP requests if a specific timeout is not supplied
        /// </summary>
        TimeSpan? DefaultTimeout { get; }


        public Guid SourceId { get; set; }
    }
}