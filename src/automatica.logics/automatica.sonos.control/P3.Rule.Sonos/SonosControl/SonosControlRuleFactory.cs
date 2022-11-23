using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Rule;

namespace P3.Rule.Sonos.SonosControl;

public class SonosControlRuleFactory : RuleFactory
{
    public override Version RuleVersion => new Version(0, 0, 0, 1);

    public override string RuleName => "SonosControl";
    public override Guid RuleGuid => new Guid("550c0290-40e3-4d60-b016-3588d2a367fe");

    public override void InitTemplates(IRuleTemplateFactory factory)
    {
        factory.CreateRuleTemplate(RuleGuid, "SONOS_CONTROL.NAME", "SONOS_CONTROL.DESCRIPTION", "sonos.control", "SONOS.NAME", 100, 100);

    }

    public override IRule CreateRuleInstance(IRuleContext context)
    {
        return new SonosControlRule(context);
    }
}