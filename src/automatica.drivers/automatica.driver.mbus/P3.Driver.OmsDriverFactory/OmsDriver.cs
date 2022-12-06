using System;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.MBus.Frames;
using P3.Driver.OmsDriverFactory.Helper;
using System.Threading.Tasks;
using Automatica.Core.Driver.Utility;
using P3.Driver.Oms;

namespace P3.Driver.OmsDriverFactory
{
    public class OmsDriver : DriverBase
    {

        private byte[] _aesKey;
        private readonly ILogger _logger;
        private OmsDevice _omsDevice;

        public OmsDriver(IDriverContext driverContext) : base(driverContext)
        {
            _logger = driverContext.Logger;
        }

        public override bool Init()
        {
            var key = GetProperty("mbus-oms-key").ValueString;
            var port = GetProperty("mbus-oms-port").ValueString;

            DriverContext.Logger.LogInformation($"Trying to open {port}");

            _aesKey = Utils.StringToByteArray(key);
            _omsDevice = new OmsDevice(port, _logger, TelegramMonitor, DecryptFrame);

            return base.Init();
        }

        private void DataReceived()
        {
            _logger.LogDebug("Received data...");
        }

        public override async Task<bool> Start()
        {
            var mbus = await _omsDevice.Start();

            if (!mbus)
            {
                return false;
            }
            DriverContext.Logger.LogInformation($"Start checking for messages....");
            return await base.Start();
        }

        public override async Task<bool> Stop()
        {
            await _omsDevice.Stop();

            return await base.Stop();
        }

        private void DecryptFrame(MBusFrame data)
        {
            DriverContext.Logger.LogDebug($"Check frame {data?.CiField}");
            if (data != null && data.CiField == 0x5B)
            {
                var controlHigh = data.RawData.Span[18];

                DriverContext.Logger.LogDebug($"Check frame if frame is supported {controlHigh}");

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
