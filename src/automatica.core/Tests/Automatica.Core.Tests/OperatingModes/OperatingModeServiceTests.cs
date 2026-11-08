using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.OperatingModes;
using Automatica.Core.Tests.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace Automatica.Core.Tests.OperatingModes
{
    public sealed class OperatingModeServiceTests : IDisposable
    {
        private readonly SqliteTestDatabase _database = new();
        private readonly Mock<IDispatcher> _dispatcher = new();
        private readonly ConcurrentQueue<string> _published = new();
        private Action<IDispatchable, DispatchValue> _requestCallback;
        private readonly OperatingModeService _service;

        public OperatingModeServiceTests()
        {
            using (var context = _database.CreateContext())
            {
                context.Database.Migrate();
            }

            _dispatcher.Setup(a => a.RegisterDispatch(DispatchableType.Control, OperatingModeContract.RequestId, It.IsAny<Action<IDispatchable, DispatchValue>>()))
                .Callback<DispatchableType, Guid, Action<IDispatchable, DispatchValue>>((_, _, callback) => _requestCallback = callback)
                .Returns(Task.CompletedTask);
            _dispatcher.Setup(a => a.DispatchValue(It.IsAny<IDispatchable>(), It.IsAny<object>(), It.IsAny<DispatchValueSource>()))
                .Callback<IDispatchable, object, DispatchValueSource>((source, value, _) =>
                {
                    Assert.Equal(OperatingModeContract.StateId, source.Id);
                    Assert.Equal(DispatchableType.Control, source.Type);
                    _published.Enqueue((string)value);
                })
                .Returns(Task.CompletedTask);
            _dispatcher.Setup(a => a.GetValue(DispatchableType.Control, OperatingModeContract.StateId))
                .Returns(() => _published.IsEmpty ? null : new DispatchValue(OperatingModeContract.StateId, DispatchableType.Control, _published.Last(), DateTime.Now, DispatchValueSource.Read));

            var settings = new Mock<ISettingsCache>();
            settings.Setup(a => a.GetByKey("Latitude")).Returns(new Setting { ValueKey = "Latitude", ValueDouble = 48.3 });
            settings.Setup(a => a.GetByKey("Longitude")).Returns(new Setting { ValueKey = "Longitude", ValueDouble = 14.3 });

            var factory = _database.Services().GetRequiredService<IDbContextFactory<AutomaticaContext>>();
            _service = new OperatingModeService(factory, _dispatcher.Object, settings.Object, NullLogger<OperatingModeService>.Instance)
            {
                Time = new FakeTimeProvider(new DateTimeOffset(2026, 12, 24, 10, 0, 0, TimeSpan.Zero)) { }
            };
        }

        private JsonElement LastState() => JsonDocument.Parse(_published.Last()).RootElement;

        [Fact]
        public async Task Start_WithoutSetting_PublishesTheDefaults()
        {
            await _service.StartAsync();

            Assert.Single(_published);
            var state = LastState();
            Assert.Equal("2026-12-24", state.GetProperty("today").GetString());
            Assert.Equal(48.3, state.GetProperty("latitude").GetDouble());
            Assert.Equal(3, state.GetProperty("modes").GetArrayLength());
            Assert.NotNull(_requestCallback);
        }

        [Fact]
        public async Task Save_StoresTheHiddenSetting_AndPublishes()
        {
            await _service.StartAsync();
            var configuration = OperatingModeConfiguration.CreateDefault();
            configuration.Country = "AT";
            configuration.Periods.Add(new OperatingPeriod { Mode = OperatingModeContract.Vacation, Start = new DateOnly(2026, 12, 20), End = new DateOnly(2027, 1, 6) });

            await _service.SaveAsync(configuration);

            Assert.Equal(2, _published.Count);
            Assert.Equal(OperatingModeContract.Vacation, LastState().GetProperty("active")[0].GetString());
            Assert.True(_service.IsActive("VACATION"));

            await using var context = _database.CreateContext();
            var setting = await context.Settings.SingleAsync(a => a.ValueKey == OperatingModeConfiguration.SettingKey);
            Assert.False(setting.IsVisible);
            Assert.Contains("\"country\":\"AT\"", setting.ValueText);

            // a second service (restart) loads it again
            var restarted = new OperatingModeService(_database.Services().GetRequiredService<IDbContextFactory<AutomaticaContext>>(), _dispatcher.Object, new Mock<ISettingsCache>().Object, NullLogger<OperatingModeService>.Instance);
            await restarted.StartAsync();
            Assert.Equal("AT", restarted.Configuration.Country);
            Assert.Single(restarted.Configuration.Periods);
            await restarted.StopAsync();
        }

        [Fact]
        public async Task Publish_OnlyWhenChanged_OrWhenTheDispatcherLostTheValue()
        {
            await _service.StartAsync();
            _service.Publish(force: false);
            Assert.Single(_published);

            _published.Clear();
            _service.Publish(force: false);
            Assert.Single(_published);
        }

        [Fact]
        public async Task Request_FromALogic_SwitchesTheModeManually()
        {
            await _service.StartAsync();

            await _service.HandleRequestAsync("{\"mode\":\"vacation\",\"state\":true}");
            Assert.True(_service.IsActive(OperatingModeContract.Vacation));
            Assert.Equal(OperatingModeContract.Vacation, LastState().GetProperty("active")[0].GetString());

            await _service.HandleRequestAsync("{\"mode\":\"vacation\",\"state\":null}");
            Assert.False(_service.IsActive(OperatingModeContract.Vacation));
            Assert.Null(_service.Configuration.Modes.Single(a => a.Key == OperatingModeContract.Vacation).Manual);

            // broken and unknown requests are ignored
            var count = _published.Count;
            await _service.HandleRequestAsync("{\"mode\":\"nope\",\"state\":true}");
            await _service.HandleRequestAsync("not json");
            await _service.HandleRequestAsync(null);
            Assert.Equal(count, _published.Count);
        }

        [Fact]
        public async Task SetManual_UnknownMode_IsFalse()
        {
            await _service.StartAsync();
            Assert.False(await _service.SetManualAsync("nope", true));
            Assert.True(await _service.SetManualAsync(OperatingModeContract.PublicHoliday, false));
            Assert.False(_service.Configuration.Modes.Single(a => a.Key == OperatingModeContract.PublicHoliday).Manual);
        }

        [Fact]
        public void BeforeStart_NothingIsPublished()
        {
            _service.Publish(force: true);
            Assert.Empty(_published);
            Assert.False(_service.IsActive(OperatingModeContract.Vacation));
        }

        public void Dispose()
        {
            _service.StopAsync().GetAwaiter().GetResult();
            _service.Dispose();
            _database.Dispose();
        }
    }
}
