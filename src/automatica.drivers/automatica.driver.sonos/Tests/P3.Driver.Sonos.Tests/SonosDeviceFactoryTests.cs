using System.Xml;
using P3.Driver.Sonos.Device;
using Xunit;

namespace P3.Driver.Sonos.Tests
{
    public class SonosDeviceFactoryTests
    {
        private const string DeviceDescription =
            "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" +
            "<root xmlns=\"urn:schemas-upnp-org:device-1-0\">" +
            "<specVersion><major>1</major><minor>0</minor></specVersion>" +
            "<device>" +
            "<deviceType>urn:schemas-upnp-org:device:ZonePlayer:1</deviceType>" +
            "<friendlyName>192.168.1.20 - Sonos One - RINCON_000E58123456</friendlyName>" +
            "<manufacturer>Sonos, Inc.</manufacturer>" +
            "<modelNumber>S18</modelNumber>" +
            "<modelDescription>Sonos One</modelDescription>" +
            "<modelName>Sonos One</modelName>" +
            "<softwareVersion>78.1-52020</softwareVersion>" +
            "<hardwareVersion>1.20.1.6-2</hardwareVersion>" +
            "<serialNum>00-0E-58-12-34-56:A</serialNum>" +
            "<UDN>uuid:RINCON_000E58123456</UDN>" +
            "<roomName>Kitchen</roomName>" +
            "<deviceList><device><deviceType>urn:schemas-upnp-org:device:MediaRenderer:1</deviceType>" +
            "<friendlyName>Kitchen - Sonos One Media Renderer</friendlyName></device></deviceList>" +
            "</device></root>";

        [Fact]
        public void CreateFor_DeviceDescription_MapsAllFields()
        {
            var device = new SonosDeviceFactory().CreateFor("192.168.1.20", DeviceDescription);

            Assert.Equal("192.168.1.20", device.IpAddress);
            Assert.Equal("urn:schemas-upnp-org:device:ZonePlayer:1", device.DeviceType);
            Assert.Equal("192.168.1.20 - Sonos One - RINCON_000E58123456", device.FriendlyName);
            Assert.Equal("S18", device.ModelNumber);
            Assert.Equal("Sonos One", device.ModelName);
            Assert.Equal("Sonos One", device.ModelDescription);
            Assert.Equal("78.1-52020", device.SoftwareVersion);
            Assert.Equal("1.20.1.6-2", device.HardwareVersion);
            Assert.Equal("00-0E-58-12-34-56:A", device.SerialNumber);
            Assert.Equal("uuid:RINCON_000E58123456", device.Udn);
            Assert.Equal("Kitchen", device.RoomName);
        }

        [Fact]
        public void CreateFor_MissingElements_AreEmptyStrings()
        {
            var device = new SonosDeviceFactory().CreateFor("10.0.0.1",
                "<root xmlns=\"urn:schemas-upnp-org:device-1-0\"><device><roomName>Office</roomName></device></root>");

            Assert.Equal("Office", device.RoomName);
            Assert.Equal(string.Empty, device.ModelName);
            Assert.Equal(string.Empty, device.Udn);
        }

        [Fact]
        public void CreateFor_WrongNamespace_YieldsEmptyValues()
        {
            var device = new SonosDeviceFactory().CreateFor("10.0.0.1", "<root><device><roomName>Office</roomName></device></root>");

            Assert.Equal(string.Empty, device.RoomName);
        }

        [Fact]
        public void CreateFor_DeviceDescriptionWithDtd_IsRejected()
        {
            var xml = "<?xml version=\"1.0\"?><!DOCTYPE root [<!ENTITY xxe \"INJECTED\">]>" +
                      "<root xmlns=\"urn:schemas-upnp-org:device-1-0\"><device><roomName>&xxe;</roomName></device></root>";

            Assert.Throws<XmlException>(() => new SonosDeviceFactory().CreateFor("10.0.0.1", xml));
        }
    }
}
