using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using P3.Driver.EnOcean.Data;
using Automatica.Core.Driver.Utility;
using System.IO.Ports;
using Parity = System.IO.Ports.Parity;

namespace P3.Driver.EnOcean.Serial
{
    public class SerialStream : BaseStream
    {
        private readonly string _serialPort;
        private SerialPort _stream;
        private bool _connected;

        public override event EventHandler<PacketReceivedEventArgs> TelegramReceived;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public SerialStream(string serialPort)
        {
            _serialPort = serialPort;
        }

        // the receive path keeps reading while a command waits, it hands the RESPONSE over (see WriteFrame)
        public override void Pause()
        {
        }

        public override void Continue()
        {
        }

        public override Task<bool> Open()
        {
            try
            {
                _stream = new SerialPort(_serialPort, 57600, Parity.None, 8, StopBits.One)
                {
                   
                };

                _stream.DataReceived += _stream_DataReceived;

                _stream.Open();
                _connected = true;

            }
            catch (Exception e)
            {
                Logger.Logger.Instance.LogError("Could not open serial interface {0} {1}", _serialPort, e);

                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }

        private async void _stream_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            await _semaphoreSlim.WaitAsync();

            try 
            {
                var packet = await ReadFrameInternal();

                if (packet != null && !TryCompleteResponse(packet))
                {
                    try
                    {
                        var telegram = EnOceanTelegramFactory.FromPacket(packet);
                        TelegramReceived?.Invoke(this, new PacketReceivedEventArgs(telegram));
                    }
                    catch (NotImplementedException)
                    {
                        Logger.Logger.Instance.LogHexIn(packet.RawData);
                    }
                    catch (Exception ex)
                    {
                        Logger.Logger.Instance.LogError(ex, $"Could not parse telegram {ex}");
                        Logger.Logger.Instance.LogHexIn(packet.RawData);
                    }
                }

                // also after a dropped frame (crc error), the next frame may already be in the buffer
                if (_stream.IsOpen && _stream.BytesToRead > 0)
                {
                    _stream_DataReceived(sender, e);
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.Instance.LogError(ex, $"Error handling serial data received event {ex}");
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public override Task<bool> Close()
        {
            _stream?.Close();
            if (_stream != null)
            {
                _stream.DataReceived -= _stream_DataReceived;
            }
            _connected = false;
            return Task.FromResult(true);
        }


        public bool IsConnected()
        {
            return _connected;
        }

        public override async Task<EnOceanPacket> WriteFrame(EnOceanPacket frame)
        {
            try
            {
                // no read here: the DataReceived handler is the only reader. A second reader split the answer of the
                // stick with it (lost sync byte) and then blocked on the port, eating the start of the next telegram.
                return await SendAndWaitForResponse(() =>
                {
                    Logger.Logger.Instance.LogHexOut(frame.RawData);
                    _stream.Write(frame.RawData.ToArray(), 0, frame.RawData.Length);
                    return Task.CompletedTask;
                }, ResponseTimeout);
            }
            catch (Exception e)
            {
                Logger.Logger.Instance.LogError($"Could not write frame {e}", e);
            }
            return null;
        }

        private byte ReadByteInternal()
        {
            return TryTakePending(out var b) ? b : (byte)_stream.ReadByte();
        }

        private int ReadInternal(byte[] buffer)
        {
            var read = TakePending(buffer);
            // a frame can arrive in several chunks, keep reading until it is complete
            while (read < buffer.Length)
            {
                var chunk = _stream.Read(buffer, read, buffer.Length - read);
                if (chunk <= 0)
                {
                    break;
                }
                read += chunk;
            }
            return read;
        }

        private async Task<EnOceanPacket> ReadFrameInternal()
        {
            try
            {
                var firstChar = ReadByteInternal();

                if (firstChar == EnOcean.SyncByte)
                {
                    if (PendingCount == 0)
                    {
                        await Task.Delay(100);
                    }

                    var header = new byte[6];
                    header[0] = firstChar;
                    for (var i = 1; i < header.Length; i++)
                    {
                        header[i] = ReadByteInternal();
                    }

                    if (!CheckHeaderCrc(header))
                    {
                        // the header bytes were queued again, look for the next sync byte in them
                        return await ReadFrameInternal();
                    }

                    var dataLenShort = (ushort)((header[1] << 8) | header[2]);
                    var optLen = header[3];

                    var data = new byte[dataLenShort];
                    var read = ReadInternal(data);
                    Memory<byte> dataMemory = new Memory<byte>(data);

                    if (read == dataLenShort)
                    {
                        var optData = new byte[optLen];

                        read = ReadInternal(optData);
                        Memory<byte> optMemory = new Memory<byte>(optData);

                        if (read == optLen)
                        {
                            var crcData = ReadByteInternal();

                            if (!CheckDataCrc(header, data, optData, crcData))
                            {
                                return null;
                            }

                            var packet = EnOceanPacket.Parse(new Memory<byte>(header), dataMemory, optMemory, crcData);

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
                    Logger.Logger.Instance.LogDebug($"Invalid sync byte received {firstChar}");
                    if (PendingCount > 0 || _stream.BytesToRead > 0)
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
    }
}
