using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Remote;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Habits;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Internals.Habits;
using Automatica.Core.Internals.OperatingModes;
using Automatica.Core.Tests.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace Automatica.Core.Tests.Habits
{
    public sealed class HabitRecorderTests : IDisposable
    {
        private readonly SqliteTestDatabase _database = new();
        private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 12, 18, 0, 0, TimeSpan.Zero));
        private readonly Setting _enabled = new() { ValueKey = HabitRecorder.EnabledKey, ValueInt = 1 };
        private readonly Setting _logDays = new() { ValueKey = HabitRecorder.LogDaysKey, ValueInt = 30 };
        private readonly List<NodeInstance> _nodes = new();
        private readonly Mock<IDispatcher> _dispatcher = new() { CallBase = true };
        private readonly List<Guid> _registered = new();
        private readonly HabitRecorder _recorder;

        private readonly Guid _root = Guid.NewGuid();
        private readonly Guid _light = Guid.NewGuid();
        private readonly Guid _area = Guid.NewGuid();
        private readonly Guid _category = Guid.NewGuid();

        public HabitRecorderTests()
        {
            using (var context = _database.CreateContext())
            {
                context.Database.Migrate();
            }

            var settings = new Mock<ISettingsCache>();
            settings.Setup(a => a.GetByKey(HabitRecorder.EnabledKey)).Returns(() => _enabled);
            settings.Setup(a => a.GetByKey(HabitRecorder.LogDaysKey)).Returns(() => _logDays);

            _nodes.Add(new NodeInstance { ObjId = _light, This2AreaInstance = _area, This2CategoryInstance = _category });
            var nodes = new Mock<INodeInstanceCache>();
            nodes.Setup(a => a.Get(It.IsAny<Guid>())).Returns<Guid>(id => _nodes.FirstOrDefault(a => a.ObjId == id));
            nodes.Setup(a => a.All()).Returns(() => Root());

            var modes = new Mock<IOperatingModeService>();
            modes.SetupGet(a => a.ActiveModes).Returns(new[] { "workday", "school" });

            _dispatcher.Setup(a => a.RegisterDispatch(DispatchableType.NodeInstance, It.IsAny<Guid>(), It.IsAny<Action<IDispatchable, DispatchValue>>()))
                .Callback<DispatchableType, Guid, Action<IDispatchable, DispatchValue>>((_, id, _) => _registered.Add(id))
                .Returns(Task.CompletedTask);
            _dispatcher.Setup(a => a.UnRegisterDispatch(DispatchableType.NodeInstance, It.IsAny<Guid>(), It.IsAny<Action<IDispatchable, DispatchValue>>()))
                .Callback<DispatchableType, Guid, Action<IDispatchable, DispatchValue>>((_, id, _) => _registered.Remove(id))
                .Returns(Task.CompletedTask);

            var factory = _database.Services().GetRequiredService<IDbContextFactory<AutomaticaContext>>();
            _recorder = new HabitRecorder(factory, settings.Object, nodes.Object, Mock.Of<ILogicInterfaceInstanceCache>(), Mock.Of<ILogicInstanceCache>(),
                modes.Object, _dispatcher.Object, NullLogger<HabitRecorder>.Instance)
            {
                Time = _time
            };
        }

        // like the real cache: All() only holds the root, the data points are its children
        private NodeInstance[] Root() => new[] { new NodeInstance { ObjId = _root, InverseThis2ParentNodeInstanceNavigation = _nodes } };

        private List<HabitAction> Recorded()
        {
            using var context = _database.CreateContext();
            return context.HabitActions.OrderBy(a => a.UtcTime).ToList();
        }

        private static DispatchValue Value(Guid id, object value, DispatchValueSource source) =>
            new(id, DispatchableType.NodeInstance, value, DateTime.Now, source);

        [Fact]
        public async Task Disabled_RecordsNothing()
        {
            _enabled.ValueInt = 0;

            _recorder.RecordVisu(_light, true, Guid.NewGuid());
            await _recorder.FlushAsync(all: true);

            Assert.Empty(Recorded());
        }

        [Fact]
        public async Task WritesWithinTheWindow_AreOneActionWithTheLastValue()
        {
            var user = Guid.NewGuid();
            _recorder.RecordVisu(_light, 10.0, user);
            _time.Advance(TimeSpan.FromSeconds(3));
            _recorder.RecordVisu(_light, 40.0, user);
            _time.Advance(TimeSpan.FromSeconds(5));
            _recorder.RecordVisu(_light, 75.5, user);

            // still within 10 s of the first write: nothing is due yet
            await _recorder.FlushAsync();
            Assert.Empty(Recorded());

            _time.Advance(TimeSpan.FromSeconds(2));
            await _recorder.FlushAsync();

            var action = Assert.Single(Recorded());
            Assert.Equal("75.5", action.Value);
            Assert.Equal(new DateTime(2026, 11, 12, 18, 0, 0), action.UtcTime);
            Assert.Equal(_light, action.TargetId);
            Assert.Equal(_area, action.This2AreaInstance);
            Assert.Equal(_category, action.This2CategoryInstance);
            Assert.Equal(user, action.UserId);
            Assert.Equal(HabitActionOrigin.Visualization, action.Origin);
            Assert.Equal("workday,school", action.OperatingModes);
        }

        [Fact]
        public async Task ContinuousChanges_AreCutIntoOneActionPerWindow()
        {
            // a sensor changing every 2 s: the window counts from the first value, so it is written every 10 s
            for (var i = 0; i < 10; i++)
            {
                _recorder.RecordVisu(_light, i, null);
                _time.Advance(TimeSpan.FromSeconds(2));
            }
            await _recorder.FlushAsync(all: true);

            Assert.Equal(new[] { "4", "9" }, Recorded().Select(a => a.Value));
        }

        [Fact]
        public async Task AllDataPoints_AreSubscribedWhileEnabled_AndOnlyReportedChangesCount()
        {
            var button = Guid.NewGuid();
            _nodes.Add(new NodeInstance { ObjId = button });
            _nodes.Add(new NodeInstance { ObjId = Guid.NewGuid(), IsDeleted = true });

            await _recorder.ScanDataPointsAsync();
            Assert.Equal(new[] { _root, _light, button }.Order(), _registered.Order());

            // writes of logics and automations are not reported values
            await _recorder.OnDataPointValue(null, Value(button, true, DispatchValueSource.Write));
            await _recorder.OnDataPointValue(null, Value(button, true, DispatchValueSource.User));
            await _recorder.FlushAsync(all: true);
            Assert.Empty(Recorded());

            await _recorder.OnDataPointValue(null, Value(button, true, DispatchValueSource.Read));
            await _recorder.FlushAsync(all: true);
            var action = Assert.Single(Recorded());
            Assert.Equal(HabitActionOrigin.DataPoint, action.Origin);
            Assert.Null(action.UserId);

            // a polling driver reporting the same value again is no change
            _time.Advance(TimeSpan.FromMinutes(1));
            await _recorder.OnDataPointValue(null, Value(button, true, DispatchValueSource.Read));
            await _recorder.OnDataPointValue(null, Value(button, false, DispatchValueSource.Read));
            await _recorder.FlushAsync(all: true);
            Assert.Equal(new[] { "True", "False" }, Recorded().Select(a => a.Value));

            _enabled.ValueInt = 0;
            await _recorder.ScanDataPointsAsync();
            Assert.Empty(_registered);
        }

        [Fact]
        public async Task ValuesThroughTheRealDispatcher_AreRecorded()
        {
            var dispatcher = new Base.IO.Dispatcher(NullLogger<Base.IO.Dispatcher>.Instance, Mock.Of<IDataBroadcast>(), Mock.Of<IRemoteSender>(),
                Mock.Of<IRemanentHandler>(), Mock.Of<IRuleInstanceVisuNotify>());
            var factory = _database.Services().GetRequiredService<IDbContextFactory<AutomaticaContext>>();
            var settings = new Mock<ISettingsCache>();
            settings.Setup(a => a.GetByKey(HabitRecorder.EnabledKey)).Returns(() => _enabled);
            var nodes = new Mock<INodeInstanceCache>();
            nodes.Setup(a => a.Get(It.IsAny<Guid>())).Returns<Guid>(id => _nodes.FirstOrDefault(a => a.ObjId == id));
            nodes.Setup(a => a.All()).Returns(() => Root());
            var recorder = new HabitRecorder(factory, settings.Object, nodes.Object, Mock.Of<ILogicInterfaceInstanceCache>(), Mock.Of<ILogicInstanceCache>(),
                Mock.Of<IOperatingModeService>(a => a.ActiveModes == Array.Empty<string>()), dispatcher, NullLogger<HabitRecorder>.Instance) { Time = _time };

            await recorder.ScanDataPointsAsync();
            var light = new Mock<IDispatchable>();
            light.SetupGet(a => a.Id).Returns(_light);
            light.SetupGet(a => a.Type).Returns(DispatchableType.NodeInstance);
            light.SetupGet(a => a.Name).Returns("light");
            await dispatcher.DispatchValue(light.Object, true, DispatchValueSource.Read);

            for (var i = 0; i < 50 && Recorded().Count == 0; i++)
            {
                await Task.Delay(20);
                await recorder.FlushAsync(all: true);
            }
            Assert.Equal("True", Assert.Single(Recorded()).Value);
        }

        [Fact]
        public async Task Cleanup_DeletesOldActions_AndEverythingWhenDisabled()
        {
            // several days old: cleaned up a day per statement
            foreach (var value in new[] { "a", "b", "c" })
            {
                _recorder.RecordVisu(_light, value, null);
                await _recorder.FlushAsync(all: true);
                _time.Advance(TimeSpan.FromDays(2));
            }
            _time.Advance(TimeSpan.FromDays(31));
            _recorder.RecordVisu(_light, "new", null);
            await _recorder.FlushAsync(all: true);

            await _recorder.CleanupAsync();
            Assert.Equal("new", Assert.Single(Recorded()).Value);

            _enabled.ValueInt = 0;
            await _recorder.CleanupAsync();
            Assert.Empty(Recorded());
        }

        public void Dispose() => _database.Dispose();
    }
}
