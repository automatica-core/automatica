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

        public static event EventHandler<PairSetupCompleteEventArgs> PairingCompleted;

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
            tlvData.AddType(Constants.State, state++);
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
                            if (_pairController != null && state == 1)
                            {
                                return ReturnError(state, ErrorCodes.Busy);
                            }

                            if (state == 1 && _pairController == null)
                            {
                                _pairController =
                                    new PairSetupController(_logger, _pairCode);
                            }

                            if (_pairController == null)
                            {
                                _logger.LogError($"Something is wrong here, closing tcp connection");
                                throw new ArgumentException($"Something is wrong here, closing tcp connection");
                            }

                            var data = _pairController.Post(parts, session);

                            var raw = TlvParser.Serialize(data.TlvData);

                            if (!data.Ok)
                            {
                                _pairController = null;
                                _sessions.TryRemove(connectionId, out var _);
                            }
                            else if (data.State == 5)
                            {
                                PairingCompleted?.Invoke(this, new PairSetupCompleteEventArgs(data.Ltsk, data.Ltpk));
                                _pairController = null;
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
                            var data = pair.Post(parts);

                            var raw = TlvParser.Serialize(data.TlvData);
                            return new Tuple<string, byte[]>(data.ContentType, raw);
                        }

                        if (url.EndsWith("identify"))
                        {
                            var identify = new IdentifyController();
                            var data = identify.Post(inputData);

                            return new Tuple<string, byte[]>(data.ContentType, new byte[0]);
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
                            return new Tuple<string, byte[]>(data.ContentType, new byte[0]);
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
                _pairController = null;
            }
            
            throw new InvalidOperationException();
        }

        public void TerminateSession(string session)
        {
            _sessions.TryRemove(session, out var _);
        }
    }
}
