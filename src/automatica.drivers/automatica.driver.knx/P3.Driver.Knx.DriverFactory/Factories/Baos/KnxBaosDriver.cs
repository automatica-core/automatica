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
using Knx.Falcon;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Factories.Baos
{
    public class KnxBaosDriver : KnxDriver, IKnxDriver, IDatapointInd
    {
        private readonly BaosDriver _driver;

        public KnxBaosDriver(IDriverContext driverContext) : base(driverContext, false, KnxLevel.TwoLevel) //move new layer in between, just a quick fix!
        {
            _driver = new BaosDriver("/dev/ttyAMA0", DriverContext.Logger, this);
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            // the BAOS module is connected via the serial port, there is no KNXnet/IP connection to create
            return InitWithoutConnection(token);
        }

        public override async Task<bool> Start(CancellationToken token = default)
        {
            if(await _driver.Start())
            {
                return await StartWithoutConnection(token);
            }
            return false;
        }

        public override async Task<bool> Stop(CancellationToken token = default)
        {
            var ret = await _driver.Stop();
            await StopWithoutConnection(token);

            return ret;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return new KnxBaosDatapoints(ctx, this);
        }

        public void AddAddressNotifier(string address, Action<object> callback)
        {
            AddGroupAddress(address, callback);
        }

        public override async Task<bool> Read(string address)
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

        public override Task<bool> Write(KnxGroupAddress source, string address, GroupValue groupValue, CancellationToken token)
        {
            return Write(address, groupValue.Value);
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

                // the group address nodes know the size of their datapoint type and decode the raw value
                RawValueReceived(dpId, value.Data.ToArray());
            }
            return Task.CompletedTask;
        }
    }
}
