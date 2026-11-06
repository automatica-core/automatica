using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using P3.Driver.EnOcean.Data;
using P3.Driver.EnOcean.Data.Packets;
using P3.Driver.EnOcean.Tcp;
using Xunit;

namespace P3.Driver.EnOcean.Tests
{
    public class StreamCrcTests
    {
        // F6 0x37 from FEF7D96E and A5 0x0123450E from 0181B744, both with valid CRC8H/CRC8D
        private const string GoodRps = "55000707017AF637FEF7D96E3001FFFFFFFF4600DC";
        private const string Good4Bs = "55000A0701EBA50123450E0181B7440001FFFFFFFF460004";

        private static byte[] Hex(string hex) => Automatica.Core.Driver.Utility.Utils.StringToByteArray(hex);

        [Fact]
        public async Task TcpStream_DropsCorruptFrames_AndResyncs()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var stream = new TcpSerialStream("127.0.0.1", port);
            var received = new ConcurrentQueue<EnOceanTelegram>();
            stream.TelegramReceived += (s, e) => received.Enqueue(e.Telegram);

            var acceptTask = listener.AcceptTcpClientAsync();
            Assert.True(await stream.Open());
            using var server = await acceptTask;

            var badDataCrc = Hex(GoodRps);
            badDataCrc[badDataCrc.Length - 1] ^= 0xFF;

            var bytes = new List<byte> { 0x00, 0x12 };                    // garbage before the first sync byte
            bytes.AddRange(new byte[] { 0x55, 0x00, 0x07, 0x07, 0x01, 0x00 }); // header with bad CRC8H
            bytes.AddRange(Hex(GoodRps));
            bytes.AddRange(badDataCrc);                                    // bad CRC8D -> dropped
            bytes.AddRange(Hex(Good4Bs));

            await server.GetStream().WriteAsync(bytes.ToArray());

            var sw = Stopwatch.StartNew();
            while (received.Count < 2 && sw.Elapsed < TimeSpan.FromSeconds(10))
            {
                await Task.Delay(50);
            }
            await Task.Delay(500);

            await stream.Close();
            listener.Stop();

            var telegrams = received.ToArray();
            Assert.Equal(2, telegrams.Length);

            var first = Assert.IsType<RadioErp1Packet>(telegrams[0]);
            Assert.Equal(Rorg.Rps, first.Rorg);
            Assert.Equal("FEF7D96E", first.SenderIdString);

            var second = Assert.IsType<RadioErp1Packet>(telegrams[1]);
            Assert.Equal(Rorg.FourBs, second.Rorg);
            Assert.Equal("0181B744", second.SenderIdString);
        }
    }
}
