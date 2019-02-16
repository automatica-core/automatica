using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Com.AugustCellars.CoAP;
using Com.AugustCellars.CoAP.DTLS;
using Com.AugustCellars.COSE;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using P3.Driver.IkeaTradfri.Controllers;
using P3.Driver.IkeaTradfri.Models;
using PeterO.Cbor;
using Zeroconf;

namespace P3.Driver.IkeaTradfri
{
    public class DeviceStateChangedEventArgs
    {
        public long Id { get; set; }
        public object Value { get; set; }
    }
    public class IkeaTradfriDriver
    {
        private readonly string _gatewayIp;
        private readonly string _name;
        private CoapClient _client;

        internal CoapClient Client => _client;
        
        public string Psk { get; set; }

        private readonly Dictionary<TradfriDeviceType, Dictionary<long, IList<Action<JToken>>>> _observers =
            new Dictionary<TradfriDeviceType, Dictionary<long, IList<Action<JToken>>>>();

        public IkeaTradfriDriver(string gatewayIp, string name, string psk)
        {
            Psk = psk;
            _gatewayIp = gatewayIp;
            _name = name;
        }

        public void Connect()
        {
            OneKey authKey = new OneKey();
            authKey.Add(CoseKeyKeys.KeyType, GeneralValues.KeyType_Octet);
            authKey.Add(CoseKeyParameterKeys.Octet_k, CBORObject.FromObject(Encoding.UTF8.GetBytes(Psk)));
            authKey.Add(CoseKeyKeys.KeyIdentifier, CBORObject.FromObject(Encoding.UTF8.GetBytes(_name)));

            DTLSClientEndPoint ep = new DTLSClientEndPoint(authKey);
            CoapClient cc = new CoapClient(new Uri($"coaps://{_gatewayIp}"))
            {
                EndPoint = ep
            };

            ep.Start();

            GatewayController gc = new GatewayController(cc);
            GatewayInfo = gc.GetGatewayInfo();
            _client = cc;
        }

        public void RegisterChange(Action<JToken> changeAction, TradfriDeviceType deviceType, long id)
        {
            lock (_observers)
            {
                if (!_observers.ContainsKey(deviceType))
                {
                    _observers.Add(deviceType, new Dictionary<long, IList<Action<JToken>>>());
                }

                if (!_observers[deviceType].ContainsKey(id))
                {
                    _observers[deviceType].Add(id, new List<Action<JToken>>());
                }

                _observers[deviceType][id].Add(changeAction);
            }
        }

        internal void HandleObserveResponse(Response obj)
        {
            try
            {
                var idProp = ((int) TradfriConstAttribute.ID).ToString();
                var jObject = JsonConvert.DeserializeObject<JObject>(obj.PayloadString);
                Console.WriteLine(obj.PayloadString);
                foreach (var value in Enum.GetValues(typeof(TradfriDeviceType)))
                {
                    var intValue = (int) value;
                    if (jObject.ContainsKey(intValue.ToString()) && jObject.ContainsKey(idProp))
                    {
                        var tradfriDevice = jObject[intValue.ToString()];
                        var id = Convert.ToInt64(jObject[idProp]);

                        lock (_observers)
                        {
                            if (_observers.ContainsKey((TradfriDeviceType) value) &&
                                _observers[(TradfriDeviceType) value].ContainsKey(id))
                            {
                                foreach (var action in _observers[(TradfriDeviceType) value][id])
                                {
                                    action.Invoke(tradfriDevice);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                //Thread.Sleep(1000);
                //Observe(HandleObserveResponse);
            }
        }

        public static async Task<IEnumerable<Tuple<string, string>>> Discover()
        {
            var ret = new List<Tuple<string, string>>();
            var responses = await ZeroconfResolver.ResolveAsync("_coap._udp.local.");
            foreach (var resp in responses)
            {
                ret.Add(new Tuple<string, string>(resp.DisplayName, resp.IPAddress));
            }

            return ret;
        }

        public static TradfriAuth GeneratePsk(string gatwayIp, string appName, string secret)
        {
            try
            {
                var authKey = new OneKey();
                authKey.Add(CoseKeyKeys.KeyType, GeneralValues.KeyType_Octet);
                authKey.Add(CoseKeyParameterKeys.Octet_k, CBORObject.FromObject(Encoding.UTF8.GetBytes(secret)));
                authKey.Add(CoseKeyKeys.KeyIdentifier, CBORObject.FromObject(Encoding.UTF8.GetBytes("Client_identity")));
                var ep = new DTLSClientEndPoint(authKey);
                ep.Start();

                var request = new Request(Method.POST);
                request.SetUri($"coaps://{gatwayIp}" + $"/{(int)TradfriConstRoot.Gateway}/{(int)TradfriConstAttribute.Auth}/");
                request.EndPoint = ep;
                request.AckTimeout = 5000;
                request.SetPayload($@"{{""{(int)TradfriConstAttribute.Identity}"":""{appName}""}}");

                request.Send();
                var resp = request.WaitForResponse(5000);
                if (resp == null)
                {
                    return null;
                }

                return JsonConvert.DeserializeObject<TradfriAuth>(resp.PayloadString);
            }
            catch (Exception)
            {
                throw;
            }
        }
        
        public List<TradfriDevice> LoadDevices()
        {
            var ret = new List<TradfriDevice>();
            GatewayController gwc = new GatewayController(_client);
            foreach (long deviceId in gwc.GetDevices())
            {
                DeviceController dc = new DeviceController(deviceId, this);
                TradfriDevice device = dc.GetTradFriDevice();
                ret.Add(device);
            }

            return ret;
        }

        public TradfriDevice GetDevice(long id)
        {
            return new DeviceController(id, this).GetTradFriDevice(true);
        }

        public void SwitchOn(long id)
        {
            DeviceController dc = new DeviceController(id, this);
            dc.TurnOn();
        }
        public void SwitchOff(long id)
        {
            DeviceController dc = new DeviceController(id, this);
            dc.TurnOff();
        }

        public void SetColor(long id, string color)
        {
            DeviceController dc = new DeviceController(id, this);
            dc.SetColor(color);
        }
        public void SetDimmer(long id, int dimmer)
        {
            DeviceController dc = new DeviceController(id, this);
            dc.SetDimmer(dimmer);
        }

        public GatewayInfo GatewayInfo { get; set; }
    }
}
