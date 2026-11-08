using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Logic;
using P3.Logic.Surveillance.Caller;
using P3.Logic.Surveillance.Security;
using P3.Logic.Surveillance.Tests.TestSupport;
using Xunit;
using F = P3.Logic.Surveillance.Caller.CallerLogicFactory;

namespace P3.Logic.Surveillance.Tests.Caller
{
    internal sealed class FakeTelephony : ITelephonyProvider
    {
        public ConcurrentQueue<CallRequest> Calls { get; } = new();
        public bool Fail { get; set; }
        public string Name => "fake";

        public Task<CallResult> PlaceCallAsync(CallRequest request, CancellationToken token = default)
        {
            Calls.Enqueue(request);
            return Task.FromResult(Fail ? CallResult.Failed("busy") : CallResult.Ok("queued"));
        }
    }

    public class CallerTests : SecurityLogicTest<CallerLogicFactory, CallerLogic>
    {
        private readonly FakeTelephony _phone = new();

        protected override CallerLogic Create(ILogicContext context, ISecurityStateStore store) => new(context, store, _ => _phone);

        private void Wait() => Sut.Calls.GetAwaiter().GetResult();

        private void Setup(string numbers = "0043 664 1234567", string message = "Alarm in <v1>, <v2.1> degrees")
        {
            SetParam(F.ParamNumbers, numbers);
            SetParam(F.ParamMessage, message);
            SetParam(F.ParamLanguage, "de-DE");
        }

        [Fact]
        public void Trigger_CallsWithPlaceholders()
        {
            Setup();
            Start();
            Input(F.InValues[0], "Kitchen");
            Input(F.InValues[1], 21.46);

            Input(F.InTrigger, true);
            Wait();

            var call = Assert.Single(_phone.Calls);
            Assert.Equal("+436641234567", call.To);
            Assert.Equal("Alarm in Kitchen, 21.5 degrees", call.Message);
            Assert.Equal("de-DE", call.Language);
            Assert.Equal(1, LastNumber(F.OutCalls));
            Assert.False(LastBool(F.OutError));
        }

        [Fact]
        public void SeveralNumbers_NumberFromValue()
        {
            Setup("+4312345678; <v3>");
            Start();
            Input(F.InValues[2], "0049301234567");

            Pulse(F.InTrigger);
            Wait();

            Assert.Equal(new[] { "+4312345678", "+49301234567" }, _phone.Calls.Select(a => a.To));
        }

        [Fact]
        public void InvalidNumber_IsReportedMasked_NotCalled()
        {
            Setup("12ab, +431234567");
            Start();

            Pulse(F.InTrigger);
            Wait();

            Assert.Single(_phone.Calls);
            Assert.Contains(Newtonsoft.Json.Linq.JObject.FromObject(Sut.GetDataForVisu())["log"]!, e => (string)e["event"] == "call-invalid-number" && (string)e["detail"] == "***2ab");
        }

        [Fact]
        public void Log_NeverContainsFullNumberOrMessage()
        {
            Setup(message: "secret door code 4711");
            Start();
            Pulse(F.InTrigger);
            Wait();

            var raw = Store.Raw(Sut.Id);
            Assert.DoesNotContain("1234567", raw);
            Assert.DoesNotContain("4711", raw);
            Assert.Contains("***567", raw);
        }

        [Fact]
        public void TriggerDelay_WaitsForValues()
        {
            Setup();
            SetParam(F.ParamTriggerDelay, 1000);
            Start();

            Input(F.InTrigger, true);
            Input(F.InValues[0], "Hall");
            Input(F.InValues[1], 0);
            Assert.Empty(_phone.Calls);

            Advance(1);
            Wait();
            Assert.Equal("Alarm in Hall, 0.0 degrees", Assert.Single(_phone.Calls).Message);
        }

        [Fact]
        public void UpdateInterval_RepeatsWhileTriggerIsOn()
        {
            Setup();
            SetParam(F.ParamUpdateInterval, 60);
            Start();

            Input(F.InTrigger, true);
            Wait();
            Advance(60);
            Wait();
            Assert.Equal(2, _phone.Calls.Count);

            Input(F.InTrigger, false);
            Advance(60);
            Wait();
            Assert.Equal(2, _phone.Calls.Count);
        }

        [Fact]
        public void RateLimit_DropsCallsOverOneHundredPerHour()
        {
            Setup();
            Start();

            for (var i = 0; i < CallerLogic.MaxCallsPerHour + 5; i++)
            {
                Pulse(F.InTrigger);
                Wait();
            }

            Assert.Equal(CallerLogic.MaxCallsPerHour, _phone.Calls.Count);
            Assert.True(LastBool(F.OutError));

            Advance(3601);
            Pulse(F.InTrigger);
            Wait();
            Assert.Equal(CallerLogic.MaxCallsPerHour + 1, _phone.Calls.Count);
        }

        [Fact]
        public void RateLimit_SurvivesRestart()
        {
            Setup();
            Start();
            for (var i = 0; i < 3; i++)
            {
                Pulse(F.InTrigger);
                Wait();
            }

            Restart();

            Assert.Equal(3, LastNumber(F.OutCalls));
        }

        [Fact]
        public void FailedCall_SetsError()
        {
            Setup();
            _phone.Fail = true;
            Start();

            Pulse(F.InTrigger);
            Wait();

            Assert.True(LastBool(F.OutError));
            Assert.Contains("busy", (string)Last(F.OutResult));
        }

        [Fact]
        public void Off_PreventsCalls()
        {
            Setup();
            Start();
            Input(F.InOff, true);
            Pulse(F.InTrigger);
            Wait();
            Assert.Empty(_phone.Calls);
        }

        [Fact]
        public void ReplayedTriggerAfterStart_DoesNotCall()
        {
            Setup();
            Start(settle: false);
            Input(F.InTrigger, true);
            Wait();
            Assert.Empty(_phone.Calls);
        }

        [Theory]
        [InlineData("0043 664 1234567", "+436641234567")]
        [InlineData("+49-30/1234567", "+49301234567")]
        [InlineData("0664 1234567", null)]
        [InlineData("", null)]
        public void PhoneNumbers_Normalize(string input, string expected) => Assert.Equal(expected, PhoneNumbers.Normalize(input));

        [Fact]
        public void Twilio_TwiML_EscapesText()
        {
            Assert.Equal("<Response><Say language=\"de-DE\" loop=\"2\">Fire &amp; smoke &lt;now&gt;</Say></Response>",
                TwilioTelephonyProvider.TwiML("Fire & smoke <now>", "de-DE"));
        }

        [Fact]
        public async Task Twilio_NotConfigured_FailsWithoutRequest()
        {
            var result = await new TwilioTelephonyProvider("", "", "").PlaceCallAsync(new CallRequest("+431234567", "x", "de-DE"));
            Assert.False(result.Success);
        }
    }
}
