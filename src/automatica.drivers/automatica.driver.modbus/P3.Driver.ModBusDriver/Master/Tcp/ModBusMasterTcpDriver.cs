using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Microsoft.Extensions.Logging;
using Automatica.Core.Driver.Utility;

namespace P3.Driver.ModBusDriver.Master.Tcp
{
    public class ModBusMasterTcpDriver : ModBusMasterDriver<ModBusMasterTcpConfig>
    {
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private bool _connected;
        private ushort _transactionId;
        private ushort _expectedTransactionId;

        public override bool Connected => _connected;

        public ModBusMasterTcpDriver(ModBusMasterTcpConfig config, ITelegramMonitorInstance monitor, ILogger logger = null) : base(config, monitor, logger)
        {
        }
        protected override bool OpenConnection()
        {
            try
            {
                _tcpClient = new TcpClient();
                // Port is a short, ports > 32767 are stored wrapped (negative)
                _tcpClient.Connect(Config.IpAddress, (ushort)Config.Port);
                _networkStream = new NetworkStream(_tcpClient.Client);
                _connected = true;
            }
            catch (Exception e)
            {
                Logger.LogError($"Could not connect to modbus tcp slave {Config.IpAddress}:{Config.Port}\n{e}");
                return false;
            }
            return true;
        }
        protected override Task<bool> Close()
        {
            try
            {
                _tcpClient.Close();
                _tcpClient.Dispose();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Could not close tcp connection {e}");
            }

            return Task.FromResult(true);
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

        protected override byte[] BuildReadFrame(byte slaveId, int address, int numberOfRegister, ModBusFunction function)
        {
            _transactionId++;

            var headerFrame = BuildHeaderFromDataFrame(new byte[]{}, slaveId, function);
            var dataFrame = new List<byte>();
            dataFrame.AddRange(headerFrame);
            dataFrame.Add((byte)function);

            dataFrame.Add(Utils.ShiftRight(address, 8));
            dataFrame.Add((byte)(address & 0x00FF));
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

      

        protected override async Task<bool> WriteFrame(byte[] data, CancellationToken cts = default)
        {
            Logger.LogHexOut(data);
            if (_networkStream == null)
            {
                return false;
            }

            if (data.Length >= 2)
            {
                // the response has to echo the transaction id of this request
                _expectedTransactionId = Utils.GetUShort(data[0], data[1]);
            }
            await _networkStream.WriteAsync(data, 0, data.Length, cts);
            return true;
        }

        protected virtual ushort GetLengthToRead(byte[] buffer)
        {
            return Utils.GetUShort(buffer[4], buffer[5]);
        }

        protected virtual int HeaderLength => 6;

        /// <summary>
        /// Returns true if the response <paramref name="header"/> belongs to the last request (same MBAP transaction id).
        /// </summary>
        protected virtual bool IsResponseToLastRequest(byte[] header)
        {
            return Utils.GetUShort(header[0], header[1]) == _expectedTransactionId;
        }

        protected override async Task<byte[]> ReadFrame(ModBusFunction function, int numberOfRegisters, CancellationToken cts = default)
        {
            var networkStream = _networkStream;
            if (networkStream == null)
            {
                return null;
            }

            using (var timeoutCts = new CancellationTokenSource(Config.Timeout))
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts, timeoutCts.Token))
            {
                try
                {
                    while (true)
                    {
                        // header exactly, then exactly the announced length (unit id + pdu)
                        var header = new byte[HeaderLength];
                        if (!await ModBusFrameReader.ReadExactlyAsync(networkStream, header, linkedCts.Token))
                        {
                            _connected = false;
                            throw new IOException("ModBus tcp connection closed by the remote side");
                        }
                        Logger.LogHexIn(header);

                        var lengthToRead = GetLengthToRead(header);
                        var data = new byte[lengthToRead];
                        if (!await ModBusFrameReader.ReadExactlyAsync(networkStream, data, linkedCts.Token))
                        {
                            _connected = false;
                            throw new IOException("ModBus tcp connection closed by the remote side");
                        }
                        Logger.LogHexIn(data);

                        if (!IsResponseToLastRequest(header))
                        {
                            // late answer to an earlier (timed out) request, wait for the matching one
                            Logger.LogWarning($"Dropped ModBus tcp response with transaction id {Utils.GetUShort(header[0], header[1])}, expected {_expectedTransactionId}");
                            continue;
                        }

                        return data;
                    }
                }
                catch (IOException)
                {
                    throw;
                }
                catch (OperationCanceledException)
                {
                    // no (complete) response in time, a partial frame would desync the stream, reconnect
                    _connected = false;
                    Logger.LogError($"Timeout reading ModBus frame from {Config.IpAddress}:{Config.Port}");
                }
                catch (Exception e)
                {
                    Logger.LogError(e, $"Error reading ModBus frame {e}");
                }
            }
            return null;
        }


    }
}
