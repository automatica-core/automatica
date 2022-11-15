using System;
using System.Text;
using System.Threading;
using System.Timers;
using Automatica.Core.Base.TelegramMonitor;
using P3.Driver.MBus.Config;
using P3.Driver.MBus.Frames;
using P3.Driver.MBus.Serial;
using P3.Driver.OmsDriverFactory.Helper;
using Automatica.Core.Driver.Utility;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Automatica.Core.Driver;

namespace P3.Driver.MBus.ConsoleTest
{
    public class MBusTest
    {
        private readonly MBusSerial _mbus;

        private readonly System.Timers.Timer _timer = new System.Timers.Timer(1000);

        private readonly byte[] _aesKey;

        private readonly SemaphoreSlim _waitSemaphore = new SemaphoreSlim(1);
        public MBusTest(string port)
        {
            var key = "57B7CBDF2154C01795C75CCCEAD572CF";
            _mbus = new MBusSerial(new MBusSerialConfig
            {
                Baudrate = 9600,
                Timeout = 2000,
                ResetBeforeRead = true,
                Port = port
            }, new EmptyTelegramMonitorInstance(), new ConsoleLogger());

            _timer.Elapsed += _timer_Elapsed;

            _aesKey = Utils.StringToByteArray(key);




        }

        public void Start()
        {
            if (!_mbus.Open())
            {
                Console.Error.WriteLine("Could not open interface...");
                throw new Exception("Error not handleded...");

            }
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
                Console.WriteLine($"try read oms frame...");
                var frame = await _mbus.TryReadFrame();
                if (frame != null)
                {
                    Console.WriteLine($"Read frame...1({frame.GetType()})");

                    MBusFrame data = null;
                    if (frame is ShortFrame)
                    {

                        await _mbus.SendAck();
                        data = await _mbus.TryReadFrame();
                    }
                    else
                    {
                        data = frame;
                    }
                    if (data != null)
                    {
                        Console.WriteLine($"Read frame...2({data.GetType()})");
                        DecryptFrame(data);
                        await _mbus.SendAck();
                    }
                    else
                    {
                        Console.WriteLine($"could not read frame");
                    }

                }

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
