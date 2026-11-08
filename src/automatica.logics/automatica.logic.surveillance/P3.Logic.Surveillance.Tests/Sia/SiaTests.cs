using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Logic;
using P3.Logic.Surveillance.Security;
using P3.Logic.Surveillance.Sia;
using P3.Logic.Surveillance.Tests.TestSupport;
using Xunit;
using F = P3.Logic.Surveillance.Sia.SiaLogicFactory;

namespace P3.Logic.Surveillance.Tests.Sia
{
    /// <summary>
    /// A receiver that answers with ACK (or nothing / NAK) and records the decoded frames
    /// </summary>
    internal sealed class FakeReceiver : ISiaTransport
    {
        private readonly SiaAccount _account;
        private readonly byte[] _key;

        public FakeReceiver(SiaAccount account, byte[] key)
        {
            _account = account;
            _key = key;
        }

        public ConcurrentQueue<(string Host, string Frame)> Frames { get; } = new();
        public HashSet<string> Down { get; } = new();
        public bool Nak { get; set; }

        public Task<string> SendAsync(string host, int port, string frame, TimeSpan timeout, CancellationToken token = default)
        {
            Frames.Enqueue((host, frame));
            if (Down.Contains(host))
            {
                return Task.FromResult<string>(null);
            }

            var seq = int.Parse(Regex.Match(frame, "\"[^\"]+\"(\\d{4})").Groups[1].Value);
            if (Nak)
            {
                const string nak = "\"NAK\"0000R0L0A0[]_10:00:00,11-08-2026";
                return Task.FromResult($"\n{SiaProtocol.Crc16(nak):X4}0{nak.Length:X3}{nak}\r");
            }
            return Task.FromResult(SiaProtocol.BuildAck(_account, seq, DateTimeOffset.UtcNow, _key));
        }
    }

    public class SiaTests : SecurityLogicTest<SiaLogicFactory, SiaLogic>
    {
        private const string Key = "000102030405060708090A0B0C0D0E0F";
        private static readonly SiaAccount Account = new("1234", "0", "");

        private FakeReceiver _receiver = new(Account, null);

        protected override SiaLogic Create(ILogicContext context, ISecurityStateStore store) => new(context, store, _receiver);

        private void Wait() => Sut.Sending.GetAwaiter().GetResult();

        private void Setup(string key = "")
        {
            _receiver = new FakeReceiver(Account, SiaProtocol.ParseKey(key));
            // the instance was created with the old receiver
            SetParam(F.ParamServer, "primary:12300");
            SetParam(F.ParamBackupServer, "backup:12300");
            SetParam(F.ParamAccount, "1234");
            SetParam(F.ParamKey, key);
            SetParam(F.ParamPolling, 0);
            SetParam(F.ParamBackupPolling, 0);
            SetParam(F.ParamMinInterval, 0);
            SetParam(F.ParamOnMessages[0], "BA01^Living room");
            SetParam(F.ParamOffMessages[0], "BR01^Living room");
            Restart(settle: true);
        }

        private string Decoded(string frame, string key = null)
        {
            var body = frame.Substring(frame.IndexOf('[') + 1).TrimEnd('\r');
            return key == null ? body : SiaProtocol.Decrypt(body, SiaProtocol.ParseKey(key));
        }

        [Fact]
        public void Crc16_MatchesArcCheckValue()
        {
            // CRC-16/ARC check value of "123456789"
            Assert.Equal(0xBB3D, SiaProtocol.Crc16("123456789"));
        }

        [Fact]
        public void Frame_LayoutAndCrc()
        {
            var time = new DateTimeOffset(2026, 11, 8, 14, 58, 53, TimeSpan.Zero);
            var frame = SiaProtocol.BuildFrame(SiaFormat.SiaDcs, new SiaAccount("101010", "0", "1"), 5, SiaProtocol.EventData(SiaFormat.SiaDcs, "FA01^Kitchen"), time, null);

            const string content = "\"SIA-DCS\"0005R1L0#101010[#101010|NFA01^Kitchen]_14:58:53,11-08-2026";
            Assert.Equal($"\n{SiaProtocol.Crc16(content):X4}0{content.Length:X3}{content}\r", frame);
        }

        [Fact]
        public void EncryptedFrame_DecryptsToPaddedData()
        {
            var key = SiaProtocol.ParseKey(Key);
            var time = new DateTimeOffset(2026, 11, 8, 10, 0, 0, TimeSpan.Zero);
            var frame = SiaProtocol.BuildFrame(SiaFormat.SiaDcs, Account, 7, "NBA01", time, key);

            Assert.Contains("\"*SIA-DCS\"0007L0#1234[", frame);
            var hex = frame.Substring(frame.IndexOf('[') + 1).TrimEnd('\r');
            Assert.Equal(0, hex.Length % 32);
            Assert.Equal("#1234|NBA01]_10:00:00,11-08-2026", SiaProtocol.Decrypt(hex, key));
        }

        [Theory]
        [InlineData("")]
        [InlineData(Key)]
        public void Ack_RoundTrip(string keyHex)
        {
            var key = SiaProtocol.ParseKey(keyHex);
            var response = SiaProtocol.ParseResponse(SiaProtocol.BuildAck(Account, 42, DateTimeOffset.UtcNow, key), key);
            Assert.Equal(SiaResponseKind.Ack, response.Kind);
            Assert.Equal(42, response.Sequence);
        }

        [Fact]
        public void ParseResponse_RejectsBadCrc()
        {
            var ack = SiaProtocol.BuildAck(Account, 1, DateTimeOffset.UtcNow, null);
            var broken = ack.Substring(0, 1) + (ack[1] == '0' ? '1' : '0') + ack.Substring(2);
            Assert.Equal(SiaResponseKind.Invalid, SiaProtocol.ParseResponse(broken, null).Kind);
        }

        [Theory]
        [InlineData("FA01^Kitchen", true)]
        [InlineData("RP", true)]
        [InlineData("fa01", false)]
        [InlineData("FA01 Kitchen", false)]
        [InlineData("FA01^bad]", false)]
        public void EventData_ValidatesSiaSyntax(string message, bool valid)
        {
            if (valid)
            {
                Assert.Equal("N" + message, SiaProtocol.EventData(SiaFormat.SiaDcs, message));
            }
            else
            {
                Assert.Throws<FormatException>(() => SiaProtocol.EventData(SiaFormat.SiaDcs, message));
            }
        }

        [Fact]
        public void EventData_ContactId()
        {
            Assert.Equal("1130 01 015", SiaProtocol.EventData(SiaFormat.ContactId, "113001015"));
            Assert.Throws<FormatException>(() => SiaProtocol.EventData(SiaFormat.ContactId, "FA01"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(Key)]
        public void Trigger_SendsAlarmAndRestore(string key)
        {
            Setup(key);

            Input(F.InTriggers[0], true);
            Wait();
            Input(F.InTriggers[0], false);
            Wait();

            var frames = _receiver.Frames.Select(a => a.Frame).ToList();
            Assert.Equal(2, frames.Count);
            Assert.StartsWith("#1234|NBA01^Living room]", Decoded(frames[0], key == "" ? null : key));
            Assert.StartsWith("#1234|NBR01^Living room]", Decoded(frames[1], key == "" ? null : key));
            Assert.Equal(0, LastNumber(F.OutQueue));
            Assert.True(LastBool(F.OutOk));
            Assert.Equal("BR01^Living room", Last(F.OutLastEvent));
        }

        [Fact]
        public void ValuePlaceholder_And_MessageInput()
        {
            Setup();
            SetParam(F.ParamOnMessages[1], "ZA<v>^Zone");

            Input(F.InTriggers[1], 52);
            Wait();
            Input(F.InMessage, "MA07^Bathroom NM");
            Wait();

            var frames = _receiver.Frames.Select(a => Decoded(a.Frame)).ToList();
            Assert.StartsWith("#1234|NZA52^Zone]", frames[0]);
            Assert.StartsWith("#1234|NMA07^Bathroom NM]", frames[1]);
        }

        [Fact]
        public void PrimaryDown_UsesBackup()
        {
            Setup();
            _receiver.Down.Add("primary");

            Input(F.InTriggers[0], true);
            Wait();

            Assert.Equal(new[] { "primary", "backup" }, _receiver.Frames.Select(a => a.Host));
            Assert.Equal(0, LastNumber(F.OutQueue));
            Assert.Contains(VisuState()["log"]!, e => (string)e["event"] == "sia-sent-backup");
        }

        [Fact]
        public void BothDown_QueuesAndRetriesWithSameSequence_SurvivingRestart()
        {
            Setup();
            _receiver.Down.Add("primary");
            _receiver.Down.Add("backup");

            Input(F.InTriggers[0], true);
            Wait();
            Assert.Equal(1, LastNumber(F.OutQueue));
            Assert.False(LastBool(F.OutOk));
            Assert.Equal("receiver not reachable", Last(F.OutError));

            // the receiver is back before the restart: the queued event is sent by the first tick
            _receiver.Down.Clear();
            Restart();
            Wait();

            Assert.Equal(0, LastNumber(F.OutQueue));
            Assert.True(LastBool(F.OutOk));
            var sequences = _receiver.Frames.Select(a => Regex.Match(a.Frame, "\"SIA-DCS\"(\\d{4})").Groups[1].Value).Distinct();
            Assert.Single(sequences);
        }

        [Fact]
        public void Nak_KeepsEventQueued()
        {
            Setup();
            _receiver.Nak = true;

            Input(F.InTriggers[0], true);
            Wait();

            Assert.Equal(1, LastNumber(F.OutQueue));
            Assert.Contains("NAK", (string)Last(F.OutError));
        }

        [Fact]
        public void Polling_SendsNullMessages()
        {
            Setup();
            SetParam(F.ParamPolling, 90);
            Advance(1);
            Wait();

            var first = Assert.Single(_receiver.Frames);
            Assert.Contains("\"NULL\"", first.Frame);
            Advance(60);
            Wait();
            Assert.Single(_receiver.Frames);
            Advance(31);
            Wait();
            Assert.Equal(2, _receiver.Frames.Count);
        }

        [Fact]
        public void MinInterval_SendsLatestStateLater()
        {
            Setup();
            SetParam(F.ParamMinInterval, 5);

            Input(F.InTriggers[0], true);
            Wait();
            Input(F.InTriggers[0], false);
            Wait();
            Assert.Single(_receiver.Frames);

            Advance(5);
            Wait();
            Assert.Equal(2, _receiver.Frames.Count);
            Assert.Contains("NBR01", _receiver.Frames.Last().Frame);
        }

        [Fact]
        public void InvalidKey_IsReported_NothingSent()
        {
            Setup();
            SetParam(F.ParamKey, "1234");

            Input(F.InTriggers[0], true);
            Wait();

            Assert.Empty(_receiver.Frames);
            Assert.Equal("invalid encryption key", Last(F.OutError));
        }

        [Fact]
        public void ReplayAfterRestart_IsNotSentAgain()
        {
            Setup();
            Input(F.InTriggers[0], true);
            Wait();

            Restart(settle: false);
            Input(F.InTriggers[0], true);
            Advance(11);
            Wait();

            Assert.Single(_receiver.Frames);
        }

        [Fact]
        public async Task TcpTransport_TalksToLocalReceiver()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var received = new TaskCompletionSource<string>();

            var server = Task.Run(async () =>
            {
                using var client = await listener.AcceptTcpClientAsync();
                var stream = client.GetStream();
                var buffer = new byte[1024];
                var text = new StringBuilder();
                while (!text.ToString().Contains('\r'))
                {
                    var read = await stream.ReadAsync(buffer);
                    if (read == 0) break;
                    text.Append(Encoding.ASCII.GetString(buffer, 0, read));
                }
                received.SetResult(text.ToString());
                var ack = SiaProtocol.BuildAck(Account, 3, DateTimeOffset.UtcNow, null);
                await stream.WriteAsync(Encoding.ASCII.GetBytes(ack));
            });

            try
            {
                var frame = SiaProtocol.BuildFrame(SiaFormat.SiaDcs, Account, 3, "NRP", DateTimeOffset.UtcNow, null);
                var answer = await TcpSiaTransport.Instance.SendAsync("127.0.0.1", port, frame, TimeSpan.FromSeconds(5));

                Assert.Equal(frame, await received.Task);
                var response = SiaProtocol.ParseResponse(answer, null);
                Assert.Equal(SiaResponseKind.Ack, response.Kind);
                Assert.Equal(3, response.Sequence);
                await server;
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task TcpTransport_NoReceiver_ReturnsNull()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            Assert.Null(await TcpSiaTransport.Instance.SendAsync("127.0.0.1", port, "x", TimeSpan.FromSeconds(2)));
        }
    }
}
