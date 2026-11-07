using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.Loxone.Miniserver.Driver;
using P3.Driver.Loxone.Miniserver.Driver.Data;
using P3.Driver.Loxone.Miniserver.Driver.Data.Message;
using Xunit;
using static P3.Driver.Loxone.Miniserver.Tests.LoxoneFrames;

namespace P3.Driver.Loxone.Miniserver.Tests
{
    /// <summary>
    /// The real connection against an in-process miniserver on 127.0.0.1 (http + websocket, ephemeral port).
    /// </summary>
    public class MiniserverIntegrationTests : IAsyncLifetime
    {
        private const string StateUuid = "0f86a2fe-0378-3e08-ffff112233445566";

        private FakeMiniserver _server;
        private LoxoneMiniserverConnection _connection;
        private readonly ConcurrentQueue<BinaryMessage> _messages = new ConcurrentQueue<BinaryMessage>();
        private int _closedEvents;
        private int _establishedEvents;

        public async Task InitializeAsync()
        {
            _server = await FakeMiniserver.StartAsync();
        }

        public async Task DisposeAsync()
        {
            try
            {
                _connection?.Close();
            }
            catch
            {
                // already closed
            }
            await _server.DisposeAsync();
        }

        private LoxoneMiniserverConnection CreateConnection(bool useTls = false, int? tlsPort = null, bool allowFallback = true)
        {
            _connection = new LoxoneMiniserverConnection("127.0.0.1", _server.Port, _server.User, _server.Password, NullLogger.Instance,
                useTls, tlsPort ?? _server.Port, allowFallback);
            _connection.OnMessage += (s, e) => _messages.Enqueue(e.Message);
            _connection.OnConnectionClosed += (s, e) => System.Threading.Interlocked.Increment(ref _closedEvents);
            _connection.OnConnectionEstablished += (s, e) => System.Threading.Interlocked.Increment(ref _establishedEvents);
            return _connection;
        }

        private async Task<LoxoneMiniserverConnection> ConnectAuthenticated()
        {
            var connection = CreateConnection();
            Assert.True(await connection.Connect());
            Assert.True(await FakeMiniserver.WaitUntil(() => connection.IsAuthenticated), "the websocket was not authenticated: " + _server.Diagnostics());
            return connection;
        }

        private static Task<LoxoneApiResponse<WriteValueResponse>> WriteUnderGermanCulture(LoxoneMiniserverConnection connection, string uuid, object value)
        {
            var culture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                // the command is built synchronously
                return connection.WriteValue(uuid, value);
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
            }
        }

        [Fact]
        public async Task Connect_PlainTransport_AuthenticatesTheWebSocketWithAToken()
        {
            var connection = CreateConnection();

            Assert.True(await connection.Connect());
            Assert.True(await FakeMiniserver.WaitUntil(() => connection.IsAuthenticated));

            Assert.False(connection.IsTlsActive);
            Assert.Equal(1, _establishedEvents);
            Assert.True(_server.TokenHashValid, "the gettoken hash does not match user, password and key");

            var commands = _server.DecryptedCommands.ToArray();
            Assert.Equal($"jdev/sys/getkey2/{_server.User}", commands[0]);
            Assert.StartsWith("jdev/sys/gettoken/", commands[1]);
            Assert.EndsWith($"/{_server.User}/4/{LoxoneMiniserverConnection.LoxoneUuid}/AutomaticaCoreServer", commands[1]);
            Assert.Equal("jdev/sps/enablebinstatusupdate", commands[2]);
        }

        [Fact]
        public async Task Connect_PlainTransport_PasswordIsNeverSentInClearOverTheWebSocket()
        {
            await ConnectAuthenticated();

            Assert.DoesNotContain(_server.WebSocketMessages, m => m.Contains(_server.Password));
            Assert.DoesNotContain(_server.DecryptedCommands, m => m.Contains(_server.Password));
            Assert.All(_server.WebSocketMessages.Skip(1), m => Assert.StartsWith("jdev/sys/", m));
        }

        [Fact]
        public async Task Connect_LoadsLoxAppDataWithBasicAuth()
        {
            _server.LoxAppJson = "{\"msInfo\":{},\"rooms\":{},\"cats\":{},\"controls\":{\"0f86a2fe-0378-3e08-ffff112233445566\":{\"name\":\"Light\",\"type\":\"Switch\"}}}";

            var connection = await ConnectAuthenticated();

            Assert.Equal("Light", connection.LoxData.Controls.Single().Value.Name);
            var request = _server.HttpRequests.First(a => a.RawTarget == "/data/LoxAPP3.json");
            var expected = "Basic " + Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_server.User}:{_server.Password}"));
            Assert.Equal(expected, request.Authorization);
        }

        [Fact]
        public async Task Connect_LoxAppUnauthorized_ReturnsFalseWithoutOpeningTheWebSocket()
        {
            _server.LoxAppStatusCode = 401;
            var connection = CreateConnection();

            Assert.False(await connection.Connect());

            Assert.False(connection.IsAuthenticated);
            Assert.False(_server.WebSocketConnected);
            Assert.Empty(_server.WebSocketMessages);
            Assert.Equal(0, _establishedEvents);
        }

        [Fact]
        public async Task Connect_TlsNotAvailable_FallsBackToPlainHttp()
        {
            // the https port answers plain http only, the TLS handshake fails
            var connection = CreateConnection(useTls: true, tlsPort: _server.Port, allowFallback: true);
            Assert.True(connection.IsTlsActive);

            Assert.True(await connection.Connect());
            Assert.True(await FakeMiniserver.WaitUntil(() => connection.IsAuthenticated));

            Assert.False(connection.IsTlsActive);
            Assert.Single(_server.HttpRequests, a => a.RawTarget == "/data/LoxAPP3.json");
        }

        [Fact]
        public async Task Connect_TlsNotAvailable_FallbackDisabled_ReturnsFalse()
        {
            var connection = CreateConnection(useTls: true, tlsPort: _server.Port, allowFallback: false);

            Assert.False(await connection.Connect());

            Assert.True(connection.IsTlsActive);
            Assert.False(connection.IsAuthenticated);
            Assert.Empty(_server.HttpRequests);
            Assert.False(_server.WebSocketConnected);
        }

        [Fact]
        public async Task LoadLoxAppData_TlsNotAvailable_FallbackDisabled_Throws()
        {
            var connection = CreateConnection(useTls: true, tlsPort: _server.Port, allowFallback: false);

            await Assert.ThrowsAsync<System.Net.Http.HttpRequestException>(() => connection.LoadLoxAppData());
            Assert.Empty(_server.HttpRequests);
        }

        [Fact]
        public async Task WriteValue_Authenticated_SendsTheEncryptedCommandOverTheWebSocket()
        {
            var connection = await ConnectAuthenticated();

            var result = await WriteUnderGermanCulture(connection, StateUuid, 21.5);

            Assert.Equal(200, result.Data.Code);
            Assert.Equal($"jdev/sps/io/{StateUuid}/21.5", result.Data.Control);
            Assert.True(await FakeMiniserver.WaitUntil(() => _server.DecryptedCommands.Contains($"jdev/sps/io/{StateUuid}/21.5")));

            // no credentials over http for the write
            Assert.DoesNotContain(_server.HttpRequests, a => a.RawTarget.StartsWith("/jdev/sps/io/"));
            // enc: the miniserver does not answer in encrypted form
            Assert.Contains(_server.WebSocketMessages, a => a.StartsWith("jdev/sys/enc/"));
        }

        [Theory]
        [InlineData(true, "on")]
        [InlineData(false, "off")]
        public async Task WriteValue_AuthenticatedBool_SendsOnOff(bool value, string expected)
        {
            var connection = await ConnectAuthenticated();

            await connection.WriteValue(StateUuid, value);

            Assert.True(await FakeMiniserver.WaitUntil(() => _server.DecryptedCommands.Contains($"jdev/sps/io/{StateUuid}/{expected}")));
        }

        [Fact]
        public async Task WriteValue_AuthenticatedSpecialCharacters_AreUrlEncodedInTheCommand()
        {
            var connection = await ConnectAuthenticated();

            await connection.WriteValue(StateUuid, "a b/../c&d");

            Assert.True(await FakeMiniserver.WaitUntil(() => _server.DecryptedCommands.Contains($"jdev/sps/io/{StateUuid}/a+b%2F..%2Fc%26d")));
        }

        [Fact]
        public async Task WriteValue_NotAuthenticated_UsesHttpWithInvariantNumber()
        {
            var connection = CreateConnection();

            var result = await WriteUnderGermanCulture(connection, StateUuid, 0.5);

            Assert.Equal(200, result.Data.Code);
            var request = Assert.Single(_server.HttpRequests);
            Assert.Equal("GET", request.Method);
            Assert.Equal($"/jdev/sps/io/{StateUuid}/0.5", request.RawTarget);
        }

        [Fact]
        public async Task WriteValue_NotAuthenticated_UuidAndValueAreUrlEncoded_NoPathTraversal()
        {
            var connection = CreateConnection();

            await connection.WriteValue("my uuid/..", "1/../../dev/sys/reboot");

            var request = Assert.Single(_server.HttpRequests);
            Assert.Equal("/jdev/sps/io/my+uuid%2F../1%2F..%2F..%2Fdev%2Fsys%2Freboot", request.RawTarget);
            Assert.Equal(6, request.RawTarget.Split('/').Length);
        }

        [Fact]
        public async Task WriteValue_NotAuthenticated_ReturnsTheMiniserverCode()
        {
            _server.WriteResponseCode = 500;
            var connection = CreateConnection();

            var result = await connection.WriteValue(StateUuid, 1);

            Assert.Equal(500, result.Data.Code);
        }

        [Fact]
        public async Task BinaryFrames_HeaderAndPayloadInSeparateWebSocketMessages_RaiseOnMessage()
        {
            await ConnectAuthenticated();

            var payload = ValueState(UuidA, 23.5);
            await _server.SendBinaryAsync(EstimatedHeader(HeaderIdentifier.EventTableOfValueStates, 0));
            await _server.SendBinaryAsync(HeaderBytes(HeaderIdentifier.EventTableOfValueStates, (uint)payload.Length));
            await _server.SendBinaryAsync(payload);

            Assert.True(await FakeMiniserver.WaitUntil(() => _messages.Count == 1));
            Assert.Equal(23.5, SingleValue(_messages.Single()));
        }

        [Fact]
        public async Task BinaryFrames_TextAndDaytimerTables_RaiseOnMessage()
        {
            await ConnectAuthenticated();

            await _server.SendBinaryAsync(Message(HeaderIdentifier.EventTableOfTextStates, TextState(UuidA, UuidB, "on air")));
            await _server.SendBinaryAsync(Message(HeaderIdentifier.EventTableOfDaytimerStates, DaytimerBytes(UuidC, 1, DaytimerEntryBytes(0, 0, 60, 0, 2))));

            Assert.True(await FakeMiniserver.WaitUntil(() => _messages.Count == 2));
            var messages = _messages.ToArray();
            Assert.Equal("on air", Assert.IsType<EventTableOfTextStates>(messages[0]).Values.Single().Value.Text);
            Assert.Equal(2, Assert.IsType<EventTableOfDaytimerStates>(messages[1]).Values.Single().Value.DaytimerEntries.Single().Value);
        }

        [Fact]
        public async Task Connect_LeftoversOfThePreviousStream_AreClearedOnReconnect()
        {
            var connection = CreateConnection();

            // a header of the previous connection whose payload never arrived
            connection.ProcessData(HeaderBytes(HeaderIdentifier.EventTableOfValueStates, 24));

            Assert.True(await connection.Connect());
            Assert.True(await FakeMiniserver.WaitUntil(() => connection.IsAuthenticated));

            await _server.SendBinaryAsync(ValueMessage((UuidA, 7)));

            Assert.True(await FakeMiniserver.WaitUntil(() => _messages.Count >= 1));
            Assert.Equal(7, SingleValue(Assert.Single(_messages)));
        }

        [Fact]
        public async Task WebSocketClosedByMiniserver_RaisesOnConnectionClosed_AndWritesFallBackToHttp()
        {
            var connection = await ConnectAuthenticated();

            await _server.CloseWebSocketAsync();

            Assert.True(await FakeMiniserver.WaitUntil(() => _closedEvents > 0), "OnConnectionClosed was not raised");
            Assert.False(connection.IsAuthenticated);

            await connection.WriteValue(StateUuid, 3);
            Assert.Contains(_server.HttpRequests, a => a.RawTarget == $"/jdev/sps/io/{StateUuid}/3");
        }

        [Fact]
        public async Task Close_DisconnectsTheWebSocket()
        {
            var connection = await ConnectAuthenticated();
            Assert.True(_server.WebSocketConnected);

            connection.Close();

            Assert.True(await FakeMiniserver.WaitUntil(() => !_server.WebSocketConnected));
        }
    }
}
