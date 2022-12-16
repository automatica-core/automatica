using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.TelegramMonitor;
using Microsoft.Extensions.Logging;
using Automatica.Core.Driver.Utility;

namespace P3.Driver.ModBusDriver.Master.Tcp
{
    public class ModBusMasterTcpDriver : ModBusMasterDriver<ModBusMasterTcpConfig>
    {
        private readonly TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private bool _connected = false;
        private ushort _transactionId = 0;
        private readonly System.Timers.Timer _receiveTimeoutTimer = new System.Timers.Timer();
        private CancellationTokenSource _cts;

        public override bool Connected => _connected;

        public ModBusMasterTcpDriver(ModBusMasterTcpConfig config, ITelegramMonitorInstance monitor) : base(config, monitor)
        {
            _tcpClient = new TcpClient();
            _receiveTimeoutTimer.Elapsed += _receiveTimeoutTimer_Elapsed;
            _receiveTimeoutTimer.Interval = config.Timeout;
        }

        private void _receiveTimeoutTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _cts?.Cancel(true);
            _receiveTimeoutTimer.Stop();
            _connected = false;

        }

        protected override byte[] UpdateWriteFrame(byte[] frame)
        {
            return frame;
        }

        protected override byte[] BuildHeaderFromDataFrame(byte[] dataFrame, byte slaveId, ModBusFunction function)
        {
            _transactionId++;
            var frame = new List<byte>
            {
                Utils.ShiftRight(_transactionId, 8),
                (byte) (_transactionId & 0x00FF),
                0,
                0,
                0,
                0,
                slaveId
            };


            //length will be set later
            //length will be set later

            frame.AddRange(dataFrame);

            var length = GetRequestLength(frame.ToArray());

            frame[4] = Utils.ShiftRight(length, 8);
            frame[5] = (byte)(length & 0x00FF);

            return frame.ToArray();
        }

        protected override byte[] BuildReadFrame(byte slaveId, int addr, int numberOfRegister, ModBusFunction function)
        {
            _transactionId++;

            var headerFrame = BuildHeaderFromDataFrame(new byte[]{}, slaveId, function);
            var dataFrame = new List<byte>();
            dataFrame.AddRange(headerFrame);
            dataFrame.Add((byte)function);

            dataFrame.Add(Utils.ShiftRight(addr, 8));
            dataFrame.Add((byte)(addr & 0x00FF));
            dataFrame.Add(Utils.ShiftRight(numberOfRegister, 8));
            dataFrame.Add((byte)(numberOfRegister & 0x00FF));

            var frame = dataFrame.ToArray();
            var length = GetRequestLength(frame);
            frame[4] = Utils.ShiftRight(length, 8);
            frame[5] = (byte)(length & 0x00FF);

            return frame;

        }

        protected override short GetRequestLength(byte[] request)
        {
            var length = request.Length - HeaderLength;
            return (short)length;
        }

        protected override bool OpenConnection()
        {
            try
            {
                _tcpClient.Connect(Config.IpAddress, Config.Port);
                _networkStream = new NetworkStream(_tcpClient.Client);
                _connected = true;
            }
            catch (Exception e)
            {
                ModBus.Logger.LogError($"Could not connect to modbus tcp slave {Config.IpAddress}:{Config.Port}\n{e}");
                return false;
            }
            return true;
        }

        protected override async Task<bool> WriteFrame(byte[] data)
        {
            ModBus.Logger.LogHexOut(data);
            await _networkStream.WriteAsync(data, 0, data.Length);
            return true;
        }

        protected virtual ushort GetLengthToRead(byte[] buffer)
        {
            return Utils.GetUShort(buffer[4], buffer[5]);
        }

        protected virtual int HeaderLength => 6;

        protected override async Task<byte[]> ReadFrame(ModBusFunction function, int numberOfRegisters)
        {
            try
            {
                _receiveTimeoutTimer.Start();
                _cts = new CancellationTokenSource();

                byte[] buffer = new byte[HeaderLength];
                var read = await _networkStream.ReadAsync(buffer, 0, HeaderLength, _cts.Token);
                _receiveTimeoutTimer.Stop();
                _cts = null;

                if (read == HeaderLength)
                {
                    ModBus.Logger.LogHexIn(buffer);
                    var lengthToRead = GetLengthToRead(buffer);
                    _receiveTimeoutTimer.Start();
                    byte[] data = new byte[lengthToRead];
                    _cts = new CancellationTokenSource();
                    read = await _networkStream.ReadAsync(data, 0, lengthToRead, _cts.Token);
                    _cts = null;
                    _receiveTimeoutTimer.Stop();

                    ModBus.Logger.LogHexIn(data);
                    if (read == lengthToRead)
                    {
                        return data;
                    }
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
            _tcpClient.Dispose();
            return new Task<bool>(() => true);
        }
    }
}
