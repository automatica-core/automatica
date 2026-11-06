using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Control.Base;
using Automatica.Core.UnitTests.Base.Drivers;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.HomeKit.Hap.Model;
using P3.Driver.HomeKitFactory;
using P3.Driver.HomeKitFactory.NodeInstances;
using P3.Driver.HomeKitFactory.NodeInstances.Nodes;
using Xunit;

namespace P3.Driver.HomeKit.UnitTests.Factory
{
    public class HomeKitDriverTests : DriverFactoryTestBase<HomeKitFactory.HomeKitFactory>
    {
        private sealed class FakeBlind : IBlind
        {
            private readonly List<Action> _callbacks = new();

            public Guid Id { get; } = Guid.NewGuid();
            public string Name => "blind";
            public bool IsMoving { get; set; }
            public int Direction { get; set; }
            public int Position { get; set; }

            public void Changed() => _callbacks.ForEach(a => a());

            public Guid RegisterValueCallback(Action callback)
            {
                _callbacks.Add(callback);
                return Guid.NewGuid();
            }

            public void UnregisterValueCallback(Guid id)
            {
            }

            public Task StopAsync(CancellationToken token = default) => Task.CompletedTask;
            public Task MoveUpAsync(CancellationToken token = default) => Task.CompletedTask;
            public Task MoveDownAsync(CancellationToken token = default) => Task.CompletedTask;
            public Task MoveAbsoluteAsync(int pos, CancellationToken token = default) => Task.CompletedTask;
        }

        private DriverContextMock Context(string templateKey)
        {
            var node = Factory.CreateNodeInstanceByKey(templateKey);
            node.ObjId = Guid.NewGuid();
            return new DriverContextMock(node, DriverFactory, Factory, Dispatcher, NullLoggerFactory.Instance, TimeProvider.System);
        }

        [Fact]
        public void LightBulbBrightnessUsesTheExistingCharacteristic()
        {
            var driverNode = CreateNodeInstance(DriverFactory.DriverGuid);
            var driver = new HomeKitDriver(new DriverContextMock(driverNode, DriverFactory, Factory, Dispatcher, NullLoggerFactory.Instance, TimeProvider.System));

            var accessory = AccessoryFactory.CreateLightBulbAccessory(2, "lamp", "AutomaticaCore", "1", false, 0);
            var folder = new FolderNodeInstance(Context("light-bulb-folder"), driver, accessory);

            var node = (BaseNode)folder.CreateDriverNode(Context("light-bulb-brightness"));

            var brightness = accessory.Specific.Characteristics.Where(a => a.Type == CharacteristicBase.BrightnessType).ToList();
            Assert.Single(brightness);
            Assert.Same(brightness[0], node.Characteristic);
            Assert.Equal(2, accessory.Specific.Characteristics.Count);
        }

        [Fact]
        public void BlindInitializesTargetPositionFromPosition()
        {
            var accessory = AccessoryFactory.CreateWindowCovering(3, "blind", "AutomaticaCore", "1", 0);
            var blind = new FakeBlind { Position = 30 }; // 30 % closed => HomeKit 70 % open

            HomeKitDriver.BindBlind(accessory, blind, _ => { }, NullLogger.Instance);

            Assert.Equal(70, accessory.TargetPosition.Value);
            Assert.Equal(70, accessory.CurrentPosition.Value);
            Assert.Equal(2, accessory.PositionType.Value); // stopped
        }

        [Fact]
        public void BlindChangesNotifyCurrentPositionAndPositionState()
        {
            var accessory = AccessoryFactory.CreateWindowCovering(3, "blind", "AutomaticaCore", "1", 0);
            var blind = new FakeBlind { Position = 0 };
            var written = new List<(Characteristic Characteristic, object Value)>();

            HomeKitDriver.BindBlind(accessory, blind, c => written.Add((c, c.Value)), NullLogger.Instance);

            blind.IsMoving = true;
            blind.Direction = 1; // down => closing
            blind.Position = 40;
            blind.Changed();

            Assert.Equal(new (Characteristic, object)[] { (accessory.CurrentPosition, 60), (accessory.PositionType, 0) }, written);
            Assert.Equal(100, accessory.TargetPosition.Value); // the target is not touched by position updates
        }

        [Fact]
        public void BlindStopSetsTheTargetToThePosition()
        {
            var accessory = AccessoryFactory.CreateWindowCovering(3, "blind", "AutomaticaCore", "1", 0);
            var blind = new FakeBlind { Position = 0 };
            var written = new List<(Characteristic Characteristic, object Value)>();

            HomeKitDriver.BindBlind(accessory, blind, c => written.Add((c, c.Value)), NullLogger.Instance);

            // a value update while standing still (e.g. before a HomeKit move starts) keeps the target
            accessory.TargetPosition.Value = 20;
            blind.Changed();
            Assert.Equal(20, accessory.TargetPosition.Value);

            // moved by a wall switch
            blind.IsMoving = true;
            blind.Direction = 1;
            blind.Position = 30;
            blind.Changed();
            Assert.DoesNotContain(written, a => a.Characteristic == accessory.TargetPosition);

            blind.IsMoving = false;
            blind.Position = 60;
            blind.Changed();

            Assert.Equal(40, accessory.TargetPosition.Value);
            Assert.Equal((accessory.TargetPosition, (object)40), written[^1]);
            Assert.Equal(2, accessory.PositionType.Value);
        }
    }
}
