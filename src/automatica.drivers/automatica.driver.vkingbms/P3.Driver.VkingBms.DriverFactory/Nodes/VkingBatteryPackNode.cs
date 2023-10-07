using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.VkingBms.Driver;
using P3.Driver.VkingBms.Driver.Interfaces;

namespace P3.Driver.VkingBms.DriverFactory.Nodes
{
    internal class VkingBatteryPackNode : DriverNoneAttributeBase
    {

        private VkingBatteryCellsNode _cellsNode;
        private VkingBatteryTempsNode _tempsNode;

        private VkingBatteryValueNode _voltage;
        private VkingBatteryValueNode _current;
        private VkingBatteryValueNode _soh;
        private VkingBatteryValueNode _soc;
        private VkingBatteryValueNode _version;
        private VkingBatteryValueNode _remainCapacity;
        private VkingBatteryValueNode _fullCharge;
        private VkingBatteryValueNode _lastUpdate;
        private VkingBatteryValueNode _cycleTimes;
        private VkingBatteryValueNode _bmsTime;
        private VkingBatteryValueNode _minCell;
        private VkingBatteryValueNode _maxCell;
        private VkingBatteryValueNode _cellDiff;


        public byte PackId { get; private set; }
        public VkingBatteryPackNode(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            PackId = (byte)GetProperty("vking-pack-id").ValueInt!.Value;
            return base.Init();
        }

        public bool Read(IAnalogDataResponse analogData, IVersionIdResponse version)
        {
            try
            {
                _voltage?.DispatchRead(Convert.ToDouble(analogData.Voltage) / 100);
                _current?.DispatchRead(Convert.ToDouble(analogData.Current) / 100);
                _soh?.DispatchRead(analogData.Soh);
                _soc?.DispatchRead(analogData.Soc);
                _version?.DispatchRead(version.VersionId);
                _remainCapacity?.DispatchRead(analogData.RemainingCapacity);
                _fullCharge?.DispatchRead(analogData.FullCapacity);
                _cycleTimes?.DispatchRead(analogData.CycleNumber);
                //_bmsTime?.DispatchValue(analogData.Voltage);

                var minCell = analogData.CellVoltages.Min();
                var maxCell = analogData.CellVoltages.Max();

                _minCell?.DispatchRead(minCell);
                _maxCell?.DispatchRead(maxCell);
                _cellDiff?.DispatchRead(maxCell - minCell);

                _cellsNode?.Cell1?.DispatchRead(analogData.CellVoltages[0]);
                _cellsNode?.Cell2?.DispatchRead(analogData.CellVoltages[1]);
                _cellsNode?.Cell3?.DispatchRead(analogData.CellVoltages[2]);
                _cellsNode?.Cell4?.DispatchRead(analogData.CellVoltages[3]);
                _cellsNode?.Cell5?.DispatchRead(analogData.CellVoltages[4]);
                _cellsNode?.Cell6?.DispatchRead(analogData.CellVoltages[5]);
                _cellsNode?.Cell7?.DispatchRead(analogData.CellVoltages[6]);
                _cellsNode?.Cell8?.DispatchRead(analogData.CellVoltages[7]);
                _cellsNode?.Cell9?.DispatchRead(analogData.CellVoltages[8]);
                _cellsNode?.Cell10?.DispatchRead(analogData.CellVoltages[9]);
                _cellsNode?.Cell11?.DispatchRead(analogData.CellVoltages[10]);
                _cellsNode?.Cell12?.DispatchRead(analogData.CellVoltages[11]);
                _cellsNode?.Cell13?.DispatchRead(analogData.CellVoltages[12]);
                _cellsNode?.Cell14?.DispatchRead(analogData.CellVoltages[13]);
                _cellsNode?.Cell15?.DispatchRead(analogData.CellVoltages[14]);
                _cellsNode?.Cell16?.DispatchRead(analogData.CellVoltages[15]);

                _tempsNode.Environment?.DispatchRead(analogData.Temperatures[0]);
                _tempsNode.Mos?.DispatchRead(analogData.Temperatures[1]);
                _tempsNode.CellT1?.DispatchRead(analogData.Temperatures[2]);
                _tempsNode.CellT2?.DispatchRead(analogData.Temperatures[3]);
                _tempsNode.CellT3?.DispatchRead(analogData.Temperatures[4]);
                _tempsNode.CellT4?.DispatchRead(analogData.Temperatures[5]);


                _lastUpdate?.DispatchRead(DateTime.Now);
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, $"{e} {PackId}: could not read data...");
                return false;
            }

            return true;
        }


        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation.Key.Replace("vking-bms-pack-cells-", "");


            DriverBase? ret = null;
            switch (key)
            {
                case "vking-bms-pack-cells":
                    _cellsNode = new VkingBatteryCellsNode(ctx);
                    ret = _cellsNode;
                    break;
                case "temperatures":
                    _tempsNode = new VkingBatteryTempsNode(ctx);
                    ret = _tempsNode;
                    break;
                case "voltage":
                    _voltage = new VkingBatteryValueNode(ctx);
                    ret = _voltage;
                    break;
                case "current":
                    _current = new VkingBatteryValueNode(ctx);
                    ret = _current;
                    break;
                case "soh":
                    _soh = new VkingBatteryValueNode(ctx);
                    ret = _soh;
                    break;
                case "soc":
                    _soc = new VkingBatteryValueNode(ctx);
                    ret = _soc;
                    break;
                case "remain_capacity":
                    _remainCapacity= new VkingBatteryValueNode(ctx);
                    ret = _remainCapacity;
                    break;
                case "full_charge":
                    _fullCharge = new VkingBatteryValueNode(ctx);
                    ret = _fullCharge;
                    break;
                case "cycle_times":
                    _cycleTimes = new VkingBatteryValueNode(ctx);
                    ret = _cycleTimes;
                    break;
                case "last_update":
                    _lastUpdate = new VkingBatteryValueNode(ctx);
                    ret = _lastUpdate;
                    break;
                case "version":
                    _version = new VkingBatteryValueNode(ctx);
                    ret = _version;
                    break;
                case "bms_time":
                    _bmsTime = new VkingBatteryValueNode(ctx);
                    ret = _bmsTime;
                    break;
                case "cell_min":
                    _minCell = new VkingBatteryValueNode(ctx);
                    ret = _minCell;
                    break;
                case "cell_max":
                    _maxCell = new VkingBatteryValueNode(ctx);
                    ret = _maxCell;
                    break;
                case "cell_difference":
                    _cellDiff = new VkingBatteryValueNode(ctx);
                    ret = _cellDiff;
                    break;

            }

            return ret;
        }
    }
}
