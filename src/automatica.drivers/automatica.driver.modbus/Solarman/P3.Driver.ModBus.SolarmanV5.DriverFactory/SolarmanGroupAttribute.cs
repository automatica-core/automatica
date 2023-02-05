using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly SolarmanDriver _parent;
        private readonly DeviceMap _map;
        private readonly DeviceGroupMap _groupMap;
        private readonly Dictionary<string, SolarmanAttrribute> _nameMap;
        private int _failCount;
        private int _successCount;

        public SolarmanGroupAttribute(IDriverContext driverContext, DeviceMap map, DeviceGroupMap groupMap,  SolarmanDriver parent, Dictionary<string, SolarmanAttrribute> nameMap) : base(driverContext)
        {
            _parent = parent;
            _nameMap = nameMap;
            _map = map;
            _groupMap = groupMap;
        }


        internal async Task FetchValues(ModBusRegisterValueReturn modbusRegisterValue, (int start, int end) groupRead)
        {
            foreach (var register in _map.NameRegisterMap)
            {
                if (_nameMap.ContainsKey(register.Key))
                {
                    if (register.Value[0] >= groupRead.start && register.Value[0] <= groupRead.end)
                    {
                        var offset = register.Value[0] - groupRead.start;
                        var length = register.Value.Count;

                        var data = new ushort[length];
                        Array.Copy(modbusRegisterValue.Data.ToArray(), offset, data, 0, length);

                        var val = await _nameMap[register.Key].ConvertValue(data);
                        _nameMap[register.Key].DispatchValue(val);
                    }
                }
                else
                {
                    DriverContext.Logger.LogInformation($"Could not find instance for {register.Key}");
                }
            }
            
        }

        internal async Task<bool> PollAttributes()
        { 
            DriverContext.Logger.LogInformation($"Polling solarman device...");
            if (_parent.Driver == null)
            {
                throw new ArgumentException("Driver not initialized...");
            }

            foreach (var register in _map.NameRegisterMap)
            {
                try
                {
                    if (_parent.Driver == null)
                    {

                        DriverContext.Logger.LogInformation($"Driver is not initialized!");
                        break;
                    }

                    if (_nameMap.ContainsKey(register.Key))
                    {
                        DriverContext.Logger.LogInformation($"Read value for {register.Key}");

                        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                        var value = await _parent.Driver.ReadRegisters(_parent.DeviceId, (ushort)register.Value[0],
                            register.Value.Count, cancellationTokenSource.Token);

                        if (value is { ModBusRequestStatus: ModBusRequestStatus.Success }
                            and ModBusRegisterValueReturn modbusRegisterValue)
                        {
                            var val = await _nameMap[register.Key].ConvertValue(modbusRegisterValue.Data.ToArray());
                            _nameMap[register.Key].DispatchValue(val);
                            _successCount++;

                            if (_successCount >= 10)
                            {
                                _failCount = 0;
                                _successCount = 0;
                            }
                        }
                        else
                        {
                            _failCount++;
                        }

                        if (_failCount >= 20)
                        {
                            _failCount = 0;
                            return false;
                        }
                    }
                    else
                    {
                        DriverContext.Logger.LogInformation($"Could not find instance for {register.Key}");
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
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

            return true;
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
