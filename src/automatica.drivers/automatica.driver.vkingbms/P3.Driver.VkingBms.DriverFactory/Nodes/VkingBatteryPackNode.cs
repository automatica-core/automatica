using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.VkingBms.Driver;
using P3.Driver.VkingBms.Driver.Interfaces;

namespace P3.Driver.VkingBms.DriverFactory.Nodes
{
    internal class VkingBatteryPackNode : DriverBase
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

        public override bool Init()
        {
            PackId = (byte)GetProperty("vking-pack-id").ValueInt!.Value;
            return base.Init();
        }

        public bool Read(IAnalogDataResponse analogData, IVersionIdResponse version)
        {
            try
            {
                _voltage?.DispatchValue(Convert.ToDouble(analogData.Voltage) / 100);
                _current?.DispatchValue(Convert.ToDouble(analogData.Current) / 100);
                _soh?.DispatchValue(analogData.Soh);
                _soc?.DispatchValue(analogData.Soc);
                _version?.DispatchValue(version.VersionId);
                _remainCapacity?.DispatchValue(analogData.RemainingCapacity);
                _fullCharge?.DispatchValue(analogData.FullCapacity);
                _cycleTimes?.DispatchValue(analogData.CycleNumber);
                //_bmsTime?.DispatchValue(analogData.Voltage);

                var minCell = analogData.CellVoltages.Min();
                var maxCell = analogData.CellVoltages.Max();

                _minCell?.DispatchValue(minCell);
                _maxCell?.DispatchValue(maxCell);
                _cellDiff?.DispatchValue(maxCell - minCell);

                _cellsNode?.Cell1?.DispatchValue(analogData.CellVoltages[0]);
                _cellsNode?.Cell2?.DispatchValue(analogData.CellVoltages[1]);
                _cellsNode?.Cell3?.DispatchValue(analogData.CellVoltages[2]);
                _cellsNode?.Cell4?.DispatchValue(analogData.CellVoltages[3]);
                _cellsNode?.Cell5?.DispatchValue(analogData.CellVoltages[4]);
                _cellsNode?.Cell6?.DispatchValue(analogData.CellVoltages[5]);
                _cellsNode?.Cell7?.DispatchValue(analogData.CellVoltages[6]);
                _cellsNode?.Cell8?.DispatchValue(analogData.CellVoltages[7]);
                _cellsNode?.Cell9?.DispatchValue(analogData.CellVoltages[8]);
                _cellsNode?.Cell10?.DispatchValue(analogData.CellVoltages[9]);
                _cellsNode?.Cell11?.DispatchValue(analogData.CellVoltages[10]);
                _cellsNode?.Cell12?.DispatchValue(analogData.CellVoltages[11]);
                _cellsNode?.Cell13?.DispatchValue(analogData.CellVoltages[12]);
                _cellsNode?.Cell14?.DispatchValue(analogData.CellVoltages[13]);
                _cellsNode?.Cell15?.DispatchValue(analogData.CellVoltages[14]);
                _cellsNode?.Cell16?.DispatchValue(analogData.CellVoltages[15]);

                _tempsNode.Environment?.DispatchValue(analogData.Temperatures[0]);
                _tempsNode.Mos?.DispatchValue(analogData.Temperatures[1]);
                _tempsNode.CellT1?.DispatchValue(analogData.Temperatures[2]);
                _tempsNode.CellT2?.DispatchValue(analogData.Temperatures[3]);
                _tempsNode.CellT3?.DispatchValue(analogData.Temperatures[4]);
                _tempsNode.CellT4?.DispatchValue(analogData.Temperatures[5]);


                _lastUpdate?.DispatchValue(DateTime.Now);
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
