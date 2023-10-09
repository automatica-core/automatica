namespace Automatica.Driver.Shelly.Options
{
    /// <summary>
    /// Common authentication options across all shelly devices
    /// </summary>
    public interface IShellyAuthOptions
    {
        string UserName { get; }
        string Password { get; }
    }
}