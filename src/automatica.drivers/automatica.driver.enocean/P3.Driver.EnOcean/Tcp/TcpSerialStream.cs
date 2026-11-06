using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver.Utility;
using Microsoft.Extensions.Logging;
using P3.Driver.EnOcean.Data;
using P3.Driver.EnOcean.Data.Packets;

namespace P3.Driver.EnOcean.Tcp
{
    public class TcpSerialStream : BaseStream
    {
        private readonly string _ip;
        private readonly int _port;
        private TcpClient _tcpClient;
        private readonly TcpSerialStreamReceiver _tcpReceiver;
        private volatile bool _connectionLost;

        private const int ReadTimeoutMs = 5000;
        private static readonly TimeSpan ReconnectInitialDelay = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan ReconnectMaxDelay = TimeSpan.FromSeconds(60);
        public override event EventHandler<PacketReceivedEventArgs> TelegramReceived;

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        private NetworkStream _stream;

        public TcpSerialStream(string ip, int port)
        {
            _ip = ip;
            _port = port;

            _tcpReceiver = new TcpSerialStreamReceiver(this);
        }

        public override async Task<bool> Open()
        {
            await ConnectAsync(CancellationToken.None);

            _tcpReceiver.Start();

            await Task.Delay(200);

            return _tcpClient.Connected;
        }

        private async Task ConnectAsync(CancellationToken token)
        {
            var client = new TcpClient();
            try
            {
                await client.ConnectAsync(_ip, _port, token);

                var stream = client.GetStream();
                stream.ReadTimeout = ReadTimeoutMs;
                stream.WriteTimeout = ReadTimeoutMs;

                _tcpClient = client;
                _stream = stream;
                _connectionLost = false;
            }
            catch
            {
                client.Dispose();
                throw;
            }
        }

        private void CloseClient()
        {
            try
            {
                _tcpClient?.Close();
            }
            catch (Exception e)
            {
                Logger.Logger.Instance.LogError($"Could not close tcp client {e}", e);
            }
        }

        private void MarkConnectionLost(string reason)
        {
            Logger.Logger.Instance.LogWarning($"EnOcean tcp connection lost: {reason}");
            _connectionLost = true;
            CloseClient();
        }

        internal int Available
        {
            get
            {
                try
                {
                    return _tcpClient?.Available ?? 0;
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        internal async Task<bool> CheckConnectionAsync()
        {
            if (_connectionLost)
            {
                return false;
            }

            await _semaphoreSlim.WaitAsync();
            try
            {
                var socket = _tcpClient?.Client;
                if (socket == null)
                {
                    MarkConnectionLost("no socket");
                    return false;
                }

                // readable with no data available means the remote side closed the connection
                if (socket.Poll(0, SelectMode.SelectRead) && socket.Available == 0)
                {
                    MarkConnectionLost("remote closed the connection");
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                MarkConnectionLost(e.Message);
                return false;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        internal async Task ReconnectAsync(CancellationToken token)
        {
            var delay = ReconnectInitialDelay;
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(delay, token);

                await _semaphoreSlim.WaitAsync(token);
                try
                {
                    CloseClient();
                    await ConnectAsync(token);
                    Logger.Logger.Instance.LogInformation($"EnOcean tcp connection re-established");
                    return;
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Logger.Logger.Instance.LogWarning($"Could not reconnect to {_ip}:{_port}, retry in {delay.TotalSeconds}s: {e.Message}");
                }
                finally
                {
                    _semaphoreSlim.Release();
                }

                delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, ReconnectMaxDelay.Ticks));
            }
        }

        public override async Task<bool> Close()
        {
            _tcpReceiver.Stop();

            await _semaphoreSlim.WaitAsync();
            try
            {
                CloseClient();
            }
            finally
            {
                _semaphoreSlim.Release();
            }
            return true;
        }

        public override async Task<EnOceanPacket> WriteFrame(EnOceanPacket frame)
        {
            try
            {
                _tcpReceiver.Pause();
                Logger.Logger.Instance.LogHexOut(frame.RawData);
                _tcpClient.Client.SendAsync(frame.RawData.ToArray());
                return await ReadFrame();
            }
            catch (Exception e)
            {
                Logger.Logger.Instance.LogError($"Could not write frame {e}", e);
            }
            finally
            {
                _tcpReceiver.Continue();
            }

            return null;
        }

  

        public override void Pause()
        {

        }

        public override void Continue()
        {
        }

        private async Task<EnOceanPacket> ReadFrameInternal()
        {
            if (_connectionLost)
            {
                return null;
            }

            var timeoutCts = new CancellationTokenSource(ReadTimeoutMs);
            try
            {
                int firstRead;
                try
                {
                    firstRead = _stream.ReadByte();
                }
                catch (IOException ioe) when ((ioe.InnerException as SocketException)?.SocketErrorCode == SocketError.TimedOut)
                {
                    Logger.Logger.Instance.LogWarning($"Timeout waiting for frame");
                    return null;
                }

                if (firstRead < 0)
                {
                    MarkConnectionLost("remote closed the connection");
                    return null;
                }

                var firstChar = (byte)firstRead;

                if (firstChar == EnOcean.SyncByte)
                {
                    var dataLen = new byte[2];
                    var dataLenRead = await _stream.ReadAsync(dataLen, 0, 2, timeoutCts.Token);

                    if (dataLenRead == 2)
                    {
                        var dataLenShort = BitConverter.ToUInt16(dataLen.Reverse().ToArray(), 0);

                        var optLen = (byte)_stream.ReadByte();
                        var packetType = (byte)_stream.ReadByte();
                        var crc8Header = (byte)_stream.ReadByte();

                        var header = new byte[] { firstChar, dataLen[0], dataLen[1], optLen, packetType, crc8Header };

                        var data = new byte[dataLenShort];
                        Memory<byte> dataMemory = new Memory<byte>(data);
                        var read = await _stream.ReadAsync(dataMemory, timeoutCts.Token);

                        if (read == dataLenShort)
                        {
                            var optData = new byte[optLen];
                            Memory<byte> optMemory = new Memory<byte>(optData);

                            read = await _stream.ReadAsync(optMemory, timeoutCts.Token);

                            if (read == optLen)
                            {
                                var crcData = (byte)_stream.ReadByte();
                                var packet = EnOceanPacket.Parse(new Memory<byte>(header), dataMemory, optMemory,
                                    crcData);

                                Logger.Logger.Instance.LogHexIn(packet.RawData);
                                return packet;
                            }
                        }
                        else
                        {
                            Logger.Logger.Instance.LogDebug($"In buffer length to short {read}/{dataLenShort}");
                        }
                    }
                    else
                    {
                        Logger.Logger.Instance.LogDebug($"Could not read length of package");
                    }
                }
                else
                {
                    Logger.Logger.Instance.LogDebug($"Read wrong sync byte {firstChar} - {firstChar:X2} - {_tcpClient.Available} bytes in buffer");
                    if (_tcpClient.Available > 0)
                    {
                        return await ReadFrameInternal();
                    }
                }
            }
            catch (IOException ioe)
            {
                Logger.Logger.Instance.LogError($"Could not read frame {ioe}", ioe);
                MarkConnectionLost(ioe.Message);
            }
            catch (OperationCanceledException)
            {
                MarkConnectionLost("timeout reading frame");
            }
            catch (Exception e)
            {
                Logger.Logger.Instance.LogError($"Could not read frame {e}", e);
            }
            finally
            {
                timeoutCts.Dispose();
            }
            return null;
        }


        public async Task<EnOceanPacket> ReadFrame()
        {
            try
            {
                await _semaphoreSlim.WaitAsync();
                return await ReadFrameInternal();
            }
            finally
            {
                _semaphoreSlim.Release(1);
            }
        }

        public void OnPacketReceived(EnOceanTelegram telegram)
        {
            TelegramReceived?.Invoke(this, new PacketReceivedEventArgs(telegram));
        }
    }


}
