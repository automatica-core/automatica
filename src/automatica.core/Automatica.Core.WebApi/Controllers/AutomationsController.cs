using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Automation.Definition;
using Automatica.Core.Automation.Engine;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Automations;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using AutomationEntity = Automatica.Core.EF.Models.Automations.Automation;

namespace Automatica.Core.WebApi.Controllers
{
    public class AutomationSaveDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string Mode { get; set; }
        public int MaxRuns { get; set; } = 10;
        public Guid? This2AreaInstance { get; set; }
        public Guid? This2CategoryInstance { get; set; }
        public JToken Definition { get; set; }
    }

    public class AutomationDefinitionDto
    {
        public JToken Definition { get; set; }
        public string TriggerId { get; set; }
    }

    public class AutomationEnabledDto
    {
        public bool Enabled { get; set; }
    }

    public class AutomationDto
    {
        public Guid ObjId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsEnabled { get; set; }
        public string Mode { get; set; }
        public int MaxRuns { get; set; }
        public Guid? This2AreaInstance { get; set; }
        public Guid? This2CategoryInstance { get; set; }
        public JToken Definition { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public DateTime? LastRunAt { get; set; }
        public string LastRunState { get; set; }
        public string LastError { get; set; }
        public int ActiveRuns { get; set; }
        public JToken Problems { get; set; }
    }

    public class AutomationRunDto
    {
        public Guid ObjId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string State { get; set; }
        public string TriggerId { get; set; }
        public string Error { get; set; }
        public JToken Steps { get; set; }
    }

    /// <summary>
    /// "If this then that" automations, see Automatica.Core.Automation/README.md. Admins only.
    /// </summary>
    [Route("webapi/automations")]
    [Authorize(Policy = Role.AdminRole)]
    public class AutomationsController : BaseController
    {
        private readonly IAutomationEngine _engine;
        private readonly ILogger<AutomationsController> _logger;

        public AutomationsController(AutomaticaContext dbContext, IAutomationEngine engine, ILogger<AutomationsController> logger) : base(dbContext)
        {
            _engine = engine;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IEnumerable<AutomationDto>> GetAll(CancellationToken token)
        {
            var automations = await DbContext.Automations.AsNoTracking().OrderBy(a => a.Name).ToListAsync(token);
            var result = new List<AutomationDto>();
            foreach (var automation in automations)
            {
                result.Add(await ToDto(automation, token));
            }
            return result;
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<AutomationDto>> Get(Guid id, CancellationToken token)
        {
            var automation = await DbContext.Automations.AsNoTracking().SingleOrDefaultAsync(a => a.ObjId == id, token);
            if (automation == null)
            {
                return NotFound();
            }
            return await ToDto(automation, token);
        }

        [HttpGet("where-used/{objectId:guid}")]
        public async Task<IEnumerable<AutomationDto>> WhereUsed(Guid objectId, CancellationToken token)
        {
            var ids = await DbContext.AutomationReferences.AsNoTracking()
                .Where(a => a.ReferenceId == objectId)
                .Select(a => a.This2Automation)
                .Distinct()
                .ToListAsync(token);

            var automations = await DbContext.Automations.AsNoTracking().Where(a => ids.Contains(a.ObjId)).ToListAsync(token);
            var result = new List<AutomationDto>();
            foreach (var automation in automations)
            {
                result.Add(await ToDto(automation, token));
            }
            return result;
        }

        [HttpPost]
        public Task<ActionResult<AutomationDto>> Create([FromBody] AutomationSaveDto dto, CancellationToken token)
        {
            return Save(null, dto, token);
        }

        [HttpPut("{id:guid}")]
        public Task<ActionResult<AutomationDto>> Update(Guid id, [FromBody] AutomationSaveDto dto, CancellationToken token)
        {
            return Save(id, dto, token);
        }

        private async Task<ActionResult<AutomationDto>> Save(Guid? id, AutomationSaveDto dto, CancellationToken token)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest("A name is required");
            }

            if (!TryParseMode(dto.Mode, out var mode))
            {
                return BadRequest($"Unknown mode {dto.Mode}");
            }

            AutomationDefinition definition;
            try
            {
                definition = AutomationJson.Parse(dto.Definition?.ToString(Newtonsoft.Json.Formatting.None));
            }
            catch (AutomationDefinitionException e)
            {
                return BadRequest(e.Message);
            }

            var problems = await _engine.ValidateAsync(definition, id, token);
            if (dto.IsEnabled && problems.Any(a => a.Severity == ValidationSeverity.Error))
            {
                return BadRequest(ToJToken(problems));
            }

            var now = DateTime.UtcNow;
            AutomationEntity entity;
            if (id.HasValue)
            {
                entity = await DbContext.Automations.Include(a => a.References).SingleOrDefaultAsync(a => a.ObjId == id.Value, token);
                if (entity == null)
                {
                    return NotFound();
                }
                DbContext.AutomationReferences.RemoveRange(entity.References);
                entity.References.Clear();
            }
            else
            {
                entity = new AutomationEntity
                {
                    ObjId = Guid.NewGuid(),
                    CreatedAt = now
                };
                DbContext.Automations.Add(entity);
            }

            entity.Name = dto.Name.Trim();
            entity.Description = dto.Description;
            entity.IsEnabled = dto.IsEnabled;
            entity.Mode = mode;
            entity.MaxRuns = Math.Clamp(dto.MaxRuns, 1, 50);
            entity.This2AreaInstance = dto.This2AreaInstance;
            entity.This2CategoryInstance = dto.This2CategoryInstance;
            entity.Definition = AutomationJson.Serialize(definition);
            entity.ModifiedAt = now;

            foreach (var (referenceId, type, role) in AutomationReferences.Collect(definition))
            {
                entity.References.Add(new AutomationReference
                {
                    ObjId = Guid.NewGuid(),
                    This2Automation = entity.ObjId,
                    ReferenceId = referenceId,
                    ReferenceType = type,
                    Role = role
                });
            }

            await DbContext.SaveChangesAsync(token);
            _logger.LogInformation($"Automation {entity.Name} ({entity.ObjId}) saved");

            await _engine.ReloadAsync(entity.ObjId, token);

            return await ToDto(entity, token, problems);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken token)
        {
            var entity = await DbContext.Automations.SingleOrDefaultAsync(a => a.ObjId == id, token);
            if (entity == null)
            {
                return NotFound();
            }

            await _engine.RemoveAsync(id);

            DbContext.Automations.Remove(entity);
            await DbContext.SaveChangesAsync(token);
            _logger.LogInformation($"Automation {entity.Name} ({entity.ObjId}) deleted");
            return Ok();
        }

        [HttpPost("{id:guid}/enabled")]
        public async Task<ActionResult<AutomationDto>> SetEnabled(Guid id, [FromBody] AutomationEnabledDto dto, CancellationToken token)
        {
            var entity = await DbContext.Automations.AsNoTracking().SingleOrDefaultAsync(a => a.ObjId == id, token);
            if (entity == null)
            {
                return NotFound();
            }

            if (dto.Enabled)
            {
                var problems = await _engine.ValidateAsync(AutomationJson.Parse(entity.Definition), id, token);
                if (problems.Any(a => a.Severity == ValidationSeverity.Error))
                {
                    return BadRequest(ToJToken(problems));
                }
            }

            await _engine.SetEnabledAsync(id, dto.Enabled, token);

            entity = await DbContext.Automations.AsNoTracking().SingleAsync(a => a.ObjId == id, token);
            return await ToDto(entity, token);
        }

        [HttpPost("{id:guid}/run")]
        public async Task<IActionResult> Run(Guid id, [FromQuery] bool skipConditions, CancellationToken token)
        {
            if (!await DbContext.Automations.AnyAsync(a => a.ObjId == id, token))
            {
                return NotFound();
            }

            var runId = await _engine.RunAsync(id, skipConditions, token);
            return Ok(new { runId });
        }

        [HttpPost("{id:guid}/runs/{runId:guid}/stop")]
        public IActionResult StopRun(Guid id, Guid runId)
        {
            return _engine.StopRun(id, runId) ? Ok() : NotFound();
        }

        [HttpGet("{id:guid}/runs")]
        public async Task<IEnumerable<AutomationRunDto>> GetRuns(Guid id, CancellationToken token)
        {
            var runs = await _engine.GetRunsAsync(id, 50, token);
            return runs.Select(a => new AutomationRunDto
            {
                ObjId = a.RunId,
                StartedAt = AsUtc(a.StartedAt),
                FinishedAt = a.FinishedAt.HasValue ? AsUtc(a.FinishedAt.Value) : null,
                State = ToCamel(a.State),
                TriggerId = a.TriggerId,
                Error = a.Error,
                Steps = ToJToken(a.Steps)
            });
        }

        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] AutomationDefinitionDto dto, [FromQuery] Guid? id, CancellationToken token)
        {
            AutomationDefinition definition;
            try
            {
                definition = AutomationJson.Parse(dto?.Definition?.ToString(Newtonsoft.Json.Formatting.None));
            }
            catch (AutomationDefinitionException e)
            {
                return BadRequest(e.Message);
            }

            return Ok(ToJToken(await _engine.ValidateAsync(definition, id, token)));
        }

        [HttpPost("test")]
        public IActionResult Test([FromBody] AutomationDefinitionDto dto)
        {
            AutomationDefinition definition;
            try
            {
                definition = AutomationJson.Parse(dto?.Definition?.ToString(Newtonsoft.Json.Formatting.None));
            }
            catch (AutomationDefinitionException e)
            {
                return BadRequest(e.Message);
            }

            return Ok(ToJToken(_engine.Test(definition, dto?.TriggerId)));
        }

        private async Task<AutomationDto> ToDto(AutomationEntity automation, CancellationToken token, IReadOnlyList<ValidationProblem> problems = null)
        {
            JToken definition;
            if (problems == null)
            {
                try
                {
                    var parsed = AutomationJson.Parse(automation.Definition);
                    problems = await _engine.ValidateAsync(parsed, automation.ObjId, token);
                }
                catch (AutomationDefinitionException e)
                {
                    problems = new[] { new ValidationProblem("", "invalidDefinition", ValidationSeverity.Error, e.Message) };
                }
            }

            try
            {
                definition = JToken.Parse(string.IsNullOrEmpty(automation.Definition) ? "{}" : automation.Definition);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                definition = new JObject();
            }

            return new AutomationDto
            {
                ObjId = automation.ObjId,
                Name = automation.Name,
                Description = automation.Description,
                IsEnabled = automation.IsEnabled,
                Mode = ToCamel(automation.Mode),
                MaxRuns = automation.MaxRuns,
                This2AreaInstance = automation.This2AreaInstance,
                This2CategoryInstance = automation.This2CategoryInstance,
                Definition = definition,
                CreatedAt = AsUtc(automation.CreatedAt),
                ModifiedAt = AsUtc(automation.ModifiedAt),
                LastRunAt = automation.LastRunAt.HasValue ? AsUtc(automation.LastRunAt.Value) : null,
                LastRunState = automation.LastRunState.HasValue ? ToCamel(automation.LastRunState.Value) : null,
                LastError = automation.LastError,
                ActiveRuns = _engine.GetActiveRunCount(automation.ObjId),
                Problems = ToJToken(problems)
            };
        }

        private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

        private static string ToCamel(Enum value) => JsonNamingPolicy.CamelCase.ConvertName(value.ToString());

        private static bool TryParseMode(string mode, out AutomationMode result)
        {
            if (string.IsNullOrEmpty(mode))
            {
                result = AutomationMode.Single;
                return true;
            }
            return Enum.TryParse(mode, true, out result) && Enum.IsDefined(result);
        }

        /// <summary>
        /// The engine types are serialized with System.Text.Json (camelCase enums), MVC uses Newtonsoft
        /// </summary>
        private static JToken ToJToken<T>(T value)
        {
            return JToken.Parse(JsonSerializer.Serialize(value, AutomationJson.Options));
        }
    }
}
