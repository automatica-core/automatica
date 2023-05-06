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
        private readonly TcpClient _tcpClient;
        private readonly TcpSerialStreamReceiver _tcpReceiver;
        public override event EventHandler<PacketReceivedEventArgs> TelegramReceived;

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        private NetworkStream _stream;

        public TcpSerialStream(string ip, int port)
        {
            _ip = ip;
            _port = port;

            _tcpClient = new TcpClient();
            _tcpReceiver = new TcpSerialStreamReceiver(_tcpClient, this);
        }

        public override async Task<bool> Open()
        {
            await Task.CompletedTask;
            
            await _tcpClient.ConnectAsync(_ip, _port);

            _stream = _tcpClient.GetStream();
            _tcpReceiver.Start();

            await Task.Delay(200);

            return _tcpClient.Connected;
        }

       

        public override Task<bool> Close()
        {
            _tcpReceiver.Stop();
            _tcpClient.Close();
            return Task.FromResult(true);
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
            try
            {
                var firstChar = (byte)_stream.ReadByte();

                if (firstChar == EnOcean.SyncByte)
                {
                    var dataLen = new byte[2];
                    var dataLenRead = await _stream.ReadAsync(dataLen, 0, 2);

                    if (dataLenRead == 2)
                    {
                        var dataLenShort = BitConverter.ToUInt16(dataLen.Reverse().ToArray(), 0);

                        var optLen = (byte)_stream.ReadByte();
                        var packetType = (byte)_stream.ReadByte();
                        var crc8Header = (byte)_stream.ReadByte();

                        var header = new byte[] { firstChar, dataLen[0], dataLen[1], optLen, packetType, crc8Header };

                        var data = new byte[dataLenShort];
                        Memory<byte> dataMemory = new Memory<byte>(data);
                        var read = await _stream.ReadAsync(dataMemory);

                        if (read == dataLenShort)
                        {
                            var optData = new byte[optLen];
                            Memory<byte> optMemory = new Memory<byte>(optData);

                            read = await _stream.ReadAsync(optMemory);

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
                await Close();
                Thread.Sleep(100);
                await Open();
            }
            catch (Exception e)
            {
                Logger.Logger.Instance.LogError($"Could not read frame {e}", e);
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
