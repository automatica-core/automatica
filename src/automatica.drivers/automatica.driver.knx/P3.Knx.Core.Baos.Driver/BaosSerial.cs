using Automatica.Core.Driver.Utility;
using Microsoft.Extensions.Logging;
using P3.Knx.Core.Baos.Driver.Frames;
using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Knx.Core.Baos.Driver
{
    public class BaosSerial
    {
        private readonly string _port;
        private SerialPort _stream;
        private bool _connected;
        private bool _eventHandling = true;
        private readonly SemaphoreSlim _waitSemaphore = new SemaphoreSlim(1);

        public IDatapointInd Driver { get; }
        public ILogger Logger { get; }

        internal static bool ControlValueIndicator { get; private set; }

        public BaosSerial(IDatapointInd driver, string port, ILogger logger)
        {
            Driver = driver;
            _port = port;
            Logger = logger;
        }

        public Task Start()
        {
            return Task.CompletedTask;
        }


        internal async Task WriteFrame(BaosFrame frame)
        {
            await _waitSemaphore.WaitAsync();

            try
            {
                _stream.DiscardOutBuffer();
                var buffer = frame.ToByteFrame();
                Logger.LogHexOut(buffer);
                _stream.Write(buffer.ToArray(),0, buffer.Length);

                if (frame is AckFrame)
                {
                    return;
                }

                ControlValueIndicator = !ControlValueIndicator;
            }
            catch (Exception e)
            {
                Logger.LogError($"Could not write frame {e}", e);
            }
            finally
            {
                _waitSemaphore.Release();
            }
        }

        public bool Open()
        {
            try
            {
                _stream = new SerialPort(_port, 19200,  Parity.Even, 8,
                    StopBits.One);

                _stream.ReadTimeout = 500;

                _stream.DataReceived += _stream_DataReceived;
                _stream.Open();
                Logger.LogDebug($"Opneing serial interface {_port} 19200 8e1");
                _connected = true;

            }
            catch (Exception e)
            {
                Logger.LogError("Could not open serial interface {0}", e);

                return false;
            }
            return true;
        }

        internal async Task SendAck()
        {
            await WriteFrame(new AckFrame());
        }

        internal async Task<BaosFrame> SendResetFrame()
        {
            await WriteFrame(ShortFrame.CreateResetFrame());
            ControlValueIndicator = true;
            return await ReadFrame();
        }

        internal void DisableEventHandler()
        {
            _eventHandling = false;
        }

        internal void EnableEventHandler()
        {
            _eventHandling = true;
        }

        private async void _stream_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!_eventHandling)
            {
                return;
            }

            var frame = await ReadFrame();

            if (frame != null)
            {

                if (frame is LongFrame longFrame)
                {
                    if (frame.UserData.ToArray()[1] == 0xC1 || frame.UserData.ToArray()[1] == 0x85)
                    {
                        Logger.LogDebug($"DatapointValue.Ind");

                        var data = BaosDriver.ParseDatapointValues(longFrame);
                        await Driver.DataPointInd(data);
                    }
                }
                else if (frame is ShortFrame)
                {
                    await SendResetFrame();
                }

                Logger.LogDebug($"End read frame from event handler...{frame.GetType()}");
                Logger.LogHexIn(frame.ToByteFrame());
            }
            else
            {
                Logger.LogDebug($"End read frame from event handler...");
            }
        }

        internal async Task<BaosFrame> ReadFrame()
        {
            await _waitSemaphore.WaitAsync();
            try
            {
                int firstChar = _stream.ReadByte();

                // Logger.LogDebug($"First char is {firstChar}");
                if (firstChar == BaosFrame.ShortFrameStart)
                {
                    // Logger.LogDebug("Start parsing short frame...");
                    Thread.Sleep(100);
                    byte[] shortFrame = new byte[5];
                    shortFrame[0] = (byte)firstChar;
                    var bytesRead = _stream.Read(shortFrame, 1, 4);

                    if (bytesRead != 4)
                    {
                        Logger.LogDebug($"Could not parse short frame (read bytes {bytesRead}/4)");
                        return null;
                    }
                    Logger.LogHexIn(shortFrame);

                    var frame = BaosFrame.FromByteArray(BaosFrameType.ShortFrame, Logger, shortFrame);

                    return frame;
                }
                if (firstChar == BaosFrame.SingleCharFrame)
                {
                    Logger.LogHexIn(new byte[] { (byte)firstChar });
                    return new AckFrame();
                }

                if (firstChar == BaosFrame.ControlFrameLongFrameStart)
                {
                    //    Logger.LogDebug("Start parsing long frame...");
                    Thread.Sleep(200);

                    byte[] headerBuffer = new byte[4];
                    headerBuffer[0] = (byte)firstChar;
                    var bytesRead = _stream.Read(headerBuffer, 1, 3);

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

                        bytesRead = _stream.Read(data, 0, data.Length);

                        var dataMemory = new Memory<byte>(data);
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
            finally
            {
                _waitSemaphore.Release();
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
