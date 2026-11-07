using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Automatica.Core.Runtime.Core;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Http
{
    /// <summary>
    /// webapi/satellite: the client key is write only and stored as a hash, the own satellite is never deleted.
    /// The save replaces the whole list, so every test posts its complete list (the tests of a class run one after another).
    /// </summary>
    public class SatellitesHttpTests : IClassFixture<WebApiHostFixture>
    {
        private const string Url = "webapi/satellite";
        private const string ValidKey = "test-satellite-key-0001";

        private readonly WebApiHost _host;
        private readonly HttpClient _admin;

        public SatellitesHttpTests(WebApiHostFixture fixture)
        {
            _host = fixture.Host;
            _admin = _host.CreateClient(_host.CreateAdminToken());
        }

        private static Slave Satellite(string name, string key, Guid? id = null) => new()
        {
            ObjId = id ?? Guid.NewGuid(),
            Name = name,
            Description = "",
            ClientId = "client-" + name,
            ClientKey = key
        };

        private Slave Self()
        {
            using var db = _host.CreateDbContext();
            var self = db.Slaves.AsNoTracking().Single(a => a.ObjId == ServerInfo.SelfSlaveGuid);
            self.ClientKey = null;
            return self;
        }

        private Slave Stored(Guid id)
        {
            using var db = _host.CreateDbContext();
            return db.Slaves.AsNoTracking().SingleOrDefault(a => a.ObjId == id);
        }

        private List<Slave> AllStored()
        {
            using var db = _host.CreateDbContext();
            return db.Slaves.AsNoTracking().ToList();
        }

        [Fact]
        public async Task SaveInstances_NewPlaintextKey_IsStoredAsVerifiableHash()
        {
            var satellite = Satellite("hashed", ValidKey);

            var response = await _admin.PostJson(Url, new[] { Self(), satellite });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var stored = Stored(satellite.ObjId);
            Assert.True(SatelliteClientKey.IsHashed(stored.ClientKey));
            Assert.DoesNotContain(ValidKey, stored.ClientKey);
            Assert.True(SatelliteClientKey.Verify(ValidKey, stored.ClientKey, out var needsRehash));
            Assert.False(needsRehash);
            Assert.False(SatelliteClientKey.Verify(ValidKey + "x", stored.ClientKey, out _));
        }

        [Fact]
        public async Task SaveInstances_SameKeyTwice_GetsDifferentSalts()
        {
            var a = Satellite("salt-a", ValidKey);
            var b = Satellite("salt-b", ValidKey);

            await _admin.PostJson(Url, new[] { Self(), a, b });

            Assert.NotEqual(Stored(a.ObjId).ClientKey, Stored(b.ObjId).ClientKey);
        }

        [Fact]
        public async Task SaveInstances_KeyShorterThan16_Returns400WithErrorKey_AndSavesNothing()
        {
            var before = AllStored().Select(a => a.ObjId).OrderBy(a => a).ToList();
            var valid = Satellite("valid", ValidKey);
            var tooShort = Satellite("short", new string('k', SatelliteClientKey.MinimumKeyLength - 1));

            var response = await _admin.PostJson(Url, new[] { Self(), valid, tooShort });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.ReadJson();
            Assert.Equal("SATELLITE_KEY_TOO_SHORT", (string)body["errorText"]);
            Assert.Equal("WebApiException", (string)(body["typeInfo"] ?? body["TypeInfo"]));
            Assert.Equal((int)Automatica.Core.Base.Exceptions.ExceptionSeverity.Warning, (int)body["severity"]);

            // validated before anything is written: no partial save, no deletes
            Assert.Equal(before, AllStored().Select(a => a.ObjId).OrderBy(a => a).ToList());
        }

        [Fact]
        public async Task SaveInstances_KeyOfExactly16Characters_IsAccepted()
        {
            var satellite = Satellite("sixteen", new string('k', SatelliteClientKey.MinimumKeyLength));

            var response = await _admin.PostJson(Url, new[] { Self(), satellite });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(SatelliteClientKey.IsHashed(Stored(satellite.ObjId).ClientKey));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task SaveInstances_EmptyKey_KeepsTheStoredHash(string emptyKey)
        {
            var satellite = Satellite("keep", ValidKey);
            await _admin.PostJson(Url, new[] { Self(), satellite });
            var hash = Stored(satellite.ObjId).ClientKey;

            satellite.ClientKey = emptyKey;
            satellite.Name = "keep-renamed";
            var response = await _admin.PostJson(Url, new[] { Self(), satellite });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var stored = Stored(satellite.ObjId);
            Assert.Equal("keep-renamed", stored.Name);
            Assert.Equal(hash, stored.ClientKey);
        }

        [Fact]
        public async Task SaveInstances_NewKeyForExistingSatellite_ReplacesTheHash()
        {
            var satellite = Satellite("rotate", ValidKey);
            await _admin.PostJson(Url, new[] { Self(), satellite });

            satellite.ClientKey = "rotated-satellite-key-02";
            await _admin.PostJson(Url, new[] { Self(), satellite });

            var stored = Stored(satellite.ObjId).ClientKey;
            Assert.True(SatelliteClientKey.Verify("rotated-satellite-key-02", stored, out _));
            Assert.False(SatelliteClientKey.Verify(ValidKey, stored, out _));
        }

        [Fact]
        public async Task SaveInstances_AlreadyHashedKey_IsStoredUnchanged()
        {
            var hash = SatelliteClientKey.Hash(ValidKey);
            var satellite = Satellite("prehashed", hash);

            await _admin.PostJson(Url, new[] { Self(), satellite });

            Assert.Equal(hash, Stored(satellite.ObjId).ClientKey);
        }

        [Fact]
        public async Task GetAll_NeverReturnsTheClientKey()
        {
            await _admin.PostJson(Url, new[] { Self(), Satellite("secret", ValidKey) });

            var list = (JArray)await (await _admin.GetAsync(Url)).ReadJson();

            Assert.NotEmpty(list);
            Assert.All(list, a => Assert.True(a["clientKey"] == null || a["clientKey"].Type == JTokenType.Null));
            Assert.DoesNotContain(SatelliteClientKey.Prefix, list.ToString());
        }

        [Fact]
        public async Task SaveInstances_Response_NeverReturnsTheClientKey()
        {
            var response = await _admin.PostJson(Url, new[] { Self(), Satellite("response", ValidKey) });

            var body = await response.ReadJson();
            Assert.DoesNotContain(SatelliteClientKey.Prefix, body.ToString());
            Assert.DoesNotContain(ValidKey, body.ToString());
        }

        [Fact]
        public async Task SaveInstances_ListWithoutSelf_DeletesOthersButNeverTheSelfSatellite()
        {
            var keep = Satellite("posted", ValidKey);
            var removed = Satellite("removed", ValidKey);
            await _admin.PostJson(Url, new[] { Self(), keep, removed });

            var response = await _admin.PostJson(Url, new[] { keep });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(Stored(keep.ObjId));
            Assert.Null(Stored(removed.ObjId));
            Assert.NotNull(Stored(ServerInfo.SelfSlaveGuid));
        }

        [Fact]
        public async Task SaveInstances_EmptyList_DeletesNothing()
        {
            await _admin.PostJson(Url, new[] { Self(), Satellite("survivor", ValidKey) });
            var before = AllStored().Select(a => a.ObjId).OrderBy(a => a).ToList();

            var response = await _admin.PostJson(Url, Array.Empty<Slave>());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(before, AllStored().Select(a => a.ObjId).OrderBy(a => a).ToList());
            Assert.Equal(before.Count, ((JArray)await response.ReadJson()).Count);
        }

        [Fact]
        public async Task SaveInstances_NullBody_ReturnsTheListAndChangesNothing()
        {
            var before = AllStored().Select(a => a.ObjId).OrderBy(a => a).ToList();

            var response = await _admin.PostAsync(Url, HttpTestExtensions.RawJson("null"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(before, AllStored().Select(a => a.ObjId).OrderBy(a => a).ToList());
        }

        [Fact]
        public async Task GetAll_ConnectedFlag_ComesFromTheBrokerClientIds_SelfIsAlwaysConnected()
        {
            var online = Satellite("online", ValidKey);
            var offline = Satellite("offline", ValidKey);
            await _admin.PostJson(Url, new[] { Self(), online, offline });
            _host.ConnectedSatellites.Clear();
            // the broker lists client ids, not object ids
            _host.ConnectedSatellites.Add(online.ClientId);
            _host.ConnectedSatellites.Add(offline.ObjId.ToString());

            try
            {
                var list = (JArray)await (await _admin.GetAsync(Url)).ReadJson();

                bool Connected(Guid id) => (bool)list.Single(a => (Guid)a["objId"] == id)["connected"];
                Assert.True(Connected(online.ObjId));
                Assert.False(Connected(offline.ObjId));
                Assert.True(Connected(ServerInfo.SelfSlaveGuid));
            }
            finally
            {
                _host.ConnectedSatellites.Clear();
            }
        }

        [Fact]
        public async Task Satellites_AreAdminOnly()
        {
            var visu = _host.CreateClient(_host.CreateVisuToken());

            Assert.Equal(HttpStatusCode.Forbidden, (await visu.GetAsync(Url)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await visu.PostJson(Url, new[] { Satellite("evil", ValidKey) })).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await _host.CreateClient().GetAsync(Url)).StatusCode);
        }
    }
}
