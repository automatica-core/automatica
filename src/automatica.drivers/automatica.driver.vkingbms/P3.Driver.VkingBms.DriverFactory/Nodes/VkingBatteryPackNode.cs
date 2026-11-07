#nullable enable annotations

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

                var cells = analogData.CellVoltages ?? Array.Empty<ushort>();
                if (cells.Length > 0)
                {
                    var minCell = cells.Min();
                    var maxCell = cells.Max();

                    _minCell?.DispatchRead(minCell);
                    _maxCell?.DispatchRead(maxCell);
                    _cellDiff?.DispatchRead(maxCell - minCell);
                }

                // packs can have less than 16 cells / 6 temperature sensors
                DispatchAt(_cellsNode?.Cell1, cells, 0);
                DispatchAt(_cellsNode?.Cell2, cells, 1);
                DispatchAt(_cellsNode?.Cell3, cells, 2);
                DispatchAt(_cellsNode?.Cell4, cells, 3);
                DispatchAt(_cellsNode?.Cell5, cells, 4);
                DispatchAt(_cellsNode?.Cell6, cells, 5);
                DispatchAt(_cellsNode?.Cell7, cells, 6);
                DispatchAt(_cellsNode?.Cell8, cells, 7);
                DispatchAt(_cellsNode?.Cell9, cells, 8);
                DispatchAt(_cellsNode?.Cell10, cells, 9);
                DispatchAt(_cellsNode?.Cell11, cells, 10);
                DispatchAt(_cellsNode?.Cell12, cells, 11);
                DispatchAt(_cellsNode?.Cell13, cells, 12);
                DispatchAt(_cellsNode?.Cell14, cells, 13);
                DispatchAt(_cellsNode?.Cell15, cells, 14);
                DispatchAt(_cellsNode?.Cell16, cells, 15);

                var temps = analogData.Temperatures ?? Array.Empty<short>();
                DispatchAt(_tempsNode?.Environment, temps, 0);
                DispatchAt(_tempsNode?.Mos, temps, 1);
                DispatchAt(_tempsNode?.CellT1, temps, 2);
                DispatchAt(_tempsNode?.CellT2, temps, 3);
                DispatchAt(_tempsNode?.CellT3, temps, 4);
                DispatchAt(_tempsNode?.CellT4, temps, 5);


                _lastUpdate?.DispatchRead(DateTime.Now);
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, $"{e} {PackId}: could not read data...");
                return false;
            }

            return true;
        }

        private static void DispatchAt<T>(VkingBatteryValueNode node, T[] values, int index)
        {
            if (node != null && index < values.Length)
            {
                node.DispatchRead(values[index]);
            }
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
