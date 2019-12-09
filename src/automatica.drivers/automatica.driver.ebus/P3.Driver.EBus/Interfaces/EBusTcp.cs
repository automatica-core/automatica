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


        public EBusTcp(IEBusIpConfig config) : base(config)
        {
            _config = config;
            _tcpClient = new TcpClient();
        }

        protected override async Task<byte[]> Receive()
        {
            byte[] header = new byte[5];

            var value = _tcpClient.GetStream().ReadByte();

            if (value == Constants.SYN_BYTE)
            {
                Console.WriteLine("SYN rec");
                return new byte[0];
            }

            await _tcpClient.GetStream().ReadAsync(header);
            Console.WriteLine(Utils.ByteArrayToString(in header));

            await Task.Delay(100);
            return header;
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
