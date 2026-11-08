using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Internals.Cache.Common;

namespace Automatica.Core.Internals.Mail
{
    /// <summary>
    /// How the connection to the SMTP server is secured
    /// </summary>
    public enum SmtpSecurity
    {
        /// <summary>
        /// TLS on connect for port 465, STARTTLS when the server offers it on the other ports
        /// </summary>
        [EnumName("COMMON.PROPERTY.SMTPSECURITY.AUTO")]
        Auto = 0,

        /// <summary>
        /// Plain connection, upgraded with STARTTLS; fails if the server does not offer it
        /// </summary>
        [EnumName("COMMON.PROPERTY.SMTPSECURITY.STARTTLS")]
        StartTls = 1,

        /// <summary>
        /// TLS from the first byte (usually port 465)
        /// </summary>
        [EnumName("COMMON.PROPERTY.SMTPSECURITY.SSL")]
        SslOnConnect = 2,

        /// <summary>
        /// No encryption at all (only for servers in the local network)
        /// </summary>
        [EnumName("COMMON.PROPERTY.SMTPSECURITY.NONE")]
        None = 3
    }

    /// <summary>
    /// The SMTP server the core sends its mails through, instead of the Automatica cloud
    /// </summary>
    public sealed record SmtpConfiguration(
        bool Enabled,
        string Host,
        int Port,
        SmtpSecurity Security,
        string User,
        string Password,
        string From,
        string FromName,
        bool AcceptInvalidCertificate)
    {
        public bool IsComplete => !string.IsNullOrWhiteSpace(Host) && Port is > 0 and < 65536 && !string.IsNullOrWhiteSpace(From);
    }

    /// <summary>
    /// Server settings of the group <see cref="Group"/> (seeded by DatabaseInit, edited on the settings page).
    /// <see cref="PasswordKey"/> has the type password, so it is encrypted in the database.
    /// </summary>
    public static class SmtpSettings
    {
        public const string Group = "SERVER.SMTP";

        public const string EnabledKey = "smtpEnabled";
        public const string HostKey = "smtpHost";
        public const string PortKey = "smtpPort";
        public const string SecurityKey = "smtpSecurity";
        public const string UserKey = "smtpUser";
        public const string PasswordKey = "smtpPassword";
        public const string FromKey = "smtpFrom";
        public const string FromNameKey = "smtpFromName";
        public const string AcceptInvalidCertificateKey = "smtpAcceptInvalidCertificate";

        /// <summary>
        /// A button on the settings page that sends a test mail
        /// </summary>
        public const string TestKey = "smtpTest";

        public const int DefaultPort = 587;

        public static SmtpConfiguration GetSmtpConfiguration(this ISettingsCache cache)
        {
            return new SmtpConfiguration(
                cache.GetByKey(EnabledKey)?.ValueInt is > 0,
                cache.GetByKey(HostKey)?.ValueText?.Trim(),
                cache.GetByKey(PortKey)?.ValueInt ?? DefaultPort,
                Enum.IsDefined(typeof(SmtpSecurity), cache.GetByKey(SecurityKey)?.ValueInt ?? 0)
                    ? (SmtpSecurity)(cache.GetByKey(SecurityKey)?.ValueInt ?? 0)
                    : SmtpSecurity.Auto,
                cache.GetByKey(UserKey)?.ValueText?.Trim(),
                cache.GetByKey(PasswordKey)?.ValueText,
                cache.GetByKey(FromKey)?.ValueText?.Trim(),
                cache.GetByKey(FromNameKey)?.ValueText?.Trim(),
                cache.GetByKey(AcceptInvalidCertificateKey)?.ValueInt is > 0);
        }
    }
}
