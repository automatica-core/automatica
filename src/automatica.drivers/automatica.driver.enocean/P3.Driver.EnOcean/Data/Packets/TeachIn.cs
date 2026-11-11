using System;

namespace P3.Driver.EnOcean.Data.Packets
{
    /// <summary>
    /// How a device asks to be taught in.
    /// </summary>
    public enum TeachInKind
    {
        /// <summary>
        /// RPS (rocker, key card, window handle): every telegram, no profile in it.
        /// </summary>
        Rps,

        /// <summary>
        /// 1BS teach-in (LRN bit cleared): only D5-00-01 uses 1BS.
        /// </summary>
        OneBs,

        /// <summary>
        /// 4BS teach-in without profile (variation 1): the user has to pick the profile.
        /// </summary>
        FourBs,

        /// <summary>
        /// 4BS teach-in with profile and manufacturer (variation 2/3), a bidirectional device waits for a response.
        /// </summary>
        FourBsWithEep,

        /// <summary>
        /// Universal teach-in (UTE, RORG D4) of VLD and bidirectional devices: profile, manufacturer and channels.
        /// </summary>
        Ute
    }

    /// <summary>
    /// What a teach-in telegram tells about the device. Parse returns null for telegrams that are no teach-in.
    /// </summary>
    public sealed class TeachIn
    {
        private TeachIn(TeachInKind kind, Rorg rorg)
        {
            Kind = kind;
            Rorg = rorg;
        }

        public TeachInKind Kind { get; }

        /// <summary>
        /// The RORG of the profile: for UTE the RORG the device sends its data with (e.g. D2), not D4.
        /// </summary>
        public Rorg Rorg { get; private set; }

        /// <summary>
        /// FUNC of the profile, null if the telegram does not carry it.
        /// </summary>
        public byte? Func { get; private set; }

        /// <summary>
        /// TYPE of the profile, null if the telegram does not carry it.
        /// </summary>
        public byte? Type { get; private set; }

        /// <summary>
        /// 11 bit manufacturer id, null if the telegram does not carry it.
        /// </summary>
        public int? Manufacturer { get; private set; }

        /// <summary>
        /// The device waits for a teach-in response (4BS bidirectional query, UTE with response expected).
        /// </summary>
        public bool ResponseExpected { get; private set; }

        /// <summary>
        /// UTE: a deletion (teach-out) request, never answered with a teach-in.
        /// </summary>
        public bool IsDeletion { get; private set; }

        /// <summary>
        /// The profile is known (RORG, FUNC and TYPE).
        /// </summary>
        public bool HasProfile => Func.HasValue && Type.HasValue;

        /// <summary>
        /// The profile as written in the EEP spec, e.g. A5-20-01, null if unknown.
        /// </summary>
        public string Eep => HasProfile ? $"{(byte)Rorg:X2}-{Func.Value:X2}-{Type.Value:X2}" : null;

        /// <returns>null if <paramref name="radio"/> is no teach-in telegram</returns>
        public static TeachIn Parse(RadioErp1Packet radio)
        {
            if (radio == null)
            {
                return null;
            }

            var data = radio.Data.Span;
            switch (radio.Rorg)
            {
                case Rorg.Rps:
                    return new TeachIn(TeachInKind.Rps, Rorg.Rps);
                case Rorg.OneBs:
                    if (!RadioErp1Packet.IsLrnTeachIn(radio))
                    {
                        return null;
                    }

                    return new TeachIn(TeachInKind.OneBs, Rorg.OneBs) { Func = 0x00, Type = 0x01 };
                case Rorg.FourBs:
                    if (!RadioErp1Packet.IsLrnTeachIn(radio))
                    {
                        return null;
                    }

                    // DB0.7 LRN type: 1 = with EEP and manufacturer, DB0.4 LRN status: 0 = query
                    if ((data[3] & 0x80) == 0)
                    {
                        return new TeachIn(TeachInKind.FourBs, Rorg.FourBs);
                    }

                    var func = (byte)(data[0] >> 2);
                    return new TeachIn(TeachInKind.FourBsWithEep, Rorg.FourBs)
                    {
                        Func = func,
                        Type = (byte)(((data[0] & 0x03) << 5) | (data[1] >> 3)),
                        Manufacturer = ((data[1] & 0x07) << 8) | data[2],
                        // only A5-20 HVAC components wait for the controller
                        ResponseExpected = (data[3] & 0x10) == 0 && func == 0x20
                    };
                case Rorg.Ute:
                    return ParseUte(data);
            }

            return null;
        }

        /// <summary>
        /// UTE teach-in query (DB6 first): DB6.7 bidirectional, DB6.6 0 = response expected, DB6.5..4 request
        /// (00 teach-in or deletion, 01 teach-in, 10 deletion), DB6.3..0 command 0 = query; DB5 channels,
        /// DB4..DB3 manufacturer, DB2 TYPE, DB1 FUNC, DB0 RORG.
        /// </summary>
        private static TeachIn ParseUte(ReadOnlySpan<byte> data)
        {
            if (data.Length < 7 || (data[0] & 0x0F) != 0)
            {
                return null;
            }

            return new TeachIn(TeachInKind.Ute, (Rorg)data[6])
            {
                Func = data[5],
                Type = data[4],
                Manufacturer = ((data[3] & 0x07) << 8) | data[2],
                ResponseExpected = (data[0] & 0x40) == 0,
                IsDeletion = ((data[0] >> 4) & 0x03) == 0x02
            };
        }

        /// <summary>
        /// UTE teach-in response to <paramref name="query"/> (the user data of the query, DB6 first): bidirectional,
        /// teach-in accepted, command 1 = response, channels, manufacturer and profile copied from the query.
        /// </summary>
        public static byte[] UteResponse(ReadOnlySpan<byte> query)
        {
            if (query.Length < 7)
            {
                throw new ArgumentException("A UTE query has 7 data bytes", nameof(query));
            }

            var response = query.Slice(0, 7).ToArray();
            // DB6.7 bidirectional, DB6.5..4 = 01 request accepted (teach-in successful), DB6.3..0 = 1 response
            response[0] = 0x91;
            return response;
        }
    }
}
