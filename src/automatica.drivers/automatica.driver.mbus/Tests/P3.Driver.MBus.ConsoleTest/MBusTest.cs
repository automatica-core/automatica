using System.Threading;
using System.Timers;
using Automatica.Core.Base.TelegramMonitor;
using P3.Driver.MBus.Config;
using P3.Driver.MBus.Frames;
using P3.Driver.MBus.Serial;
using P3.Driver.OmsDriverFactory.Helper;
using Automatica.Core.Driver.Utility;
using Microsoft.Extensions.Logging.Abstractions;

namespace P3.Driver.MBus.ConsoleTest
{
    public class MBusTest
    {
        private readonly MBusSerial _mbus;

        private readonly System.Timers.Timer _timer = new System.Timers.Timer(1000);

        private readonly byte[] _aesKey;

        private readonly SemaphoreSlim _waitSemaphore = new SemaphoreSlim(1);
        public MBusTest()
        {
            var key = "57B7CBDF2154C01795C75CCCEAD572CF";
            _mbus = new MBusSerial(new MBusSerialConfig
            {
                Baudrate = 9600,
                Timeout = 2000,
                ResetBeforeRead = true,
                Port = "COM6"
            }, new EmptyTelegramMonitorInstance(), NullLogger.Instance);

            _timer.Elapsed += _timer_Elapsed;

            _aesKey = Utils.StringToByteArray(key);




        }

        public void Start()
        {
             _mbus.Open();

         
            _timer.Start();
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
                        System.Console.WriteLine(
                            $"AES with dyn.init vector  {countEncBlocks} d 16 byte blocks plus % d unencrypted data {data.RawData.Length - 12 - 16 * countEncBlocks}");
                            
                        var encrypted = OmsHelper.AesDecrypt(_aesKey, data, NullLogger.Instance);
                        break;
                    }
                }
            }
        }

        private async void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await _waitSemaphore.WaitAsync();
            try
            {
                var data = await _mbus.SendAck();
                DecryptFrame(data);
               
            }
            finally
            {
                _waitSemaphore.Release(1);
            }

        }



        public void Stop()
        {
            _timer.Stop();
            _timer.Elapsed -= _timer_Elapsed;
        }
    }
}
