using System.ComponentModel;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Logic;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Automatica.Core.Mcp.Tools
{
    [McpServerToolType]
    public class LogicTools
    {
        private readonly ILogicPageCache _logicPageCache;
        private readonly ILogicInstanceCache _logicInstanceCache;
        private readonly IDispatcher _dispatcher;
        private readonly NodeDescriber _describer;
        private readonly McpAccess _access;

        public LogicTools(ILogicPageCache logicPageCache, ILogicInstanceCache logicInstanceCache, IDispatcher dispatcher, NodeDescriber describer, McpAccess access)
        {
            _logicPageCache = logicPageCache;
            _logicInstanceCache = logicInstanceCache;
            _dispatcher = dispatcher;
            _describer = describer;
            _access = access;
        }

        [McpServerTool(Name = "list_logics", Title = "List logics", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Lists the logic pages and the logics (rules such as timers, comparisons, math, presence simulation) on them. Use get_logic for inputs, outputs and live values.")]
        public IList<LogicPageInfo> ListLogics(
            [Description("Only logics whose name, description or type contains this text (case insensitive).")] string query = null)
        {
            var logics = _logicInstanceCache.All()
                .Where(a => !a.IsDeleted && _access.CanSee(a.This2UserGroup))
                .Where(a => string.IsNullOrWhiteSpace(query) || $"{a.Name} {a.Description} {a.This2RuleTemplateNavigation?.Name}".Contains(query, StringComparison.OrdinalIgnoreCase))
                .GroupBy(a => a.This2RulePage)
                .ToDictionary(a => a.Key, a => a.ToList());

            return _logicPageCache.All()
                .Where(a => !a.IsDeleted)
                .Select(page => new LogicPageInfo(page.ObjId, page.Name, page.Description,
                    logics.GetValueOrDefault(page.ObjId, new List<RuleInstance>())
                        .OrderBy(a => a.Name)
                        .Select(a => new LogicSummary(a.ObjId, a.Name, a.This2RuleTemplateNavigation?.Name, _describer.GetAreaName(a.This2AreaInstance), _describer.GetCategoryName(a.This2CategoryInstance)))
                        .ToList()))
                .Where(a => string.IsNullOrWhiteSpace(query) || a.Logics.Count > 0)
                .OrderBy(a => a.Name)
                .ToList();
        }

        [McpServerTool(Name = "get_logic", Title = "Get logic details", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Returns a logic with its inputs, outputs and parameters, including their live values. Logic inputs can be written with write_value.")]
        public LogicDetails GetLogic([Description("Logic id from list_logics.")] Guid id)
        {
            var logic = _logicInstanceCache.Get(id);
            if (logic == null || logic.IsDeleted || !_access.CanSee(logic.This2UserGroup))
            {
                throw new McpException($"Logic {id} not found");
            }

            var interfaces = logic.RuleInterfaceInstance
                .Where(a => !a.IsDeleted)
                .OrderBy(a => a.This2RuleInterfaceTemplateNavigation?.This2RuleInterfaceDirection)
                .ThenBy(a => a.This2RuleInterfaceTemplateNavigation?.Name)
                .Select(a => new LogicInterfaceInfo(
                    a.ObjId,
                    a.This2RuleInterfaceTemplateNavigation?.Name,
                    a.This2RuleInterfaceTemplateNavigation == null ? null : ((LogicInterfaceDirection)a.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection).ToString(),
                    ReadParameterValue(a),
                    McpValues.ToCurrentValue(_dispatcher.GetValue(a.ObjId))))
                .ToList();

            var page = _logicPageCache.Get(logic.This2RulePage);
            return new LogicDetails(logic.ObjId, logic.Name, logic.Description, logic.This2RuleTemplateNavigation?.Name, logic.This2RuleTemplateNavigation?.Description,
                page?.Name, _describer.GetAreaName(logic.This2AreaInstance), _describer.GetCategoryName(logic.This2CategoryInstance), interfaces);
        }

        private static object ReadParameterValue(RuleInterfaceInstance instance)
        {
            if (instance.This2RuleInterfaceTemplateNavigation?.This2RuleInterfaceDirection != (long)LogicInterfaceDirection.Param)
            {
                return null;
            }

            return (object)instance.ValueBool ?? (object)instance.ValueInteger ?? (object)instance.ValueDouble ?? instance.ValueString;
        }
    }

    public record LogicSummary(Guid Id, string Name, string Type, string Area, string Category);

    public record LogicPageInfo(Guid Id, string Name, string Description, IList<LogicSummary> Logics);

    public record LogicInterfaceInfo(Guid Id, string Name, string Direction, object ParameterValue, CurrentValue CurrentValue);

    public record LogicDetails(
        Guid Id,
        string Name,
        string Description,
        string Type,
        string TypeDescription,
        string Page,
        string Area,
        string Category,
        IList<LogicInterfaceInfo> Interfaces);
}
