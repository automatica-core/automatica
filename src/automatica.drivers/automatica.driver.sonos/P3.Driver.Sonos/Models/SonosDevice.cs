using System;

namespace P3.Driver.Sonos.Models
{
    public class SonosDevice
    {
        public string IpAddress { get; }

        public string ModelNumber { get; set; }
		
        public string ModelName { get; set; }
		
        public string ModelDescription { get; set; }
		
        public string DeviceType { get; set; }
		
        public string FriendlyName { get; set; }
		
        public string SoftwareVersion { get; set; }
		
        public string HardwareVersion { get; set; }
		
        public string SerialNumber { get; set; }
		
        public string Udn { get; set; }
		
        public string RoomName { get; set; }

        public SonosDevice(string ipAddress)
        {
            if(string.IsNullOrEmpty(ipAddress))
                throw new ArgumentException("IP Address cannot be null or empty.", nameof(ipAddress));

            IpAddress = ipAddress;
        }
    }
}
