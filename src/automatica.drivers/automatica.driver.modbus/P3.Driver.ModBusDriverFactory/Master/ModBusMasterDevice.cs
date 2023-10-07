﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.ModBusDriver.Master;
using P3.Driver.ModBusDriverFactory.Attributes;
using P3.Driver.ModBusDriverFactory.Common;

namespace P3.Driver.ModBusDriverFactory.Master
{
    public class ModBusMasterDevice : DriverNoneAttributeBase
    {
        private readonly IModBusMasterDriver _modBusDriver;
        public int PollInterval { get; private set; }
        public byte DeviceId { get; private set; }

        public List<ModBusMasterAttribute> Attributes { get; } = new();

        private readonly System.Timers.Timer _pollTimer = new System.Timers.Timer();

        private readonly SemaphoreSlim _waitSemaphore;

        public ModBusMasterDevice(IDriverContext driverContext, IModBusMasterDriver modBusDriver, SemaphoreSlim semaphore) : base(driverContext)
        {
            _modBusDriver = modBusDriver;
            _waitSemaphore = semaphore;
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            PollInterval = GetPropertyValueInt("modbus-poll-interval");
            DeviceId = (byte)GetPropertyValueInt("modbus-device-id");

            _pollTimer.Interval = PollInterval;

            DriverContext.Logger.LogInformation($"Polling {DriverContext.NodeInstance.Name} (Address: {DeviceId}) every {PollInterval/60}seconds...");

            return base.Init(token);
        }


        public override Task<bool> Start(CancellationToken token = default)
        {
            _pollTimer.Elapsed += PollTimerOnElapsed;
            _pollTimer.Start();

#pragma warning disable 4014
            PollAttributes();
#pragma warning restore 4014
            return base.Start(token);
        }

        private async void PollTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            await _waitSemaphore.WaitAsync();
            try
            {
                if (!_modBusDriver.Connected)
                {
                    DriverContext.Logger
                        .LogWarning($"Could not read device {DeviceId}, connection state is false, try to reconnect...");

                    await _modBusDriver.Reconnect();

                    return;
                }
                DriverContext.Logger.LogInformation($"Start polling {DriverContext.NodeInstance.Name} at address: {DeviceId}...");
                await PollAttributes();
            }
            finally
            {
                _waitSemaphore.Release();
            }
        }

        private async Task PollAttributes()
        {
            foreach (var attribute in Attributes)
            {
                try
                {
                    if (attribute.DriverContext.NodeInstance.IsReadable)
                    {
                        var value = await attribute.ReadValue();
                        if (value != null)
                        {
                            attribute.DispatchRead(value);
                        }
                    }
                }
                catch (IOException)
                {
                    await _modBusDriver.Stop();
                    _modBusDriver.Open();

                }
                catch (ModBusException)
                {
                    // ignore
                }
                catch (Exception e)
                {
                    DriverContext.Logger.LogError(e, $"Unknown error occurred {e}");
                }
            }
        }

        public override async Task<bool> Stop(CancellationToken token = default)
        {
            _pollTimer.Elapsed -= PollTimerOnElapsed;
            
            return await base.Stop(token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            Type t;
            switch (ctx.NodeInstance.This2NodeTemplateNavigation.Key)
            {
                case "modbus-+2byte":
                case "modbus-2byte":
                    t = typeof(ModBus2ByteInteger);
                    break;
                case "modbus-+4byte":
                case "modbus-4byte":
                    t = typeof(ModBus4ByteInteger);
                    break;
                case "modbus-+8byte":
                case "modbus-8byte":
                    t = typeof(ModBus8ByteInteger);
                    break;
                case "modbus-4float":
                    t = typeof(ModBus4ByteFloat);
                    break;
                case "modbus-8float":
                    t = typeof(ModBus8ByteFloat);
                    break;
                case "modbus-binary":
                    t = typeof(ModBusBinaryAttribute);
                    break;
                default:
                    return null;
            }


            var attribute = Activator.CreateInstance(t, ctx) as ModBusAttribute;
            var driverNode = new ModBusMasterAttribute(ctx, this, _modBusDriver, attribute);
            Attributes.Add(driverNode);
            return driverNode;
        }
    }
}
