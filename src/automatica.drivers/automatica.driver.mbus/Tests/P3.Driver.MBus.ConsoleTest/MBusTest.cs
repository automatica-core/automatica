using System;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using P3.Driver.MBus.Frames;
using P3.Driver.OmsDriverFactory.Helper;
using Automatica.Core.Driver.Utility;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using P3.Driver.Oms;

namespace P3.Driver.MBus.ConsoleTest
{
    public class MBusTest
    {
        private readonly ConsoleLogger _logger;
        private readonly byte[] _aesKey;
        private readonly OmsDevice _omsDevice;

        public MBusTest(string port)
        { 
            _logger = new ConsoleLogger();
            var key = "57B7CBDF2154C01795C75CCCEAD572CF";

            _aesKey = Utils.StringToByteArray(key);
            _omsDevice = new OmsDevice(port, _logger, new EmptyTelegramMonitorInstance(), DecryptFrame);
        }

    

        public async Task Start()
        {
            if (!await _omsDevice.Start())
            {
                _logger.LogDebug("Could not open interface...");
                throw new Exception("Error not handled...");

            }
        }

        private const int OffsetUserData = 7;

        private void DecryptFrame(MBusFrame data)
        {
            if (data != null && data.CiField == 0x5B)
            {
                var controlHigh = data.RawData.Span[OffsetUserData + 11];
                var controlLow = data.RawData.Span[OffsetUserData + 10];

                switch (controlHigh & 0x0F)
                {
                    case 5:
                    {
                        int countEncBlocks = controlLow >> 4;
                        _logger.LogDebug(
                            $"AES with dyn.init vector  {countEncBlocks} d 16 byte blocks plus % d unencrypted data {data.RawData.Length - 12 - 16 * countEncBlocks}");
                            
                        var encrypted = OmsHelper.AesDecrypt(_aesKey, data, NullLogger.Instance);
                        break;
                    }
                }
            }
        }


      

        public async Task Stop()
        {
            await _omsDevice.Stop();
        }
    }
}
