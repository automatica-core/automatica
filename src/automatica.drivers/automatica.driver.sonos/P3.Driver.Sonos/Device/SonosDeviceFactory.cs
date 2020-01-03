using System.Linq;
using System.Xml.Linq;
using P3.Driver.Sonos.Models;

namespace P3.Driver.Sonos.Device
{
    internal class SonosDeviceFactory
    {
        private static readonly XNamespace UpnpNamespace = @"urn:schemas-upnp-org:device-1-0";

        public SonosDevice CreateFor(string ipAddress, string xml)
        {
            var xDoc = XDocument.Parse(xml);

            return new SonosDevice(ipAddress)
            {
                DeviceType = GetDevicePropertyValue(xDoc, "deviceType"),
                FriendlyName = GetDevicePropertyValue(xDoc, "friendlyName"),
                ModelNumber = GetDevicePropertyValue(xDoc, "modelNumber"),
                ModelName = GetDevicePropertyValue(xDoc, "modelName"),
                ModelDescription = GetDevicePropertyValue(xDoc, "modelDescription"),
                SoftwareVersion = GetDevicePropertyValue(xDoc, "softwareVersion"),
                HardwareVersion = GetDevicePropertyValue(xDoc, "hardwareVersion"),
                SerialNumber = GetDevicePropertyValue(xDoc, "serialNum"),
                Udn = GetDevicePropertyValue(xDoc, "UDN"),
                RoomName = GetDevicePropertyValue(xDoc, "roomName")
            };
        }

        private static string GetDevicePropertyValue(XDocument xDoc, string elementName)
        {
            var node = xDoc.Descendants(UpnpNamespace + "root")
                           .Descendants(UpnpNamespace + "device")
                           .Descendants(UpnpNamespace + elementName)
                           .FirstOrDefault();

            return node?.Value ?? string.Empty;
        }
    }
}
