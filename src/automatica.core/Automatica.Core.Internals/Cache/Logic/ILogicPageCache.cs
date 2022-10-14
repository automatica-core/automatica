using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Internals.Cache.Logic
{
    public interface ILogicPageCache : IStore<RulePage>
    {
        void AddNodeInstance(NodeInstance2RulePage nodeInstance);
        void UpdateNodeInstance(NodeInstance2RulePage nodeInstance);

        void AddRuleInstance(RuleInstance ruleInstance);
        void UpdateRuleInstance(RuleInstance ruleInstance);

        void AddLink(Link link);
        void UpdateLink(Link link);


        void RemoveNodeInstance(NodeInstance2RulePage nodeInstance);
        void RemoveRuleInstance(RuleInstance ruleInstance);
        void RemoveLink(Link link);

    }
}
