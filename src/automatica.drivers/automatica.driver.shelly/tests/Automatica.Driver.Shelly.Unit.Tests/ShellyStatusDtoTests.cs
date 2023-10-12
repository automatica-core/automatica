using System.IO;
using Automatica.Driver.Shelly.Dtos;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Automatica.Driver.Shelly.Unit.Tests
{
    public class ShellyStatusDtoTests
    {
        [Fact]
        public void Shelly1PmStatusDto_CanDeserializeJsonResponseFromShelly1()
        {
            var status = GetShellyStatusFromFile("./status_response.json");

            // assert
            status.Should().NotBeNull();
            status.Uptime.Should().Be(32372);
            status.RamTotal.Should().Be(50568);
            status.RamFree.Should().Be(39748);
            status.FileSystemSize.Should().Be(233681);
            status.FileSystemFree.Should().Be(152106);
            status.HasUpdate.Should().BeTrue();
            status.MacAddress.Should().Be("0123456789AB");
            status.WiFiStatus.Connected.Should().BeTrue();
            status.WiFiStatus.SSID.Should().Be("WuTangLan");
            status.WiFiStatus.Ip.Should().Be("172.16.1.115");
            status.WiFiStatus.RSSI.Should().Be(-87);
            status.ShellyCloud.Enabled.Should().BeTrue();
            status.ShellyCloud.Connected.Should().BeTrue();
            status.Mqtt.Connected.Should().BeTrue();
            status.Time.Should().Be("17:09");
            status.UnixTime.Should().Be(1588525744);
            status.Serial.Should().Be(123);

        }

        [Fact]
        public void Shelly1PMStatusDto_CanDeserializeJsonResponseFromShelly1PM()
        {
            var status = GetShellyStatusFromFile("./status_response_1PM.json");

            // assert
            status.Should().NotBeNull();
            status.Meters.Count.Should().Be(1);
            status.Meters[0].Power.Should().Be(95.35);
            status.Meters[0].IsValid.Should().BeTrue();
            status.Meters[0].TimeStamp.Should().Be(1588532068);
            status.Meters[0].Total.Should().Be(8849733);
            status.Relays.Count.Should().Be(2);
            status.Relays[0].IsOn.Should().BeTrue();
            status.Relays[0].HasTimer.Should().BeFalse();
            status.Relays[0].TimerRemaining.Should().Be(0);
            status.Relays[1].IsOn.Should().BeFalse();
            status.Relays[1].HasTimer.Should().BeTrue();
            status.Relays[1].TimerRemaining.Should().Be(60);
        }

        [Fact]
        public void Shelly25PMStatusDto_CanDeserializeJsonResponseFromShelly1PM()
        {
            var status = GetShellyStatusFromFile("./status_response_25PM.json");

            // assert
            status.Should().NotBeNull();
            status.Meters.Count.Should().Be(2);
            status.Meters[0].Power.Should().Be(0);
            status.Meters[0].IsValid.Should().BeTrue();
            status.Meters[0].TimeStamp.Should().Be(1696970387);
            status.Meters[0].Total.Should().Be(9470);
            status.Relays.Count.Should().Be(0);

            status.Rollers.Count.Should().Be(1);
        }

        private ShellyStatusDto GetShellyStatusFromFile(string statusResponsePmJson)
        {
            string json = File.ReadAllText(statusResponsePmJson);
            var status = JsonConvert.DeserializeObject<ShellyStatusDto>(json);
            return status;
        }
    }
}
