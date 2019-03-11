using Microsoft.Extensions.Logging;
using P3.Knx.Core.Baos.Driver.Data;
using P3.Knx.Core.Baos.Driver.Exceptions;
using P3.Knx.Core.Baos.Driver.Frames;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("P3.Knx.Core.Baos.Tests")]

namespace P3.Knx.Core.Baos.Driver
{
    public class BaosDriver
    {
        private readonly string _port;
        private readonly BaosSerial _serial;
        private readonly ILogger _logger;

        public BaosDriver(string port, ILogger logger, IDatapointInd dpInd)
        {
            _logger = logger;
            _port = port;
            _serial = new BaosSerial(dpInd, _port, logger);
        }



        public async Task<BaosFrame> GetDatapointDescriptions()
        {
            byte[] data = { 0xF0, 0x03, 0x00, 0x00, 0x03, 0xE8 };
            var frame = BaosFrame.Create(data);
            return await SendFrame(frame);
        }

        public async Task<BaosFrame> SetDatapointValue(ushort datapointId, ReadOnlyMemory<byte> value)
        {
            var dp = BitConverter.GetBytes(datapointId).Reverse().ToArray();
            byte[] data = { 0xF0, 0x06, dp[0], dp[1], 0x00, 0x01, dp[0], dp[1], 0x03, (byte)value.Length };
            byte[] dg = new byte[data.Length + value.Length];

            Array.Copy(data, 0, dg, 0, data.Length);
            Array.Copy(value.ToArray(), 0, dg, data.Length, value.Length);

            var frame = BaosFrame.Create(dg);
            return await SendFrame(frame);
        }

        public async Task<IReadOnlyCollection<DatapointValue>> GetDatapointValue(short startDatapointId, short numberOfDatapoints)
        {
            var dp = BitConverter.GetBytes(startDatapointId).Reverse().ToArray();
            var dpCount = BitConverter.GetBytes(numberOfDatapoints).Reverse().ToArray();
            byte[] data = { 0xF0, 0x05, dp[0], dp[1], dpCount[0], dpCount[1], 0x00 };
            var frame = BaosFrame.Create(data);
            var response = await SendFrame(frame);

            if (response != null)
            {
                return ParseDatapointValues((LongFrame)response);
            }
            return null;
        }

        internal static IReadOnlyCollection<DatapointValue> ParseDatapointValues(LongFrame frame)
        {
            if (frame == null)
            {
                throw new ArgumentException($"{nameof(frame)} cannot be null");
            }

            var list = new List<DatapointValue>();

            var responseData = frame.UserData;

            var numberOfDps = BitConverter.ToInt16(responseData.Slice(4, 2).ToArray().Reverse().ToArray());

            if (numberOfDps == 0)
            {
                throw new RequestException((ErrorCodes)responseData.ToArray()[6]);
            }

            var pos = 6;

            for (int i = 0; i < numberOfDps; i++)
            {
                var dpId = BitConverter.ToUInt16(ReverseSpan(responseData.Slice(pos, 2)));
                var dpState = IndexOfSpan(responseData, pos + 2);
                var dpLength = IndexOfSpan(responseData, pos + 3);
                var dpValue = responseData.Slice(pos + 4, dpLength);

                pos += 4 + dpLength;

                var dp = new DatapointValue
                {
                    Data = dpValue,
                    DatapointId = dpId,
                    Length = dpLength,
                    State = dpState
                };
                list.Add(dp);
            }

            return list;
        }

        private static byte IndexOfSpan(ReadOnlyMemory<byte> data, int index)
        {
            return data.ToArray()[index];
        }
        private static byte[] ReverseSpan(ReadOnlyMemory<byte> data)
        {
            return data.ToArray().Reverse().ToArray();
        }

        private async Task<BaosFrame> SendFrame(BaosFrame frame)
        {
            _serial.DisableEventHandler();
            _logger.LogDebug("Start sending frame...");
            try
            {
                await _serial.WriteFrame(frame); //write frame

                var ackFrame = await _serial.ReadFrame(); // wait for ack

                if (ackFrame == null || ackFrame.GetType() != typeof(AckFrame))
                {
                    _logger.LogError($"Could not get ack frame");
                    return null;
                }
                var response = await _serial.ReadFrame(); //read the response frame

                if (response == null)
                {
                    _logger.LogDebug("Could not get response...");
                    return null;
                }

                await _serial.SendAck();
                _logger.LogDebug("End sending frame...");

                return response;
            }
            finally
            {
                _serial.EnableEventHandler();
            }
        }

        public async Task<bool> Start()
        {
            _serial.DisableEventHandler();
            try
            {

                if (_serial.Open())
                {
                    _logger.LogInformation($"Opened {_port}");
                    var frame = await _serial.SendResetFrame();

                    if (frame == null || frame.GetType() != typeof(AckFrame))
                    {
                        return false;
                    }
                    return true;
                }
                return false;
            }
            finally
            {
                _serial.EnableEventHandler();
            }
        }
        public Task<bool> Stop()
        {
            return Task.FromResult(_serial.Close());
        }


    }
}
