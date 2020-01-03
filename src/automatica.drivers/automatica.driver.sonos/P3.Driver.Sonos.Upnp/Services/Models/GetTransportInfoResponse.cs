namespace P3.Driver.Sonos.Upnp.Services.Models
{
    public class GetTransportInfoResponse
    {
        public TransportState CurrentTransportState { get; set; }

        public string CurrentTransportStatus { get; set; }

        public string CurrentSpeed { get; set; }
    }
}
