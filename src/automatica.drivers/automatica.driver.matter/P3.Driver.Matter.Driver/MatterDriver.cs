using Automatica.Core.Control.Base;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.Model;
using Microsoft.Extensions.Logging;
using P3.Driver.Matter.Commissioning;
using P3.Driver.Matter.DataModel.Clusters;

namespace P3.Driver.Matter.Driver
{
    /// <summary>Publishes the configured Automatica controls as bridged Matter devices.</summary>
    public class MatterDriver : DriverNoneAttributeBase
    {
        private readonly Dictionary<string, MatterValueNode> _valueNodes = new();
        private readonly List<MatterDeviceNode> _deviceNodes = new();
        private MatterBridge _bridge;
        private volatile bool _running;

        public MatterDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override async Task<bool> Start(CancellationToken token = default)
        {
            var port = GetPropertyValueInt(MatterDriverFactory.PortKey);
            var options = new MatterNodeOptions
            {
                Port = port is > 0 and <= ushort.MaxValue ? port : 5540,
                DeviceName = string.IsNullOrWhiteSpace(DriverContext.NodeInstance.Name) ? "Automatica Bridge" : DriverContext.NodeInstance.Name,
                VendorName = "Automatica",
                ProductName = "Automatica Matter Bridge",
                SoftwareVersionString = typeof(MatterDriver).Assembly.GetName().Version?.ToString() ?? "1.0.0"
            };

            var storage = new PropertyMatterStorage(DriverContext, GetProperty(MatterDriverFactory.StateKey));
            _bridge = new MatterBridge(options, storage, DriverContext.Logger);
            await _bridge.InitializeAsync(token);

            _running = true;
            var controlsProperty = GetProperty(MatterDriverFactory.ControlsKey);
            if (controlsProperty?.Value is ControlConfiguration controlConfig)
            {
                foreach (var reference in controlConfig.Controls)
                {
                    try
                    {
                        var control = await DriverContext.ControlContext.GetAsync(reference, token);
                        if (control != null)
                        {
                            AddControl(control);
                        }
                    }
                    catch (Exception e)
                    {
                        DriverContext.Logger.LogError(e, "Could not bridge control {Control}", reference);
                    }
                }
            }

            foreach (var deviceNode in _deviceNodes)
            {
                try
                {
                    _bridge.Add(deviceNode.CreateDevice());
                }
                catch (Exception e)
                {
                    DriverContext.Logger.LogError(e, "Could not bridge device {Name}", deviceNode.Name);
                }
            }

            _bridge.Node.StatusChanged += OnStatusChanged;
            await _bridge.StartAsync(token);
            PublishStatus();

            DriverContext.Logger.LogInformation("Matter bridge started with {Count} device(s), pairing code {Code}, QR {Qr}",
                _bridge.Devices.Count, SetupPayload.FormatManualCode(_bridge.Node.ManualPairingCode), _bridge.Node.QrCodePayload);

            return await base.Start(token);
        }

        private void AddControl(IControl control)
        {
            var id = control.Id.ToString("N");
            switch (control)
            {
                case IDimmer dimmer:
                {
                    var device = _bridge.Add(new BridgedDimmableLight(id, control.Name)
                    {
                        SwitchRequested = on => Guard(() => dimmer.SwitchAsync(on)),
                        LevelRequested = percent => Guard(() => dimmer.DimAsync(percent))
                    });
                    device.SetState(dimmer.DimmerState, dimmer.DimmerValue);
                    dimmer.RegisterValueCallback(() =>
                    {
                        if (_running)
                        {
                            device.SetState(dimmer.DimmerState, dimmer.DimmerValue);
                        }
                    });
                    break;
                }
                case ISwitch @switch:
                {
                    var device = _bridge.Add(new BridgedOnOffDevice(id, control.Name)
                    {
                        SwitchRequested = on => Guard(() => @switch.SwitchAsync(on))
                    });
                    device.SetOnOff(@switch.State == SwitchState.On);
                    @switch.RegisterValueCallback(() =>
                    {
                        if (_running)
                        {
                            device.SetOnOff(@switch.State == SwitchState.On);
                        }
                    });
                    break;
                }
                case IBlind blind:
                {
                    var device = _bridge.Add(new BridgedWindowCovering(id, control.Name)
                    {
                        CommandRequested = (command, target) => Guard(() => command switch
                        {
                            CoveringCommand.Open => blind.MoveUpAsync(),
                            CoveringCommand.Close => blind.MoveDownAsync(),
                            CoveringCommand.Stop => blind.StopAsync(),
                            _ => blind.MoveAbsoluteAsync(target ?? blind.Position)
                        })
                    });
                    device.SetState(blind.Position, Movement(blind));
                    blind.RegisterValueCallback(() =>
                    {
                        if (_running)
                        {
                            device.SetState(blind.Position, Movement(blind));
                        }
                    });
                    break;
                }
                default:
                    DriverContext.Logger.LogWarning("Control {Name} ({Type}) has no Matter mapping and is skipped", control.Name, control.GetType().Name);
                    return;
            }

            DriverContext.Logger.LogDebug("Bridged control {Name} as {Type}", control.Name, control.GetType().Name);
        }

        private static CoveringMovement Movement(IBlind blind)
        {
            if (!blind.IsMoving)
            {
                return CoveringMovement.Stopped;
            }

            return blind.Direction == 0 ? CoveringMovement.Opening : CoveringMovement.Closing;
        }

        private async Task Guard(Func<Task> action)
        {
            if (!_running)
            {
                return;
            }

            await action();
        }

        private void OnStatusChanged(object sender, EventArgs e) => PublishStatus();

        private void PublishStatus()
        {
            var node = _bridge?.Node;
            if (node?.State == null)
            {
                return;
            }

            Publish(MatterDriverFactory.PairingCodeNodeKey, SetupPayload.FormatManualCode(node.ManualPairingCode));
            Publish(MatterDriverFactory.QrCodeNodeKey, node.QrCodePayload);
            Publish(MatterDriverFactory.CommissionedNodeKey, node.CommissionedFabricCount > 0);
            Publish(MatterDriverFactory.FabricsNodeKey, node.CommissionedFabricCount);
        }

        private void Publish(string key, object value)
        {
            if (_valueNodes.TryGetValue(key, out var node))
            {
                node.Publish(value);
            }
        }

        public override Task<IList<NodeInstance>> CustomAction(string actionName, CancellationToken token = default)
        {
            switch (actionName)
            {
                case MatterDriverFactory.OpenWindowKey:
                    DriverContext.Logger.LogInformation("Opening the Matter commissioning window for 15 minutes");
                    _bridge?.Node.OpenCommissioningWindow(TimeSpan.FromMinutes(15));
                    break;
                case MatterDriverFactory.ResetPairingsKey:
                    DriverContext.Logger.LogInformation("Removing all Matter pairings");
                    _bridge?.Node.RemoveAllFabrics();
                    break;
            }

            return base.CustomAction(actionName, token);
        }

        public override async Task<bool> Stop(CancellationToken token = default)
        {
            _running = false;
            if (_bridge != null)
            {
                _bridge.Node.StatusChanged -= OnStatusChanged;
                await _bridge.Node.SaveAsync();
                await _bridge.DisposeAsync();
                _bridge = null;
            }

            return await base.Stop(token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation?.Key;
            if (key is MatterDriverFactory.PairingCodeNodeKey or MatterDriverFactory.QrCodeNodeKey
                or MatterDriverFactory.CommissionedNodeKey or MatterDriverFactory.FabricsNodeKey)
            {
                var node = new MatterValueNode(ctx);
                _valueNodes[key] = node;
                return node;
            }

            var kind = MatterDeviceTemplates.KindForFolder(key);
            if (kind != null)
            {
                var deviceNode = new MatterDeviceNode(ctx, kind);
                _deviceNodes.Add(deviceNode);
                return deviceNode;
            }

            return null;
        }
    }
}
