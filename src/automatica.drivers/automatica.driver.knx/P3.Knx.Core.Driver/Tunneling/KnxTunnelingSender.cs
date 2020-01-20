using System;
using System.Net.Sockets;
using Automatica.Core.Driver.Utility;
using Microsoft.Extensions.Logging;
using P3.Knx.Core.Driver.Frames;

namespace P3.Knx.Core.Driver.Tunneling
{
    internal class KnxTunnelingSender : KnxSender
    {
        private readonly UdpClient _client;
        public KnxTunnelingSender(KnxConnection connection, UdpClient client) : base(connection)
        {
            _client = client;
        }

        internal override bool SendFrame(KnxFrame frame)
        {
            var byteFrame = frame.ToFrame();

            KnxHelper.Logger.LogTrace($"Writing {frame}");
            KnxHelper.Logger.LogHexOut(byteFrame);

            try
            {
                var length = _client?.Send(byteFrame, byteFrame.Length);

                return length == byteFrame.Length;
            }
            catch (Exception e)
            {
                KnxHelper.Logger.LogError(e, "Error writing value...");
            }

            return false;
        }

        internal override void Start()
        {
           
        }

        internal override void Stop()
        {
            _client.Dispose();
        }
    }
}
