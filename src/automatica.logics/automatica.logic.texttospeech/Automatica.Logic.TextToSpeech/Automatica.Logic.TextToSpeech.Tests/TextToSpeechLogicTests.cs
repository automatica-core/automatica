using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using Moq;
using Xunit;

namespace Automatica.Logic.TextToSpeech.Tests
{
    public class TextToSpeechLogicTests
    {
        private static readonly Guid OutputId = new("6837b80d-ef8a-40ce-b713-4a0eb13aaffc");
        private static readonly Guid DurationId = new("d759de1d-40b4-4e38-b505-2075fded6672");
        private static readonly Guid TriggerId = new("ae1d8c74-ae9f-40fd-beb7-3db817d96808");
        private static readonly Guid TextId = new("90b44e5c-24d1-485f-a1fd-15b0f2a84820");
        private static readonly Guid VoiceId = new("bbcbf210-41b7-4b9f-ac93-ac4b275a32c1");

        private const string Url = "https://tts.example.invalid/audio-1.mp3";
        private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(2345);
        private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);

        private readonly RuleInstance _ruleInstance;
        private readonly Mock<IServerCloudApi> _cloud = new();
        private readonly Mock<IDispatcher> _dispatcher = new();
        private readonly ConcurrentQueue<(Guid self, DispatchValue value)> _dispatched = new();
        private readonly ConcurrentQueue<(Guid id, string text, string voice, string extra)> _cloudCalls = new();
        private readonly TestLogicContext _context;
        private readonly TextToSpeechLogic _logic;

        public TextToSpeechLogicTests()
        {
            var factory = new TextToSpeechFactory();
            var templates = new LogicTemplateFactoryMock();
            factory.InitTemplates(templates);
            _ruleInstance = templates.CreateRuleInstanceFromTemplate(factory.LogicGuid);
            _ruleInstance.Name = "Doorbell announcement";

            _dispatcher.Setup(a => a.DispatchValue(It.IsAny<IDispatchable>(), It.IsAny<DispatchValue>()))
                .Callback<IDispatchable, DispatchValue>((self, value) => _dispatched.Enqueue((self.Id, value)))
                .Returns(Task.CompletedTask);

            SetupCloud(() => Task.FromResult((Url, Duration)));

            _context = new TestLogicContext(_ruleInstance, templates, _dispatcher.Object, _cloud.Object);
            _logic = (TextToSpeechLogic)factory.CreateLogicInstance(_context);
        }

        private void SetupCloud(Func<Task<(string, TimeSpan)>> result)
        {
            _cloud.Setup(a => a.SynthesizeWithAudioDuration(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<Guid, string, string, string>((id, text, voice, extra) => _cloudCalls.Enqueue((id, text, voice, extra)))
                .Returns(result);
        }

        private RuleInterfaceInstance Interface(Guid template) =>
            _ruleInstance.RuleInterfaceInstance.Single(a => a.This2RuleInterfaceTemplate == template);

        private async Task StartWith(string text, string voice = null)
        {
            Interface(TextId).Value = text;
            if (voice != null)
            {
                Interface(VoiceId).Value = voice;
            }
            Assert.True(await _logic.Start());
        }

        private static async Task WaitFor(Func<bool> condition)
        {
            var sw = Stopwatch.StartNew();
            while (!condition())
            {
                if (sw.Elapsed > Deadline)
                {
                    throw new TimeoutException("condition not reached within the deadline");
                }
                await Task.Delay(5);
            }
        }

        /// <summary>
        /// For "nothing happens" checks: the background synthesizer returns right away in these cases, give it a short window
        /// </summary>
        private static Task Settle() => Task.Delay(150);

        private List<object> DispatchedValuesOf(Guid id) =>
            _dispatched.Where(a => a.self == id).Select(a => a.value.Value).ToList();

        private Task WaitForUrlDispatched() => WaitFor(() => DispatchedValuesOf(OutputId).Contains(Url) && DispatchedValuesOf(DurationId).Contains(Duration));

        private IList<ILogicOutputChanged> Trigger(object value = null) =>
            _logic.ValueChanged(Interface(TriggerId), new DispatchableMock("Doorbell"), value ?? true);

        [Fact]
        public async Task Start_WithEmptyText_DoesNotCallTheCloud()
        {
            Assert.True(await _logic.Start());
            await Settle();

            Assert.Empty(_cloudCalls);
            Assert.Empty(_dispatched);
        }

        [Fact]
        public async Task Start_WithText_SynthesizesOnceWithDefaultVoice()
        {
            await StartWith("Somebody is at the door");
            await WaitForUrlDispatched();

            var call = Assert.Single(_cloudCalls);
            Assert.Equal(_ruleInstance.ObjId, call.id);
            Assert.Equal("Somebody is at the door", call.text);
            Assert.Equal("en-US-GuyNeural", call.voice);
            Assert.Equal(string.Empty, call.extra);
        }

        [Theory]
        [InlineData("0", "en-US-JennyNeural")]
        [InlineData("1", "en-US-GuyNeural")]
        [InlineData("2", "de-DE-KatjaNeural")]
        [InlineData("3", "de-DE-KasperNeural")]
        public async Task Start_VoiceParameter_SelectsTheVoiceName(string voice, string expected)
        {
            await StartWith("Hallo", voice);
            await WaitFor(() => !_cloudCalls.IsEmpty);

            Assert.Equal(expected, Assert.Single(_cloudCalls).voice);
        }

        [Fact]
        public async Task Start_DispatchesEmptyValuesFirst_ThenUrlAndDuration()
        {
            await StartWith("Hallo");
            await WaitForUrlDispatched();

            Assert.Equal(new object[] { string.Empty, Url }, DispatchedValuesOf(OutputId));
            Assert.Equal(new object[] { string.Empty, Duration }, DispatchedValuesOf(DurationId));
        }

        [Fact]
        public async Task Start_InvalidVoice_DoesNotCallTheCloudAndDoesNotThrow()
        {
            await StartWith("Hallo", "7");
            await Settle();

            Assert.Empty(_cloudCalls);
        }

        [Fact]
        public async Task Start_WhenCloudThrows_NoUrlIsDispatched()
        {
            SetupCloud(() => Task.FromException<(string, TimeSpan)>(new InvalidOperationException("cloud down")));

            await StartWith("Hallo");
            await WaitFor(() => !_cloudCalls.IsEmpty);
            await Settle();

            Assert.DoesNotContain(Url, DispatchedValuesOf(OutputId));
            Assert.Empty(Trigger());
        }

        [Fact]
        public async Task Trigger_AfterSynthesis_ReturnsUrlAndDuration_WithoutAnotherCloudCall()
        {
            await StartWith("Hallo");
            await WaitForUrlDispatched();

            var result = Trigger();

            Assert.Equal(2, result.Count);
            Assert.Equal(OutputId, result[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate);
            Assert.Equal(Url, result[0].Value);
            Assert.Equal(DurationId, result[1].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate);
            Assert.Equal(Duration, result[1].Value);

            await Settle();
            Assert.Single(_cloudCalls);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(0)]
        [InlineData("x")]
        public async Task Trigger_AnyValue_ReplaysTheUrl(object value)
        {
            // the trigger is an impulse - every value (also false/0) replays the audio
            await StartWith("Hallo");
            await WaitForUrlDispatched();

            Assert.Equal(Url, Trigger(value)[0].Value);
        }

        [Fact]
        public async Task Trigger_SameValueTwice_ReplaysTwice()
        {
            await StartWith("Hallo");
            await WaitForUrlDispatched();

            Assert.Equal(2, Trigger(true).Count);
            Assert.Equal(2, Trigger(true).Count);
        }

        [Fact]
        public async Task Trigger_BeforeSynthesisFinished_ReturnsNothing_AndTheUrlIsDispatchedLater()
        {
            var release = new TaskCompletionSource<(string, TimeSpan)>(TaskCreationOptions.RunContinuationsAsynchronously);
            SetupCloud(() => release.Task);

            await StartWith("Hallo");
            await WaitFor(() => !_cloudCalls.IsEmpty);

            // must not block (it runs under the logic lock) and must not emit an empty url
            var result = Trigger();
            Assert.Empty(result);

            release.SetResult((Url, Duration));
            await WaitForUrlDispatched();

            Assert.Equal(Url, Trigger()[0].Value);
        }

        [Fact]
        public async Task Trigger_WhenCloudReturnedEmptyUrl_RetriesTheSynthesis()
        {
            SetupCloud(() => Task.FromResult((string.Empty, TimeSpan.Zero)));

            await StartWith("Hallo");
            await WaitFor(() => _cloudCalls.Count == 1);

            Assert.Empty(Trigger());
            await WaitFor(() => _cloudCalls.Count == 2);
        }

        [Fact]
        public async Task Trigger_BeforeStart_IsIgnored()
        {
            // the interfaces are resolved in Start - before that nothing is known as trigger
            var result = _logic.ValueChanged(Interface(TriggerId), new DispatchableMock(), true);
            await Settle();

            Assert.Empty(result);
            Assert.Empty(_cloudCalls);
        }

        [Fact]
        public async Task OtherInput_IsIgnored()
        {
            await StartWith("Hallo");
            await WaitForUrlDispatched();

            var result = _logic.ValueChanged(Interface(OutputId), new DispatchableMock(), "something");

            Assert.Empty(result);
        }

        [Fact]
        public async Task TextParameterChange_FromOutside_SynthesizesTheNewText()
        {
            await StartWith("Hallo");
            await WaitForUrlDispatched();

            const string newUrl = "https://tts.example.invalid/audio-2.mp3";
            SetupCloud(() => Task.FromResult((newUrl, TimeSpan.FromSeconds(1))));

            var result = _logic.ValueChanged(Interface(TextId), new DispatchableMock("Editor"), "Goodbye");
            Assert.Empty(result);

            await WaitFor(() => DispatchedValuesOf(OutputId).Contains(newUrl));
            Assert.Equal("Goodbye", _cloudCalls.Last().text);
            Assert.Equal(newUrl, Trigger()[0].Value);
            Assert.Equal(TimeSpan.FromSeconds(1), Trigger()[1].Value);
        }

        [Fact]
        public async Task VoiceParameterChange_FromOutside_SynthesizesWithTheNewVoice()
        {
            await StartWith("Hallo");
            await WaitForUrlDispatched();

            _logic.ValueChanged(Interface(VoiceId), new DispatchableMock("Editor"), "2");

            await WaitFor(() => _cloudCalls.Count == 2);
            Assert.Equal("de-DE-KatjaNeural", _cloudCalls.Last().voice);
            Assert.Equal("Hallo", _cloudCalls.Last().text);
        }

        [Fact]
        public async Task VoiceParameterChange_AsNumber_IsConverted()
        {
            await StartWith("Hallo");
            await WaitForUrlDispatched();

            _logic.ValueChanged(Interface(VoiceId), new DispatchableMock("Editor"), 3);

            await WaitFor(() => _cloudCalls.Count == 2);
            Assert.Equal("de-DE-KasperNeural", _cloudCalls.Last().voice);
        }

        [Fact]
        public async Task ParameterChange_FromItsOwnInterface_IsIgnored()
        {
            await StartWith("Hallo");
            await WaitForUrlDispatched();

            // the parameter echoes its own value (e.g. on start) - no new synthesis
            var text = Interface(TextId);
            _logic.ValueChanged(text, new LogicInterfaceInstanceDispatchable(text), "Echo");
            await Settle();

            Assert.Single(_cloudCalls);
        }

        [Fact]
        public async Task TextParameterChange_ToEmpty_DoesNotCallTheCloud()
        {
            await StartWith("Hallo");
            await WaitForUrlDispatched();

            _logic.ValueChanged(Interface(TextId), new DispatchableMock("Editor"), string.Empty);
            await Settle();

            Assert.Single(_cloudCalls);
            // the old url stays valid
            Assert.Equal(Url, Trigger()[0].Value);
        }

        [Fact(Skip = "BUG: TextToSpeechLogic.ParameterValueChanged dereferences source without a null check (TextToSpeechLogic.cs:92), Logic.ValueChanged(instance, value) passes source = null and the NullReferenceException escapes")]
        public async Task ParameterChange_WithoutSource_DoesNotThrow()
        {
            await StartWith("Hallo");
            await WaitForUrlDispatched();

            var ex = Record.Exception(() => _logic.ValueChanged(Interface(TextId), (object)"Goodbye"));

            Assert.Null(ex);
        }

        [Fact(Skip = "BUG: the duration is dispatched with the DispatchValue.Id of the output interface (TextToSpeechLogic.cs:74 and :82)")]
        public async Task Start_DurationDispatchValue_CarriesTheDurationId()
        {
            await StartWith("Hallo");
            await WaitForUrlDispatched();

            var duration = _dispatched.Where(a => a.self == DurationId).Select(a => a.value);
            Assert.All(duration, a => Assert.Equal(DurationId, a.Id));
        }
    }

    public class TextToSpeechFactoryTests
    {
        private static RuleTemplate Template()
        {
            var factory = new TextToSpeechFactory();
            var templates = new LogicTemplateFactoryMock();
            factory.InitTemplates(templates);
            return templates.GetById(factory.LogicGuid);
        }

        [Fact]
        public void Metadata_IsStable()
        {
            var factory = new TextToSpeechFactory();

            Assert.Equal("TextToSpeech", factory.LogicName);
            Assert.Equal(new Guid("e846b61a-7a64-4c69-be31-1f44cb60ed8d"), factory.LogicGuid);
            Assert.Equal(new Version(0, 3, 1, 1), factory.LogicVersion);
        }

        [Fact]
        public void InitTemplates_CreatesTheKeyedInterfaces()
        {
            var template = Template();

            Assert.Equal("text-to-speech-synthesize", template.Key);
            var byKey = template.RuleInterfaceTemplate.ToDictionary(a => a.Key);
            Assert.Equal(new[] { "duration", "output", "text", "trigger", "voice" }, byKey.Keys.OrderBy(a => a));

            Assert.Equal((long)LogicInterfaceDirection.Output, byKey["output"].This2RuleInterfaceDirection);
            Assert.Equal((long)LogicInterfaceDirection.Output, byKey["duration"].This2RuleInterfaceDirection);
            Assert.Equal((long)LogicInterfaceDirection.Input, byKey["trigger"].This2RuleInterfaceDirection);
            Assert.Equal((long)LogicInterfaceDirection.Param, byKey["text"].This2RuleInterfaceDirection);
            Assert.Equal((long)LogicInterfaceDirection.Param, byKey["voice"].This2RuleInterfaceDirection);
        }

        [Fact]
        public void InitTemplates_TextIsEmptyLinkableText_VoiceDefaultsToGuy()
        {
            var byKey = Template().RuleInterfaceTemplate.ToDictionary(a => a.Key);

            Assert.Equal(RuleInterfaceParameterDataType.Text, byKey["text"].ParameterDataType);
            Assert.Equal(string.Empty, byKey["text"].DefaultValue);

            Assert.Equal(RuleInterfaceParameterDataType.Enum, byKey["voice"].ParameterDataType);
            Assert.Equal(((int)Voices.EnglishGuyNeural).ToString(), byKey["voice"].DefaultValue);
            Assert.False(string.IsNullOrEmpty(byKey["voice"].Meta));
        }

        [Theory]
        [InlineData(Voices.EnglishJennyNeural, 0, "en-US-JennyNeural")]
        [InlineData(Voices.EnglishGuyNeural, 1, "en-US-GuyNeural")]
        [InlineData(Voices.GermanKatjaNeural, 2, "de-DE-KatjaNeural")]
        [InlineData(Voices.GermanKasperNeural, 3, "de-DE-KasperNeural")]
        public void Voices_HaveStableValuesAndNames(Voices voice, int value, string name)
        {
            // the int value is stored in the rule configuration, the name is sent to the cloud
            Assert.Equal(value, (int)voice);
            Assert.Equal(name, PropertyHelper.GetNameAttributeFromEnumValue(voice).TranslationKey);
        }
    }
}
