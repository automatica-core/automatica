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
using NModbus.Utility;

namespace P3.Driver.ModBusDriver.Master.Rtu
{
    public class ModBusMasterRtuDriver : ModBusMasterDriver<ModBusMasterRtuConfig>
    {
        private readonly SerialPortStream _serialPortStream;
        private bool _connected;
        private readonly System.Timers.Timer _receiveTimeoutTimer = new System.Timers.Timer();
        private CancellationTokenSource _cts;

        public override bool Connected => _connected;

        public ModBusMasterRtuDriver(ModBusMasterRtuConfig config, ITelegramMonitorInstance monitor) : base(config, monitor)
        {
            _serialPortStream = new SerialPortStream(config.Port, config.Baud, config.DataBits,
                FromString(config.Parity), FromString(config.StopBits));

            _serialPortStream.ReadTimeout = 1000;
            _serialPortStream.WriteTimeout = 1000;

            _receiveTimeoutTimer.Elapsed += _receiveTimeoutTimer_Elapsed;
            _receiveTimeoutTimer.Interval = config.Timeout;
        }

        private static Parity FromString(string parity)
        {
            switch (parity)
            {
                case "NoParity":
                    return Parity.None;
                case "EventParity":
                    return Parity.Even;
                case "OddParity":
                    return Parity.Odd;
                default:
                    return Parity.None;
            }
        }
        private static StopBits FromString(double stopBits)
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
        private void _receiveTimeoutTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _cts?.Cancel(true);
            _receiveTimeoutTimer.Stop();
            _connected = false;

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
                _serialPortStream.Open();
                _connected = true;
            }
            catch (Exception e)
            {
                ModBus.Logger.LogError($"Could not connect to modbus tcp slave {Config.Port}, {Config.Baud}\n{e}");
                return false;
            }
            return true;
        }

        protected override async Task<bool> WriteFrame(byte[] data)
        {
            ModBus.Logger.LogHexOut(data);

            await _serialPortStream.WriteAsync(data, 0, data.Length);
            return true;
        }

        protected override async Task<byte[]> ReadFrame(ModBusFunction function, int numberOfRegisters)
        {
            try
            {
                var readLength = 5;

                if(function == ModBusFunction.ReadInputRegisters ||
                   function == ModBusFunction.ReadHoldingRegisters)
                {
                    readLength += numberOfRegisters * 2;
                }
                else if (function == ModBusFunction.ReadDiscreteInputs ||
                         function == ModBusFunction.ReadCoils)
                {
                    readLength += (numberOfRegisters + 7) / 8;
                }
                else if (function == ModBusFunction.WriteMultipleRegisters ||
                         function == ModBusFunction.WriteSingleRegister)
                {
                    readLength += 3;
                }
                else if (function == ModBusFunction.WriteMultipleCoils ||
                         function == ModBusFunction.WriteSingleCoil)
                {
                    readLength += 3;
                }
                
                _receiveTimeoutTimer.Start();
                _cts = new CancellationTokenSource();

                var data = new byte[readLength];
                var read = await _serialPortStream.ReadAsync(data, 0, readLength);
                ModBus.Logger.LogHexIn(data);
                if (read == readLength)
                {

                    var readCrc = data.Skip(data.Length - 2);
                    var calcCrc = ModbusUtility.CalculateCrc(data.SkipLast(2).ToArray());
                    if (!readCrc.SequenceEqual(calcCrc))
                    {
                        throw new Exception("Invalid crc...");
                    }
                    return data;
                }
                else
                {
                    if (readLength == 5 && data[1] == 0x86)
                    {
                        throw new Exception($"modbus exception thrown..({data[3]})");
                    }

                    throw new Exception("modbus exception thrown..");
                }
            }
            catch (Exception e)
            {
                ModBus.Logger.LogError(e, $"Error reading ModBus frame {e}");
            }
            return null;
        }

        protected override Task<bool> Close()
        {
            _serialPortStream.Dispose();
            return new Task<bool>(() => true);
        }
    }
}
