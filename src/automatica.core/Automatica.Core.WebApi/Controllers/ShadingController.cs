using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Control.Base;
using Automatica.Core.Control.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Shading;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Model.Models.User;
using Automatica.Core.Shading.Definition;
using Automatica.Core.Shading.Engine;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace Automatica.Core.WebApi.Controllers
{
    public class ShadingDefinitionDto
    {
        public JToken Definition { get; set; }
    }

    public class ShadingFacadeDto
    {
        public Guid ObjId { get; set; }
        public string Name { get; set; }
        public double Azimuth { get; set; }
        public bool IsEnabled { get; set; } = true;
        public JToken Definition { get; set; }
    }

    public class ShadingBlindDto
    {
        public Guid ControlId { get; set; }
        public string Name { get; set; }
        public Guid? This2ShadingFacade { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool HasSlats { get; set; }
        public bool IsLoaded { get; set; }

        /// <summary>
        /// Read only: the automatic is paused (visualization or automation)
        /// </summary>
        public bool IsPaused { get; set; }

        public JToken Definition { get; set; }
    }

    public class ShadingAvailableBlindDto
    {
        public Guid ControlId { get; set; }
        public string Name { get; set; }
        public bool HasSlats { get; set; }
        public bool IsConfigured { get; set; }
    }

    public class ShadingAutomaticDto
    {
        public bool Enabled { get; set; }
    }

    /// <summary>
    /// Automatic shading of roller and venetian blinds, see Automatica.Core.Shading/README.md. Admins only.
    /// </summary>
    [Route("webapi/shading")]
    [Authorize(Policy = Role.AdminRole)]
    public class ShadingController : BaseController
    {
        private readonly IShadingEngine _engine;
        private readonly IControlCache _controlCache;
        private readonly ISettingsCache _settingsCache;

        public ShadingController(AutomaticaContext dbContext, IShadingEngine engine, IControlCache controlCache, ISettingsCache settingsCache) : base(dbContext)
        {
            _engine = engine;
            _controlCache = controlCache;
            _settingsCache = settingsCache;
        }

        #region settings

        [HttpGet("settings")]
        public async Task<ShadingDefinitionDto> GetSettings(CancellationToken token)
        {
            var row = await DbContext.ShadingSettings.AsNoTracking().SingleOrDefaultAsync(token);
            return new ShadingDefinitionDto { Definition = ToJToken(ShadingJson.Parse<ShadingSettingsDefinition>(row?.Definition)) };
        }

        [HttpPut("settings")]
        public async Task<ActionResult<ShadingDefinitionDto>> SaveSettings([FromBody] ShadingDefinitionDto dto, CancellationToken token)
        {
            if (!TryParse<ShadingSettingsDefinition>(dto?.Definition, out var settings, out var error))
            {
                return BadRequest(error);
            }

            var problems = ShadingValidator.Validate(settings);
            if (problems.Count > 0)
            {
                return BadRequest(ToJToken(problems));
            }

            var row = await DbContext.ShadingSettings.SingleOrDefaultAsync(token);
            if (row == null)
            {
                row = new ShadingSettings { ObjId = ShadingSettings.SingletonId };
                DbContext.ShadingSettings.Add(row);
            }

            row.Definition = ShadingJson.Serialize(settings);
            row.ModifiedAt = DateTime.UtcNow;
            await DbContext.SaveChangesAsync(token);
            await _engine.ReloadAsync(token);

            return new ShadingDefinitionDto { Definition = ToJToken(settings) };
        }

        #endregion

        #region facades

        [HttpGet("facades")]
        public async Task<IEnumerable<ShadingFacadeDto>> GetFacades(CancellationToken token)
        {
            var facades = await DbContext.ShadingFacades.AsNoTracking().OrderBy(a => a.Azimuth).ToListAsync(token);
            return facades.Select(ToDto).ToList();
        }

        [HttpPost("facades")]
        public Task<ActionResult<ShadingFacadeDto>> CreateFacade([FromBody] ShadingFacadeDto dto, CancellationToken token)
        {
            return SaveFacade(null, dto, token);
        }

        [HttpPut("facades/{id:guid}")]
        public Task<ActionResult<ShadingFacadeDto>> UpdateFacade(Guid id, [FromBody] ShadingFacadeDto dto, CancellationToken token)
        {
            return SaveFacade(id, dto, token);
        }

        private async Task<ActionResult<ShadingFacadeDto>> SaveFacade(Guid? id, ShadingFacadeDto dto, CancellationToken token)
        {
            if (dto == null)
            {
                return BadRequest("No facade");
            }

            if (!TryParse<FacadeDefinition>(dto.Definition, out var definition, out var error))
            {
                return BadRequest(error);
            }

            var problems = ShadingValidator.Validate(dto.Name, dto.Azimuth, definition);
            if (problems.Count > 0)
            {
                return BadRequest(ToJToken(problems));
            }

            ShadingFacade entity;
            var now = DateTime.UtcNow;
            if (id.HasValue)
            {
                entity = await DbContext.ShadingFacades.SingleOrDefaultAsync(a => a.ObjId == id.Value, token);
                if (entity == null)
                {
                    return NotFound();
                }
            }
            else
            {
                entity = new ShadingFacade { ObjId = Guid.NewGuid(), CreatedAt = now };
                DbContext.ShadingFacades.Add(entity);
            }

            entity.Name = dto.Name.Trim();
            entity.Azimuth = dto.Azimuth;
            entity.IsEnabled = dto.IsEnabled;
            entity.Definition = ShadingJson.Serialize(definition);
            entity.ModifiedAt = now;

            await DbContext.SaveChangesAsync(token);
            await _engine.ReloadAsync(token);
            return ToDto(entity);
        }

        [HttpDelete("facades/{id:guid}")]
        public async Task<IActionResult> DeleteFacade(Guid id, CancellationToken token)
        {
            var entity = await DbContext.ShadingFacades.SingleOrDefaultAsync(a => a.ObjId == id, token);
            if (entity == null)
            {
                return NotFound();
            }

            // the blinds keep their settings, they lose the facade (SetNull)
            foreach (var blind in await DbContext.ShadingBlinds.Where(a => a.This2ShadingFacade == id).ToListAsync(token))
            {
                blind.This2ShadingFacade = null;
            }

            DbContext.ShadingFacades.Remove(entity);
            await DbContext.SaveChangesAsync(token);
            await _engine.ReloadAsync(token);
            return Ok();
        }

        private static ShadingFacadeDto ToDto(ShadingFacade entity)
        {
            return new ShadingFacadeDto
            {
                ObjId = entity.ObjId,
                Name = entity.Name,
                Azimuth = entity.Azimuth,
                IsEnabled = entity.IsEnabled,
                Definition = ToJToken(ShadingJson.Parse<FacadeDefinition>(entity.Definition))
            };
        }

        #endregion

        #region blinds

        /// <summary>
        /// All blind controls of the server and whether they are configured for shading
        /// </summary>
        [HttpGet("blinds/available")]
        public async Task<IEnumerable<ShadingAvailableBlindDto>> GetAvailableBlinds(CancellationToken token)
        {
            var configured = (await DbContext.ShadingBlinds.AsNoTracking().Select(a => a.This2RuleInstance).ToListAsync(token)).ToHashSet();
            return _controlCache.All().OfType<IBlind>()
                .Select(a => new ShadingAvailableBlindDto
                {
                    ControlId = a.Id,
                    Name = a.Name,
                    HasSlats = a is ISlatBlind { HasSlats: true },
                    IsConfigured = configured.Contains(a.Id)
                })
                .OrderBy(a => a.Name)
                .ToList();
        }

        [HttpGet("blinds")]
        public async Task<IEnumerable<ShadingBlindDto>> GetBlinds(CancellationToken token)
        {
            var blinds = await DbContext.ShadingBlinds.AsNoTracking().Include(a => a.This2RuleInstanceNavigation).ToListAsync(token);
            return blinds.Select(ToDto).OrderBy(a => a.Name).ToList();
        }

        [HttpPut("blinds/{controlId:guid}")]
        public async Task<ActionResult<ShadingBlindDto>> SaveBlind(Guid controlId, [FromBody] ShadingBlindDto dto, CancellationToken token)
        {
            if (dto == null)
            {
                return BadRequest("No blind");
            }

            if (!TryParse<BlindDefinition>(dto.Definition, out var definition, out var error))
            {
                return BadRequest(error);
            }

            var problems = ShadingValidator.Validate(definition).ToList();

            var rule = await DbContext.RuleInstances.AsNoTracking().SingleOrDefaultAsync(a => a.ObjId == controlId, token);
            if (rule == null)
            {
                return NotFound();
            }

            if (_controlCache.Get(controlId) is { } control && control is not IBlind)
            {
                problems.Add(new ShadingProblem("controlId", "NOT_A_BLIND"));
            }

            if (dto.This2ShadingFacade.HasValue && !await DbContext.ShadingFacades.AnyAsync(a => a.ObjId == dto.This2ShadingFacade.Value, token))
            {
                problems.Add(new ShadingProblem("facade", "NOT_FOUND"));
            }

            if (!dto.This2ShadingFacade.HasValue && !definition.Azimuth.HasValue)
            {
                problems.Add(new ShadingProblem("azimuth", "REQUIRED", "a blind needs a facade or its own direction"));
            }

            if (problems.Count > 0)
            {
                return BadRequest(ToJToken(problems));
            }

            var now = DateTime.UtcNow;
            var entity = await DbContext.ShadingBlinds.SingleOrDefaultAsync(a => a.This2RuleInstance == controlId, token);
            if (entity == null)
            {
                entity = new ShadingBlind { ObjId = Guid.NewGuid(), This2RuleInstance = controlId, CreatedAt = now };
                DbContext.ShadingBlinds.Add(entity);
            }

            entity.This2ShadingFacade = dto.This2ShadingFacade;
            entity.IsEnabled = dto.IsEnabled;
            entity.Definition = ShadingJson.Serialize(definition);
            entity.ModifiedAt = now;

            await DbContext.SaveChangesAsync(token);
            await _engine.ReloadAsync(token);

            entity.This2RuleInstanceNavigation = rule;
            return ToDto(entity);
        }

        [HttpDelete("blinds/{controlId:guid}")]
        public async Task<IActionResult> DeleteBlind(Guid controlId, CancellationToken token)
        {
            var entity = await DbContext.ShadingBlinds.SingleOrDefaultAsync(a => a.This2RuleInstance == controlId, token);
            if (entity == null)
            {
                return NotFound();
            }

            DbContext.ShadingBlinds.Remove(entity);
            await DbContext.SaveChangesAsync(token);
            await _engine.ReloadAsync(token);
            return Ok();
        }

        /// <summary>
        /// Pauses (false) or resumes (true) the automatic of a blind (saved, survives a restart)
        /// </summary>
        [HttpPost("blinds/{controlId:guid}/automatic")]
        public async Task<IActionResult> SetAutomatic(Guid controlId, [FromBody] ShadingAutomaticDto dto, CancellationToken token)
        {
            return await _engine.SetAutomaticAsync(controlId, dto?.Enabled ?? true, token) ? Ok() : NotFound();
        }

        /// <summary>
        /// Ends a manual override
        /// </summary>
        [HttpPost("blinds/{controlId:guid}/resume")]
        public async Task<IActionResult> Resume(Guid controlId, CancellationToken token)
        {
            return await _engine.ResumeAsync(controlId, token) ? Ok() : NotFound();
        }

        private ShadingBlindDto ToDto(ShadingBlind entity)
        {
            var blind = _controlCache.Get(entity.This2RuleInstance) as IBlind;
            return new ShadingBlindDto
            {
                ControlId = entity.This2RuleInstance,
                Name = blind?.Name ?? entity.This2RuleInstanceNavigation?.Name ?? entity.This2RuleInstance.ToString(),
                This2ShadingFacade = entity.This2ShadingFacade,
                IsEnabled = entity.IsEnabled,
                HasSlats = blind is ISlatBlind { HasSlats: true },
                IsLoaded = blind != null,
                IsPaused = entity.IsPaused,
                Definition = ToJToken(ShadingJson.Parse<BlindDefinition>(entity.Definition))
            };
        }

        #endregion

        #region status

        [HttpGet("status")]
        public JToken GetStatus()
        {
            return ToJToken(_engine.GetStatus());
        }

        /// <summary>
        /// What the blinds would do at <paramref name="at"/> (ISO 8601), with the calculated sun position and the current sensor values
        /// </summary>
        [HttpGet("preview")]
        public ActionResult<JToken> Preview([FromQuery] string at)
        {
            var time = DateTimeOffset.UtcNow;
            if (!string.IsNullOrEmpty(at) && !DateTimeOffset.TryParse(at, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out time))
            {
                return BadRequest("Invalid time");
            }

            return ToJToken(_engine.Preview(time));
        }

        /// <summary>
        /// Sun positions of a day (every <paramref name="stepMinutes"/> minutes while the sun is up), for the compass
        /// </summary>
        [HttpGet("sun-path")]
        public ActionResult<JToken> SunPath([FromQuery] string date, [FromQuery] int stepMinutes = 15)
        {
            var latitude = _settingsCache.GetByKey("Latitude")?.ValueDouble ?? 0;
            var longitude = _settingsCache.GetByKey("Longitude")?.ValueDouble ?? 0;
            if (latitude == 0 && longitude == 0)
            {
                return BadRequest("No location in the settings");
            }

            var day = DateOnly.FromDateTime(DateTime.Now);
            if (!string.IsNullOrEmpty(date) && !DateOnly.TryParse(date, CultureInfo.InvariantCulture, out day))
            {
                return BadRequest("Invalid date");
            }

            stepMinutes = Math.Clamp(stepMinutes, 5, 120);
            var zone = TimeZoneInfo.Local;
            var points = new List<object>();
            for (var minute = 0; minute < 24 * 60; minute += stepMinutes)
            {
                var local = day.ToDateTime(TimeOnly.MinValue).AddMinutes(minute);
                var at = new DateTimeOffset(local, zone.GetUtcOffset(local));
                var sun = SunGeometry.Calculate(latitude, longitude, at);
                if (sun.Elevation > 0)
                {
                    points.Add(new { at, sun.Azimuth, sun.Elevation });
                }
            }

            return ToJToken(points);
        }

        #endregion

        private static bool TryParse<T>(JToken token, out T value, out string error) where T : class, new()
        {
            try
            {
                value = ShadingJson.Parse<T>(token == null || token.Type == JTokenType.Null ? null : token.ToString(Newtonsoft.Json.Formatting.None));
                error = null;
                return true;
            }
            catch (ShadingDefinitionException e)
            {
                value = null;
                error = e.Message;
                return false;
            }
        }

        internal static JToken ToJToken<T>(T value)
        {
            // keep timestamps as strings (with their offset)
            using var reader = new Newtonsoft.Json.JsonTextReader(new System.IO.StringReader(ShadingJson.Serialize(value)))
            {
                DateParseHandling = Newtonsoft.Json.DateParseHandling.None
            };
            return JToken.Load(reader);
        }
    }
}
