using Automatica.Core.Base.Calendar;
using Automatica.Core.Base.IO;
using Automatica.Core.Control.Base;
using Automatica.Core.Control.Cache;
using Automatica.Core.Internals.Cache.Common;

namespace Automatica.Core.Shading.Engine
{
    /// <summary>
    /// The shading engine's view of the running core: values from the dispatcher, blinds from the control cache,
    /// the location from the server settings.
    /// </summary>
    internal sealed class CoreShadingEnvironment : IShadingEnvironment
    {
        private readonly IDispatcher _dispatcher;
        private readonly IControlCache _controlCache;
        private readonly ISettingsCache _settingsCache;

        public CoreShadingEnvironment(IDispatcher dispatcher, IControlCache controlCache, ISettingsCache settingsCache)
        {
            _dispatcher = dispatcher;
            _controlCache = controlCache;
            _settingsCache = settingsCache;
        }

        public TimeProvider Time => DateTimeHelper.ProviderInstance;

        public (double Latitude, double Longitude)? Location
        {
            get
            {
                var latitude = _settingsCache.GetByKey("Latitude")?.ValueDouble ?? 0;
                var longitude = _settingsCache.GetByKey("Longitude")?.ValueDouble ?? 0;
                return latitude == 0 && longitude == 0 ? null : (latitude, longitude);
            }
        }

        public ShadingValue GetValue(Guid id)
        {
            var value = _dispatcher.GetValue(id);
            if (value == null)
            {
                return null;
            }

            DateTimeOffset? timestamp = value.Timestamp == default
                ? null
                : value.Timestamp.Kind == DateTimeKind.Utc
                    ? new DateTimeOffset(value.Timestamp)
                    : new DateTimeOffset(value.Timestamp, Time.LocalTimeZone.GetUtcOffset(value.Timestamp));
            return new ShadingValue(value.Value, timestamp);
        }

        public async Task<IDisposable> SubscribeAsync(Guid id, Func<object, Task> callback)
        {
            Task OnValue(IDispatchable _, DispatchValue value)
            {
                return value.ValueSource == DispatchValueSource.Read ? callback(value.Value) : Task.CompletedTask;
            }

            var node = await _dispatcher.Subscribe(DispatchableType.NodeInstance, id, OnValue);
            var logic = await _dispatcher.Subscribe(DispatchableType.RuleInstance, id, OnValue);
            return new CompositeDisposable(node, logic);
        }

        public IBlind GetBlind(Guid controlId)
        {
            return _controlCache.Get(controlId) as IBlind;
        }

        private sealed class CompositeDisposable(params IDisposable[] items) : IDisposable
        {
            public void Dispose()
            {
                foreach (var item in items)
                {
                    item?.Dispose();
                }
            }
        }
    }
}
