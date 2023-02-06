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

        public VkingDriver(string port, ITelegramMonitorInstance monitor, ILogger logger)
        {
            _monitor = monitor;
            _logger = logger;
            _serialPort = new
                SerialPortStream(port, 9600, 8, Parity.None, StopBits.One);
            _serialPort.ReadTimeout = 1000; 
            _serialPort.WriteTimeout = 1000;
        }

        public bool IsOpen { get; private set; }

        public void Open()
        {
            IsOpen = true;
            _serialPort.Open();
        }

        public void Close()
        {
            IsOpen = false;
            _serialPort.Close();
            _serialPort.Dispose();
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

            var header = new byte[13];
            var read = await _serialPort.ReadAsync(header, token);

            if (read != 13)
            {
                throw new DataReadException();
            }

            ret.AddRange(header);
            var byteValue = _serialPort.ReadByte();
            ret.Add((byte)byteValue);
            do
            {
                byteValue = _serialPort.ReadByte();
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