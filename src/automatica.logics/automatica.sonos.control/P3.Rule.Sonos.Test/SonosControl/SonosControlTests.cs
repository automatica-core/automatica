using Automatica.Core.UnitTests.Base.Rules;
using P3.Rule.Sonos.SonosControl;
using System.Threading.Tasks;
using Xunit;

namespace P3.Rule.Sonos.Tests.SonosControl;

public class SonosControlTests : RuleTest<SonosControlRuleFactory>
{
    [Fact]
    public async void TestRule()
    {
        await Context.Dispatcher.ClearValues();
        await Context.Dispatcher.ClearRegistrations();

        RuleInputChanged(GetRuleInterfaceByTemplate(SonosControlRuleFactory.PlayPauseTrigger), true);

        await Task.Delay(500);

        var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);


    }
}