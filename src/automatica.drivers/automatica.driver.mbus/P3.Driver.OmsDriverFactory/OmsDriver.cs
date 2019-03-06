using System;
using System.Threading;
using System.Timers;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.MBus.Config;
using P3.Driver.MBus.Frames;
using P3.Driver.MBus.Serial;
using P3.Driver.OmsDriverFactory.Helper;
using System.Threading.Tasks;

namespace P3.Driver.OmsDriverFactory
{
    public class OmsDriver : DriverBase
    {
        private MBusSerial _mbus;

        private readonly System.Timers.Timer _timer = new System.Timers.Timer(2000);

        private byte[] _aesKey;

        private readonly SemaphoreSlim _waitSemaphore = new SemaphoreSlim(1);
        private ILogger _logger;

        public OmsDriver(IDriverContext driverContext) : base(driverContext)
        {
            _logger = driverContext.Logger;
        }

        public override bool Init()
        {
            var key = GetProperty("mbus-oms-key").ValueString;

            var port = GetProperty("mbus-oms-port").ValueString;

            DriverContext.Logger.LogInformation($"Trying to open {port}");

            _mbus = new MBusSerial(new MBusSerialConfig
            {
                Baudrate = 9600,
                Timeout = 1500,
                ResetBeforeRead = true,
                Port = port
            }, TelegramMonitor, _logger);

            _timer.Elapsed += _timer_Elapsed;

            _aesKey = Automatica.Core.Driver.Utility.Utils.StringToByteArray(key);
            return base.Init();
        }

        public override Task<bool> Start()
        {
            var mbus = _mbus.Open();

            if (!mbus)
            {
                return Task.FromResult(false);
            }

            _timer.Start();
            return base.Start();
        }

        public override Task<bool> Stop()
        {
            _timer.Elapsed -= _timer_Elapsed;
            _mbus.Close();

            return base.Stop();
        }

        private async void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();

            await _waitSemaphore.WaitAsync();
            try
            {
                DriverContext.Logger.LogTrace("Try read oms device...");
                var frame = await _mbus.TryReadFrame();

                if (frame != null)
                {
                    DriverContext.Logger.LogTrace($"Read frame...1({frame.GetType()})");

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
                        DriverContext.Logger.LogTrace($"Read frame...2({data.GetType()})");
                        DecryptFrame(data);
                        await _mbus.SendAck();
                    }
                    else
                    {
                        DriverContext.Logger.LogDebug($"could not read frame");
                    }
                    
                }
            }
            finally
            {
                _waitSemaphore.Release(1);
            }

            _timer.Start();
        }

        private void DecryptFrame(MBusFrame data)
        {
            DriverContext.Logger.LogTrace($"Check frame {data?.CiField}");
            if (data != null && data.CiField == 0x5B)
            {
                var controlHigh = data.RawData.Span[18];

                DriverContext.Logger.LogTrace($"Check frame if frame is supported {controlHigh}");

                switch (controlHigh & 0x0F)
                {
                    case 5:
                    {
                        if (!(OmsHelper.AesDecrypt(_aesKey, data, _logger) is VariableDataFrame variableDataFrame))
                        {
                                DriverContext.Logger.LogError("Could not encrypt data frame");
                            return;
                        }

                        foreach (var child in Children)
                        {
                            if (child is OmsDriverAttribute att)
                            {
                                att.SetData(variableDataFrame);
                            }
                        }

                        break;
                    }
                }
            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            switch (ctx.NodeInstance.This2NodeTemplateNavigation.Key)
            {
                case "mbus-oms-datetime":
                    return new OmsDriverAttribute(ctx, 0);
                case "mbus-oms-a+":
                    return new OmsDriverAttribute(ctx, 1);
                case "mbus-oms-a-":
                    return new OmsDriverAttribute(ctx, 2);
                case "mbus-oms-r+":
                    return new OmsDriverAttribute(ctx, 3);
                case "mbus-oms-r-":
                    return new OmsDriverAttribute(ctx, 4);
                case "mbus-oms-power+":
                    return new OmsDriverAttribute(ctx, 5);
                case "mbus-oms-power-":
                    return new OmsDriverAttribute(ctx, 6);
                case "mbus-oms-reactive-power+":
                    return new OmsDriverAttribute(ctx, 7);
                case "mbus-oms-reactive-power-":
                    return new OmsDriverAttribute(ctx, 8);
            }

            throw new NotImplementedException();
        }
    }
}
