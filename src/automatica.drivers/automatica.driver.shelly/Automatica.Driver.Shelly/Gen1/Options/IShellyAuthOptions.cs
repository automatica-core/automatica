using Automatica.Driver.Shelly.Common;

namespace Automatica.Driver.Shelly.Gen1.Options
{
    /// <summary>
    /// Common authentication options across all shelly devices
    /// </summary>
    public interface IShellyAuthOptions : IShellyCommonAuthOptions
    {
        string UserName { get; }
    }
}