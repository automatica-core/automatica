using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Automatica.Core.Driver.Utility;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using P3.Driver.EBus.Config;

namespace P3.Driver.EBus.Interfaces
{
    public class EBusTcp : EBusIp
    {
        private readonly IEBusIpConfig _config;
        private readonly TcpClient _tcpClient;

        private byte[] _buffer = new byte[1024];


        public EBusTcp(IEBusIpConfig config) : base(config)
        {
            _config = config;
            _tcpClient = new TcpClient();
        }

        protected override Task<byte[]> Receive()
        {
            var value = _tcpClient.GetStream().ReadByte();
            return Task.FromResult(Add((byte)value));
        }

        protected override async Task StartReceive()
        {
            await _tcpClient.ConnectAsync(_config.Ip, _config.Port);

            var byteArray = new byte[_tcpClient.Available];
            await _tcpClient.GetStream().ReadAsync(byteArray);
        }

        protected override Task StopReceive()
        {
            _tcpClient.Dispose();

            return Task.CompletedTask;
        }
    }
}
