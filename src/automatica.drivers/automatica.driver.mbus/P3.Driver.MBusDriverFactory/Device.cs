using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Driver;
using Automatica.Core.Driver.Utility;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;
using P3.Driver.MBus.Frames;
using P3.Driver.MBus.Frames.VariableData;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace P3.Driver.MBusDriverFactory
{
    public class Device : DriverBase
    {
        private readonly Driver _parent;
        private readonly Timer _pollTimer = new Timer();

        private readonly List<Attribute> _attributes = new List<Attribute>();
        private readonly Dictionary<string, Attribute> _attributeMap = new Dictionary<string, Attribute>();

        private readonly ILogger _logger;


        public Device(IDriverContext driverContext, Driver parent) : base(driverContext)
        {
            _parent = parent;
            _logger = driverContext.Logger;
        }
        protected override bool CreateCustomLogger()
        {
            return true;
        }

        public override Task<bool> Start()
        {
            _pollTimer?.Start();

            foreach (var att in _attributes)
            {
                if (_attributeMap.ContainsKey(att.Address))
                {
                    _logger.LogWarning($"Ignore Attribute {att.Name}, address ({att.Address}) already existing");
                    continue;
                }
                _attributeMap.Add(att.Address, att);
            }
            if (_attributes.Count > 0)
            {
                _pollTimer_Elapsed(null, null);
            }

            return base.Start();
           
        }

        public override Task<bool> Stop()
        {
            _pollTimer?.Stop();
            if (_pollTimer != null)
            {
                _pollTimer.Elapsed -= _pollTimer_Elapsed;
            }

            return base.Stop();
        }


        public override bool Init()
        {
            DeviceId = GetProperty("mbus-deviceId").ValueInt.Value;
            PollInterval = GetProperty("mbus-pollInterval").ValueInt.Value;
            ResetBeforeRead = GetProperty("mbus-resetBeforePoll").ValueBool.Value;
            DeviceTimeout = GetProperty("mbus-device-timeout").ValueInt.Value;

            _pollTimer.Interval = PollInterval * 1000;

            _pollTimer.Elapsed += _pollTimer_Elapsed;

            return true;
        }

        private async void _pollTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_attributes.Count > 0)
            {
                _pollTimer.Stop();
                try
                {
                    await ReadInternal();
                }
                finally
                {
                    _pollTimer.Start();
                }
            }
            else
            {
                _logger.LogWarning($"{_parent.Name}-{Name} Ignoring device read because no attributes have been configured");
            }

           
        }

        public override async Task<bool> Read()
        {
            await ReadInternal();
            return true;
        }

        private async Task ReadInternal()
        {
            await Task.Delay(TimeSpan.FromSeconds(2));

            _logger.LogInformation($"Read MBus device with id {DeviceId}");

            var frame = await _parent.Read(this);

            if (frame is VariableDataFrame dataFrame)
            {
                if (dataFrame.AddressField != DeviceId)
                {
                    _logger.LogWarning($"Received frame is from different MBus device (Received address {dataFrame.AddressField}, our id {DeviceId}");
                    return;
                }
                foreach (var vdb in dataFrame.DataBlocks)
                {
                    var address = $"{(int) vdb.ValueInformationField.Unit}-{vdb.StorageNumber}-{vdb.Tariff}";
                    SetValue(address, vdb.Value);
                    _logger.LogDebug($"{vdb.DataInformationField.DataFieldType}: {vdb.Value} {vdb.ValueInformationField.Unit} ({Utils.ByteArrayToString(vdb.Data)})");
                }
            }
            else
            {
                _logger.LogWarning($"Could not read device {Name} with ID {DeviceId}");
            }
        }
        private void SetValue(string address, object value)
        {
            foreach (var att in _attributes)
            {
                if (att.Address == address)
                {
                    att.SetValue(value);
                }
            }
        }

        public int DeviceTimeout { get; private set; }

        public bool ResetBeforeRead { get; private set; }

        public int DeviceId { get; private set; }

        public int PollInterval { get; private set; }

        public override async Task<IList<NodeInstance>> Scan()
        {
            var attributes = new List<NodeInstance>();
            var frame = await _parent.ScanDevice(DeviceId, DeviceTimeout);
       
            if (frame is VariableDataFrame vdf)
            {
                foreach (var d in vdf.DataBlocks)
                {
                    var att = CreateNodeInstanceFromDataBlock(d);
                    if(att != null)
                    {
                        attributes.Add(att);
                        att.SetProperty("mbus-storageNumber", d.StorageNumber);
                        att.SetProperty("mbus-tariff", d.Tariff);
                        att.SetProperty("mbus-type", (int) d.ValueInformationField.Unit);

                        att.Name = $"Attribute {d.ValueInformationField.Unit}-{d.StorageNumber}-{d.Tariff}";
                        att.Description = att.Name;
                    }
                }
            }


            return attributes;
        }

        public NodeInstance CreateNodeInstanceFromDataBlock(VariableDataBlock block)
        {

            switch (block.ValueInformationField.Unit)
            {
                case Unit.ActualityDuration:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusActualityDuration);
                case Unit.EnergyW:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusEnergyW);
                case Unit.EnergyJ:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusEnergyJ);
                case Unit.Volume:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusVolume);
                case Unit.Mass:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusMass);
                case Unit.OnTime:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusOnTime);
                case Unit.OperatingTime:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusOperatingTime);
                case Unit.PowerW:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusPowerW);
                case Unit.PowerJh:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusPowerJh);
                case Unit.VolumeFlowh:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusVolumeFlowh);
                case Unit.VolumeFlowmin:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusVolumeFlowmin);
                case Unit.VolumeFlows:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusVolumeFlows);
                case Unit.MassFlow:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusMassFlow);
                case Unit.FlowTemperature:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusFlowTemperature);
                case Unit.ReturnTemperature:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusReturnTemperature);
                case Unit.TemperatureDifference:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusTemperatureDifference);
                case Unit.ExternalTemperature:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusExternalTemperature);
                case Unit.Pressure:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusPressure);
                case Unit.TimePoint:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusTimePoint);
                case Unit.UnitsForHca:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusUnitsForHca);
                case Unit.AveragingDuration:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusAveragingDuration);
                case Unit.EnhancedId:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusEnhancedId);
                case Unit.BusAddress:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusBusAddress);
                case Unit.FabricationNumber:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusFabricationNumber);
                case Unit.ManufacturerSpecific:
                    return DriverContext.NodeTemplateFactory.CreateNodeInstance(MBusUdpFactory.MbusManufacturerSpecific);
                case Unit.Volt:
                    break;
                case Unit.Ampere:
                    break;
                    
            }
            return null;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var att = new Attribute(ctx);

            _attributes.Add(att);

            return att;
        }
    }
}
