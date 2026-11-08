using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Mail;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using MimeKit;
using Xunit;

namespace Automatica.Core.Tests.Mail
{
    public class MailServiceTests
    {
        private readonly FakeSettingsCache _settings = new();
        private readonly RecordingSmtpTransport _transport = new();
        private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 8, 10, 0, 0, TimeSpan.Zero));
        private readonly MailService _service;

        public MailServiceTests()
        {
            _service = new MailService(_settings, _transport, new ServiceCollection().BuildServiceProvider(), _time, NullLogger<MailService>.Instance);
        }

        private void ConfigureSmtp(bool enabled = true, int port = 587, SmtpSecurity security = SmtpSecurity.Auto, string from = "home@example.com")
        {
            _settings.Set(SmtpSettings.EnabledKey, PropertyTemplateType.Bool, enabled);
            _settings.Set(SmtpSettings.HostKey, PropertyTemplateType.Text, "smtp.example.com");
            _settings.Set(SmtpSettings.PortKey, PropertyTemplateType.Integer, port);
            _settings.Set(SmtpSettings.SecurityKey, PropertyTemplateType.Enum, (int)security);
            _settings.Set(SmtpSettings.UserKey, PropertyTemplateType.Text, "home");
            _settings.Set(SmtpSettings.PasswordKey, PropertyTemplateType.Password, "secret");
            _settings.Set(SmtpSettings.FromKey, PropertyTemplateType.Text, from);
            _settings.Set(SmtpSettings.FromNameKey, PropertyTemplateType.Text, "My house");
        }

        [Fact]
        public async Task Enabled_SendsThroughSmtp()
        {
            ConfigureSmtp();

            var result = await _service.SendAsync(new List<string> { "a@example.com;b@example.com", "A@example.com", "broken" }, "Alarm", "Window open");

            Assert.True(result.Success);
            Assert.Equal(MailService.SmtpTransport, result.Transport);
            var (configuration, message) = Assert.Single(_transport.Sent);
            Assert.Equal("smtp.example.com", configuration.Host);
            Assert.Equal("secret", configuration.Password);
            Assert.Equal("\"My house\" <home@example.com>", message.From.Single().ToString());
            Assert.Equal(new[] { "a@example.com", "b@example.com" }, message.To.Mailboxes.Select(a => a.Address));
            Assert.Equal("Alarm", message.Subject);
            Assert.Equal("Window open", message.TextBody);
        }

        [Fact]
        public async Task TransportError_IsReturnedNotThrown()
        {
            ConfigureSmtp();
            _transport.Error = new InvalidOperationException("535 authentication failed");

            var result = await _service.SendAsync(new List<string> { "a@example.com" }, "x", "y");

            Assert.False(result.Success);
            Assert.Equal("535 authentication failed", result.Error);
        }

        [Fact]
        public async Task Incomplete_IsReported()
        {
            ConfigureSmtp(from: "");

            var result = await _service.SendAsync(new List<string> { "a@example.com" }, "x", "y");

            Assert.False(result.Success);
            Assert.Empty(_transport.Sent);
        }

        [Fact]
        public async Task HourlyLimit_StopsLoops()
        {
            ConfigureSmtp();

            for (var i = 0; i < MailService.MaxSmtpMailsPerHour; i++)
            {
                Assert.True((await _service.SendAsync(new List<string> { "a@example.com" }, "x", "y")).Success);
            }

            Assert.False((await _service.SendAsync(new List<string> { "a@example.com" }, "x", "y")).Success);

            _time.Advance(TimeSpan.FromHours(1));
            Assert.True((await _service.SendAsync(new List<string> { "a@example.com" }, "x", "y")).Success);
        }

        [Fact]
        public async Task Test_UsesSmtpEvenWhenSwitchedOff_AndDefaultsToTheSender()
        {
            ConfigureSmtp(enabled: false);

            var result = await _service.SendSmtpTestAsync(null);

            Assert.True(result.Success);
            Assert.Equal("home@example.com", Assert.Single(_transport.Sent).Message.To.Mailboxes.Single().Address);
        }

        [Theory]
        [InlineData(587, SmtpSecurity.Auto, MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable)]
        [InlineData(465, SmtpSecurity.Auto, MailKit.Security.SecureSocketOptions.SslOnConnect)]
        [InlineData(25, SmtpSecurity.StartTls, MailKit.Security.SecureSocketOptions.StartTls)]
        [InlineData(25, SmtpSecurity.None, MailKit.Security.SecureSocketOptions.None)]
        public void SocketOptions_FollowTheSecuritySetting(int port, SmtpSecurity security, MailKit.Security.SecureSocketOptions expected)
        {
            ConfigureSmtp(port: port, security: security);
            Assert.Equal(expected, MailKitSmtpTransport.ToSocketOptions(_settings.GetSmtpConfiguration()));
        }

        private sealed class RecordingSmtpTransport : ISmtpTransport
        {
            public List<(SmtpConfiguration Configuration, MimeMessage Message)> Sent { get; } = new();
            public Exception Error { get; set; }

            public Task SendAsync(SmtpConfiguration configuration, MimeMessage message, CancellationToken token)
            {
                if (Error != null)
                {
                    throw Error;
                }

                Sent.Add((configuration, message));
                return Task.CompletedTask;
            }
        }

        internal sealed class FakeSettingsCache : ISettingsCache
        {
            private readonly Dictionary<string, Setting> _settings = new();

            public void Set(string key, PropertyTemplateType type, object value)
            {
                _settings[key] = new Setting { ValueKey = key, Type = (long)type, Value = value };
            }

            public Setting GetByKey(string key) => _settings.GetValueOrDefault(key);
            public void UpdateByKey(string key, Setting value) => _settings[key] = value;
            public void Add(long key, Setting value) => throw new NotSupportedException();
            public void Update(long key, Setting value) => throw new NotSupportedException();
            public void Remove(long key) => throw new NotSupportedException();
            public bool Contains(long key) => false;
            public Setting Get(long key) => null;
            public ICollection<Setting> Get(params long[] keys) => new List<Setting>();
            public ICollection<Setting> All() => _settings.Values.ToList();
            public void Clear() { }
            public void ClearAndLoad() { }
            public IDictionary<long, Setting> Dictionary() => new Dictionary<long, Setting>();
        }
    }
}
