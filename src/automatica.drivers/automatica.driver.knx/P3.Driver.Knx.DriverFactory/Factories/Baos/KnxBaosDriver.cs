using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Knx.Core.Abstractions;
using P3.Knx.Core.Baos.Driver;
using P3.Knx.Core.Baos.Driver.Data;
using System;
using System.Collections.Generic;
using System.Threading;
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
        

        public override async Task<bool> Start(CancellationToken token = default)
        {
            if(await _driver.Start())
            {
                return await base.Start(token);
            }
            return false;
        }

        public override async Task<bool> Stop(CancellationToken token = default)
        {
            var ret = await _driver.Stop();
            await base.Stop(token);

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
            DriverContext.Logger.LogDebug($"Read value on address {address}");
            var dpValue = await _driver.GetDatapointValue(Convert.ToInt16(address), 1);

            if(dpValue != null)
            {
                await DataPointInd(dpValue);
                return true;
            }
            return false;
        }

        public async Task<bool> Write(string address, ReadOnlyMemory<byte> data)
        {
            DriverContext.Logger.LogDebug($"Write value on address {address} data {Automatica.Core.Driver.Utility.Utils.ByteArrayToString(data)}");
            return await _driver.SetDatapointValue(Convert.ToUInt16(address), data) != null;
        }

        public Task DataPointInd(IReadOnlyCollection<DatapointValue> values)
        {
            foreach (var value in values)
            {
                var dpId = $"{value.DatapointId}";

                DriverContext.Logger.LogDebug($"Datagram on {dpId}");

                TelegramMonitor.NotifyTelegram(TelegramDirection.Input, null, dpId, null, Automatica.Core.Driver.Utility.Utils.ByteArrayToString(value.Data));

                if (_callbackMap.TryGetValue(dpId, out var dataPoint))
                {
                    foreach (var ac in dataPoint)
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
