#nullable enable annotations
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.ModBus.SolarmanV5.DriverFactory.Attributes;
using P3.Driver.ModBus.SolarmanV5.DriverFactory.Devices;
using P3.Driver.ModBusDriver.Master;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory
{
    internal class SolarmanGroupAttribute : DriverNoneAttributeBase
    {
        private readonly SolarmanDriver _parent;
        private readonly DeviceMap _map;
        private readonly DeviceGroupMap _groupMap;
        private readonly Dictionary<string, SolarmanAttrribute> _nameMap;

        public SolarmanGroupAttribute(IDriverContext driverContext, DeviceMap map, DeviceGroupMap groupMap,  SolarmanDriver parent, Dictionary<string, SolarmanAttrribute> nameMap) : base(driverContext)
        {
            _parent = parent;
            _nameMap = nameMap;
            _map = map;
            _groupMap = groupMap;
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            _parent.Read(token).ConfigureAwait(false);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Dispatches the values of every node whose registers lie completely in <paramref name="groupRead"/>.
        /// </summary>
        /// <param name="except">Node that is not dispatched (it dispatches the value it wrote itself).</param>
        internal async Task FetchValues(ModBusRegisterValueReturn modbusRegisterValue, (int start, int end) groupRead, SolarmanAttrribute? except = null)
        {
            var registers = modbusRegisterValue.Data.ToArray();
            foreach (var register in _map.NameRegisterMap)
            {
                if (_nameMap.TryGetValue(register.Key, out var attribute))
                {
                    var first = register.Value[0];
                    var length = register.Value.Count;
                    var offset = first - groupRead.start;

                    // a node is only dispatched when all its registers were read
                    if (first >= groupRead.start && first + length - 1 <= groupRead.end && offset + length <= registers.Length)
                    {
                        if (ReferenceEquals(attribute, except))
                        {
                            continue;
                        }

                        var data = new ushort[length];
                        Array.Copy(registers, offset, data, 0, length);

                        var val = await attribute.ConvertValue(data);
                        attribute.DispatchRead(val);
                    }
                }
                else
                {
                    // nodes the user did not create (e.g. the time of use group) are part of the map, too
                    DriverContext.Logger.LogDebug($"Could not find instance for {register.Key}");
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
            if (_map.GridChargeKeys.Contains(key))
            {
                attribute = CreateGridChargeAttribute(ctx, attributeMap);
            }
            else if (_map.Decoders.TryGetValue(key, out var decoder))
            {
                attribute = new SolarmanDecodedAttribute(ctx, this, decoder);
            }
            else if (ctx.NodeInstance.This2NodeTemplateNavigation.This2NodeDataType ==
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

            attribute.SignedInRegisterMap = _map.IsSigned(key);
            _nameMap.Add(key, attribute);

            return attribute;
        }

        private SolarmanAttrribute? CreateGridChargeAttribute(IDriverContext ctx, List<int> registers)
        {
            var (start, count) = _map.TimeOfUseChargeRegisters;
            if (registers.Count == count && registers[0] == start)
            {
                return new SolarmanGridChargeAttribute(ctx, this, null);
            }

            if (registers.Count == 1 && registers[0] >= start && registers[0] < start + count)
            {
                return new SolarmanGridChargeAttribute(ctx, this, registers[0] - start);
            }

            return null;
        }

        internal Task<bool> WriteGridCharge(int? slotIndex, bool enabled, SolarmanAttrribute source, CancellationToken token)
        {
            return _parent.WriteGridCharge(slotIndex, enabled, source, token);
        }

    }
}
