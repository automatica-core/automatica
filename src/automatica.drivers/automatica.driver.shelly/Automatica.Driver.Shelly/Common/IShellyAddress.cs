namespace Automatica.Driver.Shelly.Common
{
    public interface IShellyAddress
    {
        string IpAddress { get; set; }
        string ShellyId { get; set; }
    }
}
