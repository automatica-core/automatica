using P3.Driver.EnOcean.Data;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace P3.Driver.EnOcean.Tcp
{
    internal class TcpSerialStreamReceiver
    {
        private readonly TcpSerialStream _tcpStream;
        private Task _receiverThread;
        private bool _isRunning;

        private bool _pause = false;

        private CancellationTokenSource _cancellationToken;

        public TcpSerialStreamReceiver(TcpSerialStream tcpStream)
        {
            _tcpStream = tcpStream;
        }

        private async Task Run(CancellationToken token)
        {
            while (_isRunning && !token.IsCancellationRequested)
            {
                try
                {
                    if (_pause)
                    {
                        await Task.Delay(100, token);
                        continue;
                    }

                    if (!await _tcpStream.CheckConnectionAsync())
                    {
                        await _tcpStream.ReconnectAsync(token);
                        continue;
                    }

                    if (_tcpStream.Available > 0)
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
                            catch (Exception e)
                            {
                                Logger.Logger.Instance.LogError($"Could not handle telegram {e}", e);
                            }
                        }
                    }

                    await Task.Delay(100, token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception e)
                {
                    Logger.Logger.Instance.LogError($"Error in tcp receive loop {e}", e);
                    try
                    {
                        await Task.Delay(1000, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
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
            var token = _cancellationToken.Token;
            _receiverThread = Task.Run(() => Run(token), CancellationToken.None);

        }
        internal void Stop()
        {
            _isRunning = false;
            _cancellationToken?.Cancel();
        }
    }
}
