using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.TelegramMonitor;
using Microsoft.Extensions.Logging;
using Automatica.Core.Driver.Utility;
using RJCP.IO.Ports;

namespace P3.Driver.ModBusDriver.Master.Rtu
{
    public class ModBusMasterRtuDriver : ModBusMasterDriver<ModBusMasterRtuConfig>
    {
        private SerialPortStream _serialPortStream;
        private bool _connected;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public override bool Connected => _connected;

        public ModBusMasterRtuDriver(ModBusMasterRtuConfig config, ITelegramMonitorInstance monitor, ILogger logger = null) : base(config, monitor, logger)
        {
            _serialPortStream = CreateSerialPortStream();
        }

        private SerialPortStream CreateSerialPortStream()
        {
            var serialPortStream = new SerialPortStream(Config.Port, Config.Baud, Config.DataBits,
                FromString(Config.Parity), FromString(Config.StopBits));

            serialPortStream.ReadTimeout = 1000;
            serialPortStream.WriteTimeout = 1000;
            return serialPortStream;
        }

        /// <summary>
        /// Maps the stored parity value (meta values "n/ep/o" or the enum names) case-insensitively.
        /// "even" is the old template default: it was never mapped before and the port ran without parity, so it stays no parity.
        /// </summary>
        internal static Parity FromString(string parity)
        {
            switch (parity?.Trim().ToLowerInvariant())
            {
                case "n":
                case "none":
                case "noparity":
                // legacy template default, never applied before 213804f6, existing installs run 8N1 with it
                case "even":
                    return Parity.None;
                case "e":
                case "ep":
                case "evenparity":
                case "eventparity":
                    return Parity.Even;
                case "o":
                case "odd":
                case "oddparity":
                    return Parity.Odd;
                case "m":
                case "mark":
                case "markparity":
                    return Parity.Mark;
                case "s":
                case "space":
                case "spaceparity":
                    return Parity.Space;
                default:
                    ModBus.Logger.LogWarning($"Unknown parity \"{parity}\", using no parity");
                    return Parity.None;
            }
        }
        internal static StopBits FromString(double stopBits)
        {
            switch (stopBits)
            {
                case 1d:
                    return StopBits.One;
                case 1.5d:
                    return StopBits.One5;
                case 2d:
                    return StopBits.Two;
                default:
                    return StopBits.One;
            }
        }
        protected override byte[] UpdateWriteFrame(byte[] frame)
        {
            var crc = ModbusUtility.CalculateCrc(frame);

            var retFrame = new byte[frame.Length + 2];

            Array.Copy(frame, retFrame, frame.Length);

            Array.Copy(crc, 0, retFrame, frame.Length, 2);

            return retFrame;
        }

        protected override byte[] BuildHeaderFromDataFrame(byte[] dataFrame, byte slaveId, ModBusFunction function)
        {
            var frame = new List<byte>
            {
                slaveId
            };
            frame.AddRange(dataFrame);
            return frame.ToArray();
        }

        protected override byte[] BuildReadFrame(byte slaveId, int addr, int numberOfRegister, ModBusFunction function)
        {
            var headerFrame = BuildHeaderFromDataFrame(new byte[]{}, slaveId, function);
            var dataFrame = new List<byte>();
            dataFrame.AddRange(headerFrame);
            dataFrame.Add((byte)function);

            dataFrame.Add(Utils.ShiftRight(addr, 8));
            dataFrame.Add((byte)(addr & 0x00FF));
            dataFrame.Add(Utils.ShiftRight(numberOfRegister, 8));
            dataFrame.Add((byte)(numberOfRegister & 0x00FF));

            var crc = ModbusUtility.CalculateCrc(dataFrame.ToArray());

            dataFrame.AddRange(crc);

            return dataFrame.ToArray();

        }

        protected override short GetRequestLength(byte[] request)
        {
            return -1;
        }

        protected override bool OpenConnection()
        {
            try
            {
                // a closed SerialPortStream is disposed and cannot be reopened, always use a new instance
                if (_serialPortStream == null || _serialPortStream.IsDisposed || _serialPortStream.IsOpen)
                {
                    _serialPortStream?.Dispose();
                    _serialPortStream = CreateSerialPortStream();
                }

                _serialPortStream.Open();
                _connected = true;
            }
            catch (Exception e)
            {
                Logger.LogError($"Could not connect to modbus tcp slave {Config.Port}, {Config.Baud}\n{e}");
                return false;
            }
            return true;
        }

        protected override async Task<bool> WriteFrame(byte[] data, CancellationToken cts = default)
        {
            Logger.LogHexOut(data);

            var serialPortStream = _serialPortStream;
            if (serialPortStream == null || serialPortStream.IsDisposed || !serialPortStream.IsOpen)
            {
                return false;
            }

            // drop late/garbage bytes of a previous (timed out) response, they would be read as the answer to this request
            serialPortStream.DiscardInBuffer();

            await serialPortStream.WriteAsync(data, 0, data.Length, cts);
            return true;
        }

        protected override async Task<byte[]> ReadFrame(ModBusFunction function, int numberOfRegisters, CancellationToken cts = default)
        {
            var serialPortStream = _serialPortStream;
            if (serialPortStream == null || serialPortStream.IsDisposed || !serialPortStream.IsOpen)
            {
                return Array.Empty<byte>();
            }

            try
            {
                await _semaphore.WaitAsync(cts);
            }
            catch (OperationCanceledException)
            {
                return null;
            }

            try
            {
                if (serialPortStream.IsDisposed || !serialPortStream.IsOpen)
                {
                    _connected = false;
                    throw new Exception("object disposed or closed...");
                }

                byte[] data;
                // receive timeout, linked with the callers token
                using (var timeoutCts = new CancellationTokenSource(Config.Timeout))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts, timeoutCts.Token))
                {
                    // reads until the expected length (or the exception reply length) arrived, checks the crc
                    data = await ModBusFrameReader.ReadRtuFrameAsync(serialPortStream, function, numberOfRegisters, linkedCts.Token);
                }
                Logger.LogHexIn(data);

                if ((data[1] & 0x80) != 0)
                {
                    Logger.LogWarning($"ModBus exception reply from {data[0]} for function {function}: {(ModBusExceptionCode)data[2]}");
                }

                // an exception reply is returned as well, the caller surfaces its exception code
                return data;
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error reading ModBus frame {e}");
            }
            finally
            {
                _semaphore.Release(1);
            }

            return null;

        }

        protected override async Task<bool> Close()
        {
            _connected = false;
            var serialPortStream = _serialPortStream;
            if (serialPortStream == null || serialPortStream.IsDisposed)
            {
                return true;
            }
            serialPortStream.Close();
            await serialPortStream.DisposeAsync();

            return true;
        }
    }
}
