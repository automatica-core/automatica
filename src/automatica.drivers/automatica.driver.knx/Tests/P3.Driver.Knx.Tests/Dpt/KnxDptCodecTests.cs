using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Knx.Falcon;
using Knx.Falcon.ApplicationData;
using Knx.Falcon.ApplicationData.DatapointTypes;
using P3.Driver.Knx.DriverFactory.Dpt;
using Xunit;

namespace P3.Driver.Knx.Tests.Dpt
{
    /// <summary>
    /// Encoding/decoding of the datapoint types in <see cref="KnxDptCatalog"/>. Every vector is checked against the
    /// KNX standard byte layout and, where possible, cross-checked with the Falcon SDK (generated from the KNX master data).
    /// </summary>
    public class KnxDptCodecTests
    {
        private const string V = KnxDptCodec.SingleValueKey;

        private static IKnxDptCodec Codec(string key, int subType = 0)
        {
            var entry = KnxDptCatalog.GetByKey(key);
            Assert.NotNull(entry);
            return entry.CreateCodec(subType == 0 ? entry.DefaultSubType : subType);
        }

        private static byte[] Hex(string hex) => Convert.FromHexString(hex.Replace(" ", "").Replace("-", ""));

        private static string Encode(string key, int subType, string changedKey, params (string Key, object Value)[] values)
        {
            var data = Codec(key, subType).Encode(values.ToDictionary(a => a.Key, a => a.Value), changedKey ?? values.Last().Key);
            return Convert.ToHexString(data);
        }

        private static string EncodeSingle(string key, object value, int subType = 0) => Encode(key, subType, V, (V, value));

        private static IReadOnlyDictionary<string, object> Decode(string key, string hex, int subType = 0) => Codec(key, subType).Decode(Hex(hex));

        private static object Falcon(int main, int sub, string hex) => DptFactory.Default.Get(main, sub).ToValue(new GroupValue(Hex(hex)));

        private static string FalconEncode(int main, int sub, object value) => Convert.ToHexString(DptFactory.Default.Get(main, sub).ToGroupValue(value).Value);

        private static void AssertFalconFields(int main, int sub, string hex, params (string Field, object Expected)[] expected)
        {
            var compound = Assert.IsType<KnxCompoundValue>(Falcon(main, sub, hex));
            foreach (var (field, value) in expected)
            {
                Assert.True(compound.TryGetValue(field, out var actual), $"Falcon has no field '{field}' ({string.Join("|", compound.Keys)})");
                if (value is bool b)
                {
                    Assert.Equal(b, Convert.ToBoolean(actual));
                }
                else
                {
                    Assert.Equal(Convert.ToDouble(value), Convert.ToDouble(actual), 3);
                }
            }
        }

        // ------------------------------------------------------------------ catalogue

        [Fact]
        public void Catalog_GuidsAndKeysAreUnique()
        {
            var guids = new List<Guid>();
            foreach (var entry in KnxDptCatalog.Entries)
            {
                guids.Add(entry.TemplateGuid);
                if (entry.IsComposite)
                {
                    guids.Add(entry.InterfaceGuid);
                }

                guids.AddRange(entry.Children.Select(a => a.Guid));
            }

            Assert.DoesNotContain(Guid.Empty, guids);
            Assert.Equal(guids.Count, guids.Distinct().Count());

            // property templates are derived from the template guid (last byte + 1..10), they must not hit another template
            var derived = KnxDptCatalog.Entries.SelectMany(e => Enumerable.Range(1, 10).Select(i => Derive(e.TemplateGuid, i)));
            Assert.Empty(derived.Intersect(guids));

            var keys = KnxDptCatalog.Entries.Select(a => a.Key).ToList();
            Assert.Equal(keys.Count, keys.Distinct().Count());
        }

        private static Guid Derive(Guid guid, int c)
        {
            var bytes = guid.ToByteArray();
            bytes[^1] = (byte)(bytes[^1] + c);
            return new Guid(bytes);
        }

        [Fact]
        public void Catalog_ChildrenMatchCodecValues()
        {
            foreach (var entry in KnxDptCatalog.Entries)
            {
                foreach (var subType in entry.SubTypes.DefaultIfEmpty(entry.DefaultSubType))
                {
                    var codec = entry.CreateCodec(subType);
                    if (entry.IsComposite)
                    {
                        Assert.Equal(codec.Values.Select(a => a.Key).OrderBy(a => a), entry.Children.Select(a => a.FieldKey).OrderBy(a => a));
                    }
                    else
                    {
                        Assert.Equal(V, Assert.Single(codec.Values).Key);
                    }
                }
            }
        }

        [Fact]
        public void Catalog_DefaultSubTypeIsDefined()
        {
            foreach (var entry in KnxDptCatalog.Entries.Where(a => a.SubTypeEnum != null))
            {
                Assert.True(Enum.IsDefined(entry.SubTypeEnum, entry.DefaultSubType), entry.Key);
            }
        }

        [Theory]
        [InlineData(4, 1, "knx-dpt4")]
        [InlineData(14, 56, "knx-dpt14")]
        [InlineData(225, 1, "knx-dpt225.001")]
        [InlineData(225, 2, "knx-dpt225.002")]
        [InlineData(232, 600, "knx-dpt232")]
        [InlineData(251, 600, "knx-dpt251")]
        [InlineData(242, 600, "knx-dpt242")]
        public void Catalog_FindsEtsDatapointType(int main, int sub, string key)
        {
            Assert.Equal(key, KnxDptCatalog.GetByDpt(main, sub).Key);
        }

        [Fact]
        public void Catalog_UnsupportedEtsDatapointType()
        {
            Assert.Null(KnxDptCatalog.GetByDpt(254, 600));
        }

        [Fact]
        public void ShortValuesAreSentAsShortGroupValue()
        {
            var codec = Codec("knx-dpt23");
            var value = KnxDptCatalog.ToGroupValue(codec, codec.Encode(new Dictionary<string, object> { { V, 2 } }, V));
            Assert.True(value.IsShort);
            Assert.Equal(2, value.SizeInBit);
            Assert.Equal(new byte[] { 2 }, value.Value);
        }

        // ------------------------------------------------------------------ float helpers

        [Theory]
        [InlineData(21.0, "0C1A")]
        [InlineData(21.5, "0C33")]
        [InlineData(-21.5, "8BCD")]
        [InlineData(0, "0000")]
        [InlineData(-0.01, "87FF")]
        [InlineData(670433.28, "7FFE")]
        public void Float16_Encode(double value, string expected)
        {
            Assert.Equal(expected, KnxFloat16.Encode(value).ToString("X4"));
            Assert.Equal(expected, FalconEncode(9, 1, (float)value));
        }

        [Fact]
        public void Float16_InvalidIsNull()
        {
            Assert.Null(KnxFloat16.Decode(0x7FFF));
        }

        [Fact]
        public void Float16_OutOfRangeIsClampedToLargestValidValue()
        {
            Assert.Equal(0x7FFE, KnxFloat16.Encode(670760));
            Assert.Equal(0x7FFE, KnxFloat16.Encode(1e9));
            Assert.Equal(0xF800, KnxFloat16.Encode(-1e9));
        }

        // ------------------------------------------------------------------ single value types

        [Theory]
        [InlineData(1, "A", "41")]
        [InlineData(1, "ä", "3F")]
        [InlineData(2, "ä", "E4")]
        [InlineData(2, "", "00")]
        public void Dpt4_Character(int sub, string value, string expected)
        {
            Assert.Equal(expected, EncodeSingle("knx-dpt4", value, sub));
        }

        [Fact]
        public void Dpt4_Decode()
        {
            Assert.Equal("ä", Decode("knx-dpt4", "E4", 2)[V]);
            Assert.Equal(FalconEncode(4, 2, 'ä'), EncodeSingle("knx-dpt4", "ä", 2));
            Assert.Throws<FormatException>(() => Decode("knx-dpt4", "E4", 1));
        }

        [Fact]
        public void Dpt12_Unsigned32()
        {
            Assert.Equal("EE6B2800", EncodeSingle("knx-dpt12", 4000000000L));
            Assert.Equal("EE6B2800", FalconEncode(12, 1, 4000000000u));
            Assert.Equal(4000000000.0, Decode("knx-dpt12", "EE6B2800")[V]);
            Assert.Equal("00000000", EncodeSingle("knx-dpt12", -5));
            Assert.Equal("FFFFFFFF", EncodeSingle("knx-dpt12", 1e12));
        }

        [Theory]
        [InlineData(56, 1234.5, "449A5000")]
        [InlineData(68, -21.25, "C1AA0000")]
        [InlineData(19, 0.1, "3DCCCCCD")]
        public void Dpt14_Float32(int sub, double value, string expected)
        {
            Assert.Equal(expected, EncodeSingle("knx-dpt14", value, sub));
            Assert.Equal(expected, FalconEncode(14, sub, (float)value));
            Assert.Equal(value, Decode("knx-dpt14", expected, sub)[V]);
        }

        [Fact]
        public void Dpt17_SceneNumber()
        {
            Assert.Equal("3F", EncodeSingle("knx-dpt17", 63));
            Assert.Equal("05", EncodeSingle("knx-dpt17", 5));
            Assert.Equal(FalconEncode(17, 1, (byte)5), EncodeSingle("knx-dpt17", 5));
            Assert.Equal(5, Decode("knx-dpt17", "C5")[V]); // reserved bits are ignored
        }

        [Fact]
        public void Dpt19_DateTime_Encode()
        {
            var dt = new DateTime(2026, 1, 15, 13, 45, 30); // Thursday
            var flags = dt.IsDaylightSavingTime() ? "21" : "20";
            Assert.Equal($"7E010F8D2D1E{flags}00", EncodeSingle("knx-dpt19", dt));

            var falcon = new KnxDateTime(dt, DayOfWeek.Thursday, false, null, true, true, true, true, dt.IsDaylightSavingTime(), false);
            Assert.Equal(FalconEncode(19, 1, falcon), EncodeSingle("knx-dpt19", dt));
        }

        [Fact]
        public void Dpt19_DateTime_Decode()
        {
            Assert.Equal(new DateTime(2026, 1, 15, 13, 45, 30), Decode("knx-dpt19", "7E010F8D2D1E2000")[V]);
            Assert.Equal(new DateTime(2026, 1, 16, 0, 0, 0), Decode("knx-dpt19", "7E010F98000020 00")[V]); // 24:00
            Assert.Equal(new DateTime(2026, 1, 15), Decode("knx-dpt19", "7E010F8D2D1E2200")[V]); // no time
            Assert.Empty(Decode("knx-dpt19", "7E010F8D2D1EA000")); // fault
            Assert.Empty(Decode("knx-dpt19", "7E010F8D2D1E3000")); // no year
            Assert.Throws<FormatException>(() => Decode("knx-dpt19", "7E0D0F8D2D1E2000")); // month 13
        }

        [Theory]
        [InlineData(102, 3, "03")]
        [InlineData(1, 255, "FF")]
        public void Dpt20_Enum8(int sub, int value, string expected)
        {
            Assert.Equal(expected, EncodeSingle("knx-dpt20", value, sub));
            Assert.Equal(expected, FalconEncode(20, sub, (byte)value));
            Assert.Equal(value, Decode("knx-dpt20", expected, sub)[V]);
        }

        [Fact]
        public void Dpt23_Enum2()
        {
            Assert.Equal("02", EncodeSingle("knx-dpt23", 2));
            Assert.Equal("03", EncodeSingle("knx-dpt23", 7)); // clamped to 2 bit
            Assert.Equal(2, Decode("knx-dpt23", "02")[V]);
            Assert.Equal("02", FalconEncode(23, 1, (sbyte)2));
        }

        [Fact]
        public void Dpt24_VariableString8859()
        {
            Assert.Equal("48E400", EncodeSingle("knx-dpt24", "Hä"));
            Assert.Equal(FalconEncode(24, 1, "Hä"), EncodeSingle("knx-dpt24", "Hä"));
            Assert.Equal("00", EncodeSingle("knx-dpt24", ""));
            Assert.Equal("Hä", Decode("knx-dpt24", "48E400")[V]);
            Assert.Equal("H", Decode("knx-dpt24", "48")[V]);
        }

        [Fact]
        public void Dpt28_VariableStringUtf8()
        {
            Assert.Equal("48C3A400", EncodeSingle("knx-dpt28", "Hä"));
            Assert.Equal(FalconEncode(28, 1, "Hä"), EncodeSingle("knx-dpt28", "Hä"));
            Assert.Equal("Hä€", Decode("knx-dpt28", "48C3A4E282AC00")[V]);
        }

        [Fact]
        public void Dpt29_Signed64()
        {
            Assert.Equal("FFFFFFFFFFFFFFFE", EncodeSingle("knx-dpt29", -2L));
            Assert.Equal(FalconEncode(29, 10, -2L), EncodeSingle("knx-dpt29", -2L));
            Assert.Equal("0020000000000001", EncodeSingle("knx-dpt29", 9007199254740993L)); // 2^53 + 1 keeps precision
            Assert.Equal("00000000000004D2", EncodeSingle("knx-dpt29", 1234.4));
            Assert.Equal(-2.0, Decode("knx-dpt29", "FFFFFFFFFFFFFFFE")[V]);
        }

        [Fact]
        public void Dpt234_LanguageCode()
        {
            Assert.Equal("6465", EncodeSingle("knx-dpt234", "de"));
            Assert.Equal(FalconEncode(234, 1, "de"), EncodeSingle("knx-dpt234", "de"));
            Assert.Equal("6500", EncodeSingle("knx-dpt234", "e"));
            Assert.Equal("AT", Decode("knx-dpt234", "4154", 2)[V]);
            Assert.Throws<FormatException>(() => Decode("knx-dpt234", "415441"));
        }

        // ------------------------------------------------------------------ multi-field types

        [Fact]
        public void Dpt15_AccessData()
        {
            var hex = Encode("knx-dpt15", 0, "code", ("code", 123456), ("error", true), ("permission", false), ("direction", false), ("encrypted", true), ("index", 10));
            Assert.Equal("1234569A", hex);
            AssertFalconFields(15, 0, hex, ("Field0", 1), ("Field5", 6), ("Detection error", true), ("Permission", false),
                ("Encryption of access information", true), ("Index of access identification code", 10));

            var decoded = Decode("knx-dpt15", "1234569A");
            Assert.Equal(123456, decoded["code"]);
            Assert.Equal(true, decoded["error"]);
            Assert.Equal(true, decoded["encrypted"]);
            Assert.Equal(10, decoded["index"]);
            Assert.Throws<FormatException>(() => Decode("knx-dpt15", "A234569A"));
        }

        [Fact]
        public void Dpt18_SceneControl()
        {
            Assert.Equal("05", Encode("knx-dpt18", 0, "activate", ("activate", 5), ("learn", 9)));
            Assert.Equal("89", Encode("knx-dpt18", 0, "learn", ("activate", 5), ("learn", 9)));
            Assert.Equal("89", FalconEncode(18, 1, new KnxSceneControl(true, 9)));
            Assert.Equal(9, Decode("knx-dpt18", "89")["learn"]);
            Assert.Equal(5, Decode("knx-dpt18", "05")["activate"]);
            Assert.False(Decode("knx-dpt18", "05").ContainsKey("learn"));
        }

        [Fact]
        public void Dpt21_BitSet8()
        {
            Assert.Equal("81", Encode("knx-dpt21", 0, "bit7", ("bit0", true), ("bit7", true), ("value", 3)));
            Assert.Equal("42", Encode("knx-dpt21", 0, "value", ("bit0", true), ("value", 0x42)));

            var decoded = Decode("knx-dpt21", "81");
            Assert.Equal(129, decoded["value"]);
            Assert.Equal(true, decoded["bit0"]);
            Assert.Equal(false, decoded["bit1"]);
            Assert.Equal(true, decoded["bit7"]);
        }

        [Fact]
        public void Dpt22_BitSet16()
        {
            Assert.Equal("8001", Encode("knx-dpt22", 0, "value", ("value", 0x8001)));
            Assert.Equal("8000", Encode("knx-dpt22", 0, "bit15", ("bit15", true)));
            Assert.Equal(true, Decode("knx-dpt22", "8001")["bit15"]);
            Assert.Equal(true, Decode("knx-dpt22", "8001")["bit0"]);
            Assert.Equal(false, Decode("knx-dpt22", "8001")["bit8"]);
        }

        [Fact]
        public void Dpt25_DoubleNibble()
        {
            var hex = Encode("knx-dpt25", 0, null, ("busy", 3), ("nak", 2));
            Assert.Equal("32", hex);
            AssertFalconFields(25, 1000, hex, ("Busy", 3), ("Nak", 2));
            Assert.Equal(3, Decode("knx-dpt25", "32")["busy"]);
        }

        [Fact]
        public void Dpt26_SceneInfo()
        {
            Assert.Equal("05", Encode("knx-dpt26", 0, null, ("scene", 5), ("active", true)));
            Assert.Equal("45", Encode("knx-dpt26", 0, null, ("scene", 5), ("active", false)));
            Assert.Equal("05", FalconEncode(26, 1, new KnxSceneInfo(true, 5)));
            Assert.Equal("45", FalconEncode(26, 1, new KnxSceneInfo(false, 5)));
            Assert.Equal(false, Decode("knx-dpt26", "45")["active"]);
            Assert.Equal(5, Decode("knx-dpt26", "45")["scene"]);
        }

        [Fact]
        public void Dpt27_CombinedInfoOnOff()
        {
            var hex = Encode("knx-dpt27", 0, null, ("output1", true), ("output16", false));
            Assert.Equal("80010001", hex);
            AssertFalconFields(27, 1, hex, ("Mask Bit Info On Off Output 1", true), ("Mask Bit Info On Off Output 16", true),
                ("Mask Bit Info On Off Output 2", false), ("Info On Off Output 1", true), ("Info On Off Output 16", false));

            var decoded = Decode("knx-dpt27", "80018000");
            Assert.Equal(true, decoded["output16"]);
            Assert.Equal(false, decoded["output1"]);
            Assert.False(decoded.ContainsKey("output2"));
        }

        [Fact]
        public void Dpt206_TimeDelayAndMode()
        {
            var hex = Encode("knx-dpt206", 0, null, ("delay", 300), ("mode", 1));
            Assert.Equal("012C01", hex);
            AssertFalconFields(206, 100, hex, ("delay time (min)", 300), ("HVAC mode", 1));
        }

        [Fact]
        public void Dpt207_StatusLightingActuator()
        {
            var hex = Encode("knx-dpt207", 0, null, ("actual-value", 100), ("failure", true), ("actual-value-valid", true));
            Assert.Equal("FF81", hex);
            AssertFalconFields(207, 600, hex, ("ActualValue", 100), ("Failure", true), ("Locked", false), ("ValidActualValue", true));

            Assert.Equal("8001", Encode("knx-dpt207", 0, null, ("actual-value", 50.2), ("actual-value-valid", true)));
            Assert.Equal(50.2, Decode("knx-dpt207", "8001")["actual-value"]);
            Assert.Equal(true, Decode("knx-dpt207", "8002")["locked"]);
        }

        [Fact]
        public void Dpt217_Version()
        {
            var hex = Encode("knx-dpt217", 0, null, ("magic", 1), ("version", 2), ("revision", 3));
            Assert.Equal("0883", hex);
            AssertFalconFields(217, 1, hex, ("Magic Number", 1), ("Version Number", 2), ("Revision Number", 3));
        }

        [Fact]
        public void Dpt219_AlarmInfo()
        {
            var hex = Encode("knx-dpt219", 0, null, ("log-number", 1), ("priority", 2), ("application-area", 3), ("error-class", 4),
                ("ack-supported", true), ("in-alarm", true), ("locked", true));
            Assert.Equal("010203040105", hex);
            AssertFalconFields(219, 1, hex, ("Log Number", 1), ("Alarm Priority", 2), ("Application Area", 3), ("Error Class", 4),
                ("Ack_Sup", true), ("TimeStamp_Sup", false), ("InAlarm", true), ("Locked", true), ("AlarmUnAck", false));
        }

        [Fact]
        public void Dpt222_Setpoints3()
        {
            var hex = Encode("knx-dpt222", 0, null, ("comfort", 21), ("standby", 19), ("economy", 17));
            Assert.Equal("0C1A076C06A4", hex);
            AssertFalconFields(222, 100, hex, ("TempSetpComf", 21), ("TempSetpStdby", 19), ("TempSetpEco", 17));
            Assert.Equal(21.0, Decode("knx-dpt222", hex)["comfort"]);
        }

        [Fact]
        public void Dpt225_ScalingSpeed()
        {
            var hex = Encode("knx-dpt225.001", 0, null, ("time", 1000), ("percent", 100));
            Assert.Equal("000AFF", hex);
            AssertFalconFields(225, 1, hex, ("time period", 1000), ("percent", 100));
            Assert.Equal(1000, Decode("knx-dpt225.001", hex)["time"]);
        }

        [Fact]
        public void Dpt225_ScalingStepTime()
        {
            var hex = Encode("knx-dpt225.002", 0, null, ("time", 1000), ("percent", 50.2));
            Assert.Equal("03E880", hex);
            AssertFalconFields(225, 2, hex, ("time period", 1000), ("percent", 50.196));
        }

        [Fact]
        public void Dpt229_MeteringValue()
        {
            var hex = Encode("knx-dpt229", 0, null, ("count", -1), ("value-information", 4), ("fault", true));
            Assert.Equal("FFFFFFFF0402", hex);
            AssertFalconFields(229, 1, hex, ("CountVal", -1), ("ValInfField", 4), ("Fault", true), ("OutOfService", false), ("InAlarm", false));
            Assert.Equal(-1, Decode("knx-dpt229", hex)["count"]);
        }

        [Fact]
        public void Dpt230_MBusAddress()
        {
            var hex = Encode("knx-dpt230", 0, null, ("manufacturer", 0x1234), ("ident-number", 0x12345678), ("version", 1), ("medium", 7));
            Assert.Equal("1234123456780107", hex);
            AssertFalconFields(230, 1000, hex, ("ManufactID", 0x1234), ("IdentNumber", 0x12345678), ("Version", 1), ("Medium", 7));
            Assert.Equal((double)0x12345678, Decode("knx-dpt230", hex)["ident-number"]);
        }

        [Fact]
        public void Dpt232_Rgb()
        {
            Assert.Equal("010203", Encode("knx-dpt232", 0, "red", ("red", 1), ("green", 2), ("blue", 3)));
            Assert.Equal("010203", FalconEncode(232, 600, new KnxColorRGB(1, 2, 3)));
            Assert.Equal("FF8000", Encode("knx-dpt232", 0, "hex", ("red", 1), ("hex", "#FF8000")));

            var decoded = Decode("knx-dpt232", "FF8000");
            Assert.Equal("#FF8000", decoded["hex"]);
            Assert.Equal(255, decoded["red"]);
            Assert.Equal(128, decoded["green"]);
            Assert.Throws<ArgumentException>(() => Encode("knx-dpt232", 0, "hex", ("hex", "#FF80")));
        }

        [Fact]
        public void Dpt235_TariffActiveEnergy()
        {
            var hex = Encode("knx-dpt235", 0, null, ("energy", -2), ("tariff", 5), ("energy-invalid", true), ("tariff-invalid", false));
            Assert.Equal("FFFFFFFE0502", hex);
            AssertFalconFields(235, 1, hex, ("ActiveElectricalEnergy", -2), ("Tariff", 5), ("ElectricalEngergyValidity", true), ("TariffValidity", false));
            Assert.Equal(true, Decode("knx-dpt235", "FFFFFFFE0501")["tariff-invalid"]);
        }

        [Fact]
        public void Dpt236_PrioritisedModeControl()
        {
            var hex = Encode("knx-dpt236", 0, null, ("deactivate", true), ("priority", 3), ("mode", 5));
            Assert.Equal("B5", hex);
            AssertFalconFields(236, 1, hex, ("Field0", true), ("priority level", 3), ("mode level", 5));
        }

        [Fact]
        public void Dpt237_DaliControlGearDiagnostic()
        {
            var hex = Encode("knx-dpt237", 0, null, ("ballast-failure", true), ("read-response", true), ("address", 10));
            Assert.Equal("028A", hex);
            AssertFalconFields(237, 600, hex, ("Convertor error", false), ("Ballast Failure", true), ("Lamp Failure", false),
                ("Read or Response", true), ("Address Indicator", false), ("DALI Device Address or DALI Group Address", 10));
        }

        [Fact]
        public void Dpt238_DaliDiagnostics()
        {
            Assert.Equal("FF", Encode("knx-dpt238", 0, null, ("ballast-failure", true), ("lamp-failure", true), ("address", 63)));
            var hex = Encode("knx-dpt238", 0, null, ("lamp-failure", true), ("address", 1));
            Assert.Equal("41", hex);
            AssertFalconFields(238, 600, hex, ("Ballast Failure", false), ("Lamp Failure", true), ("Device Address", 1));
        }

        [Fact]
        public void Dpt240_CombinedPosition()
        {
            var hex = Encode("knx-dpt240", 0, null, ("height", 100), ("slats", 50.2));
            Assert.Equal("FF8003", hex);
            AssertFalconFields(240, 800, hex, ("Height Position", 100), ("Slats Position", 50.196), ("Validity Height Position", true), ("Validity Slats Position", true));

            Assert.Equal("00FF01", Encode("knx-dpt240", 0, null, ("slats", 100))); // height unknown => invalid
            var decoded = Decode("knx-dpt240", "40FF02");
            Assert.Equal(25.1, decoded["height"]);
            Assert.False(decoded.ContainsKey("slats"));
        }

        [Fact]
        public void Dpt242_ColourXyY()
        {
            var hex = Encode("knx-dpt242", 0, null, ("x", 0.5), ("y", 0.25), ("brightness", 255));
            Assert.Equal("80004000FF03", hex);
            AssertFalconFields(242, 600, hex, ("x-axis", 32768), ("y-axis", 16384), ("brightness", 255), ("Validity xy", true), ("Validity brightness", true));

            var decoded = Decode("knx-dpt242", hex);
            Assert.Equal(0.5, decoded["x"]);
            Assert.Equal(0.25, decoded["y"]);
            Assert.Equal(255, decoded["brightness"]);

            Assert.Equal("000000008001", Encode("knx-dpt242", 0, null, ("brightness", 128))); // xy unknown => invalid
            Assert.False(Decode("knx-dpt242", "80004000FF01").ContainsKey("x"));
        }

        [Fact]
        public void Dpt246_BatteryInfo()
        {
            var hex = Encode("knx-dpt246", 0, null, ("fully-charged", true), ("charge-level", 80));
            Assert.Equal("0450", hex);
            AssertFalconFields(246, 600, hex, ("Battery Fully Charged", true), ("Battery Duration Failure", false), ("Battery Failure", false), ("Battery Charge Level", 80));
        }

        [Fact]
        public void Dpt251_Rgbw()
        {
            Assert.Equal("0A141E28000F", Encode("knx-dpt251", 0, "red", ("red", 10), ("green", 20), ("blue", 30), ("white", 40)));
            Assert.Equal("0A141E28000F", FalconEncode(251, 600, new KnxColorRGBW(10, 20, 30, 40)));
            Assert.Equal("0A0000000008", Encode("knx-dpt251", 0, "red", ("red", 10)));
            Assert.Equal("FF00008000" + "0F", Encode("knx-dpt251", 0, "hex", ("hex", "#FF000080")));

            var decoded = Decode("knx-dpt251", "0A141E280009");
            Assert.Equal(10, decoded["red"]);
            Assert.Equal(40, decoded["white"]);
            Assert.False(decoded.ContainsKey("green"));
            Assert.False(decoded.ContainsKey("hex"));
            Assert.Equal("#0A141E28", Decode("knx-dpt251", "0A141E28000F")["hex"]);
        }

        [Fact]
        public void Dpt255_GeographicalLocation()
        {
            var hex = Encode("knx-dpt255", 0, null, ("longitude", 16.37), ("latitude", 48.21));
            var expected = Convert.ToHexString(BitConverter.GetBytes(16.37f).Reverse().Concat(BitConverter.GetBytes(48.21f).Reverse()).ToArray());
            Assert.Equal(expected, hex);
            AssertFalconFields(255, 1, hex, ("Longitude", 16.37), ("Latitude", 48.21));
            Assert.Equal(16.37, Decode("knx-dpt255", hex)["longitude"]);
        }

        [Fact]
        public void Dpt275_Setpoints4()
        {
            var hex = Encode("knx-dpt275", 0, null, ("comfort", 21), ("standby", 19), ("economy", 17), ("protection", 7));
            Assert.Equal("0C1A076C06A402BC", hex);
            AssertFalconFields(275, 100, hex, ("room temperature setpoint comfort", 21), ("room temperature setpoint building protection", 7));
            Assert.Equal(7.0, Decode("knx-dpt275", hex)["protection"]);
        }

        [Fact]
        public void AllCodecsRoundTripTheirDefaults()
        {
            foreach (var entry in KnxDptCatalog.Entries)
            {
                foreach (var subType in entry.SubTypes.DefaultIfEmpty(entry.DefaultSubType))
                {
                    var codec = entry.CreateCodec(subType);
                    var values = codec.Values.Where(a => a.Key != "hex").ToDictionary(a => a.Key, a => SampleValue(a.DataType));
                    var data = codec.Encode(values, values.Keys.First());

                    if (codec.SizeInBit > 0)
                    {
                        Assert.Equal(Math.Max(1, (codec.SizeInBit + 7) / 8), data.Length);
                    }

                    var decoded = codec.Decode(data);
                    Assert.NotEmpty(decoded);

                    // the payload must also be accepted by Falcon (same size as defined in the KNX master data)
                    var falcon = DptFactory.Default.Get(entry.MainType, subType == 0 ? entry.DefaultSubType : subType);
                    if (!falcon.IsVariableLength)
                    {
                        Assert.Equal((int)falcon.SizeInBit, codec.SizeInBit);
                    }
                }
            }
        }

        private static object SampleValue(Automatica.Core.Base.Templates.NodeDataType type)
        {
            switch (type)
            {
                case Automatica.Core.Base.Templates.NodeDataType.Boolean:
                    return true;
                case Automatica.Core.Base.Templates.NodeDataType.String:
                    return "de";
                case Automatica.Core.Base.Templates.NodeDataType.DateTime:
                    return new DateTime(2026, 3, 1, 10, 0, 0);
                case Automatica.Core.Base.Templates.NodeDataType.Double:
                    return 1.0;
                default:
                    return 1;
            }
        }
    }
}
