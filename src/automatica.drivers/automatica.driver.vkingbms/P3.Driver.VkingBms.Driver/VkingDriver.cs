using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.Driver.Utility;
using Microsoft.Extensions.Logging;
using P3.Driver.VkingBms.Driver.Data;
using P3.Driver.VkingBms.Driver.Exception;
using P3.Driver.VkingBms.Driver.Interfaces;
using RJCP.IO.Ports;

namespace P3.Driver.VkingBms.Driver
{
    public class VkingDriver
    {
        private readonly ITelegramMonitorInstance _monitor;
        private readonly ILogger _logger;
        private readonly SerialPortStream _serialPort;
        private const int MaxFrameLength = 4096;

        public VkingDriver(string port, ITelegramMonitorInstance monitor, ILogger logger)
        {
            _monitor = monitor;
            _logger = logger;
            _serialPort = new
                SerialPortStream(port, 9600, 8, Parity.None, StopBits.One);
            _serialPort.ReadTimeout = 1500; 
            _serialPort.WriteTimeout = 1000;
        }

        public bool IsOpen { get; private set; }

        public void Open()
        {
            _serialPort.Open();
            IsOpen = true;
        }

        public void Close()
        {
            IsOpen = false;
            if (_serialPort.IsDisposed)
            {
                return;
            }
            _serialPort.Close();
            _serialPort.Dispose();
        }

        private int ReadFrameByte(List<byte> frame, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var byteValue = _serialPort.ReadByte();
            if (byteValue < 0 || frame.Count >= MaxFrameLength)
            {
                // -1 = read timeout / end of stream, never loop forever
                throw new DataReadException();
            }
            return byteValue;
        }

        private async Task<byte[]> ReadData(Command cmd, CancellationToken token)
        {
            var input = cmd.ToByteArray();
            var ret = new List<byte>();

            _serialPort.DiscardInBuffer();
            _logger.LogHexOut(input);
            _monitor.NotifyTelegram(TelegramDirection.Output, cmd.Address.ToString(), cmd.Address.ToString(),
                Utils.ByteArrayToString(in input), "").ConfigureAwait(false).GetAwaiter();

            await _serialPort.WriteAsync(input, 0, input.Length, token);

            // a single read may return less than requested, read until the header is complete
            var header = new byte[13];
            var read = 0;
            while (read < header.Length)
            {
                var r = await _serialPort.ReadAsync(header, read, header.Length - read, token);
                if (r <= 0)
                {
                    // timeout or end of stream
                    throw new DataReadException();
                }
                read += r;
            }

            ret.AddRange(header);
            var byteValue = ReadFrameByte(ret, token);
            ret.Add((byte)byteValue);
            do
            {
                byteValue = ReadFrameByte(ret, token);
                ret.Add((byte)byteValue);
            } while (byteValue != 13);

            var retArray = ret.ToArray(); 
            
            _logger.LogHexIn(retArray); 
            _monitor.NotifyTelegram(TelegramDirection.Input, cmd.Address.ToString(), cmd.Address.ToString(),
                Utils.ByteArrayToString(in retArray), "").ConfigureAwait(false).GetAwaiter();

            var crc = Util.CalFrameCheckSum(retArray, 1, retArray.Length - 6);
            var expectedCrc = Util.GetUShort(retArray.ToArray(), retArray.Length - 5);

            if (expectedCrc != crc)
            {
                throw new ArgumentException("Invalid crc...");
            }

            return retArray;
        }

        public async Task<IAnalogDataResponse> ReadAnalogValues(byte address, CancellationToken token)
        {
            if (!IsOpen)
            {
                throw new ArgumentException("not opened!");
            }
            var readCmd = new Command(address, 66);
            readCmd.SetBody(new[] { address });

            var data = await ReadData(readCmd, token);

            return new AnalogDataResponse(data);
        }

        public async Task<IBmsInfoResponse> ReadBmsInfo(byte address, CancellationToken token)
        {
            if (!IsOpen)
            {
                throw new ArgumentException("not opened!");
            }
            var readCmd = new Command(address, 241);
            readCmd.SetBody(new[] { address });

            var data = await ReadData(readCmd, token);

            return new BmsInfoResponse(data);
        }
        public async Task<IVersionIdResponse> ReadVersionInfo(byte address, CancellationToken token)
        {
            if (!IsOpen)
            {
                throw new ArgumentException("not opened!");
            }
            var readCmd = new Command(address, 233);
            readCmd.SetBody(new[] { address });

            var data = await ReadData(readCmd, token);

            return new VersionIdResponse(data);
        }
    }
}