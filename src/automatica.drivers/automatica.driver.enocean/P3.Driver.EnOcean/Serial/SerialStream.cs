﻿using System;
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

        public override void Pause()
        {
            _stream.DataReceived -= _stream_DataReceived;
        }

        public override void Continue()
        {
            _stream.DataReceived += _stream_DataReceived;
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

                if (packet == null)
                {
                    return;
                }
                try
                {
                    var telegram = EnOceanTelegramFactory.FromPacket(packet);
                    TelegramReceived?.Invoke(this, new PacketReceivedEventArgs(telegram));
                }
                catch (NotImplementedException)
                {
                    Logger.Logger.Instance.LogHexIn(packet.RawData);
                }

                if (_stream.BytesToRead > 0)
                {
                    _stream_DataReceived(sender, e);
                }
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
                Logger.Logger.Instance.LogHexOut(frame.RawData);
                _stream.Write(frame.RawData.ToArray(), 0, frame.RawData.Length);
                return await ReadFrameInternal();
            }
            catch (Exception e)
            {
                Logger.Logger.Instance.LogError($"Could not write frame {e}", e);
            }
            return null;
        }

        private async Task<EnOceanPacket> ReadFrameInternal()
        {
            try
            {
                var firstChar = (byte)_stream.ReadByte();
                await Task.Delay(100);

                if (firstChar == EnOcean.SyncByte)
                {
                    var dataLen = new byte[2];
                    var dataLenRead = _stream.Read(dataLen, 0, 2);

                    if (dataLenRead == 2)
                    {
                        var dataLenShort = BitConverter.ToUInt16(dataLen.Reverse().ToArray(), 0);

                        var optLen = (byte) _stream.ReadByte();
                        var packetType = (byte) _stream.ReadByte();
                        var crc8Header = (byte) _stream.ReadByte();

                        var header = new byte[] {firstChar, dataLen[0], dataLen[1], optLen, packetType, crc8Header};

                        var data = new byte[dataLenShort];
                        var read = _stream.Read(data, 0, dataLenShort);
                        Memory<byte> dataMemory = new Memory<byte>(data);

                        if (read == dataLenShort)
                        {
                            var optData = new byte[optLen];

                            read = _stream.Read(optData, 0, optLen);
                            Memory<byte> optMemory = new Memory<byte>(optData);

                            if (read == optLen)
                            {
                                var crcData = (byte) _stream.ReadByte();
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
                        Logger.Logger.Instance.LogDebug($"Could not read length of package");
                    }
                }
                else
                {
                    Logger.Logger.Instance.LogDebug($"Invalid sync byte received {firstChar}");
                    if (_stream.BytesToRead > 0)
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
