using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using P3.Driver.HomeKit.Hap.Controllers;
using P3.Driver.HomeKit.Hap.TlvData;
using P3.Driver.HomeKit.Http;

namespace P3.Driver.HomeKit.Hap
{
    public class PairSetupCompleteEventArgs : System.EventArgs
    {
        public PairSetupCompleteEventArgs(string ltsk, string ltpk)
        {
            Ltsk = ltsk;
            Ltpk = ltpk;
        }
        /// <summary>
        /// Accessory long-term secret key (hex)
        /// </summary>
        public string Ltsk { get; }

        /// <summary>
        /// Serialized controller pairings (see <see cref="HapPairingStore"/>)
        /// </summary>
        public string Ltpk { get; }
    }

    internal sealed class HapResponse
    {
        public HapResponse(string contentType, byte[] body, int statusCode = 200, string reason = "OK")
        {
            ContentType = contentType;
            Body = body ?? Array.Empty<byte>();
            StatusCode = statusCode;
            Reason = reason;
        }

        public string ContentType { get; }
        public byte[] Body { get; }
        public int StatusCode { get; }
        public string Reason { get; }
    }

    internal class HapMiddleware
    {
        internal const string PairingContentType = "application/pairing+tlv8";
        internal const string HapJsonContentType = "application/hap+json";

        /// <summary>
        /// HAP status code "Request denied due to insufficient privileges"
        /// </summary>
        internal const int HapStatusInsufficientPrivileges = -70401;

        /// <summary>
        /// HAP status code "Invalid value in request"
        /// </summary>
        internal const int HapStatusInvalidValue = -70410;

        internal static readonly TimeSpan PairSetupTimeout = TimeSpan.FromSeconds(60);

        private readonly ILogger _logger;
        private readonly string _pairCode;
        private readonly HomeKitServer _homeKitServer;
        private readonly HapPairingStore _pairingStore;

        private readonly object _pairSetupLock = new object();
        private PairSetupController _pairController;
        private string _pairControllerConnection;
        private DateTime _pairControllerStarted;

        public TlvParser TlvParser { get; }

        internal Func<DateTime> UtcNow { get; set; } = () => DateTime.UtcNow;
        internal Func<string, PairSetupController> PairSetupControllerFactory { get; set; }

        private readonly ConcurrentDictionary<string, HapSession> _sessions = new ConcurrentDictionary<string, HapSession>();
        internal static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        public HapMiddleware(ILogger logger, string pairCode, HomeKitServer homeKitServer, HapPairingStore pairingStore)
        {
            _logger = logger;
            _pairCode = pairCode;
            _homeKitServer = homeKitServer;
            _pairingStore = pairingStore;

            PairSetupControllerFactory = code => new PairSetupController(_logger, code);

            TlvParser = new TlvParser(logger);
        }

        public HapPairingStore PairingStore => _pairingStore;

        public HapSession GetSessionByClientUsername(string username)
        {
            return _sessions.Values.SingleOrDefault(a => a.ClientUsername == username);
        }

        public HapSession GetSession(string connectionId)
        {
            return _sessions.GetOrAdd(connectionId, _ => new HapSession());
        }

        private HapResponse ReturnError(int responseState, ErrorCodes error)
        {
            _logger.LogDebug($"Return error code: {error} state {responseState}");
            var tlvData = new Tlv();
            tlvData.AddType(Constants.State, responseState);
            tlvData.AddType(Constants.Error, error);
            byte[] output = TlvParser.Serialize(tlvData);

            return new HapResponse(PairingContentType, output);
        }

        private static HapResponse HapStatus(int httpStatus, string reason, int hapStatus)
        {
            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { status = hapStatus }));
            return new HapResponse(HapJsonContentType, body, httpStatus, reason);
        }

        /// <summary>
        /// HTTP 470 "Connection Authorization Required" for requests on a connection that did not complete pair-verify
        /// </summary>
        internal static HapResponse ConnectionAuthorizationRequired()
        {
            return HapStatus(470, "Connection Authorization Required", HapStatusInsufficientPrivileges);
        }

        public HapResponse Invoke(string connectionId, string url, string method, byte[] inputData,
            ConnectionSession session)
        {
            var hapSession = GetSession(connectionId);

            _logger.LogDebug($"Working on request {url}");
            var queryString = new NameValueCollection();

            if (url.Contains("?"))
            {
                var parts = url.Split('?');

                queryString = HttpUtility.ParseQueryString(parts[1]);
                url = parts[0];
            }

            url = url.Trim('/');

            try
            {
                // only the pairing endpoints carry TLV bodies, everything else is never TLV parsed
                if (method == "POST" && url == "pair-setup")
                {
                    return HandlePairSetup(connectionId, TlvParser.Parse(inputData), session, hapSession);
                }

                if (method == "POST" && url == "pair-verify")
                {
                    return HandlePairVerify(TlvParser.Parse(inputData), hapSession);
                }

                if (method == "POST" && url == "identify")
                {
                    // HAP 5.8: identify is only allowed while unpaired
                    if (_pairingStore.IsPaired)
                    {
                        return HapStatus(400, "Bad Request", HapStatusInsufficientPrivileges);
                    }

                    var identify = new IdentifyController(_logger);
                    var data = identify.Post(inputData);

                    return new HapResponse(data.ContentType, Array.Empty<byte>());
                }

                // everything below needs a verified (encrypted) session
                if (!hapSession.IsVerified)
                {
                    _logger.LogWarning($"Rejecting {method} {url} on a connection that is not verified");
                    return ConnectionAuthorizationRequired();
                }

                if (method == "POST" && url == "pairings")
                {
                    _logger.LogDebug($"Working on pairings");
                    var pair = new PairingController(_logger);
                    var data = pair.Post(TlvParser.Parse(inputData), hapSession, _pairingStore);

                    if (data.RemovedControllers.Count > 0)
                    {
                        CloseControllerSessions(data.RemovedControllers, hapSession);
                    }

                    return new HapResponse(data.ContentType, data.Data);
                }

                if (method == "PUT" && url == "characteristics")
                {
                    _logger.LogDebug($"Working on characteristics");
                    var c = new CharacteristicsController(_logger);
                    var data = c.Put(inputData, hapSession, _homeKitServer);

                    // response with no data if the call was successful - errors need to be implemented
                    return new HapResponse(data.ContentType, Array.Empty<byte>());
                }

                if (method == "GET" && url == "accessories")
                {
                    _logger.LogDebug($"Working on accessories");

                    var accessoryController = new AccesoriesController(_logger);
                    var accessories = accessoryController.Get(_homeKitServer, queryString);
                    var strValue = JsonConvert.SerializeObject(accessories, JsonSettings);

                    _logger.LogTrace($"Sending {strValue}");

                    return new HapResponse(HapJsonContentType, Encoding.UTF8.GetBytes(strValue));
                }

                if (method == "GET" && url == "characteristics")
                {
                    _logger.LogDebug($"Working on characteristics");

                    var idParameter = queryString["id"];
                    if (string.IsNullOrEmpty(idParameter))
                    {
                        return HapStatus(400, "Bad Request", HapStatusInvalidValue);
                    }

                    var ids = idParameter.Split(',');
                    var c = new CharacteristicsController(_logger);
                    var data = c.Get(ids, _homeKitServer);
                    var strValue = JsonConvert.SerializeObject(data, JsonSettings);

                    _logger.LogTrace($"Sending {strValue}");

                    return new HapResponse(data.ContentType, Encoding.UTF8.GetBytes(strValue));
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{e}: Error occured while processing request");
                ReleasePairSetup(connectionId);
            }

            throw new InvalidOperationException();
        }

        private HapResponse HandlePairVerify(Tlv parts, HapSession hapSession)
        {
            _logger.LogDebug($"Working on pair-verify request");
            var state = parts.GetTypeAsInt(Constants.State);

            if (!_pairingStore.IsPaired)
            {
                return ReturnError(state + 1, ErrorCodes.Authentication);
            }

            var verify = new PairVerifyController(_logger);
            var data = verify.Post(parts, hapSession, _pairingStore);

            if (data == null)
            {
                return ReturnError(state + 1, ErrorCodes.Unknown);
            }

            var raw = TlvParser.Serialize(data.TlvData);
            return new HapResponse(data.ContentType, raw);
        }

        private HapResponse HandlePairSetup(string connectionId, Tlv parts, ConnectionSession session, HapSession hapSession)
        {
            _logger.LogDebug($"Working on pair-setup request");
            var state = parts.GetTypeAsInt(Constants.State);

            lock (_pairSetupLock)
            {
                // HAP 5.6.1: an accessory that is already paired must answer kTLVError_Unavailable
                if (hapSession.IsVerified || _pairingStore.IsPaired)
                {
                    _logger.LogWarning("Pair setup rejected, the accessory is already paired");
                    return ReturnError(state + 1, ErrorCodes.Unavailable);
                }

                if (_pairingStore.IsLockedOut)
                {
                    _logger.LogWarning($"Pair setup rejected after {_pairingStore.FailedSetupAttempts} failed attempts, clear the pairings to unlock");
                    return ReturnError(state + 1, ErrorCodes.MaxTries);
                }

                if (_pairController != null && UtcNow() - _pairControllerStarted > PairSetupTimeout)
                {
                    _logger.LogInformation("Pending pair setup timed out");
                    ReleasePairSetupUnlocked();
                }

                if (state == 1)
                {
                    if (_pairController != null && _pairControllerConnection != connectionId)
                    {
                        _logger.LogWarning($"Error busy...");
                        return ReturnError(2, ErrorCodes.Busy);
                    }

                    session.Salt = null;
                    session.Verifier = null;
                    session.ServerEphemeral = null;
                    session.ServerSession = null;

                    _pairController = PairSetupControllerFactory(_pairCode);
                    _pairControllerConnection = connectionId;
                    _pairControllerStarted = UtcNow();
                }
                else if (_pairController == null || _pairControllerConnection != connectionId)
                {
                    _logger.LogWarning($"Pair setup M{state} without a pending setup on this connection");
                    return ReturnError(state + 1, ErrorCodes.Unknown);
                }

                var data = _pairController.Post(parts, session);

                if (data == null)
                {
                    ReleasePairSetupUnlocked();
                    return ReturnError(state + 1, ErrorCodes.Unknown);
                }

                if (!data.Ok)
                {
                    if (state == 3 && _pairingStore.RegisterFailedSetupAttempt())
                    {
                        _logger.LogWarning($"Pair setup locked after {HapPairingStore.MaxFailedSetupAttempts} failed attempts");
                    }

                    ReleasePairSetupUnlocked();
                }
                else if (data.State == 5)
                {
                    ReleasePairSetupUnlocked();

                    if (!_pairingStore.CompletePairSetup(data.Ltsk, data.Identifier, data.Ltpk))
                    {
                        return ReturnError(6, ErrorCodes.Unavailable);
                    }

                    _logger.LogInformation("Pair setup completed");
                }

                return new HapResponse(PairSetupReturn.ContentType, TlvParser.Serialize(data.TlvData));
            }
        }

        private void ReleasePairSetup(string connectionId)
        {
            lock (_pairSetupLock)
            {
                if (_pairControllerConnection == connectionId)
                {
                    ReleasePairSetupUnlocked();
                }
            }
        }

        private void ReleasePairSetupUnlocked()
        {
            _pairController = null;
            _pairControllerConnection = null;
        }

        private void CloseControllerSessions(IList<string> controllerIds, HapSession current)
        {
            foreach (var session in _sessions.Values.ToArray())
            {
                if (session.ControllerId == null || !controllerIds.Contains(session.ControllerId))
                {
                    continue;
                }

                if (session == current)
                {
                    // the response has to be sent first
                    session.CloseAfterResponse = true;
                    continue;
                }

                CloseSession(session);
            }
        }

        private void CloseSession(HapSession session)
        {
            try
            {
                session.Client?.Close();
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, "Could not close connection");
            }
        }

        /// <summary>
        /// Closes all connections, e.g. after the pairings were cleared
        /// </summary>
        public void CloseAllSessions()
        {
            foreach (var session in _sessions.Values.ToArray())
            {
                CloseSession(session);
            }
        }

        public void TerminateSession(string session)
        {
            _sessions.TryRemove(session, out var _);
            ReleasePairSetup(session);
        }
    }
}
