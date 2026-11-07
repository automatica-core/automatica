using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Sonos.SonosControl;
using P3.Logic.Sonos.Tests.TestSupport;
using Xunit;

// LogicTest uses a process-wide dispatcher singleton, so the test classes must not run in parallel
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace P3.Logic.Sonos.Tests.SonosControl;

/// <summary>
/// The logic only produces values for the Sonos driver datapoints - no speaker is contacted.
/// </summary>
public class SonosControlBehaviourTests : LogicTest<SonosControlLogicFactory>
{
    private readonly ValueRecorder _play;
    private readonly ValueRecorder _volume;
    private readonly ValueRecorder _mediaUrl;
    private readonly ValueRecorder _next;
    private readonly ValueRecorder _previous;

    // the logic under test dispatches into its own recording dispatcher (in call order), not into the shared one of the base class
    private new ILogic Logic { get; }

    public SonosControlBehaviourTests()
    {
        var dispatcher = new RecordingDispatcher();
        Logic = Instance.CreateLogicInstance(new LogicContextMock(RuleInstance, Factory, dispatcher));
        Logic.Start().GetAwaiter().GetResult();

        _play = new ValueRecorder(dispatcher, SonosControlLogicFactory.PlayOutputStatus);
        _volume = new ValueRecorder(dispatcher, SonosControlLogicFactory.VolumeOutputStatus);
        _mediaUrl = new ValueRecorder(dispatcher, SonosControlLogicFactory.PlayMediaUrlOutput);
        _next = new ValueRecorder(dispatcher, SonosControlLogicFactory.NextOutput);
        _previous = new ValueRecorder(dispatcher, SonosControlLogicFactory.PreviousOutput);
    }

    private IList<ILogicOutputChanged> In(Guid template, object value)
    {
        return Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value);
    }

    private void Param(Guid template, object value)
    {
        Assert.Empty(Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value));
    }

    private static Dictionary<Guid, object> ToMap(IList<ILogicOutputChanged> changes)
    {
        return changes.ToDictionary(a => a.Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate, a => a.Value);
    }

    private void StartPlaying()
    {
        var result = ToMap(In(SonosControlLogicFactory.PlayPauseTrigger, true));
        Assert.Equal(true, result[SonosControlLogicFactory.PlayOutputStatus]);
    }

    #region volume

    public static IEnumerable<object[]> VolumeData => new List<object[]>
    {
        new object[] { 20, 20L },
        new object[] { 20L, 20L },
        new object[] { "30", 30L },
        new object[] { 150, 100L },
        new object[] { 100, 100L },
        new object[] { 20.4d, 20L },
        new object[] { 20.6d, 21L },
        new object[] { true, 1L },
        // the initial volume is 0, a value clamped to 0 is no change
        new object[] { 0, null },
        new object[] { -5, null },
    };

    [Theory]
    [MemberData(nameof(VolumeData))]
    public void Volume_IsConvertedAndClampedToZeroAndMax(object value, long? expected)
    {
        var result = In(SonosControlLogicFactory.Volume, value);

        if (expected.HasValue)
        {
            var change = Assert.Single(result);
            Assert.Equal(SonosControlLogicFactory.VolumeOutputStatus, change.Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate);
            Assert.Equal(expected.Value, change.Value);
        }
        else
        {
            Assert.Empty(result);
        }
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData("abc")]
    public void Volume_NotConvertible_IsIgnored(object value)
    {
        In(SonosControlLogicFactory.Volume, 20);

        Assert.Empty(In(SonosControlLogicFactory.Volume, value));
    }

    [Fact]
    public void Volume_Null_SetsZero()
    {
        In(SonosControlLogicFactory.Volume, 20);

        Assert.Equal(0L, Assert.Single(In(SonosControlLogicFactory.Volume, null)).Value);
    }

    [Fact]
    public void Volume_SameValueTwice_OnlyTheFirstIsEmitted()
    {
        Assert.Single(In(SonosControlLogicFactory.Volume, 20));
        Assert.Empty(In(SonosControlLogicFactory.Volume, 20));
        Assert.Single(In(SonosControlLogicFactory.Volume, 21));
    }

    [Fact]
    public void Volume_AboveMaxTwice_OnlyTheFirstIsEmitted()
    {
        Assert.Equal(100L, Assert.Single(In(SonosControlLogicFactory.Volume, 120)).Value);
        Assert.Empty(In(SonosControlLogicFactory.Volume, 130));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Volume_MaxVolumeZeroOrNegative_AlwaysClampsToZero(int maxVolume)
    {
        Param(SonosControlLogicFactory.MaxVolume, maxVolume);

        Assert.Empty(In(SonosControlLogicFactory.Volume, 50));
    }

    [Fact]
    public void Volume_MaxVolumeLowered_NextValueIsClampedToTheNewMax()
    {
        In(SonosControlLogicFactory.Volume, 80);
        Param(SonosControlLogicFactory.MaxVolume, 40);

        Assert.Equal(40L, Assert.Single(In(SonosControlLogicFactory.Volume, 90)).Value);
    }

    [Fact]
    public void VolumeIncrement_NotPlaying_IsIgnored()
    {
        Assert.Empty(In(SonosControlLogicFactory.VolumeIncrement, true));
    }

    [Fact]
    public void VolumeDecrement_NotPlaying_IsIgnored()
    {
        In(SonosControlLogicFactory.Volume, 10);

        Assert.Empty(In(SonosControlLogicFactory.VolumeDecrement, true));
    }

    [Fact]
    public void VolumeIncrement_Playing_IncreasesByOne()
    {
        StartPlaying();
        In(SonosControlLogicFactory.Volume, 10);

        Assert.Equal(11L, Assert.Single(In(SonosControlLogicFactory.VolumeIncrement, true)).Value);
        Assert.Equal(12L, Assert.Single(In(SonosControlLogicFactory.VolumeIncrement, true)).Value);
    }

    [Fact]
    public void VolumeIncrement_AnyValue_Increases()
    {
        // the increment input is a trigger, the value itself is not evaluated
        StartPlaying();

        Assert.Equal(1L, Assert.Single(In(SonosControlLogicFactory.VolumeIncrement, false)).Value);
        Assert.Equal(2L, Assert.Single(In(SonosControlLogicFactory.VolumeIncrement, null)).Value);
    }

    [Fact]
    public void VolumeIncrement_AtMax_IsIgnored()
    {
        Param(SonosControlLogicFactory.MaxVolume, 3);
        StartPlaying();
        In(SonosControlLogicFactory.Volume, 3);

        Assert.Empty(In(SonosControlLogicFactory.VolumeIncrement, true));
    }

    [Fact]
    public void VolumeDecrement_Playing_DecreasesByOne()
    {
        StartPlaying();
        In(SonosControlLogicFactory.Volume, 5);

        Assert.Equal(4L, Assert.Single(In(SonosControlLogicFactory.VolumeDecrement, true)).Value);
    }

    [Fact]
    public void VolumeDecrement_AtZero_IsIgnored()
    {
        StartPlaying();

        Assert.Empty(In(SonosControlLogicFactory.VolumeDecrement, true));
    }

    [Fact]
    public void VolumeDecrement_FromOne_ReachesZero()
    {
        StartPlaying();
        In(SonosControlLogicFactory.Volume, 1);

        Assert.Equal(0L, Assert.Single(In(SonosControlLogicFactory.VolumeDecrement, true)).Value);
        Assert.Empty(In(SonosControlLogicFactory.VolumeDecrement, true));
    }

    #endregion

    #region play / pause

    [Theory]
    [InlineData(true)]
    [InlineData("true")]
    [InlineData(1)]
    public void PlayPause_True_SetsPlayAndClearsPause(object value)
    {
        var result = ToMap(In(SonosControlLogicFactory.PlayPauseTrigger, value));

        Assert.Equal(2, result.Count);
        Assert.Equal(true, result[SonosControlLogicFactory.PlayOutputStatus]);
        Assert.Equal(false, result[SonosControlLogicFactory.PauseOutputStatus]);
    }

    [Fact]
    public void PlayPause_TrueTwice_SecondIsIgnored()
    {
        StartPlaying();

        Assert.Empty(In(SonosControlLogicFactory.PlayPauseTrigger, true));
    }

    [Fact]
    public void PlayPause_FalseWhilePlaying_SetsPauseAndClearsPlay()
    {
        StartPlaying();

        var result = ToMap(In(SonosControlLogicFactory.PlayPauseTrigger, false));

        Assert.Equal(false, result[SonosControlLogicFactory.PlayOutputStatus]);
        Assert.Equal(true, result[SonosControlLogicFactory.PauseOutputStatus]);
        Assert.Empty(In(SonosControlLogicFactory.PlayPauseTrigger, false));
    }

    [Fact]
    public void PlayPause_FalseInitially_EmitsNothing()
    {
        Assert.Empty(In(SonosControlLogicFactory.PlayPauseTrigger, false));
        Assert.Empty(In(SonosControlLogicFactory.PlayPauseTrigger, 0));
    }

    [Fact]
    public void PlayPause_Toggle_AlternatesTheOutputs()
    {
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(true, ToMap(In(SonosControlLogicFactory.PlayPauseTrigger, true))[SonosControlLogicFactory.PlayOutputStatus]);
            Assert.Equal(true, ToMap(In(SonosControlLogicFactory.PlayPauseTrigger, false))[SonosControlLogicFactory.PauseOutputStatus]);
        }
    }

    [Theory]
    [InlineData("abc", 0)]
    [InlineData(double.NaN, 2)]
    public void PlayPause_NotConvertibleOrNaN_DoesNotThrow(object value, int expectedOutputs)
    {
        // NaN converts to true, "abc" throws and is swallowed by the base logic
        var result = In(SonosControlLogicFactory.PlayPauseTrigger, value);

        Assert.Equal(expectedOutputs, result.Count);
    }

    [Fact]
    public async Task PlayDefault_True_EmitsStationVolumePauseAndPlay_AndPlayAgainLater()
    {
        var result = ToMap(In(SonosControlLogicFactory.PlayDefaultTrigger, true));

        Assert.Equal(4, result.Count);
        Assert.Equal(SonosControlLogicFactory.DefaultRadioStation, result[SonosControlLogicFactory.RadioStationOutputValue]);
        Assert.Equal(SonosControlLogicFactory.DefaultVolume, result[SonosControlLogicFactory.VolumeOutputStatus]);
        Assert.Equal(false, result[SonosControlLogicFactory.PauseOutputStatus]);
        Assert.Equal(true, result[SonosControlLogicFactory.PlayOutputStatus]);

        // play is sent a second time after 100ms (the station has to be loaded first)
        var play = await _play.WaitForCountAsync(1);
        Assert.Equal(true, play.Single());
    }

    [Fact]
    public async Task PlayDefault_UsesTheConfiguredStationAndVolume()
    {
        Param(SonosControlLogicFactory.RadioStation, "s1234");
        Param(SonosControlLogicFactory.VolumeOnPlay, 25);

        var result = ToMap(In(SonosControlLogicFactory.PlayDefaultTrigger, true));

        Assert.Equal("s1234", result[SonosControlLogicFactory.RadioStationOutputValue]);
        Assert.Equal(25L, result[SonosControlLogicFactory.VolumeOutputStatus]);
        await _play.WaitForCountAsync(1);
    }

    [Fact]
    public async Task PlayDefault_VolumeOnPlay_IsTheBaseForIncrement()
    {
        Param(SonosControlLogicFactory.VolumeOnPlay, 25);
        In(SonosControlLogicFactory.PlayDefaultTrigger, true);
        await _play.WaitForCountAsync(1);

        Assert.Equal(26L, Assert.Single(In(SonosControlLogicFactory.VolumeIncrement, true)).Value);
    }

    [Fact]
    public async Task PlayDefault_FalseWhilePlaying_PausesAndStops()
    {
        In(SonosControlLogicFactory.PlayDefaultTrigger, true);
        await _play.WaitForCountAsync(1);

        var result = ToMap(In(SonosControlLogicFactory.PlayDefaultTrigger, false));

        Assert.Equal(2, result.Count);
        Assert.Equal(true, result[SonosControlLogicFactory.PauseOutputStatus]);
        Assert.Equal(false, result[SonosControlLogicFactory.PlayOutputStatus]);
    }

    [Fact]
    public void PlayDefault_AfterPlayPauseTrue_IsIgnored()
    {
        // current behaviour: both play inputs share the last play value
        StartPlaying();

        Assert.Empty(In(SonosControlLogicFactory.PlayDefaultTrigger, true));
    }

    [Fact]
    public void PauseInputState_True_AllowsVolumeChangesWithoutPlayTrigger()
    {
        In(SonosControlLogicFactory.PauseTrigger, true);

        Assert.Equal(1L, Assert.Single(In(SonosControlLogicFactory.VolumeIncrement, true)).Value);
    }

    [Fact]
    public void PauseInputState_False_StopsVolumeChanges()
    {
        StartPlaying();
        In(SonosControlLogicFactory.PauseTrigger, false);

        Assert.Empty(In(SonosControlLogicFactory.VolumeIncrement, true));
    }

    [Fact]
    public void PauseInputState_EmitsNothing()
    {
        Assert.Empty(In(SonosControlLogicFactory.PauseTrigger, true));
        Assert.Empty(In(SonosControlLogicFactory.PauseTrigger, false));
    }

    [Fact(Skip = "BUG: the play state feedback (PauseTrigger input) does not update the last play value (SonosControlLogic.cs:213/265) - after the speaker was paused externally a play=true is ignored")]
    public void PlayPause_TrueAfterExternalPauseFeedback_PlaysAgain()
    {
        StartPlaying();
        In(SonosControlLogicFactory.PauseTrigger, false);

        var result = ToMap(In(SonosControlLogicFactory.PlayPauseTrigger, true));

        Assert.Equal(true, result[SonosControlLogicFactory.PlayOutputStatus]);
    }

    #endregion

    #region next / previous / station

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Next_AnyValue_PulsesTheNextOutput(bool value)
    {
        Assert.Empty(In(SonosControlLogicFactory.Next, value));

        var values = await _next.WaitForCountAsync(2);
        Assert.Equal(new object[] { true, false }, values);
        Assert.Empty(_previous.Values);
    }

    [Fact]
    public async Task Previous_PulsesThePreviousOutput()
    {
        Assert.Empty(In(SonosControlLogicFactory.Previous, true));

        var values = await _previous.WaitForCountAsync(2);
        Assert.Equal(new object[] { true, false }, values);
        Assert.Empty(_next.Values);
    }

    [Theory]
    [InlineData(true)]
    [InlineData("anything")]
    [InlineData(null)]
    public void RadioStationInput_AnyValue_EmitsTheConfiguredStation(object value)
    {
        var change = Assert.Single(In(SonosControlLogicFactory.RadioStationInput, value));

        Assert.Equal(SonosControlLogicFactory.RadioStationOutputValue, change.Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate);
        Assert.Equal(SonosControlLogicFactory.DefaultRadioStation, change.Value);
    }

    [Fact]
    public void RadioStationInput_AfterParameterChange_EmitsTheNewStation()
    {
        Param(SonosControlLogicFactory.RadioStation, "s42");

        Assert.Equal("s42", Assert.Single(In(SonosControlLogicFactory.RadioStationInput, true)).Value);
    }

    [Fact]
    public async Task Start_ReadsTheParameters()
    {
        GetLogicInterfaceByTemplate(SonosControlLogicFactory.RadioStation).Value = "s77";
        GetLogicInterfaceByTemplate(SonosControlLogicFactory.MaxVolume).Value = 30;
        await Logic.Start();

        Assert.Equal("s77", Assert.Single(In(SonosControlLogicFactory.RadioStationInput, true)).Value);
        Assert.Equal(30L, Assert.Single(In(SonosControlLogicFactory.Volume, 99)).Value);
    }

    [Fact]
    public void UiOnlyInputs_EmitNothing()
    {
        Assert.Empty(In(SonosControlLogicFactory.TitleInput, "title"));
        Assert.Empty(In(SonosControlLogicFactory.CreatorInput, "creator"));
        Assert.Empty(In(SonosControlLogicFactory.AlbumInput, "album"));
        Assert.Empty(In(SonosControlLogicFactory.AlbumArtUriInput, "http://127.0.0.1/art.png"));
        Assert.Empty(In(SonosControlLogicFactory.ClassInput, "object.item"));
        Assert.Empty(In(SonosControlLogicFactory.DurationInput, "0:03:00"));
        Assert.Empty(In(SonosControlLogicFactory.RelativeTimeInput, "0:01:00"));
    }

    #endregion

    #region play sound

    private void ConfigureSound(int durationMs, int count, long volume = 50)
    {
        Param(SonosControlLogicFactory.PlaySoundDuration, durationMs);
        Param(SonosControlLogicFactory.PlaySoundCount, count);
        Param(SonosControlLogicFactory.PlaySoundVolume, volume);
    }

    [Fact]
    public async Task PlaySound_UrlAndTrigger_PlaysCountTimesWithSoundVolumeAndRestoresTheVolume()
    {
        ConfigureSound(10, 2, 40);
        In(SonosControlLogicFactory.Volume, 20);
        // In() only returns the outputs, the recorders only see what the sound task dispatches
        var volume = _volume;
        var url = _mediaUrl;

        Assert.Empty(In(SonosControlLogicFactory.PlaySoundUrl, "http://127.0.0.1/bell.mp3"));
        Assert.Empty(In(SonosControlLogicFactory.PlaySoundTrigger, true));

        var volumes = await volume.WaitForCountAsync(3);
        Assert.Equal(new object[] { 40L, 40L, 20L }, volumes);

        // not playing before - the sound url is not replaced afterwards
        await Task.Delay(50);
        Assert.Equal(new object[] { "http://127.0.0.1/bell.mp3", "http://127.0.0.1/bell.mp3" }, url.Values);
    }

    [Theory]
    [InlineData(false)]
    [InlineData("true")]
    [InlineData(1)]
    [InlineData(null)]
    public async Task PlaySoundTrigger_NotBooleanTrue_DoesNotPlay(object value)
    {
        ConfigureSound(10, 1);
        In(SonosControlLogicFactory.PlaySoundUrl, "http://127.0.0.1/bell.mp3");

        In(SonosControlLogicFactory.PlaySoundTrigger, value);

        await Task.Delay(50);
        Assert.Empty(_mediaUrl.Values);
        Assert.Empty(_volume.Values);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task PlaySoundTrigger_NoUrl_DoesNotPlay(object url)
    {
        ConfigureSound(10, 1);
        In(SonosControlLogicFactory.PlaySoundUrl, url);

        In(SonosControlLogicFactory.PlaySoundTrigger, true);

        await Task.Delay(50);
        Assert.Empty(_mediaUrl.Values);
    }

    [Fact]
    public async Task PlaySound_Disabled_DoesNotPlay()
    {
        ConfigureSound(10, 1);
        Param(SonosControlLogicFactory.PlaySoundEnabled, false);

        In(SonosControlLogicFactory.PlaySoundUrl, "http://127.0.0.1/bell.mp3");
        In(SonosControlLogicFactory.PlaySoundTrigger, true);
        In(SonosControlLogicFactory.PlayMediaUrlAndStart, "http://127.0.0.1/bell.mp3");

        await Task.Delay(50);
        Assert.Empty(_mediaUrl.Values);
    }

    [Fact]
    public async Task PlaySound_EnabledAgain_Plays()
    {
        ConfigureSound(10, 1);
        Param(SonosControlLogicFactory.PlaySoundEnabled, false);
        Param(SonosControlLogicFactory.PlaySoundEnabled, true);

        In(SonosControlLogicFactory.PlayMediaUrlAndStart, "http://127.0.0.1/bell.mp3");

        await _volume.WaitForCountAsync(2);
        Assert.Equal("http://127.0.0.1/bell.mp3", _mediaUrl.Values.First());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task PlayMediaUrlAndStart_Empty_DoesNotPlay(object url)
    {
        ConfigureSound(10, 1);

        In(SonosControlLogicFactory.PlayMediaUrlAndStart, url);

        await Task.Delay(50);
        Assert.Empty(_mediaUrl.Values);
    }

    [Fact]
    public async Task PlaySound_WhileRunning_SecondStartIsIgnored()
    {
        ConfigureSound(60, 1);

        In(SonosControlLogicFactory.PlayMediaUrlAndStart, "http://127.0.0.1/a.mp3");
        In(SonosControlLogicFactory.PlayMediaUrlAndStart, "http://127.0.0.1/b.mp3");
        In(SonosControlLogicFactory.PlaySoundTrigger, true);

        // sound volume + restored volume of exactly one run
        await _volume.WaitForCountAsync(2);
        await Task.Delay(100);
        Assert.Equal(2, _volume.Count);
        Assert.Single(_mediaUrl.Values);
    }

    [Fact(Skip = "BUG: PlayMediaUrlAndStart while a sound is running is not started but still replaces the url the running sound plays (SonosControlLogic.cs:328, read again at 374)")]
    public async Task PlaySound_WhileRunning_SecondUrlDoesNotChangeTheRunningSound()
    {
        ConfigureSound(60, 2);

        In(SonosControlLogicFactory.PlayMediaUrlAndStart, "http://127.0.0.1/a.mp3");
        In(SonosControlLogicFactory.PlayMediaUrlAndStart, "http://127.0.0.1/b.mp3");

        await _volume.WaitForCountAsync(3);
        Assert.Equal(new object[] { "http://127.0.0.1/a.mp3", "http://127.0.0.1/a.mp3" }, _mediaUrl.Values);
    }

    [Fact]
    public async Task PlaySound_WhilePlaying_RestoresTheMediaFromBeforeTheSound()
    {
        ConfigureSound(80, 1, 45);
        StartPlaying();
        In(SonosControlLogicFactory.Volume, 15);
        In(SonosControlLogicFactory.PlayMediaUrl, "x-rincon-mp3radio://before");
        var volume = _volume;
        var url = _mediaUrl;

        In(SonosControlLogicFactory.PlayMediaUrlAndStart, "http://127.0.0.1/bell.mp3");
        // the media url of the sound itself arrives while the sound is running - it must not become the media to restore
        In(SonosControlLogicFactory.PlayMediaUrl, "http://127.0.0.1/bell.mp3");

        var urls = await url.WaitForCountAsync(2);
        Assert.Equal(new object[] { "http://127.0.0.1/bell.mp3", "x-rincon-mp3radio://before" }, urls);
        Assert.Equal(new object[] { 45L, 15L }, await volume.WaitForCountAsync(2));
    }

    [Fact]
    public async Task PlaySound_WhilePlayingWithoutMedia_DoesNotRestoreAUrl()
    {
        ConfigureSound(10, 1);
        StartPlaying();

        In(SonosControlLogicFactory.PlayMediaUrlAndStart, "http://127.0.0.1/bell.mp3");

        await _volume.WaitForCountAsync(2);
        await Task.Delay(50);
        Assert.Equal(new object[] { "http://127.0.0.1/bell.mp3" }, _mediaUrl.Values);
    }

    [Fact]
    public async Task PlaySound_AfterFinished_CanPlayAgain()
    {
        ConfigureSound(10, 1);

        In(SonosControlLogicFactory.PlayMediaUrlAndStart, "http://127.0.0.1/a.mp3");
        await _volume.WaitForCountAsync(2);
        await Task.Delay(30);

        In(SonosControlLogicFactory.PlayMediaUrlAndStart, "http://127.0.0.1/b.mp3");
        await _volume.WaitForCountAsync(4);

        Assert.Equal(new object[] { "http://127.0.0.1/a.mp3", "http://127.0.0.1/b.mp3" }, _mediaUrl.Values);
    }

    #endregion
}
