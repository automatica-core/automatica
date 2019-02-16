using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.Driver.Utility;
using Microsoft.Extensions.Logging;
using P3.Driver.MBus.Config;
using P3.Driver.MBus.Frames;

namespace P3.Driver.MBus.Udp
{
    public class MBusUdp : MBusConnection
    {
        private UdpClient _udpClient;
        private bool _connected = false;
        private readonly System.Timers.Timer _receiveTimeoutTimer = new System.Timers.Timer();
        private readonly MBusUdpConfig _tcpConfig;
        private CancellationTokenSource _cts;

        public MBusUdp(MBusUdpConfig config, ITelegramMonitorInstance monitor, ILogger logger) : base(config, monitor, logger)
        {
            _tcpConfig = config;
            _receiveTimeoutTimer.Elapsed += _receiveTimeoutTimer_Elapsed;
        }

        private void _receiveTimeoutTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _cts?.Cancel(true);
            _receiveTimeoutTimer.Stop();
            _connected = false;

        }

        public override bool IsConnected()
        {
            return _connected;
        }

        public override bool Open()
        {
            try
            {
                _receiveTimeoutTimer.Stop();
                OpenConnection();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        private void OpenConnection()
        {
            _udpClient?.Close();
            if (_tcpConfig != null)
            {
                _udpClient = new UdpClient();
                _receiveTimeoutTimer.Interval = _tcpConfig.Timeout;
                _udpClient.Connect(_tcpConfig.IpAddress, _tcpConfig.Port);
                _connected = true;
            }
            Thread.Sleep(10);
        }

        protected override async Task WriteFrame(MBusFrame frame)
        {
            OpenConnection();
            var data = frame.ToByteFrame();
            Logger.LogHexOut(data);
            await _udpClient.SendAsync(data.Span.ToArray(), data.Length);
        }

        protected override async Task<MBusFrame> ReadFrame()
        {
            try
            {
                _receiveTimeoutTimer.Start();
                _cts = new CancellationTokenSource();
                var read = await Automatica.Core.Base.Extensions.AsyncExtensions.WithCancellation(_udpClient.ReceiveAsync(), _cts.Token);
                
                _receiveTimeoutTimer.Stop();
                var data = read.Buffer;
                _cts = null;

                Logger.LogHexIn(data);

                if (read.Buffer.Length >= 4 && data[1] == data[2] && data[0] == data[3])
                {
                    var length = data[1];

                    if (data.Length == length + 6)
                    {
                        if (data[0] == MBus.ShortFrameStart)
                        {
                            return MBusFrame.FromByteArray(MBusFrameType.ShortFrame, Logger, data);
                        }
                        if (data[0] == MBus.ControlFrameLongFrameStart)
                        {
                            return MBusFrame.FromByteArray(
                                data[2] == 3 ? MBusFrameType.ControlFrame : MBusFrameType.LongFrame, Logger, data);
                        }
                    }
                }
                else if (data.Length == 1 && data[0] == MBus.SingleCharFrame)
                {
                    return new AckFrame();
                }
            }
            catch (Exception e)
            {
                _receiveTimeoutTimer.Stop();
                Logger.LogError($"Error reading mbus frame {e}");
            }

            _receiveTimeoutTimer.Stop();
            return null;
        }

        public override bool Close()
        {
            _udpClient.Close();
            _udpClient = null;
            return true;
        }
    }
}
