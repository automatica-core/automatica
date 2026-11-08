using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Automation.Definition;
using Automatica.Core.Automation.Engine;
using Automatica.Core.EF.Models;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using AutomationEntity = Automatica.Core.EF.Models.Automations.Automation;

namespace Automatica.Core.WebApi.Controllers
{
    public class AutomationVisuBlockDto
    {
        public string Path { get; set; }
        public string Kind { get; set; }
        public string Type { get; set; }

        /// <summary>
        /// Name of the datapoint or control the block refers to, null for time and sun blocks
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// The fields the user may change
        /// </summary>
        public IReadOnlyCollection<string> Fields { get; set; }

        /// <summary>
        /// The whole block (read only context such as the operator or the command, plus the editable values)
        /// </summary>
        public JToken Block { get; set; }
    }

    public class AutomationVisuDto
    {
        public Guid ObjId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsEnabled { get; set; }
        public Guid? This2AreaInstance { get; set; }
        public Guid? This2CategoryInstance { get; set; }
        public DateTime? LastRunAt { get; set; }
        public string LastRunState { get; set; }

        /// <summary>
        /// The current user may change it (visu and admin roles)
        /// </summary>
        public bool CanEdit { get; set; }

        public IList<AutomationVisuBlockDto> Blocks { get; set; } = new List<AutomationVisuBlockDto>();
    }

    public class AutomationVisuBlockEditDto
    {
        public string Path { get; set; }
        public JObject Values { get; set; }
    }

    public class AutomationVisuSaveDto
    {
        public IList<AutomationVisuBlockEditDto> Blocks { get; set; } = new List<AutomationVisuBlockEditDto>();
    }

    /// <summary>
    /// Automations the admins released for the visualization (<c>userAccess</c> in the definition): every signed-in user
    /// sees them, visualization users and admins switch them on or off and change the values of the blocks marked
    /// <c>userEditable</c> (times, days, thresholds, values, see <see cref="AutomationUserAccess"/>). The structure stays
    /// in <see cref="AutomationsController"/> (admins).
    /// </summary>
    [Route("webapi/automations-visu")]
    [Authorize(Policy = Role.ViewerRole)]
    public class AutomationsVisuController : BaseController
    {
        private readonly IAutomationEngine _engine;
        private readonly IAutomationEnvironment _environment;
        private readonly ILogger<AutomationsVisuController> _logger;

        public AutomationsVisuController(AutomaticaContext dbContext, IAutomationEngine engine, IAutomationEnvironment environment,
            ILogger<AutomationsVisuController> logger) : base(dbContext)
        {
            _engine = engine;
            _environment = environment;
            _logger = logger;
        }

        private bool CanEdit => IsAdmin || (User?.IsInRole(Role.VisuRole) ?? false);

        [HttpGet]
        public async Task<IEnumerable<AutomationVisuDto>> GetAll(CancellationToken token)
        {
            var automations = await DbContext.Automations.AsNoTracking().OrderBy(a => a.Name).ToListAsync(token);
            var result = new List<AutomationVisuDto>();
            foreach (var automation in automations)
            {
                var definition = TryParse(automation);
                if (definition is { UserAccess: true })
                {
                    result.Add(ToDto(automation, definition));
                }
            }
            return result;
        }

        /// <summary>
        /// Changes the values of the released blocks. Paths or fields that are not released are refused (400).
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = Role.VisuRole + "," + Role.AdminRole)]
        public async Task<ActionResult<AutomationVisuDto>> Update(Guid id, [FromBody] AutomationVisuSaveDto dto, CancellationToken token)
        {
            var entity = await DbContext.Automations.SingleOrDefaultAsync(a => a.ObjId == id, token);
            var current = entity == null ? null : TryParse(entity);
            if (current is not { UserAccess: true })
            {
                return NotFound();
            }

            var edits = (dto?.Blocks ?? new List<AutomationVisuBlockEditDto>()).Select(a => new UserBlockEdit
            {
                Path = a?.Path,
                Values = a?.Values == null ? new JsonObject() : JsonNode.Parse(a.Values.ToString(Newtonsoft.Json.Formatting.None))!.AsObject()
            }).ToList();

            var changed = AutomationUserAccess.Apply(current, edits, out var errors);
            if (changed == null)
            {
                return BadRequest(new { errors });
            }

            var problems = await _engine.ValidateAsync(changed, id, token);
            if (entity.IsEnabled && problems.Any(a => a.Severity == ValidationSeverity.Error))
            {
                return BadRequest(new { problems = problems.Select(a => new { a.Path, a.Code, a.Detail }) });
            }

            entity.Definition = AutomationJson.Serialize(changed);
            entity.ModifiedAt = DateTime.UtcNow;
            await DbContext.SaveChangesAsync(token);
            _logger.LogInformation($"Automation {entity.Name} ({entity.ObjId}) changed by visualization user {User?.Identity?.Name}");

            await _engine.ReloadAsync(entity.ObjId, token);
            return ToDto(entity, changed);
        }

        [HttpPost("{id:guid}/enabled")]
        [Authorize(Roles = Role.VisuRole + "," + Role.AdminRole)]
        public async Task<ActionResult<AutomationVisuDto>> SetEnabled(Guid id, [FromBody] AutomationEnabledDto dto, CancellationToken token)
        {
            var entity = await DbContext.Automations.AsNoTracking().SingleOrDefaultAsync(a => a.ObjId == id, token);
            var definition = entity == null ? null : TryParse(entity);
            if (definition is not { UserAccess: true } || dto == null)
            {
                return NotFound();
            }

            if (dto.Enabled)
            {
                var problems = await _engine.ValidateAsync(definition, id, token);
                if (problems.Any(a => a.Severity == ValidationSeverity.Error))
                {
                    return BadRequest(new { problems = problems.Select(a => new { a.Path, a.Code, a.Detail }) });
                }
            }

            await _engine.SetEnabledAsync(id, dto.Enabled, token);
            _logger.LogInformation($"Automation {entity.Name} ({entity.ObjId}) switched {(dto.Enabled ? "on" : "off")} by visualization user {User?.Identity?.Name}");

            entity = await DbContext.Automations.AsNoTracking().SingleAsync(a => a.ObjId == id, token);
            return ToDto(entity, definition);
        }

        private AutomationDefinition TryParse(AutomationEntity automation)
        {
            try
            {
                return AutomationJson.Parse(automation.Definition);
            }
            catch (AutomationDefinitionException e)
            {
                _logger.LogWarning($"Automation {automation.Name} ({automation.ObjId}) has an invalid definition: {e.Message}");
                return null;
            }
        }

        private AutomationVisuDto ToDto(AutomationEntity automation, AutomationDefinition definition)
        {
            var root = JsonNode.Parse(AutomationJson.Serialize(definition))!.AsObject();
            return new AutomationVisuDto
            {
                ObjId = automation.ObjId,
                Name = automation.Name,
                Description = automation.Description,
                IsEnabled = automation.IsEnabled,
                This2AreaInstance = automation.This2AreaInstance,
                This2CategoryInstance = automation.This2CategoryInstance,
                LastRunAt = automation.LastRunAt.HasValue ? DateTime.SpecifyKind(automation.LastRunAt.Value, DateTimeKind.Utc) : null,
                LastRunState = automation.LastRunState?.ToString() is { } state ? char.ToLowerInvariant(state[0]) + state[1..] : null,
                CanEdit = CanEdit,
                Blocks = AutomationUserAccess.Collect(definition).Select(block => new AutomationVisuBlockDto
                {
                    Path = block.Path,
                    Kind = block.Kind,
                    Type = block.Type,
                    Label = Label(block.Block),
                    Fields = block.Fields,
                    Block = JObject.Parse(AutomationUserAccess.Resolve(root, block.Path)?.ToJsonString() ?? "{}")
                }).ToList()
            };
        }

        private string Label(object block)
        {
            Guid? node = block switch
            {
                DatapointTrigger trigger => trigger.Node,
                DatapointCondition condition => condition.Node,
                SetValueAction setValue => setValue.Node,
                _ => null
            };
            if (node.HasValue)
            {
                return _environment.GetNode(node.Value)?.Name;
            }

            return block is ControlAction control ? _environment.GetControl(control.Control)?.Name : null;
        }
    }
}
