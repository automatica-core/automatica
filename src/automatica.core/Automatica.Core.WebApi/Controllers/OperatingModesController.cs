using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.OperatingModes;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Automatica.Core.WebApi.Controllers
{
    /// <summary>
    /// Operating modes and operating times (Loxone style): configuration for the administrators.
    /// The visualization reads the state and switches modes through <see cref="OperatingModesVisuController"/>.
    /// </summary>
    [Route("webapi/operating-modes")]
    [Authorize(Policy = Role.AdminRole)]
    public class OperatingModesController : BaseController
    {
        private readonly IOperatingModeService _service;

        public OperatingModesController(AutomaticaContext dbContext, IOperatingModeService service) : base(dbContext)
        {
            _service = service;
        }

        /// <summary>
        /// Configuration, the active modes of the next days (with the reason), the supported holiday countries
        /// </summary>
        [HttpGet]
        public JToken Get([FromQuery] int days = 31)
        {
            return ToJToken(Overview(_service, Math.Clamp(days, 1, 400)));
        }

        [HttpPut]
        public async Task<JToken> Save([FromBody] JToken configuration, CancellationToken token)
        {
            var parsed = OperatingModeConfiguration.Parse(configuration?.ToString(Newtonsoft.Json.Formatting.None));
            await _service.SaveAsync(parsed, token);
            return ToJToken(Overview(_service, 31));
        }

        /// <summary>
        /// The holidays of a country/region and year, for the preview in the settings page
        /// </summary>
        [HttpGet("holidays")]
        public JToken Holidays([FromQuery] string country, [FromQuery] string region, [FromQuery] int? year)
        {
            var y = year ?? _service.Today.Year;
            return ToJToken(HolidayCalendar.For((country ?? "").ToUpperInvariant(), (region ?? "").ToUpperInvariant(), y)
                .Select(a => new { key = a.Key, date = a.Date }).ToList());
        }

        internal static object Overview(IOperatingModeService service, int days)
        {
            var configuration = service.Configuration;
            var today = service.Today;
            return new
            {
                configuration,
                today,
                active = service.ActiveModes,
                days = service.Days(today, days).Select(a => new { date = a.Date, modes = a.Modes, reasons = a.Reasons }),
                holidays = HolidayCalendar.For(configuration.Country, configuration.Region, today.Year)
                    .Concat(HolidayCalendar.For(configuration.Country, configuration.Region, today.Year + 1))
                    .Select(a => new
                    {
                        key = a.Key,
                        date = a.Date,
                        enabled = !configuration.DisabledHolidays.Contains(a.Key, StringComparer.OrdinalIgnoreCase)
                    }),
                countries = HolidayCalendar.SupportedCountries
            };
        }

        internal static JToken ToJToken(object value)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(value, OperatingModeConfiguration.JsonOptions);
            using var reader = new Newtonsoft.Json.JsonTextReader(new System.IO.StringReader(json))
            {
                DateParseHandling = Newtonsoft.Json.DateParseHandling.None
            };
            return JToken.Load(reader);
        }
    }

    /// <summary>
    /// Operating modes for the visualization: reading for every signed-in role, switching a mode manually on/off for
    /// visualization users and admins (like the vacation switch in the Loxone app).
    /// </summary>
    [Route("webapi/operating-modes-visu")]
    [Authorize(Policy = Role.ViewerRole)]
    public class OperatingModesVisuController : BaseController
    {
        private readonly IOperatingModeService _service;

        public OperatingModesVisuController(AutomaticaContext dbContext, IOperatingModeService service) : base(dbContext)
        {
            _service = service;
        }

        /// <summary>
        /// The modes (key, name, priority, manual), the active ones today and the next 14 days
        /// </summary>
        [HttpGet]
        public JToken GetState()
        {
            var configuration = _service.Configuration;
            var today = _service.Today;
            return OperatingModesController.ToJToken(new
            {
                today,
                modes = configuration.Modes.OrderByDescending(a => a.Priority).Select(a => new { a.Key, a.Name, a.Priority, a.BuiltIn, a.Manual }),
                active = _service.ActiveModes,
                days = _service.Days(today, OperatingModeContract.PublishedDays).Select(a => new { date = a.Date, modes = a.Modes })
            });
        }

        /// <summary>
        /// Switches a mode on (true), off (false) or back to the operating times (null)
        /// </summary>
        [HttpPost("{mode}/manual")]
        [Authorize(Roles = Role.VisuRole + "," + Role.AdminRole)]
        public async Task<IActionResult> SetManual(string mode, [FromBody] OperatingModeManualDto dto, CancellationToken token)
        {
            return await _service.SetManualAsync(mode, dto?.State, token) ? Ok() : NotFound();
        }
    }

    public class OperatingModeManualDto
    {
        public bool? State { get; set; }
    }
}
