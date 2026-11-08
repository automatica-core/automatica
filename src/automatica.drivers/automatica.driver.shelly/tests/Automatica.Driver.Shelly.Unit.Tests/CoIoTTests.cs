using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Automatica.Driver.Shelly.CoIoT;
using Automatica.Driver.Shelly.Unit.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Automatica.Driver.Shelly.Unit.Tests
{
    /// <summary>
    /// Gen1 CoIoT (CoAP over UDP): status packets, the /cit/d description and the listener.
    /// </summary>
    public class CoIoTTests
    {
        private const string Payload = "{\"G\":[[0,9103,0],[0,1101,1],[0,4101,12.5],[0,4103,720],[0,3104,41.3],[0,2102,\"S\"]]}";

        [Fact]
        public void Parse_StatusPacket()
        {
            var packet = CoIoTMessage.CreateStatusPacket("SHSW-PM", "F3D2B1", 42, Payload);

            var message = CoIoTMessage.Parse(packet);

            Assert.NotNull(message);
            Assert.True(message.IsStatus);
            Assert.Equal("SHSW-PM", message.DeviceType);
            Assert.Equal("F3D2B1", message.DeviceId);
            Assert.Equal(2, message.ProtocolVersion);
            Assert.Equal(42, message.Serial);
            Assert.Equal("cit/s", message.UriPath);
            Assert.Equal(6, message.Values.Count);
            Assert.Equal(12.5, message.Values.Single(a => a.SensorId == 4101).Value.ToObject<double>());
            Assert.Equal("S", message.Values.Single(a => a.SensorId == 2102).Value.ToObject<string>());
        }

        [Fact]
        public void Parse_LongDeviceIdAndBadPackets()
        {
            var message = CoIoTMessage.Parse(CoIoTMessage.CreateStatusPacket("SHHT-1", "E8DB84D5A9F0", 300, "{\"G\":[[0,3101,22.5]]}"));
            Assert.Equal("E8DB84D5A9F0", message!.DeviceId);
            Assert.Equal(300, message.Serial);

            Assert.Null(CoIoTMessage.Parse(Array.Empty<byte>()));
            Assert.Null(CoIoTMessage.Parse(new byte[] { 0x10, 0x01 }));

            var noJson = CoIoTMessage.Parse(CoIoTMessage.CreateStatusPacket("SHSW-1", "ABCDEF", 1, "not json"));
            Assert.NotNull(noJson);
            Assert.Empty(noJson.Values);
        }

        [Fact]
        public void Description_MapsSensorIdsToBlocks()
        {
            var description = CoIoTDescription.TryParse(FakeShellyDevice.Load("gen1_shelly1pm")["citd"]!.ToString());

            Assert.NotNull(description);
            var power = description.Get(4101)!;
            Assert.Equal("power", power.Description);
            Assert.Equal("W", power.Unit);
            Assert.Equal(("relay", 0), power.BlockNameAndIndex);
            Assert.Equal(("device", 0), description.Get(3104)!.BlockNameAndIndex);
            Assert.Null(description.Get(1));

            Assert.Null(CoIoTDescription.TryParse(""));
            Assert.Null(CoIoTDescription.TryParse("{broken"));
        }

        [Fact]
        public async Task Listener_RaisesUnicastPackets()
        {
            using var listener = new CoIoTListener(NullLogger.Instance, 0, joinMulticast: false);
            var received = new BlockingCollection<(CoIoTMessage Message, IPAddress Sender)>();
            listener.MessageReceived += (m, s) => received.Add((m, s));
            listener.Start();

            using var udp = new UdpClient(AddressFamily.InterNetwork);
            await udp.SendAsync(new byte[] { 1, 2, 3 }, new IPEndPoint(IPAddress.Loopback, listener.Port));
            var packet = CoIoTMessage.CreateStatusPacket("SHSW-PM", "F3D2B1", 7, Payload);
            await udp.SendAsync(packet, new IPEndPoint(IPAddress.Loopback, listener.Port));

            Assert.True(received.TryTake(out var item, TimeSpan.FromSeconds(10)));
            Assert.Equal("F3D2B1", item.Message.DeviceId);
            Assert.Equal(IPAddress.Loopback, item.Sender);
        }
    }
}
