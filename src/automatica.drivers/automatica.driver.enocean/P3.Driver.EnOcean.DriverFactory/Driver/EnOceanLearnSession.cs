using System;
using System.Collections.Generic;
using System.Globalization;
using Automatica.Core.EF.Models;
using P3.Driver.EnOcean.Data.Packets;

namespace P3.Driver.EnOcean.DriverFactory.Driver
{
    /// <summary>
    /// What the learn dialog gets about one sender. The values travel as property instances next to the serial number
    /// (the learn mode interface of the core has no other place for them); the dialog only copies properties the
    /// created node has, so the "learn-info-*" ones never end up on a node.
    /// </summary>
    internal sealed class EnOceanLearnNotification
    {
        public const string StatusNew = "new";
        public const string StatusKnown = "known";
        public const string StatusReceiving = "receiving";

        public string Serial { get; set; }
        public string Status { get; set; }
        public string Eep { get; set; }
        public string Telegram { get; set; }
        public int? Rssi { get; set; }
        public string Known { get; set; }
        public bool Paired { get; set; }
        public string Description { get; set; }
        public IList<PropertyInstance> Properties { get; set; }
    }

    /// <summary>
    /// One learn mode session: reports every sender once (again only when what is known about it changes) and the
    /// first data telegram of a device that is set up.
    /// </summary>
    internal sealed class EnOceanLearnSession
    {
        private readonly object _lock = new object();
        private readonly Dictionary<string, string> _reported = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _receiving = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _active;

        /// <summary>
        /// The profiles most installations use, offered first when the telegram does not tell the profile.
        /// </summary>
        private static readonly string[] CommonProfiles =
        {
            "F6-02-01", "F6-02-02", "F6-03-01", "F6-04-01", "F6-10-00",
            "D5-00-01",
            "A5-02-05", "A5-04-01", "A5-04-02", "A5-06-01", "A5-06-02", "A5-07-01", "A5-08-01", "A5-09-04",
            "A5-10-06", "A5-12-01", "A5-14-09", "A5-20-01", "A5-38-08",
            "D2-01-12", "D2-05-00"
        };

        public bool Active
        {
            get
            {
                lock (_lock)
                {
                    return _active;
                }
            }
        }

        public void Start()
        {
            lock (_lock)
            {
                _reported.Clear();
                _receiving.Clear();
                _active = true;
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                _active = false;
            }
        }

        /// <returns>null if the sender was already reported with the same information</returns>
        public EnOceanLearnNotification TeachIn(string serial, TeachIn teachIn, int? rssi, string known, bool paired)
        {
            var status = known != null ? EnOceanLearnNotification.StatusKnown : EnOceanLearnNotification.StatusNew;
            var telegram = TelegramName(teachIn.Kind);
            var signature = $"{status}|{telegram}|{teachIn.Eep}|{paired}";
            lock (_lock)
            {
                if (_reported.TryGetValue(serial, out var last) && last == signature)
                {
                    return null;
                }
                _reported[serial] = signature;
            }

            return Build(serial, status, teachIn.Eep, telegram, teachIn.Manufacturer, rssi, known, paired);
        }

        /// <returns>null if the first data telegram of the device was already reported</returns>
        public EnOceanLearnNotification Receiving(string serial, int? rssi, string known)
        {
            lock (_lock)
            {
                if (!_receiving.Add(serial))
                {
                    return null;
                }
            }

            return Build(serial, EnOceanLearnNotification.StatusReceiving, null, null, null, rssi, known, false);
        }

        /// <returns>the position of the profile of <paramref name="template"/> in the common profiles, int.MaxValue if it is none</returns>
        public static int CommonRank(Guid template)
        {
            var bytes = template.ToByteArray();
            var eep = $"{bytes[10]:X2}-{bytes[11]:X2}-{bytes[12]:X2}";
            var index = Array.IndexOf(CommonProfiles, eep);
            return index < 0 ? int.MaxValue : index;
        }

        internal static string TelegramName(TeachInKind kind)
        {
            switch (kind)
            {
                case TeachInKind.Rps:
                    return "RPS";
                case TeachInKind.OneBs:
                    return "1BS";
                case TeachInKind.FourBs:
                    return "4BS";
                case TeachInKind.FourBsWithEep:
                    return "4BS EEP";
                case TeachInKind.Ute:
                    return "UTE";
                default:
                    return kind.ToString();
            }
        }

        private static EnOceanLearnNotification Build(string serial, string status, string eep, string telegram, int? manufacturer,
            int? rssi, string known, bool paired)
        {
            var parts = new List<string>();
            if (eep != null)
            {
                parts.Add(eep);
            }
            if (telegram != null)
            {
                parts.Add(telegram);
            }
            if (rssi.HasValue)
            {
                parts.Add($"{rssi.Value} dBm");
            }

            return new EnOceanLearnNotification
            {
                Serial = serial,
                Status = status,
                Eep = eep,
                Telegram = telegram,
                Rssi = rssi,
                Known = known,
                Paired = paired,
                Description = String.Join(" · ", parts),
                Properties = new List<PropertyInstance>
                {
                    Property("enocean-serialnumber", serial),
                    Property("learn-info-status", status),
                    Property("learn-info-profile", eep ?? ""),
                    Property("learn-info-telegram", telegram ?? ""),
                    Property("learn-info-rssi", rssi.HasValue ? rssi.Value.ToString(CultureInfo.InvariantCulture) : ""),
                    Property("learn-info-manufacturer", manufacturer.HasValue ? manufacturer.Value.ToString("X3", CultureInfo.InvariantCulture) : ""),
                    Property("learn-info-known", known ?? ""),
                    Property("learn-info-paired", paired ? "true" : "false")
                }
            };
        }

        private static PropertyInstance Property(string key, string value)
        {
            return new PropertyInstance
            {
                This2PropertyTemplateNavigation = new PropertyTemplate
                {
                    Key = key,
                    This2PropertyType = (long)PropertyTemplateType.Text,
                    This2PropertyTypeNavigation = new PropertyType
                    {
                        Type = (long)PropertyTemplateType.Text
                    }
                },
                Value = value
            };
        }
    }
}
