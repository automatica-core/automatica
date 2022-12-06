using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.Driver.Utility;
using Microsoft.Extensions.Logging;
using P3.Driver.MBus.Config;
using P3.Driver.MBus.Frames;
using P3.Driver.MBus.Serial;

namespace P3.Driver.Oms
{
    public class OmsDevice
    {
        private readonly ILogger _logger;
        private readonly Action<MBusFrame> _decrypt;

        private readonly MBusSerial _mbus;
        private readonly SemaphoreSlim _waitSemaphore = new SemaphoreSlim(1);

        public OmsDevice(string port, ILogger logger, ITelegramMonitorInstance telegramMonitor, Action<MBusFrame> decrypt)
        {
            _logger = logger;
            _decrypt = decrypt;

            _mbus = new MBusSerial(new MBusSerialConfig
            {
                Baudrate = 9600,
                Timeout = 2000,
                ResetBeforeRead = true,
                Port = port,
                DataReceived = DataReceived
            }, telegramMonitor, _logger);
        }

        private async void DataReceived()
        {
            await ReadFrame();
        }

        public async Task<bool> Start()
        {
            await Task.CompletedTask;
            _logger.LogDebug("Starting oms driver...");

            if (!_mbus.Open())
            {
                _logger.LogError($"Could not open mbus driver...");
                return false;
            }

            return true;
        }

        private async Task ReadFrame()
        {
            await _waitSemaphore.WaitAsync();

            try
            {
                _logger.LogDebug($"try read oms frame...");
                var frame = await _mbus.TryReadFrame();
                if (frame != null)
                {
                    _logger.LogDebug($"Read frame...1({frame.GetType()})");

                    MBusFrame data = null;
                    if (frame is ShortFrame)
                    {

                        await _mbus.SendAckWithoutRead();
                        data = await _mbus.TryReadFrame();
                    }
                    else
                    {
                        data = frame;
                    }
                    if (data != null)
                    {
                        _logger.LogDebug($"Read frame...2({data.GetType()})");
                        _decrypt.Invoke(data);
                        await _mbus.SendAckWithoutRead();
                    }
                    else
                    {
                        _logger.LogDebug($"could not read frame");
                    }

                }

            }
            finally
            {
                _waitSemaphore.Release(1);
            }
        }



        public async Task Stop()
        {
            await Task.CompletedTask;
            _mbus.Close();
        }
    }
}