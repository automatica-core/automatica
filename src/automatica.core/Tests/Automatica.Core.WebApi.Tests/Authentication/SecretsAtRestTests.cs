using System;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Security;
using Automatica.Core.WebApi.Controllers;
using Automatica.Core.WebApi.Tests.Base;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Authentication
{
    public class SecretProtectorTests
    {
        [Fact]
        public void RoundTrip()
        {
            var protectedValue = SecretProtector.Protect("my-secret");
            Assert.StartsWith(SecretProtector.Prefix, protectedValue);
            Assert.DoesNotContain("my-secret", protectedValue);
            Assert.NotEqual(protectedValue, SecretProtector.Protect("my-secret"));

            Assert.True(SecretProtector.TryUnprotect(protectedValue, out var plain));
            Assert.Equal("my-secret", plain);

            // already protected values are not encrypted twice, empty values stay empty
            Assert.Same(protectedValue, SecretProtector.Protect(protectedValue));
            Assert.Equal("", SecretProtector.Protect(""));
            Assert.Null(SecretProtector.Protect(null));
        }

        [Fact]
        public void PlainAndTamperedValues()
        {
            Assert.True(SecretProtector.TryUnprotect("plain", out var plain));
            Assert.Equal("plain", plain);

            var protectedValue = SecretProtector.Protect("my-secret");
            var bytes = Convert.FromBase64String(protectedValue.Substring(SecretProtector.Prefix.Length));
            bytes[^1] ^= 1;
            Assert.False(SecretProtector.TryUnprotect(SecretProtector.Prefix + Convert.ToBase64String(bytes), out _));
            Assert.False(SecretProtector.TryUnprotect(SecretProtector.Prefix + "not base64!", out _));
        }
    }

    public class SecretsInterceptorTests : BaseControllerTest<AuthenticationController>
    {
        private string ReadStored(string key)
        {
            using var db = new AutomaticaContext(Configuration);
            return db.Database.SqlQueryRaw<string>("SELECT ValueText AS Value FROM Settings WHERE ValueKey = {0}", key).ToList().Single();
        }

        [Fact]
        public void SecretSetting_IsEncryptedAtRest_AndReadAsPlainText()
        {
            using (var db = new AutomaticaContext(Configuration))
            {
                var setting = new Setting { ValueKey = "ngrokToken", ValueText = "token-123", Type = (long)PropertyTemplateType.Text, Group = "test" };
                db.Settings.Add(setting);
                db.SaveChanges();

                // the entity keeps the plain value and is not dirty after the save
                Assert.Equal("token-123", setting.ValueText);
                Assert.Equal(EntityState.Unchanged, db.Entry(setting).State);
            }

            var stored = ReadStored("ngrokToken");
            Assert.StartsWith(SecretProtector.Prefix, stored);
            Assert.DoesNotContain("token-123", stored);

            using (var db = new AutomaticaContext(Configuration))
            {
                Assert.Equal("token-123", db.Settings.AsNoTracking().Single(a => a.ValueKey == "ngrokToken").ValueText);

                var tracked = db.Settings.Single(a => a.ValueKey == "ngrokToken");
                Assert.Equal("token-123", tracked.ValueText);
                tracked.ValueText = "token-456";
                db.SaveChanges();
            }

            Assert.DoesNotContain("token-456", ReadStored("ngrokToken"));
            using (var db = new AutomaticaContext(Configuration))
            {
                Assert.Equal("token-456", db.Settings.AsNoTracking().Single(a => a.ValueKey == "ngrokToken").ValueText);
            }
        }

        [Fact]
        public void PlainValuesOfOldDatabases_AreEncryptedByTheMigration()
        {
            using (var db = new AutomaticaContext(Configuration))
            {
                db.Database.ExecuteSqlRaw("UPDATE Settings SET ValueText = 'plain-api-key' WHERE ValueKey = 'apiKey'");
                // plain values are read as they are
                Assert.Equal("plain-api-key", db.Settings.AsNoTracking().Single(a => a.ValueKey == "apiKey").ValueText);

                Assert.True(SecretsInterceptor.EncryptExisting(db) >= 1);
            }

            Assert.StartsWith(SecretProtector.Prefix, ReadStored("apiKey"));
            using (var db = new AutomaticaContext(Configuration))
            {
                Assert.Equal("plain-api-key", db.Settings.AsNoTracking().Single(a => a.ValueKey == "apiKey").ValueText);
                Assert.Equal(0, SecretsInterceptor.EncryptExisting(db));
            }
        }

        [Fact]
        public void OtherSettings_StayPlain()
        {
            Assert.DoesNotContain(SecretProtector.Prefix, ReadStored("language") ?? "");
        }
    }
}
