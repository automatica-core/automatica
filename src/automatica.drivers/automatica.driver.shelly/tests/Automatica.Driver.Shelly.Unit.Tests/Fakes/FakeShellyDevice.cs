using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Automatica.Driver.Shelly.Rpc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Automatica.Driver.Shelly.Unit.Tests.Fakes
{
    /// <summary>
    /// Plays one device from a recorded fixture (Fixtures/*.json) on a <see cref="FakeShellyServer"/>:
    /// Gen1: GET /shelly, /status, /settings, /cit/d and the control URLs (basic auth when a password is set),
    /// Gen2+: GET /shelly and POST /rpc (Shelly.GetDeviceInfo, Shelly.GetStatus, Shelly.GetConfig, every other method answers {}),
    /// SHA-256 digest checked like the device does when a password is set.
    /// </summary>
    internal sealed class FakeShellyDevice
    {
        private readonly FakeShellyServer _server;
        private readonly string _nonce = "6a2f1c9b";

        public JObject Fixture { get; }
        public int Generation => Fixture.Value<int>("gen");
        public JObject Status => (JObject)Fixture["status"];
        public JObject Info => (JObject)(Fixture["shelly"] ?? Fixture["deviceinfo"]);
        public string Mac => Info.Value<string>("mac");
        public string Realm => Info.Value<string>("id") ?? $"shelly-{Mac}";

        public string Password { get; set; }
        public string UserName { get; set; } = "admin";
        public bool Offline { get; set; }

        /// <summary>RPC calls and control URLs the device got (method or path+query)</summary>
        public ConcurrentQueue<(string Method, JObject Params)> Calls { get; } = new();

        public FakeShellyDevice(FakeShellyServer server, string fixture)
        {
            _server = server;
            Fixture = Load(fixture);
            _server.Handler = Handle;
        }

        public static JObject Load(string fixture)
        {
            return JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", fixture + ".json")));
        }

        private FakeResponse Handle(RecordedRequest request)
        {
            if (Offline)
            {
                return new FakeResponse(503, "");
            }

            if (request.Path == "/shelly")
            {
                return FakeResponse.Json(Info.ToString(Formatting.None));
            }

            return Generation >= 2 ? HandleRpc(request) : HandleGen1(request);
        }

        private FakeResponse HandleGen1(RecordedRequest request)
        {
            if (Password != null)
            {
                var expected = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{UserName}:{Password}"));
                if (request.Authorization != expected)
                {
                    return new FakeResponse(401, "401 Unauthorized");
                }
            }

            switch (request.Path)
            {
                case "/status":
                    return FakeResponse.Json(Status.ToString(Formatting.None));
                case "/settings":
                    return Fixture["settings"] is JObject settings ? FakeResponse.Json(settings.ToString(Formatting.None)) : new FakeResponse(404, "");
                case "/cit/d":
                    return Fixture["citd"] is JObject citd ? FakeResponse.Json(citd.ToString(Formatting.None)) : new FakeResponse(404, "");
            }

            Calls.Enqueue((request.Path + request.Query, null));

            // /relay/0?turn=on → the new relay state
            var parts = request.Path.Trim('/').Split('/');
            if (parts.Length == 2 && int.TryParse(parts[1], out var index))
            {
                var section = parts[0] switch
                {
                    "relay" => "relays",
                    "roller" => "rollers",
                    "light" or "color" or "white" => "lights",
                    "thermostats" => "thermostats",
                    _ => null
                };
                if (section != null && Status[section] is JArray array && index < array.Count)
                {
                    var query = System.Web.HttpUtility.ParseQueryString(request.Query);
                    var item = (JObject)array[index];
                    if (query["turn"] != null)
                    {
                        item["ison"] = query["turn"] == "on";
                    }
                    return FakeResponse.Json(item.ToString(Formatting.None));
                }
            }

            return FakeResponse.Json("{}");
        }

        private FakeResponse HandleRpc(RecordedRequest request)
        {
            if (request.Path != "/rpc" || request.Method != "POST")
            {
                return new FakeResponse(404, "");
            }

            if (Password != null && !ValidDigest(request.Authorization))
            {
                return new FakeResponse(401, "", Headers: new[]
                {
                    ("WWW-Authenticate", $"Digest qop=\"auth\", realm=\"{Realm}\", nonce=\"{_nonce}\", algorithm=SHA-256")
                });
            }

            var frame = JObject.Parse(request.Body);
            var method = frame.Value<string>("method");
            var parameters = frame["params"] as JObject;
            Calls.Enqueue((method, parameters));

            JToken result = method switch
            {
                "Shelly.GetDeviceInfo" => Info,
                "Shelly.GetStatus" => Status,
                "Shelly.GetConfig" => Fixture["config"] ?? new JObject(),
                _ => new JObject()
            };

            return FakeResponse.Json(new JObject { ["id"] = frame["id"], ["src"] = Realm, ["result"] = result }.ToString(Formatting.None));
        }

        private bool ValidDigest(string authorization)
        {
            if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith("Digest ", StringComparison.Ordinal))
            {
                return false;
            }

            var fields = authorization.Substring(7).Split(',').Select(a => a.Trim().Split('=', 2))
                .Where(a => a.Length == 2).ToDictionary(a => a[0], a => a[1].Trim('"'));

            if (fields.GetValueOrDefault("username") != "admin" || fields.GetValueOrDefault("realm") != Realm || fields.GetValueOrDefault("nonce") != _nonce)
            {
                return false;
            }

            var ha1 = ShellyDigestAuth.Sha256Hex($"admin:{Realm}:{Password}");
            var ha2 = ShellyDigestAuth.Sha256Hex($"POST:{fields["uri"]}");
            var expected = ShellyDigestAuth.Sha256Hex($"{ha1}:{_nonce}:{fields["nc"]}:{fields["cnonce"]}:auth:{ha2}");
            return expected == fields.GetValueOrDefault("response");
        }
    }
}
