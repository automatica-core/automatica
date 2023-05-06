using P3.Driver.EnOcean.Data;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.EnOcean.Tcp
{
    internal class TcpSerialStreamReceiver
    {
        private readonly TcpClient _tcpClient;
        private readonly TcpSerialStream _tcpStream;
        private Task<Task> _receiverThread;
        private bool _isRunning;

        private bool _pause = false;

        private CancellationTokenSource _cancellationToken;

        public TcpSerialStreamReceiver(TcpClient tcpClient, TcpSerialStream tcpStream)
        {
            _tcpClient = tcpClient;
            _tcpStream = tcpStream;
        }

        private async Task Run()
        {
            while (_isRunning)
            {
                if (!_isRunning)
                {
                    break;
                }

                if (_pause)
                {
                    await Task.Delay(100);
                    continue;
                }

                if (_tcpClient.Available > 0)
                {
                    var packet = await _tcpStream.ReadFrame();

                    if (packet != null)
                    {
                        try
                        {
                            var telegram = EnOceanTelegramFactory.FromPacket(packet);
                            _tcpStream.OnPacketReceived(telegram);
                        }
                        catch (NotImplementedException)
                        {
                            // ignore
                        }
                    }
                }

                await Task.Delay(100);
            }
        }
        internal void Pause()
        {
            _pause = true;
        }

        internal void Continue()
        {
            _pause = false;
        }

        internal void Start()
        {
            _isRunning = true;
            _cancellationToken = new CancellationTokenSource();
            _receiverThread = Task.Factory.StartNew(Run);

        }
        internal void Stop()
        {
            _isRunning = false;
            _cancellationToken.Cancel();
        }
    }
}
