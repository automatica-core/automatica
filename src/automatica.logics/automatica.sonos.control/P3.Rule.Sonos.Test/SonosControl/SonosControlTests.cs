using Automatica.Core.UnitTests.Base.Rules;
using P3.Rule.Sonos.SonosControl;
using Xunit;

namespace P3.Rule.Sonos.Tests.SonosControl;

public class SonosControlTests : RuleTest<SonosControlRuleFactory>
{
    [Fact]
    public async void TestRule()
    {
        await Context.Dispatcher.ClearValues();
        await Context.Dispatcher.ClearRegistrations();

     
    }
}