using System;
using System.Collections;
using System.Collections.Generic;
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

        Assert.Equal(4, values.Count);

        AssertInternal(values, SonosControlRuleFactory.PlayOutputStatus, true);
        AssertInternal(values, SonosControlRuleFactory.VolumeOutputStatus, SonosControlRuleFactory.DefaultVolume);
        AssertInternal(values, SonosControlRuleFactory.RadioStationOutputValue, SonosControlRuleFactory.DefaultRadioStation);
        AssertInternal(values, SonosControlRuleFactory.PauseOutputStatus, false);


        RuleInputChanged(GetRuleInterfaceByTemplate(SonosControlRuleFactory.VolumeIncrement), true);
        values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

        AssertInternal(values, SonosControlRuleFactory.VolumeOutputStatus, SonosControlRuleFactory.DefaultVolume + 1);



        RuleInputChanged(GetRuleInterfaceByTemplate(SonosControlRuleFactory.PlayPauseTrigger), false);
        values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);


        AssertInternal(values, SonosControlRuleFactory.PlayOutputStatus, false); 
        AssertInternal(values, SonosControlRuleFactory.PauseOutputStatus, true);

    }

    private void AssertInternal(IDictionary<Guid, object> values, Guid guid, object value)
    {
        Assert.Equal(value, values[guid]);
    }
}