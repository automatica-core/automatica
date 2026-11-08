using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Media;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Media;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Automatica.Core.WebApi.Controllers
{
    /// <summary>
    /// The radio station catalog: every signed-in role reads it (station picker, media player tile), the administrators
    /// manage the custom stations
    /// </summary>
    [Route("webapi/radio-stations")]
    [Authorize(Policy = Role.ViewerRole)]
    public class RadioStationsController : BaseController
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IRadioStationService _service;

        public RadioStationsController(AutomaticaContext dbContext, IRadioStationService service) : base(dbContext)
        {
            _service = service;
        }

        /// <summary>
        /// All stations (built-in and custom), sorted by country and name
        /// </summary>
        [HttpGet]
        public JToken Get()
        {
            return ToJToken(_service.Stations);
        }

        [HttpGet("custom")]
        [Authorize(Policy = Role.AdminRole)]
        public JToken GetCustom()
        {
            return ToJToken(_service.CustomStations);
        }

        /// <summary>
        /// Replaces the custom stations; 400 with the reason for an invalid station
        /// </summary>
        [HttpPut("custom")]
        [Authorize(Policy = Role.AdminRole)]
        public async Task<IActionResult> SaveCustom([FromBody] JToken stations, CancellationToken token)
        {
            List<RadioStation> parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<List<RadioStation>>(stations?.ToString(Newtonsoft.Json.Formatting.None) ?? "[]", JsonOptions)
                         ?? new List<RadioStation>();
            }
            catch (JsonException)
            {
                return BadRequest(new { message = "The stations could not be read" });
            }

            try
            {
                var saved = await _service.SaveCustomStationsAsync(parsed, token);
                return Ok(ToJToken(saved));
            }
            catch (ArgumentException e)
            {
                return BadRequest(new { message = e.Message });
            }
        }

        private static JToken ToJToken(object value)
        {
            return JToken.Parse(JsonSerializer.Serialize(value, JsonOptions));
        }
    }
}
