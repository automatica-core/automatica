using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Newtonsoft.Json;
using P3.Logic.Time.OperatingModes;

namespace P3.Logic.Time.Tests.TestSupport
{
    /// <summary>
    /// Publishes an operating mode state like the core does (JSON on <see cref="DispatchableType.Control"/>)
    /// </summary>
    internal static class OperatingModeState
    {
        public static string Json(DateOnly today, IReadOnlyDictionary<DateOnly, string[]> days = null, double? latitude = 48.2082, double? longitude = 16.3738)
        {
            days ??= new Dictionary<DateOnly, string[]>();
            var map = new Dictionary<string, string[]>();
            for (var i = 0; i < 14; i++)
            {
                var day = today.AddDays(i);
                map[day.ToString("yyyy-MM-dd")] = days.TryGetValue(day, out var keys) ? keys : Array.Empty<string>();
            }

            return JsonConvert.SerializeObject(new
            {
                version = 1,
                latitude,
                longitude,
                modes = new[]
                {
                    new { key = OperatingModeContract.PublicHoliday, name = "Public holiday", priority = 300 },
                    new { key = OperatingModeContract.Vacation, name = "Vacation", priority = 200 },
                    new { key = OperatingModeContract.SchoolHolidays, name = "School holidays", priority = 100 }
                },
                active = days.TryGetValue(today, out var active) ? active : Array.Empty<string>(),
                days = map
            });
        }

        public static void Publish(RecordingDispatcher dispatcher, string json)
        {
            dispatcher.DispatchValue(new StateDispatchable(), json).GetAwaiter().GetResult();
        }

        public static IReadOnlyList<string> Requests(RecordingDispatcher dispatcher)
        {
            return dispatcher.ValuesOf(OperatingModeContract.RequestId).Select(a => a?.ToString()).ToList();
        }

        private sealed class StateDispatchable : IDispatchable
        {
            public DispatchableSource Source => DispatchableSource.NodeInstance;
            public DispatchableType Type => DispatchableType.Control;
            public string Name => "OperatingModes";
            public Guid Id => OperatingModeContract.StateId;
            public bool IsRemanent => false;
        }
    }

    /// <summary>
    /// A value written from the visualization (DataHub.SetValue)
    /// </summary>
    internal sealed class VisuDispatchable : IDispatchable
    {
        public VisuDispatchable(Guid id)
        {
            Id = id;
        }

        public DispatchableSource Source => DispatchableSource.Visualization;
        public DispatchableType Type => DispatchableType.Visualization;
        public string Name => "Web";
        public Guid Id { get; }
        public bool IsRemanent => false;
    }
}
