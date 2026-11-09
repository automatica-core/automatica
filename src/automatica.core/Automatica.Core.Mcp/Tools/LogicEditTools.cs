using System.ComponentModel;
using System.Text.Json;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Internals.Core;
using Automatica.Core.Runtime.Abstraction.Plugins.Logic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Automatica.Core.Mcp.Tools
{
    /// <summary>
    /// Builds logic pages like the logic editor: pages, logics from templates, their parameters and the links between data points
    /// and logics. Reading the configuration needs the admin role or the API key, changing it the admin role or the API key with
    /// <c>mcpAllowWrite</c>. The database and runtime steps follow LogicsController, so the editor and the running logics see the
    /// same result as after an edit in the web UI.
    /// </summary>
    [McpServerToolType]
    public class LogicEditTools
    {
        private const float NodeColumnGap = 320;
        private const float RowHeight = 40;
        private const float LogicGap = 60;

        private readonly ILogicCacheFacade _logicCache;
        private readonly ICoreServer _coreServer;
        private readonly ILogicInstancesStore _logicInstancesStore;
        private readonly ILocalizationProvider _localization;
        private readonly IConfiguration _configuration;
        private readonly NodeDescriber _describer;
        private readonly McpAccess _access;
        private readonly ILogger<LogicEditTools> _logger;

        public LogicEditTools(ILogicCacheFacade logicCache, ICoreServer coreServer, ILogicInstancesStore logicInstancesStore, ILocalizationProvider localization,
            IConfiguration configuration, NodeDescriber describer, McpAccess access, ILogger<LogicEditTools> logger)
        {
            _logicCache = logicCache;
            _coreServer = coreServer;
            _logicInstancesStore = logicInstancesStore;
            _localization = localization;
            _configuration = configuration;
            _describer = describer;
            _access = access;
            _logger = logger;
        }

        private void EnsureCanRead()
        {
            if (!_access.CanSeeLogicConfiguration)
            {
                throw new McpException("The logic configuration is available to administrators only");
            }
        }

        private void EnsureCanEdit()
        {
            EnsureCanRead();
            if (!_access.CanEditLogics)
            {
                throw new McpException("Changing logics is disabled for the MCP API key. Enable the setting 'mcpAllowWrite' or log in as an administrator.");
            }
        }

        private string T(string key) => string.IsNullOrEmpty(key) ? key : _localization.GetTranslation(key);

        private AutomaticaContext CreateContext() => new(_configuration);

        // ---------- templates ----------

        [McpServerTool(Name = "list_logic_templates", Title = "List logic block types", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Lists the logic block types that can be added to a logic page (timers, math, comparisons, energy monitor, ...) with their group. Use get_logic_template for the inputs, outputs and parameters of one type.")]
        public IList<LogicTemplateSummary> ListLogicTemplates(
            [Description("Only types whose name, key, group or description contains this text (case insensitive).")] string query = null)
        {
            EnsureCanRead();
            return _logicCache.TemplateCache.All()
                .Select(a => new LogicTemplateSummary(a.ObjId, a.Key, T(a.Name), T(a.Group), T(a.Description)))
                .Where(a => string.IsNullOrWhiteSpace(query) || $"{a.Key} {a.Name} {a.Group} {a.Description}".Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.Group, StringComparer.OrdinalIgnoreCase)
                .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        [McpServerTool(Name = "get_logic_template", Title = "Get a logic block type", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Returns a logic block type with its inputs, outputs and parameters (key, name, description, data type, default). Inputs and outputs are linked with link_logic, parameters set with add_logic or update_logic, both by key.")]
        public LogicTemplateDetails GetLogicTemplate(
            [Description("The id or key of the type from list_logic_templates.")] string template)
        {
            EnsureCanRead();
            var found = FindTemplate(template);
            return new LogicTemplateDetails(found.ObjId, found.Key, T(found.Name), T(found.Group), T(found.Description),
                found.RuleInterfaceTemplate
                    .OrderBy(a => a.This2RuleInterfaceDirection)
                    .ThenBy(a => a.SortOrder)
                    .Select(a => new LogicTemplateInterface(a.Key, T(a.Name), T(a.Description), Direction(a),
                        a.This2RuleInterfaceDirection == (long)LogicInterfaceDirection.Param ? a.ParameterDataType.ToString() : null,
                        a.DefaultValue, a.IsLinkableParameter))
                    .ToList());
        }

        private RuleTemplate FindTemplate(string template)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                throw new McpException("No logic type given. Use list_logic_templates for the available types.");
            }

            var templates = _logicCache.TemplateCache.All();
            var found = Guid.TryParse(template, out var id)
                ? templates.FirstOrDefault(a => a.ObjId == id)
                : templates.FirstOrDefault(a => string.Equals(a.Key, template, StringComparison.OrdinalIgnoreCase));

            return found ?? throw new McpException($"Unknown logic type '{template}'. Use list_logic_templates for the available types.");
        }

        private static string Direction(RuleInterfaceTemplate template)
        {
            return ((LogicInterfaceDirection)template.This2RuleInterfaceDirection).ToString();
        }

        // ---------- pages ----------

        [McpServerTool(Name = "get_logic_page", Title = "Get a logic page", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Returns a logic page with its logics, the data points placed on it and every link (which output feeds which input). Use it to check a page after changing it.")]
        public async Task<LogicPageDetails> GetLogicPage(
            [Description("Logic page id from list_logics or create_logic_page.")] Guid pageId,
            CancellationToken token = default)
        {
            EnsureCanRead();
            await using var db = CreateContext();
            var page = await db.RulePages.AsNoTracking().SingleOrDefaultAsync(a => a.ObjId == pageId && !a.IsDeleted, token)
                       ?? throw new McpException($"Logic page {pageId} not found");

            var logics = await LoadLogics(db, a => a.This2RulePage == pageId, token);
            var placements = await db.NodeInstance2RulePages.AsNoTracking().Where(a => a.This2RulePage == pageId && !a.IsDeleted).ToListAsync(token);
            var links = await db.Links.AsNoTracking().Where(a => a.This2RulePage == pageId && !a.IsDeleted).ToListAsync(token);

            var interfaces = logics.SelectMany(l => l.RuleInterfaceInstance.Select(i => (Logic: l, Interface: i))).ToDictionary(a => a.Interface.ObjId);
            var nodes = placements.ToDictionary(a => a.ObjId);

            LinkEnd End(Guid? interfaceId, Guid? placementId)
            {
                if (interfaceId.HasValue && interfaces.TryGetValue(interfaceId.Value, out var end))
                {
                    return new LinkEnd("logic", end.Logic.ObjId, end.Logic.Name, end.Interface.This2RuleInterfaceTemplateNavigation?.Key);
                }

                if (placementId.HasValue && nodes.TryGetValue(placementId.Value, out var placement))
                {
                    var node = _describer.AllNodes().FirstOrDefault(a => a.ObjId == placement.This2NodeInstance);
                    return new LinkEnd("datapoint", placement.This2NodeInstance, node == null ? null : _describer.GetPath(node), null);
                }

                return new LinkEnd("unknown", interfaceId ?? placementId ?? Guid.Empty, null, null);
            }

            return new LogicPageDetails(page.ObjId, page.Name, page.Description,
                logics.OrderBy(a => a.Y).ThenBy(a => a.X).Select(a => new LogicOnPage(a.ObjId, a.Name, a.This2RuleTemplateNavigation?.Key, a.X, a.Y)).ToList(),
                placements.Select(a =>
                {
                    var node = _describer.AllNodes().FirstOrDefault(n => n.ObjId == a.This2NodeInstance);
                    return new PlacedDataPoint(a.ObjId, a.This2NodeInstance, node == null ? null : _describer.GetPath(node), a.X, a.Y);
                }).ToList(),
                links.Select(a => new LogicLinkInfo(a.ObjId,
                    End(a.This2RuleInterfaceInstanceOutput, a.This2NodeInstance2RulePageOutput),
                    End(a.This2RuleInterfaceInstanceInput, a.This2NodeInstance2RulePageInput))).ToList());
        }

        [McpServerTool(Name = "create_logic_page", Title = "Create a logic page", Destructive = false, Idempotent = false, OpenWorld = false)]
        [Description("Creates an empty logic page. Add logics with add_logic and connect them with link_logic.")]
        public async Task<LogicPageCreated> CreateLogicPage(
            [Description("Name of the page as shown in the logic editor.")] string name,
            [Description("Optional description.")] string description = null,
            CancellationToken token = default)
        {
            EnsureCanEdit();
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new McpException("The page needs a name");
            }

            var page = new RulePage
            {
                ObjId = Guid.NewGuid(),
                Name = name.Trim(),
                Description = description,
                This2RulePageType = 1,
                CreatedAt = DateTimeOffset.Now,
                ModifiedAt = DateTimeOffset.Now
            };

            await using (var db = CreateContext())
            {
                db.RulePages.Add(page);
                await db.SaveChangesAsync(token);
            }

            _logicCache.ClearInstances();
            _logger.LogInformation($"{_access.CallerName} created logic page {page.ObjId} {page.Name}");
            return new LogicPageCreated(page.ObjId, page.Name);
        }

        // ---------- logics ----------

        [McpServerTool(Name = "add_logic", Title = "Add a logic to a page", Destructive = false, Idempotent = false, OpenWorld = true)]
        [Description("Adds a logic block of a type from list_logic_templates to a logic page and starts it. Parameters are set by key (see get_logic_template). The result lists the block's inputs, outputs and parameters with their keys for link_logic.")]
        public async Task<LogicEdited> AddLogic(
            [Description("Logic page id.")] Guid pageId,
            [Description("The id or key of the logic type.")] string template,
            [Description("Name of the block; default: the name of the type.")] string name = null,
            [Description("Parameter values by key, e.g. {\"peakPower\": 10}.")] Dictionary<string, JsonElement> parameters = null,
            [Description("Position on the page; default: below the other logics.")] float? x = null,
            [Description("Position on the page; default: below the other logics.")] float? y = null,
            CancellationToken token = default)
        {
            EnsureCanEdit();
            var ruleTemplate = FindTemplate(template);
            RuleInstance logic;

            await using (var db = CreateContext())
            {
                if (!await db.RulePages.AnyAsync(a => a.ObjId == pageId && !a.IsDeleted, token))
                {
                    throw new McpException($"Logic page {pageId} not found");
                }

                logic = RuleInstance.CreateFromTemplate(ruleTemplate);
                logic.Name = string.IsNullOrWhiteSpace(name) ? T(ruleTemplate.Name) : name.Trim();
                logic.Description = T(ruleTemplate.Description);
                logic.This2RulePage = pageId;
                logic.CreatedAt = DateTimeOffset.Now;
                logic.ModifiedAt = DateTimeOffset.Now;

                var (freeX, freeY) = await FreeLogicPosition(db, pageId, token);
                logic.X = x ?? freeX;
                logic.Y = y ?? freeY;

                ApplyParameters(logic, parameters);

                logic.This2RuleTemplateNavigation = null;
                foreach (var ruleInterface in logic.RuleInterfaceInstance)
                {
                    ruleInterface.This2RuleInterfaceTemplateNavigation = null;
                    db.RuleInterfaceInstances.Add(ruleInterface);
                }
                db.RuleInstances.Add(logic);
                await db.SaveChangesAsync(token);
            }

            _logicCache.ClearInstances();
            await _coreServer.ReloadLogic(logic.ObjId);
            _logger.LogInformation($"{_access.CallerName} added logic {logic.ObjId} {logic.Name} ({ruleTemplate.Key}) to page {pageId}");
            return await DescribeLogic(logic.ObjId, token);
        }

        [McpServerTool(Name = "update_logic", Title = "Change a logic", Destructive = false, Idempotent = true, OpenWorld = true)]
        [Description("Renames or moves a logic block or changes its parameters (by key, see get_logic_template); the logic restarts with the new parameters.")]
        public async Task<LogicEdited> UpdateLogic(
            [Description("Logic id.")] Guid logicId,
            [Description("New name, empty to keep it.")] string name = null,
            [Description("Parameter values by key, e.g. {\"peakPower\": 10}.")] Dictionary<string, JsonElement> parameters = null,
            [Description("New position, empty to keep it.")] float? x = null,
            [Description("New position, empty to keep it.")] float? y = null,
            CancellationToken token = default)
        {
            EnsureCanEdit();
            await using (var db = CreateContext())
            {
                var logic = await db.RuleInstances
                                .Include(a => a.RuleInterfaceInstance).ThenInclude(a => a.This2RuleInterfaceTemplateNavigation)
                                .SingleOrDefaultAsync(a => a.ObjId == logicId && !a.IsDeleted, token)
                            ?? throw new McpException($"Logic {logicId} not found");

                if (!string.IsNullOrWhiteSpace(name))
                {
                    logic.Name = name.Trim();
                }
                logic.X = x ?? logic.X;
                logic.Y = y ?? logic.Y;
                ApplyParameters(logic, parameters);
                logic.ModifiedAt = DateTimeOffset.Now;
                await db.SaveChangesAsync(token);
            }

            _logicCache.ClearInstances();
            await _coreServer.ReloadLogic(logicId);
            _logger.LogInformation($"{_access.CallerName} changed logic {logicId}");
            return await DescribeLogic(logicId, token);
        }

        [McpServerTool(Name = "remove_logic", Title = "Remove a logic", Destructive = true, Idempotent = true, OpenWorld = true)]
        [Description("Stops a logic block and removes it from its page together with its links. Data points placed on the page stay; they can be linked again.")]
        public async Task<string> RemoveLogic(
            [Description("Logic id.")] Guid logicId,
            CancellationToken token = default)
        {
            EnsureCanEdit();
            RuleInstance removed;
            List<Guid> removedLinks;
            var otherLogics = new HashSet<Guid>();

            await using (var db = CreateContext())
            {
                removed = await db.RuleInstances.Include(a => a.RuleInterfaceInstance).SingleOrDefaultAsync(a => a.ObjId == logicId, token)
                          ?? throw new McpException($"Logic {logicId} not found");

                var interfaceIds = removed.RuleInterfaceInstance.Select(a => a.ObjId).ToList();
                var links = await db.Links.Where(a =>
                        (a.This2RuleInterfaceInstanceInput.HasValue && interfaceIds.Contains(a.This2RuleInterfaceInstanceInput.Value)) ||
                        (a.This2RuleInterfaceInstanceOutput.HasValue && interfaceIds.Contains(a.This2RuleInterfaceInstanceOutput.Value)))
                    .ToListAsync(token);

                // logics on the other end of a removed link restart without it
                var otherInterfaces = links.SelectMany(a => new[] { a.This2RuleInterfaceInstanceInput, a.This2RuleInterfaceInstanceOutput })
                    .Where(a => a.HasValue && !interfaceIds.Contains(a.Value)).Select(a => a!.Value).ToList();
                foreach (var other in await db.RuleInterfaceInstances.Where(a => otherInterfaces.Contains(a.ObjId)).Select(a => a.This2RuleInstance).ToListAsync(token))
                {
                    otherLogics.Add(other);
                }

                removedLinks = links.Select(a => a.ObjId).ToList();
                db.Links.RemoveRange(links);
                db.RuleInstances.Remove(removed);
                await db.SaveChangesAsync(token);
            }

            foreach (var link in removedLinks)
            {
                await _coreServer.RemoveLink(link);
                await _logicCache.RemoveLink(link);
            }

            await _coreServer.StopLogic(logicId);
            // the page cache is cleared below (removing from it fails when it was cleared before)
            _logicInstancesStore.Remove(removed);
            await _logicCache.RemoveLogic(logicId);
            _logicCache.ClearInstances();

            foreach (var other in otherLogics)
            {
                await _coreServer.ReloadLogic(other);
            }
            await _coreServer.ReloadLinks();

            _logger.LogInformation($"{_access.CallerName} removed logic {logicId} {removed.Name} and {removedLinks.Count} links");
            return $"Removed logic '{removed.Name}' and {removedLinks.Count} links";
        }

        private void ApplyParameters(RuleInstance logic, Dictionary<string, JsonElement> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return;
            }

            var templates = _logicCache.TemplateCache.Get(logic.This2RuleTemplate)?.RuleInterfaceTemplate ?? new List<RuleInterfaceTemplate>();
            foreach (var (key, value) in parameters)
            {
                var ruleInterface = logic.RuleInterfaceInstance.FirstOrDefault(a => string.Equals(InterfaceTemplate(a, templates)?.Key, key, StringComparison.OrdinalIgnoreCase));
                var template = ruleInterface == null ? null : InterfaceTemplate(ruleInterface, templates);
                if (template == null || template.This2RuleInterfaceDirection != (long)LogicInterfaceDirection.Param)
                {
                    var known = templates.Where(a => a.This2RuleInterfaceDirection == (long)LogicInterfaceDirection.Param).Select(a => a.Key);
                    throw new McpException($"'{key}' is not a parameter of '{logic.Name}'. Parameters: {string.Join(", ", known)}");
                }

                // the value setter converts by the template's data type (loaded on both paths, never replaced: EF tracks it)
                ruleInterface.This2RuleInterfaceTemplateNavigation ??= template;
                try
                {
                    ruleInterface.Value = value.ValueKind is JsonValueKind.Object or JsonValueKind.Array
                        ? value.GetRawText()
                        : McpValues.FromJson(value);
                }
                catch (Exception e) when (e is FormatException or InvalidCastException or ArgumentException or OverflowException)
                {
                    throw new McpException($"'{value}' is not a valid value for '{key}' ({template.ParameterDataType})");
                }
            }
        }

        private static RuleInterfaceTemplate InterfaceTemplate(RuleInterfaceInstance instance, ICollection<RuleInterfaceTemplate> templates)
        {
            return instance.This2RuleInterfaceTemplateNavigation ?? templates.FirstOrDefault(a => a.ObjId == instance.This2RuleInterfaceTemplate);
        }

        private static async Task<(float X, float Y)> FreeLogicPosition(AutomaticaContext db, Guid pageId, CancellationToken token)
        {
            var logics = await db.RuleInstances.AsNoTracking().Include(a => a.This2RuleTemplateNavigation)
                .Where(a => a.This2RulePage == pageId && !a.IsDeleted).ToListAsync(token);
            if (logics.Count == 0)
            {
                return (NodeColumnGap + 80, 80);
            }

            var bottom = logics.Max(a => a.Y + Math.Max(a.This2RuleTemplateNavigation?.Height ?? 0, 100));
            return (logics.Min(a => a.X), bottom + LogicGap);
        }

        private async Task<List<RuleInstance>> LoadLogics(AutomaticaContext db, System.Linq.Expressions.Expression<Func<RuleInstance, bool>> filter, CancellationToken token)
        {
            return await db.RuleInstances.AsNoTracking()
                .Include(a => a.This2RuleTemplateNavigation)
                .Include(a => a.RuleInterfaceInstance).ThenInclude(a => a.This2RuleInterfaceTemplateNavigation)
                .Where(a => !a.IsDeleted)
                .Where(filter)
                .ToListAsync(token);
        }

        private async Task<LogicEdited> DescribeLogic(Guid logicId, CancellationToken token)
        {
            await using var db = CreateContext();
            var logic = (await LoadLogics(db, a => a.ObjId == logicId, token)).Single();
            return Describe(logic);
        }

        private LogicEdited Describe(RuleInstance logic)
        {
            return new LogicEdited(logic.ObjId, logic.Name, logic.This2RuleTemplateNavigation?.Key, logic.This2RulePage, logic.X, logic.Y,
                logic.RuleInterfaceInstance
                    .Where(a => a.This2RuleInterfaceTemplateNavigation != null)
                    .OrderBy(a => a.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection)
                    .ThenBy(a => a.This2RuleInterfaceTemplateNavigation.SortOrder)
                    .Select(a => new LogicInterfaceKey(a.ObjId, a.This2RuleInterfaceTemplateNavigation.Key, T(a.This2RuleInterfaceTemplateNavigation.Name),
                        Direction(a.This2RuleInterfaceTemplateNavigation),
                        a.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection == (long)LogicInterfaceDirection.Param ? McpValues.ToJsonValue(a.Value) : null))
                    .ToList());
        }

        // ---------- links ----------

        [McpServerTool(Name = "link_logic", Title = "Link data points and logics", Destructive = false, Idempotent = true, OpenWorld = true)]
        [Description("Connects a source to a target on a logic page, like drawing a wire in the logic editor. Source: a readable data point (sourceDatapointId) or a logic output (sourceLogicId + sourceOutput). Target: a logic input or linkable parameter (targetLogicId + targetInput) or a writeable data point (targetDatapointId). At least one end is a logic; data points are placed on the logic's page automatically. Inputs/outputs are given by key (see add_logic or get_logic_template).")]
        public async Task<LinkCreated> LinkLogic(
            [Description("Readable data point that feeds the target.")] Guid? sourceDatapointId = null,
            [Description("Logic whose output feeds the target.")] Guid? sourceLogicId = null,
            [Description("Key of the source logic's output.")] string sourceOutput = null,
            [Description("Logic whose input is fed.")] Guid? targetLogicId = null,
            [Description("Key of the target logic's input.")] string targetInput = null,
            [Description("Writeable data point that receives the value.")] Guid? targetDatapointId = null,
            CancellationToken token = default)
        {
            EnsureCanEdit();
            if (sourceDatapointId.HasValue == sourceLogicId.HasValue)
            {
                throw new McpException("Give exactly one source: sourceDatapointId or sourceLogicId with sourceOutput");
            }
            if (targetDatapointId.HasValue == targetLogicId.HasValue)
            {
                throw new McpException("Give exactly one target: targetLogicId with targetInput or targetDatapointId");
            }
            if (sourceDatapointId.HasValue && targetDatapointId.HasValue)
            {
                throw new McpException("Two data points cannot be linked directly, put a logic in between");
            }

            Link link;
            var reload = new HashSet<Guid>();
            var existing = false;

            await using (var db = CreateContext())
            {
                RuleInstance sourceLogic = null, targetLogic = null;
                RuleInterfaceInstance output = null, input = null;

                if (sourceLogicId.HasValue)
                {
                    sourceLogic = await LoadLogicForLink(db, sourceLogicId.Value, token);
                    output = FindInterface(sourceLogic, sourceOutput, "sourceOutput", LogicInterfaceDirection.Output);
                }
                if (targetLogicId.HasValue)
                {
                    targetLogic = await LoadLogicForLink(db, targetLogicId.Value, token);
                    input = FindInterface(targetLogic, targetInput, "targetInput", LogicInterfaceDirection.Input);
                }
                if (sourceLogic != null && targetLogic != null && sourceLogic.This2RulePage != targetLogic.This2RulePage)
                {
                    throw new McpException("Both logics must be on the same logic page");
                }

                var anchor = targetLogic ?? sourceLogic;
                var pageId = anchor.This2RulePage;

                NodeInstance2RulePage sourceNode = null, targetNode = null;
                if (sourceDatapointId.HasValue)
                {
                    var node = FindDataPoint(sourceDatapointId.Value);
                    if (!node.IsReadable)
                    {
                        throw new McpException($"Data point '{node.Name}' is not readable and cannot feed a logic");
                    }
                    sourceNode = await PlaceDataPoint(db, pageId, node.ObjId, anchor, left: true, token);
                }
                if (targetDatapointId.HasValue)
                {
                    var node = FindDataPoint(targetDatapointId.Value);
                    if (!node.IsWriteable)
                    {
                        throw new McpException($"Data point '{node.Name}' is not writeable and cannot be fed by a logic");
                    }
                    targetNode = await PlaceDataPoint(db, pageId, node.ObjId, anchor, left: false, token);
                }

                var outputId = output?.ObjId;
                var inputId = input?.ObjId;
                var sourceNodeId = sourceNode?.ObjId;
                var targetNodeId = targetNode?.ObjId;

                link = await db.Links.AsNoTracking().FirstOrDefaultAsync(a => !a.IsDeleted && a.This2RulePage == pageId &&
                    a.This2RuleInterfaceInstanceOutput == outputId && a.This2RuleInterfaceInstanceInput == inputId &&
                    a.This2NodeInstance2RulePageOutput == sourceNodeId && a.This2NodeInstance2RulePageInput == targetNodeId, token);

                if (link == null)
                {
                    link = new Link
                    {
                        ObjId = Guid.NewGuid(),
                        This2RulePage = pageId,
                        This2RuleInterfaceInstanceOutput = outputId,
                        This2RuleInterfaceInstanceInput = inputId,
                        This2NodeInstance2RulePageOutput = sourceNodeId,
                        This2NodeInstance2RulePageInput = targetNodeId
                    };
                    db.Links.Add(link);
                }
                else
                {
                    existing = true;
                }

                await db.SaveChangesAsync(token);

                if (sourceLogic != null)
                {
                    reload.Add(sourceLogic.ObjId);
                }
                if (targetLogic != null)
                {
                    reload.Add(targetLogic.ObjId);
                }
            }

            if (!existing)
            {
                _logicCache.ClearInstances();
                foreach (var logicId in reload)
                {
                    await _coreServer.ReloadLogic(logicId);
                }
                await _coreServer.ReloadLinks();
                _logger.LogInformation($"{_access.CallerName} added link {link.ObjId} on page {link.This2RulePage}");
            }

            return new LinkCreated(link.ObjId, link.This2RulePage, existing);
        }

        [McpServerTool(Name = "remove_link", Title = "Remove a link", Destructive = true, Idempotent = true, OpenWorld = true)]
        [Description("Removes a link (see get_logic_page for the link ids). The data points stay on the page.")]
        public async Task<string> RemoveLink(
            [Description("Link id.")] Guid linkId,
            CancellationToken token = default)
        {
            EnsureCanEdit();
            var logics = new List<Guid>();
            await using (var db = CreateContext())
            {
                var link = await db.Links.SingleOrDefaultAsync(a => a.ObjId == linkId, token)
                           ?? throw new McpException($"Link {linkId} not found");

                var interfaces = new[] { link.This2RuleInterfaceInstanceOutput, link.This2RuleInterfaceInstanceInput }.Where(a => a.HasValue).Select(a => a!.Value).ToList();
                logics.AddRange(await db.RuleInterfaceInstances.Where(a => interfaces.Contains(a.ObjId)).Select(a => a.This2RuleInstance).ToListAsync(token));

                db.Links.Remove(link);
                await db.SaveChangesAsync(token);
            }

            await _coreServer.RemoveLink(linkId);
            await _logicCache.RemoveLink(linkId);
            foreach (var logic in logics.Distinct())
            {
                await _coreServer.ReloadLogic(logic);
            }
            _logicCache.LinkCache.Clear();
            _logicCache.ClearInstances();
            await _coreServer.ReloadLinks();

            _logger.LogInformation($"{_access.CallerName} removed link {linkId}");
            return "Link removed";
        }

        private static async Task<RuleInstance> LoadLogicForLink(AutomaticaContext db, Guid logicId, CancellationToken token)
        {
            return await db.RuleInstances.AsNoTracking()
                       .Include(a => a.This2RuleTemplateNavigation)
                       .Include(a => a.RuleInterfaceInstance).ThenInclude(a => a.This2RuleInterfaceTemplateNavigation)
                       .SingleOrDefaultAsync(a => a.ObjId == logicId && !a.IsDeleted, token)
                   ?? throw new McpException($"Logic {logicId} not found");
        }

        private RuleInterfaceInstance FindInterface(RuleInstance logic, string key, string argument, LogicInterfaceDirection direction)
        {
            bool Fits(RuleInterfaceTemplate t) => t.This2RuleInterfaceDirection == (long)direction ||
                                                  (direction == LogicInterfaceDirection.Input && t.This2RuleInterfaceDirection == (long)LogicInterfaceDirection.Param && t.IsLinkableParameter);

            var candidates = logic.RuleInterfaceInstance
                .Where(a => a.This2RuleInterfaceTemplateNavigation != null && Fits(a.This2RuleInterfaceTemplateNavigation))
                .OrderBy(a => a.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection)
                .ThenBy(a => a.This2RuleInterfaceTemplateNavigation.SortOrder)
                .ToList();
            var found = string.IsNullOrWhiteSpace(key)
                ? null
                : candidates.FirstOrDefault(a => Guid.TryParse(key, out var id) ? a.ObjId == id : string.Equals(a.This2RuleInterfaceTemplateNavigation.Key, key, StringComparison.OrdinalIgnoreCase));

            if (found == null)
            {
                var kind = direction == LogicInterfaceDirection.Output ? "output" : "input";
                throw new McpException($"'{key}' is not an {kind} of '{logic.Name}' ({argument}). {char.ToUpper(kind[0]) + kind[1..]}s: " +
                                       string.Join(", ", candidates.Select(a => a.This2RuleInterfaceTemplateNavigation.Key)));
            }

            return found;
        }

        private NodeInstance FindDataPoint(Guid id)
        {
            var node = _describer.AllNodes().FirstOrDefault(a => a.ObjId == id);
            if (node == null || !_describer.IsDataPoint(node) || !_access.CanSee(node.This2UserGroup))
            {
                throw new McpException($"Data point {id} not found. Use search_datapoints for the ids.");
            }
            return node;
        }

        /// <summary>
        /// The data point's placement on the page, created next to the logic when the page does not show it yet
        /// (left of the logic for sources, right of it for targets).
        /// </summary>
        private static async Task<NodeInstance2RulePage> PlaceDataPoint(AutomaticaContext db, Guid pageId, Guid nodeId, RuleInstance logic, bool left, CancellationToken token)
        {
            var placement = await db.NodeInstance2RulePages.FirstOrDefaultAsync(a => a.This2RulePage == pageId && a.This2NodeInstance == nodeId && !a.IsDeleted, token);
            if (placement != null)
            {
                return placement;
            }

            var x = left ? logic.X - NodeColumnGap : logic.X + (logic.This2RuleTemplateNavigation?.Width ?? 200) + 120;
            var column = await db.NodeInstance2RulePages.Where(a => a.This2RulePage == pageId && !a.IsDeleted && a.X == x).CountAsync(token)
                         + db.NodeInstance2RulePages.Local.Count(a => a.This2RulePage == pageId && a.X == x && db.Entry(a).State == EntityState.Added);

            placement = new NodeInstance2RulePage
            {
                ObjId = Guid.NewGuid(),
                This2RulePage = pageId,
                This2NodeInstance = nodeId,
                X = x,
                Y = logic.Y + column * RowHeight
            };
            db.NodeInstance2RulePages.Add(placement);
            return placement;
        }
    }

    public record LogicTemplateSummary(Guid Id, string Key, string Name, string Group, string Description);

    public record LogicTemplateInterface(string Key, string Name, string Description, string Direction, string ParameterType, string DefaultValue, bool LinkableParameter);

    public record LogicTemplateDetails(Guid Id, string Key, string Name, string Group, string Description, IList<LogicTemplateInterface> Interfaces);

    public record LogicPageCreated(Guid Id, string Name);

    public record LogicInterfaceKey(Guid Id, string Key, string Name, string Direction, object ParameterValue);

    public record LogicEdited(Guid Id, string Name, string Type, Guid PageId, float X, float Y, IList<LogicInterfaceKey> Interfaces);

    public record LinkCreated(Guid LinkId, Guid PageId, bool AlreadyExisted);

    public record LinkEnd(string Kind, Guid Id, string Name, string Interface);

    public record LogicLinkInfo(Guid Id, LinkEnd From, LinkEnd To);

    public record PlacedDataPoint(Guid PlacementId, Guid DataPointId, string Path, float X, float Y);

    public record LogicOnPage(Guid Id, string Name, string Type, float X, float Y);

    public record LogicPageDetails(Guid Id, string Name, string Description, IList<LogicOnPage> Logics, IList<PlacedDataPoint> DataPoints, IList<LogicLinkInfo> Links);
}
