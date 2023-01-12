using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.ModBus.SolarmanV5.DriverFactory.Attributes;
using P3.Driver.ModBus.SolarmanV5.DriverFactory.Devices;
using P3.Driver.ModBusDriver.Master;
using Exception = System.Exception;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory
{
    internal class SolarmanGroupAttribute : DriverBase
    {
        private readonly SolarmanConnection _driver;
        private readonly SolarmanDriver _parent;
        private readonly DeviceMap _map;
        private readonly Dictionary<string, SolarmanAttrribute> _nameMap;
        public SolarmanGroupAttribute(IDriverContext driverContext, DeviceMap map, SolarmanConnection driver, SolarmanDriver parent, Dictionary<string, SolarmanAttrribute> nameMap) : base(driverContext)
        {
            _driver = driver;
            _parent = parent;
            _nameMap = nameMap;
            _map = map;
        }

        internal async Task PollAttributes()
        {
            DriverContext.Logger.LogInformation($"Polling solarman device...");
            if (_driver == null)
            {
                throw new ArgumentException("Driver not initialized...");
            }

            foreach (var register in _map.NameRegisterMap)
            {
                try
                {
                    if (_nameMap.ContainsKey(register.Key))
                    {
                        DriverContext.Logger.LogInformation($"Read value for {register.Key}");

                        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                        var value = await _driver.ReadRegisters(_parent.DeviceId, (ushort)register.Value[0],
                            register.Value.Count, cancellationTokenSource.Token);

                        if (value is { ModBusRequestStatus: ModBusRequestStatus.Success }
                            and ModBusRegisterValueReturn modbusRegisterValue)
                        {
                            var val = await _nameMap[register.Key].ConvertValue(modbusRegisterValue);
                            _nameMap[register.Key].DispatchValue(val);
                        }
                    }
                    else
                    {
                        DriverContext.Logger.LogInformation($"Could not find instance for {register.Key}");
                    }
                }
                catch (IOException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    DriverContext.Logger.LogError(e, $"Could not poll attribute {e}");
                }

            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation.Key;


            if (!_map.ContainsKey(key))
            {
                DriverContext.Logger.LogError($"Could not find entry for {key} in map");
                return null;
            }
            var attributeMap = _map[key];



            SolarmanAttrribute? attribute = null;
            if (ctx.NodeInstance.This2NodeTemplateNavigation.This2NodeDataType ==
                (long)NodeDataType.Double)
            {
                if (attributeMap.Count == 1)
                {
                    attribute = new Solarman2ByteIntegerAttribute(ctx, this);
                }
                else if (attributeMap.Count == 2)
                {
                    attribute = new Solarman4ByteIntegerAttribute(ctx, this);
                }
            }
            else if (ctx.NodeInstance.This2NodeTemplateNavigation.This2NodeDataType ==
                (long)NodeDataType.Integer)
            {
                if (attributeMap.Count == 1)
                {
                    attribute = new Solarman2ByteIntegerAttribute(ctx, this);
                }
                else if (attributeMap.Count == 2)
                {
                    attribute = new Solarman4ByteIntegerAttribute(ctx, this);
                }
            }
            else if (ctx.NodeInstance.This2NodeTemplateNavigation.This2NodeDataType ==
                (long)NodeDataType.String)
            {
                attribute = new SolarmanStringAttribute(ctx, this);
            }
            else if (ctx.NodeInstance.This2NodeTemplateNavigation.This2NodeDataType ==
                     (long)NodeDataType.Boolean)
            {
                attribute = new SolarmanBooleanAttribute(ctx, this);
            }

            if (attribute == null)
            {
                DriverContext.Logger.LogError($"Could not find suitable implementation for attribute {key}");
                return null;
            }

            _nameMap.Add(key, attribute);

            return attribute;
        }
    }
}
