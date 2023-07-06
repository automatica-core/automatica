using System;
using System.Collections.Generic;
using Automatica.Core.UnitTests.Base.Logics;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using P3.Logic.Sonos.SonosControl;
using Xunit;

namespace P3.Logic.Sonos.Tests.SonosControl;

public class SonosControlTests : LogicTest<SonosControlLogicFactory>
{
    [Fact]
    public async void TestLogic()
    {
        await Context.Dispatcher.ClearValues();
        await Context.Dispatcher.ClearRegistrations();

        LogicInputChanged(GetLogicInterfaceByTemplate(SonosControlLogicFactory.PlayPauseTrigger), true);

        await Task.Delay(500);

        var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

        Assert.Equal(4, values.Count);

        AssertInternal(values, SonosControlLogicFactory.PlayOutputStatus, true);
        AssertInternal(values, SonosControlLogicFactory.VolumeOutputStatus, SonosControlLogicFactory.DefaultVolume);
        AssertInternal(values, SonosControlLogicFactory.RadioStationOutputValue, SonosControlLogicFactory.DefaultRadioStation);
        AssertInternal(values, SonosControlLogicFactory.PauseOutputStatus, false);


        LogicInputChanged(GetLogicInterfaceByTemplate(SonosControlLogicFactory.VolumeIncrement), true);
        values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

        AssertInternal(values, SonosControlLogicFactory.VolumeOutputStatus, SonosControlLogicFactory.DefaultVolume + 1);



        LogicInputChanged(GetLogicInterfaceByTemplate(SonosControlLogicFactory.PlayPauseTrigger), false);
        values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);


        AssertInternal(values, SonosControlLogicFactory.PlayOutputStatus, false); 
        AssertInternal(values, SonosControlLogicFactory.PauseOutputStatus, true);

    }

    private void AssertInternal(IDictionary<Guid, DispatchValue> values, Guid guid, object value)
    {
        Assert.Equal(value, values[guid].Value);
    }
}