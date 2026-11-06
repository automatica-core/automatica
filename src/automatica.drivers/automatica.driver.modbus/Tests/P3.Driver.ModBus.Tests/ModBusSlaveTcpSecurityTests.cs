using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.ModBusDriver.Slave.Tcp;
using Xunit;

namespace P3.Driver.ModBus.Tests
{
    public class ModBusSlaveTcpSecurityTests
    {
        private static ModBusSlaveTcpDriver CreateSlave(Action<ModBusSlaveTcpConfig> configure = null)
        {
            var config = new ModBusSlaveTcpConfig
            {
                Port = 0,
                BindAddress = IPAddress.Loopback,
                DeviceIds = new List<int> { 1 },
                IgnoreDeviceId = false
            };
            configure?.Invoke(config);

            var slave = new ModBusSlaveTcpDriver(config, new EmptyTelegramMonitorInstance(), NullLogger.Instance);
            for (ushort register = 0; register < 4; register++)
            {
                slave.InitHoldingRegister(1, register, 0);
            }
            slave.Open();
            return slave;
        }

        private static byte[] WriteSingleRegisterRequest(ushort register, ushort value)
        {
            return new byte[]
            {
                0x00, 0x01, 0x00, 0x00, 0x00, 0x06, // MBAP
                0x01, 0x06, (byte)(register >> 8), (byte)register, (byte)(value >> 8), (byte)value
            };
        }

        private static byte[] ReadHoldingRegistersRequest(ushort register, ushort count)
        {
            return new byte[]
            {
                0x00, 0x02, 0x00, 0x00, 0x00, 0x06,
                0x01, 0x03, (byte)(register >> 8), (byte)register, (byte)(count >> 8), (byte)count
            };
        }

        private static async Task<byte[]> SendAndReceive(TcpClient client, byte[] request)
        {
            var stream = client.GetStream();
            await stream.WriteAsync(request, 0, request.Length);

            var buffer = new byte[260];
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                var read = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                Array.Resize(ref buffer, read);
            }
            return buffer;
        }

        private static async Task<bool> IsClosedByServer(TcpClient client)
        {
            try
            {
                var buffer = new byte[16];
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                {
                    return await client.GetStream().ReadAsync(buffer, 0, buffer.Length, cts.Token) == 0;
                }
            }
            catch (System.IO.IOException)
            {
                return true;
            }
        }

        [Fact]
        public async Task WritesAreAppliedByDefault()
        {
            var slave = CreateSlave();
            try
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(IPAddress.Loopback, slave.LocalPort);
                    var response = await SendAndReceive(client, WriteSingleRegisterRequest(3, 42));

                    Assert.Equal(0x06, response[7]);
                    Assert.Equal(42, slave.GetHoldingRegister(1, 3));
                }
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public async Task ReadOnlySlaveRejectsWrites()
        {
            var slave = CreateSlave(c => c.ReadOnly = true);
            try
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(IPAddress.Loopback, slave.LocalPort);
                    var response = await SendAndReceive(client, WriteSingleRegisterRequest(3, 42));

                    // exception response: function | 0x80, exception code 1 = illegal function
                    Assert.Equal(0x86, response[7]);
                    Assert.Equal(0x01, response[8]);
                    Assert.Equal(0, slave.GetHoldingRegister(1, 3));

                    var read = await SendAndReceive(client, ReadHoldingRegistersRequest(0, 2));
                    Assert.Equal(0x03, read[7]);
                }
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public async Task ClientsOutsideTheAllowListAreDisconnected()
        {
            var slave = CreateSlave(c => c.AllowedClients = ModBusSlaveTcpConfig.ParseAllowedClients("10.1.2.3, 192.168.0.0/16", out _));
            try
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(IPAddress.Loopback, slave.LocalPort);
                    await client.GetStream().WriteAsync(WriteSingleRegisterRequest(3, 42), 0, 12);

                    Assert.True(await IsClosedByServer(client));
                    Assert.Equal(0, slave.GetHoldingRegister(1, 3));
                }
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public async Task ConnectionsAboveTheLimitAreDisconnected()
        {
            var slave = CreateSlave(c => c.MaxConnections = 1);
            try
            {
                using (var first = new TcpClient())
                using (var second = new TcpClient())
                {
                    await first.ConnectAsync(IPAddress.Loopback, slave.LocalPort);
                    await SendAndReceive(first, ReadHoldingRegistersRequest(0, 1));

                    await second.ConnectAsync(IPAddress.Loopback, slave.LocalPort);
                    Assert.True(await IsClosedByServer(second));

                    // the first connection still works
                    var response = await SendAndReceive(first, ReadHoldingRegistersRequest(0, 1));
                    Assert.Equal(0x03, response[7]);
                }
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public async Task IdleConnectionsAreClosed()
        {
            var slave = CreateSlave(c => c.IdleTimeout = TimeSpan.FromMilliseconds(300));
            try
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(IPAddress.Loopback, slave.LocalPort);
                    Assert.True(await IsClosedByServer(client));
                }
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public async Task OversizedFramesCloseTheConnection()
        {
            var slave = CreateSlave();
            try
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(IPAddress.Loopback, slave.LocalPort);
                    await client.GetStream().WriteAsync(new byte[] { 0, 1, 0, 0, 0xFF, 0xFF }, 0, 6);

                    Assert.True(await IsClosedByServer(client));
                }
            }
            finally
            {
                slave.Close();
            }
        }

        [Theory]
        [InlineData("", "10.0.0.1", true)]
        [InlineData("10.0.0.1", "10.0.0.1", true)]
        [InlineData("10.0.0.1", "10.0.0.2", false)]
        [InlineData("10.0.0.0/24", "10.0.0.200", true)]
        [InlineData("10.0.0.0/24; 192.168.1.5", "192.168.1.5", true)]
        [InlineData("10.0.0.0/24", "::ffff:10.0.0.7", true)]
        public void AllowListMatchesAddressesAndNetworks(string allowList, string address, bool allowed)
        {
            var config = new ModBusSlaveTcpConfig { AllowedClients = ModBusSlaveTcpConfig.ParseAllowedClients(allowList, out var invalid) };

            Assert.Empty(invalid);
            Assert.Equal(allowed, config.IsClientAllowed(IPAddress.Parse(address)));
        }

        [Fact]
        public void InvalidAllowListEntriesAreReported()
        {
            var list = ModBusSlaveTcpConfig.ParseAllowedClients("10.0.0.1, nonsense, 300.1.1.1", out var invalid);

            Assert.Single(list);
            Assert.Equal(new[] { "nonsense", "300.1.1.1" }, invalid);
        }
    }
}
