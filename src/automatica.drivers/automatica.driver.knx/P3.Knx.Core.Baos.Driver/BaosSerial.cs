using Automatica.Core.Driver.Utility;
using Microsoft.Extensions.Logging;
using P3.Knx.Core.Baos.Driver.Frames;
using RJCP.IO.Ports;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Knx.Core.Baos.Driver
{
    public class BaosSerial
    {
        private readonly string _port;
        private SerialPortStream _stream;
        private bool _connected;

        public ILogger Logger { get; }

        public BaosSerial(string port, ILogger logger)
        {
            _port = port;
            Logger = logger;
        }

        public Task Start()
        {
            return Task.CompletedTask;
        }

        public bool Open()
        {
            try
            {
                _stream = new SerialPortStream(_port, 19200, 8, Parity.None,
                    StopBits.One);

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


        private async Task<BaosFrame> ReadFrameInternal()
        {
            try
            {
                int firstChar = _stream.ReadByte();

                if (firstChar == BaosFrame.ShortFrameStart)
                {
                    Logger.LogDebug("Start parsing short frame...");
                    Thread.Sleep(100);
                    byte[] shortFrame = new byte[5];
                    shortFrame[0] = (byte)firstChar;
                    var bytesRead = await _stream.ReadAsync(shortFrame, 1, 4);

                    if (bytesRead != 4)
                    {
                        Logger.LogDebug($"Could not parse short frame (read bytes {bytesRead}/4)");
                        return null;
                    }

                    Logger.LogDebug("Short frame in...");
                    Logger.LogHexIn(shortFrame);

                    var frame = BaosFrame.FromByteArray(BaosFrameType.ShortFrame, Logger, shortFrame);

                    return frame;
                }
                if (firstChar == BaosFrame.SingleCharFrame)
                {
                    Logger.LogDebug("Ack frame in...");
                    return new AckFrame();
                }

                if (firstChar == BaosFrame.ControlFrameLongFrameStart)
                {
                    Logger.LogDebug("Start parsing long frame...");
                    Thread.Sleep(200);

                    byte[] headerBuffer = new byte[4];
                    headerBuffer[0] = (byte)firstChar;
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

                        return BaosFrame.FromSpan(BaosFrameType.LongFrame, Logger, headerBuffer.AsSpan(), dataMemory.Span);
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

        public bool Close()
        {
            _stream?.Close();
            _connected = false;
            return true;
        }

    }
}
