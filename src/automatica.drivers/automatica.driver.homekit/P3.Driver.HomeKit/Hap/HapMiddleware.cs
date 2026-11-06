using System;
using System.Collections.Concurrent;
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
        public string Ltsk { get; }
        public string Ltpk { get; }
    }

    internal class HapMiddleware
    {
        private readonly ILogger _logger; //todo: add more logs
        private readonly string _pairCode;
        private readonly HomeKitServer _homeKitServer;

        private PairSetupController _pairController;
        private string _pairControllerConnectionId;
        private readonly object _pairLock = new object();

        public static event EventHandler<PairSetupCompleteEventArgs> PairingCompleted;
        public TlvParser TlvParser { get; }

        private readonly ConcurrentDictionary<string, HapSession> _sessions = new ConcurrentDictionary<string, HapSession>();
        internal static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        public HapMiddleware(ILogger logger, string pairCode, HomeKitServer homeKitServer)
        {
            _logger = logger;
            _pairCode = pairCode;
            _homeKitServer = homeKitServer;

            TlvParser = new TlvParser(logger);
        }


        public HapSession GetSessionByClientUsername(string username)
        {
            return _sessions.Values.SingleOrDefault(a => a.ClientUsername == username);
        }
        public HapSession GetSession(string connectionId)
        {
            if (!_sessions.ContainsKey(connectionId))
            {
                _sessions.TryAdd(connectionId, new HapSession());
            }

            return _sessions[connectionId];
        }

        private Tuple<string, byte[]> ReturnError(int state, ErrorCodes error)
        {
            _logger.LogDebug($"Return error code: {error} state {state}");
            var tlvData = new Tlv();
            // errors are returned with the state of the response to the request (M1 => M2)
            tlvData.AddType(Constants.State, state + 1);
            tlvData.AddType(Constants.Error, error);
            byte[] output = TlvParser.Serialize(tlvData);

            return new Tuple<string, byte[]>("application/pairing+tlv8", output);    
        }

        public Tuple<string, byte[]> Invoke(string connectionId, string url, string method, byte[] inputData,
            ConnectionSession session)
        {
            if (!_sessions.ContainsKey(connectionId))
            {
                _sessions.TryAdd(connectionId, new HapSession());
            }


            _logger.LogDebug($"Working on request {url}");
            var queryString = new NameValueCollection();

            if (url.Contains("?"))
            {
                var parts = url.Split('?');

                queryString = HttpUtility.ParseQueryString(parts[1]);
                url = parts[0];
            }

            try
            {
                if (inputData.Length > 0)
                {
                    var parts = TlvParser.Parse(inputData);

                    if (method == "POST")
                    {
                        var state = parts.GetTypeAsInt(Constants.State);

                        if (url.EndsWith("pair-setup"))
                        {
                            _logger.LogDebug($"Working on pair-setup request");

                            PairSetupController pairController;
                            lock (_pairLock)
                            {
                                if (state == 1 && IsPaired)
                                {
                                    _logger.LogWarning($"Already paired, rejecting pair-setup...");
                                    return ReturnError(state, ErrorCodes.Unavailable);
                                }

                                if (_pairController != null && state == 1)
                                {
                                    _logger.LogWarning($"Error busy...");
                                    return ReturnError(state, ErrorCodes.Busy);
                                }

                                if (state == 1 && _pairController == null)
                                {
                                    _pairController =
                                        new PairSetupController(_logger, _pairCode);
                                    _pairControllerConnectionId = connectionId;
                                }

                                if (_pairController == null || _pairControllerConnectionId != connectionId)
                                {
                                    _logger.LogError($"Something is wrong here, closing tcp connection");
                                    throw new ArgumentException($"Something is wrong here, closing tcp connection");
                                }

                                pairController = _pairController;
                            }

                            var data = pairController.Post(parts, session);

                            var raw = TlvParser.Serialize(data.TlvData);

                            if (!data.Ok)
                            {
                                ResetPairController(connectionId);
                                _sessions.TryRemove(connectionId, out var _);
                            }
                            else if (data.State == 5)
                            {
                                PairingCompleted?.Invoke(this, new PairSetupCompleteEventArgs(data.Ltsk, data.Ltpk));
                                ResetPairController(connectionId);
                            }

                            return new Tuple<string, byte[]>(PairSetupReturn.ContentType, raw);
                        }

                        if (url.EndsWith("pair-verify"))
                        {
                            _logger.LogDebug($"Working on pair-verify request");
                            if (string.IsNullOrEmpty(HapControllerServer.HapControllerLtsk))
                            {

                                return ReturnError(state, ErrorCodes.Busy);
                            }

                            var verify = new PairVerifyController(_logger);
                            var data = verify.Post(parts, _sessions[connectionId]);

                            if (data.HapSession != null)
                            {
                                _sessions[connectionId] = data.HapSession;
                            }

                            var raw = TlvParser.Serialize(data.TlvData);
                            return new Tuple<string, byte[]>(data.ContentType, raw);
                        }

                        if (url.EndsWith("pairings"))
                        {
                            _logger.LogDebug($"Working on pairings");
                            var pair = new PairingController(_logger);
                            var hapSession = _sessions[connectionId];
                            var data = pair.Post(parts, hapSession);

                            if (data.PairingRemoved)
                            {
                                _logger.LogInformation($"Controller removed its pairing, clearing pairing...");
                                if (_homeKitServer != null)
                                {
                                    _homeKitServer.RemovePairing();
                                }
                                else
                                {
                                    HapControllerServer.ClearPairing();
                                }

                                // the response is still sent with the session keys, the controller's connections are closed afterwards
                                hapSession.CloseAfterResponse = true;
                            }

                            var raw = TlvParser.Serialize(data.TlvData);
                            return new Tuple<string, byte[]>(data.ContentType, raw);
                        }

                        if (url.EndsWith("identify"))
                        {
                            var identify = new IdentifyController(_logger);
                            var data = identify.Post(inputData);

                            return new Tuple<string, byte[]>(data.ContentType, Array.Empty<byte>());
                        }
                    }
                    else if (method == "PUT")
                    {
                        if (url.EndsWith("characteristics"))
                        {
                            _logger.LogDebug($"Working on characteristics");
                            var c = new CharacteristicsController(_logger);
                            var data = c.Put(inputData, _sessions[connectionId], _homeKitServer);

                            // response with no data if the call was successful - errors need to be implemented
                            return new Tuple<string, byte[]>(data.ContentType, Array.Empty<byte>());
                        }
                    }
                }
                else
                {
                    if (method == "GET")
                    {
                        if (url.EndsWith("accessories"))
                        {
                            _logger.LogDebug($"Working on accessories");

                            var accessoryController = new AccesoriesController(_logger);
                            var accessories = accessoryController.Get(_homeKitServer, queryString);
                            var strValue = JsonConvert.SerializeObject(accessories, JsonSettings);

                            _logger.LogDebug($"Sending {strValue}");

                            return new Tuple<string, byte[]>("application/hap+json", Encoding.UTF8.GetBytes(strValue));
                        }

                        if (url.EndsWith("characteristics"))
                        {
                            _logger.LogDebug($"Working on characteristics");

                            var ids = queryString["id"].Split(",");
                            var c = new CharacteristicsController(_logger);
                            var data = c.Get(ids, _homeKitServer);
                            var strValue = JsonConvert.SerializeObject(data, JsonSettings);

                            _logger.LogDebug($"Sending {strValue}");

                            return new Tuple<string, byte[]>(data.ContentType, Encoding.UTF8.GetBytes(strValue));

                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{e}: Error occured while processing request");
                ResetPairController(connectionId);
            }

            throw new InvalidOperationException();
        }

        private static bool IsPaired => !string.IsNullOrEmpty(HapControllerServer.HapControllerLtpk);

        /// <summary>
        /// Drops the pair-setup in progress if it belongs to <paramref name="connectionId"/>, otherwise a dropped connection would leave the accessory busy forever.
        /// </summary>
        private void ResetPairController(string connectionId)
        {
            lock (_pairLock)
            {
                if (_pairControllerConnectionId == connectionId)
                {
                    _pairController = null;
                    _pairControllerConnectionId = null;
                }
            }
        }

        public void TerminateSession(string session)
        {
            _sessions.TryRemove(session, out var _);
            ResetPairController(session);
        }
    }
}
