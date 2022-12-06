using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Microsoft.Extensions.Logging;
using P3.Driver.MBus.Config;
using P3.Driver.MBus.Frames;
using Automatica.Core.Driver.Utility;
using RJCP.IO.Ports;

namespace P3.Driver.MBus.Serial
{
    public class MBusSerial : MBusConnection
    {
        private readonly MBusSerialConfig _config;
        private SerialPortStream _stream;
        private bool _connected;

        public MBusSerial(MBusSerialConfig config, ITelegramMonitorInstance monitor, ILogger loggerInstance) : base(config, monitor, loggerInstance)
        {
            _config = config;
        }

        public override bool Close()
        {
            if (_stream != null)
            {
                _stream.DataReceived -= StreamOnDataReceived;
                _stream.Close();
            }

            _connected = false;
            return true;
        }

        public override bool Open()
        {
            try
            {
                _stream = new SerialPortStream(_config.Port, _config.Baudrate, 8, Parity.None,
                    StopBits.One)
                {
                    ReadTimeout = _config.Timeout,
                    WriteTimeout = _config.Timeout
                };

                _stream.DataReceived += StreamOnDataReceived;

                _stream.Open();
                _connected = true;

            }
            catch (Exception e)
            {
                Logger.LogError("Could not open serial interface {0}", e);

                return false;
            }
            return true;
        }

        private void StreamOnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            _config.DataReceived.Invoke();
        }

        public override bool IsConnected()
        {
            return _connected;
        }

        protected override async Task WriteFrame(MBusFrame frame)
        {
            try
            {
                var buffer = frame.ToByteFrame();
                await WriteRaw(buffer);
            }
            catch (Exception e)
            {
                Logger.LogError($"Could not write frame {e}", e);
            }
        }

        public override async Task WriteRaw(ReadOnlyMemory<byte> data)
        {
            Logger.LogHexOut(data);
            await _stream.WriteAsync(data);
        }

        protected override Task<MBusFrame> ReadFrame()
        {
            return ReadFrameInternal();
        }

        private async Task<MBusFrame> ReadFrameInternal()
        {
            try
            {
                int firstChar = _stream.ReadByte();

                if (firstChar == MBus.ShortFrameStart)
                {
                    Logger.LogDebug("Start parsing short frame...");
                    Thread.Sleep(100);
                    byte[] shortFrame = new byte[5];
                    shortFrame[0] = (byte) firstChar;
                    var bytesRead = await _stream.ReadAsync(shortFrame, 1, 4);

                    if (bytesRead != 4)
                    {
                        Logger.LogDebug($"Could not parse short frame (read bytes {bytesRead}/4)");
                        return null;
                    }

                    Logger.LogDebug("Short frame in...");
                    Logger.LogHexIn(shortFrame);

                    var frame = MBusFrame.FromByteArray(MBusFrameType.ShortFrame, Logger, shortFrame);

                    return frame;
                }
                if (firstChar == MBus.SingleCharFrame)
                {
                    Logger.LogDebug("Ack frame in...");
                    return new AckFrame();
                }

                if (firstChar == MBus.ControlFrameLongFrameStart)
                {
                    Logger.LogDebug("Start parsing long frame...");
                    Thread.Sleep(200);

                    byte[] headerBuffer = new byte[4];
                    headerBuffer[0] = (byte) firstChar;
                    var bytesRead = await _stream.ReadAsync(headerBuffer, 1, 3);

                    Logger.LogHexIn(headerBuffer);

                    if (bytesRead != 3)
                    {
                        Logger.LogDebug($"Could not read header...read {bytesRead}/3");
                        return null;
                    }

                    if (headerBuffer[0] == 0x68 && headerBuffer[0] == headerBuffer[3] &&
                        headerBuffer[1] == headerBuffer[2])
                    {
                        int lengthToRead = headerBuffer[1] + 2; //read with checksum and stop byte
                        byte[] data = new byte[lengthToRead];
                        var dataMemory = new Memory<byte>(data);

                        bytesRead = await _stream.ReadAsync(dataMemory);

                        Logger.LogHexIn(dataMemory);

                        if (bytesRead != lengthToRead)
                        {

                            Logger.LogDebug($"Invalid length of read data...({bytesRead}/{lengthToRead})");
                            return null; //invalid length of data read
                        }

                        if (data[data.Length - 1] != 0x16)
                        {
                            Logger.LogDebug("Invalid stop byte...");
                            return null; //invalid stop byte
                        }

                        

                        var packageType = headerBuffer[2] == 3 ? MBusFrameType.ControlFrame : MBusFrameType.LongFrame;
                        return MBusFrame.FromSpan(packageType, Logger, headerBuffer.AsSpan(), dataMemory.Span);
                    }
                }

            }
            catch (IOException ioe)
            {
                Logger.LogError($"Could not read frame {ioe}", ioe);
                Close();
                Thread.Sleep(100);
                Open();
            }
            catch (Exception e)
            {
                Logger.LogError($"Could not read frame {e}", e);
            }
            return null;
        }
        
    }
}
