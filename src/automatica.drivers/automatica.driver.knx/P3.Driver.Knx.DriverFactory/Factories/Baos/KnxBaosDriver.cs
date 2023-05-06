using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Knx.Core.Abstractions;
using P3.Knx.Core.Baos.Driver;
using P3.Knx.Core.Baos.Driver.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace P3.Driver.Knx.DriverFactory.Factories.Baos
{
    public class KnxBaosDriver : DriverBase, IKnxDriver, IDatapointInd
    {
        private readonly BaosDriver _driver;


        private readonly Dictionary<string, List<Action<object>>> _callbackMap = new Dictionary<string, List<Action<object>>>();

        public KnxBaosDriver(IDriverContext driverContext) : base(driverContext)
        {
            _driver = new BaosDriver("/dev/ttyAMA0", DriverContext.Logger, this);
        }

        public override bool Init()
        {
            return base.Init();
        }

        public override async Task<bool> Start()
        {
            if(await _driver.Start())
            {
                return await base.Start();
            }
            return false;
        }

        public override async Task<bool> Stop()
        {
            var ret = await _driver.Stop();
            await base.Stop();

            return ret;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return new KnxBaosDatapoints(ctx, this);
        }

        public void AddAddressNotifier(string address, Action<object> callback)
        {
            DriverContext.Logger.LogDebug($"Register for value changes on GA {address}");
            if (!_callbackMap.ContainsKey(address))
            {
                _callbackMap.Add(address, new List<Action<object>>());
            }
            _callbackMap[address].Add(callback);
        }

        

        public async Task<bool> Read(string address)
        {
            var dpValue = await _driver.GetDatapointValue(Convert.ToInt16(address), 1);

            if(dpValue != null)
            {
                await DatapointInd(dpValue);
                return true;
            }
            return false;
        }

        public async Task<bool> Write(string address, ReadOnlyMemory<byte> data)
        {
            DriverContext.Logger.LogDebug($"Write value on address {address} data {Automatica.Core.Driver.Utility.Utils.ByteArrayToString(data)}");
            return await _driver.SetDatapointValue(Convert.ToUInt16(address), data) != null;
        }

        public Task DatapointInd(IReadOnlyCollection<DatapointValue> values)
        {
            foreach (var value in values)
            {
                var dpId = $"{value.DatapointId}";

                DriverContext.Logger.LogDebug($"Datagram on {dpId}");

                TelegramMonitor.NotifyTelegram(TelegramDirection.Input, null, dpId, null, Automatica.Core.Driver.Utility.Utils.ByteArrayToString(value.Data));

                if (_callbackMap.ContainsKey(dpId))
                {
                    foreach (var ac in _callbackMap[dpId])
                    {
                        try
                        {
                            DriverContext.Logger.LogDebug($"Datagram on {dpId} - dispatch to {ac}");
                            ac.Invoke(value.Data);
                        }
                        catch (Exception e)
                        {
                            DriverContext.Logger.LogError($"{e}");
                        }
                    }
                }
                else
                {
                    DriverContext.Logger.LogWarning($"Datagram on GA - no callback registered");
                }
            }
            return Task.CompletedTask;
        }
    }
}
