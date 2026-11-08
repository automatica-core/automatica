using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cloud;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Automatica.Core.Internals.Mail
{
    public sealed record MailResult(bool Success, string Transport, string Error = null)
    {
        public static MailResult Ok(string transport) => new(true, transport);
        public static MailResult Failed(string transport, string error) => new(false, transport, error);
    }

    /// <summary>
    /// Sends the mails of logics, drivers and automations: through the own SMTP server when it is switched on in the
    /// settings, otherwise through the Automatica cloud (only to verified recipients there).
    /// </summary>
    public interface IMailService
    {
        Task<MailResult> SendAsync(IList<string> to, string subject, string body, CancellationToken token = default);

        /// <summary>
        /// Sends a test mail through the SMTP server of the settings (also when it is not switched on yet)
        /// </summary>
        Task<MailResult> SendSmtpTestAsync(string to, CancellationToken token = default);
    }

    /// <summary>
    /// The SMTP conversation, replaced in tests
    /// </summary>
    public interface ISmtpTransport
    {
        Task SendAsync(SmtpConfiguration configuration, MimeMessage message, CancellationToken token);
    }

    internal sealed class MailKitSmtpTransport : ISmtpTransport
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

        public async Task SendAsync(SmtpConfiguration configuration, MimeMessage message, CancellationToken token)
        {
            using var client = new SmtpClient();
            client.Timeout = (int)Timeout.TotalMilliseconds;
            if (configuration.AcceptInvalidCertificate)
            {
                // self signed certificate of a mail server in the local network, the admin has to switch this on
                client.ServerCertificateValidationCallback = (_, _, _, _) => true;
            }

            await client.ConnectAsync(configuration.Host, configuration.Port, ToSocketOptions(configuration), token).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(configuration.User))
            {
                await client.AuthenticateAsync(configuration.User, configuration.Password ?? string.Empty, token).ConfigureAwait(false);
            }

            await client.SendAsync(message, token).ConfigureAwait(false);
            await client.DisconnectAsync(true, token).ConfigureAwait(false);
        }

        internal static SecureSocketOptions ToSocketOptions(SmtpConfiguration configuration)
        {
            return configuration.Security switch
            {
                SmtpSecurity.StartTls => SecureSocketOptions.StartTls,
                SmtpSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
                SmtpSecurity.None => SecureSocketOptions.None,
                _ => configuration.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable
            };
        }
    }

    internal sealed class MailService : IMailService
    {
        public const string SmtpTransport = "smtp";
        public const string CloudTransport = "cloud";

        /// <summary>
        /// Protects the mail account against a logic or automation that sends in a loop
        /// </summary>
        internal const int MaxSmtpMailsPerHour = 100;

        private readonly ISettingsCache _settings;
        private readonly ISmtpTransport _transport;
        private readonly IServiceProvider _services;
        private readonly TimeProvider _time;
        private readonly ILogger<MailService> _logger;

        private readonly Queue<DateTimeOffset> _sent = new();
        private readonly object _lock = new();

        public MailService(ISettingsCache settings, ISmtpTransport transport, IServiceProvider services, TimeProvider time, ILogger<MailService> logger)
        {
            _settings = settings;
            _transport = transport;
            _services = services;
            _time = time;
            _logger = logger;
        }

        public async Task<MailResult> SendAsync(IList<string> to, string subject, string body, CancellationToken token = default)
        {
            var configuration = _settings.GetSmtpConfiguration();
            if (!configuration.Enabled)
            {
                // the cloud api is transient and needs the db settings, resolved only when it is used
                var cloud = _services.GetRequiredService<CloudApi>();
                var sent = await cloud.SendEmail(to, subject, body).ConfigureAwait(false);
                return sent ? MailResult.Ok(CloudTransport) : MailResult.Failed(CloudTransport, "The cloud did not accept the mail");
            }

            if (!TryTakeSlot())
            {
                _logger.LogWarning($"More than {MaxSmtpMailsPerHour} mails within an hour, mail \"{subject}\" not sent");
                return MailResult.Failed(SmtpTransport, $"More than {MaxSmtpMailsPerHour} mails within an hour");
            }

            return await SendSmtpAsync(configuration, to, subject, body, token).ConfigureAwait(false);
        }

        public Task<MailResult> SendSmtpTestAsync(string to, CancellationToken token = default)
        {
            var configuration = _settings.GetSmtpConfiguration();
            var recipient = string.IsNullOrWhiteSpace(to) ? configuration.From : to;
            return SendSmtpAsync(configuration, new List<string> { recipient }, "Automatica test mail",
                $"This is a test mail from your Automatica server ({_time.GetLocalNow():F}).\n\nThe SMTP settings work.", token);
        }

        internal async Task<MailResult> SendSmtpAsync(SmtpConfiguration configuration, IList<string> to, string subject, string body, CancellationToken token)
        {
            if (!configuration.IsComplete)
            {
                return MailResult.Failed(SmtpTransport, "Host, port and sender address are required");
            }

            if (!MailboxAddress.TryParse(configuration.From, out var from))
            {
                return MailResult.Failed(SmtpTransport, $"Invalid sender address {configuration.From}");
            }

            if (!string.IsNullOrWhiteSpace(configuration.FromName))
            {
                from.Name = configuration.FromName;
            }

            var recipients = ParseRecipients(to, out var invalid);
            foreach (var address in invalid)
            {
                _logger.LogWarning($"Invalid mail address {address} skipped");
            }

            if (recipients.Count == 0)
            {
                return MailResult.Failed(SmtpTransport, "No valid recipient");
            }

            var message = new MimeMessage();
            message.From.Add(from);
            message.To.AddRange(recipients);
            message.Subject = subject ?? string.Empty;
            message.Body = new TextPart("plain") { Text = body ?? string.Empty };

            try
            {
                await _transport.SendAsync(configuration, message, token).ConfigureAwait(false);
                _logger.LogInformation($"Mail \"{subject}\" sent to {recipients.Count} recipient(s) via {configuration.Host}");
                return MailResult.Ok(SmtpTransport);
            }
            catch (Exception e) when (e is not OperationCanceledException || !token.IsCancellationRequested)
            {
                // never log the password: MailKit errors contain the server answer only
                _logger.LogError(e, $"Could not send mail \"{subject}\" via {configuration.Host}:{configuration.Port}");
                return MailResult.Failed(SmtpTransport, e.Message);
            }
        }

        internal static List<MailboxAddress> ParseRecipients(IEnumerable<string> to, out List<string> invalid)
        {
            invalid = new List<string>();
            var result = new List<MailboxAddress>();
            foreach (var raw in (to ?? Enumerable.Empty<string>()).SelectMany(a => (a ?? string.Empty).Split(';', ',')).Select(a => a.Trim()).Where(a => a.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (MailboxAddress.TryParse(raw, out var address) && address.Address.Contains('@'))
                {
                    result.Add(address);
                }
                else
                {
                    invalid.Add(raw);
                }
            }

            return result;
        }

        private bool TryTakeSlot()
        {
            var now = _time.GetUtcNow();
            lock (_lock)
            {
                while (_sent.Count > 0 && now - _sent.Peek() >= TimeSpan.FromHours(1))
                {
                    _sent.Dequeue();
                }

                if (_sent.Count >= MaxSmtpMailsPerHour)
                {
                    return false;
                }

                _sent.Enqueue(now);
                return true;
            }
        }
    }
}
