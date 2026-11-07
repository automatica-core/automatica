using System;
using System.Linq;
using System.Threading.Tasks;
using P3.Driver.Loxone.Miniserver.Driver.Data.Message;
using Xunit;
using static P3.Driver.Loxone.Miniserver.Tests.LoxoneFrames;

namespace P3.Driver.Loxone.Miniserver.Tests
{
    public class EventTableParsingTests
    {
        private static T Parse<T>(HeaderIdentifier identifier, byte[] payload) where T : BinaryMessage
        {
            var header = new Header(HeaderBytes(identifier, (uint)payload.Length));
            return Assert.IsType<T>(BinaryMessage.GenerateBinaryMessage(header, payload));
        }

        #region value states

        [Fact]
        public void ValueStates_SeveralEntries_AreKeyedByLowercaseUuid()
        {
            var msg = Parse<EventTableOfValueStates>(HeaderIdentifier.EventTableOfValueStates,
                Concat(ValueState(UuidA, 1.5), ValueState(UuidB, -2), ValueState(UuidC, 0)));

            Assert.Equal(3, msg.Values.Count);
            Assert.Equal(1.5, msg.Values[UuidAString]);
            Assert.Equal(-2, msg.Values[UuidBString]);
            Assert.Equal(0, msg.Values[UuidCString]);
            Assert.All(msg.Values.Keys, key => Assert.Equal(key.ToLowerInvariant(), key));
        }

        [Fact]
        public void ValueStates_TrailingIncompleteEntry_IsIgnored()
        {
            var msg = Parse<EventTableOfValueStates>(HeaderIdentifier.EventTableOfValueStates,
                Concat(ValueState(UuidA, 3), new byte[23]));

            Assert.Equal(3, msg.Values.Single().Value);
        }

        [Fact]
        public void ValueStates_Empty_HasNoValues()
        {
            var msg = Parse<EventTableOfValueStates>(HeaderIdentifier.EventTableOfValueStates, Array.Empty<byte>());

            Assert.Empty(msg.Values);
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.MinValue)]
        [InlineData(1e-300)]
        public void ValueStates_SpecialDoubles_AreReadBitExact(double value)
        {
            var msg = Parse<EventTableOfValueStates>(HeaderIdentifier.EventTableOfValueStates, ValueState(UuidA, value));

            Assert.Equal(BitConverter.DoubleToInt64Bits(value), BitConverter.DoubleToInt64Bits(msg.Values[UuidAString]));
        }

        [Fact]
        public void ValueStates_DuplicateUuid_Throws()
        {
            var header = new Header(HeaderBytes(HeaderIdentifier.EventTableOfValueStates, 48));

            Assert.Throws<ArgumentException>(() => BinaryMessage.GenerateBinaryMessage(header, Concat(ValueState(UuidA, 1), ValueState(UuidA, 2))));
        }

        #endregion

        #region text states

        [Theory]
        [InlineData("")]
        [InlineData("a")]
        [InlineData("ab")]
        [InlineData("abc")]
        [InlineData("abcd")]
        [InlineData("abcde")]
        public void TextStates_TextLengthsNeedingDifferentPadding_AreParsed(string text)
        {
            var payload = Concat(TextState(UuidA, UuidB, text), TextState(UuidC, UuidB, "next"));

            var msg = Parse<EventTableOfTextStates>(HeaderIdentifier.EventTableOfTextStates, payload);

            var values = msg.Values.ToDictionary(a => a.Key.Uuid, a => a.Value);
            Assert.Equal(text, values[UuidAString].Text);
            Assert.Equal("next", values[UuidCString].Text);
        }

        [Fact]
        public void TextStates_Utf8Text_IsDecoded()
        {
            var msg = Parse<EventTableOfTextStates>(HeaderIdentifier.EventTableOfTextStates, TextState(UuidA, UuidB, "Grüße 21 °C €"));

            Assert.Equal("Grüße 21 °C €", msg.Values.Single().Value.Text);
        }

        [Fact]
        public void TextStates_StateAndIconUuid_AreParsedSeparately()
        {
            var msg = Parse<EventTableOfTextStates>(HeaderIdentifier.EventTableOfTextStates, TextState(UuidA, UuidC, "x"));

            var entry = msg.Values.Single();
            Assert.Equal(UuidAString, entry.Key.Uuid);
            Assert.Equal(UuidCString, entry.Value.IconId.Uuid);
        }

        [Fact]
        public void TextStates_TextLengthBeyondPayload_Throws()
        {
            var payload = Concat(UuidA, UuidB, BitConverter.GetBytes(64u), new byte[8]);
            var header = new Header(HeaderBytes(HeaderIdentifier.EventTableOfTextStates, (uint)payload.Length));

            Assert.ThrowsAny<ArgumentException>(() => BinaryMessage.GenerateBinaryMessage(header, payload));
        }

        [Fact]
        public void TextStates_HugeTextLength_ThrowsInsteadOfAllocating()
        {
            var payload = Concat(UuidA, UuidB, BitConverter.GetBytes(uint.MaxValue), new byte[8]);
            var header = new Header(HeaderBytes(HeaderIdentifier.EventTableOfTextStates, (uint)payload.Length));

            Assert.ThrowsAny<ArgumentException>(() => BinaryMessage.GenerateBinaryMessage(header, payload));
        }

        #endregion

        #region daytimer states

        [Fact]
        public void DaytimerStates_EntriesAreParsedWithAllFields()
        {
            var payload = DaytimerBytes(UuidA, 12.5,
                DaytimerEntryBytes(1, 60, 120, 0, 21.5),
                DaytimerEntryBytes(2, 600, 1440, 1, 18));

            var msg = Parse<EventTableOfDaytimerStates>(HeaderIdentifier.EventTableOfDaytimerStates, payload);

            var daytimer = msg.Values.Single();
            Assert.Equal(UuidAString, daytimer.Key.Uuid);
            Assert.Equal(12.5, daytimer.Value.DefaultValue);
            Assert.Equal(2, daytimer.Value.DaytimerEntries.Count);

            var first = daytimer.Value.DaytimerEntries[0];
            Assert.Equal(1, first.Mode);
            Assert.Equal(60, first.From);
            Assert.Equal(120, first.To);
            Assert.Equal(0, first.NeedActivate);
            Assert.Equal(21.5, first.Value);

            var second = daytimer.Value.DaytimerEntries[1];
            Assert.Equal(2, second.Mode);
            Assert.Equal(600, second.From);
            Assert.Equal(1440, second.To);
            Assert.Equal(1, second.NeedActivate);
            Assert.Equal(18, second.Value);
        }

        [Fact]
        public void DaytimerStates_SeveralDaytimersWithAndWithoutEntries_AreParsed()
        {
            var payload = Concat(
                DaytimerBytes(UuidA, 1),
                DaytimerBytes(UuidB, 2, DaytimerEntryBytes(0, 0, 10, 0, 5)),
                DaytimerBytes(UuidC, 3));

            var msg = Parse<EventTableOfDaytimerStates>(HeaderIdentifier.EventTableOfDaytimerStates, payload);

            var values = msg.Values.ToDictionary(a => a.Key.Uuid, a => a.Value);
            Assert.Equal(3, values.Count);
            Assert.Empty(values[UuidAString].DaytimerEntries);
            Assert.Equal(5, values[UuidBString].DaytimerEntries.Single().Value);
            Assert.Equal(3, values[UuidCString].DefaultValue);
        }

        [Fact]
        public void DaytimerStates_Empty_HasNoValues()
        {
            var msg = Parse<EventTableOfDaytimerStates>(HeaderIdentifier.EventTableOfDaytimerStates, Array.Empty<byte>());

            Assert.Empty(msg.Values);
        }

        [Fact]
        public void DaytimerStates_SecondDaytimerWithTooManyEntries_Throws()
        {
            var payload = Concat(DaytimerBytes(UuidA, 1), DaytimerWithCount(UuidB, 2, 2, DaytimerEntryBytes(0, 0, 1, 0, 0)));
            var header = new Header(HeaderBytes(HeaderIdentifier.EventTableOfDaytimerStates, (uint)payload.Length));

            Assert.Throws<ArgumentException>(() => BinaryMessage.GenerateBinaryMessage(header, payload));
        }

        [Fact]
        public void DaytimerStates_TrailingBytesShorterThanADaytimer_Throws()
        {
            var payload = Concat(DaytimerBytes(UuidA, 1), new byte[10]);
            var header = new Header(HeaderBytes(HeaderIdentifier.EventTableOfDaytimerStates, (uint)payload.Length));

            Assert.Throws<ArgumentException>(() => BinaryMessage.GenerateBinaryMessage(header, payload));
        }

        [Fact]
        public void DaytimerEntry_WrongLength_Throws()
        {
            Assert.Throws<ArgumentException>(() => P3.Driver.Loxone.Miniserver.Driver.Data.Message.DaytimerEntry.Parse(new byte[23]));
        }

        #endregion

        #region weather states

        [Fact]
        public void WeatherStates_EntriesAreParsedWithAllFields()
        {
            var entry = WeatherEntryBytes(3600, 2, 180, 500, 65, 21.5, 20, 12.25, 0.5, 3.75, 1013.25);
            var payload = WeatherBytes(UuidA, 1234, 1, entry);

            var msg = Parse<EventTableOfWeatherStates>(HeaderIdentifier.EventTableOfWeatherStates, payload);

            var weather = msg.Values.Single();
            Assert.Equal(UuidAString, weather.Key.Uuid);
            Assert.Equal(1234u, weather.Value.LastUpdate);

            var w = weather.Value.WeatherEntries.Single();
            Assert.Equal(3600, w.Timestamp);
            Assert.Equal(new DateTime(2009, 1, 1, 1, 0, 0, DateTimeKind.Utc), w.TimestampDate);
            Assert.Equal(DateTimeKind.Utc, w.TimestampDate.Kind);
            Assert.Equal(2, w.WeatherType);
            Assert.Equal(180, w.WindDirection);
            Assert.Equal(500, w.SolarRadiation);
            Assert.Equal(65, w.RelativeHumidity);
            Assert.Equal(21.5, w.Temperature);
            Assert.Equal(20, w.PerceivedTemperature);
            Assert.Equal(12.25, w.DewPoint);
            Assert.Equal(0.5, w.Precipitation);
            Assert.Equal(3.75, w.WindSpeed);
            Assert.Equal(1013.25, w.BarometicPressure);
        }

        [Fact]
        public void WeatherStates_SeveralEntries_KeepTheirOrder()
        {
            var payload = WeatherBytes(UuidA, 1, 3,
                WeatherEntryBytes(0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0),
                WeatherEntryBytes(3600, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0),
                WeatherEntryBytes(7200, 0, 0, 0, 0, 3, 0, 0, 0, 0, 0));

            var msg = Parse<EventTableOfWeatherStates>(HeaderIdentifier.EventTableOfWeatherStates, payload);

            Assert.Equal(new[] { 1d, 2d, 3d }, msg.Values.Single().Value.WeatherEntries.Select(a => a.Temperature));
        }

        [Fact]
        public void WeatherStates_NoEntries_IsParsed()
        {
            var msg = Parse<EventTableOfWeatherStates>(HeaderIdentifier.EventTableOfWeatherStates, WeatherBytes(UuidA, 99, 0));

            Assert.Empty(msg.Values.Single().Value.WeatherEntries);
        }

        [Fact]
        public void WeatherStates_EntryCountBeyondPayload_Throws()
        {
            var payload = WeatherBytes(UuidA, 1, 2, WeatherEntryBytes(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
            var header = new Header(HeaderBytes(HeaderIdentifier.EventTableOfWeatherStates, (uint)payload.Length));

            Assert.ThrowsAny<ArgumentException>(() => BinaryMessage.GenerateBinaryMessage(header, payload));
        }

        [Fact]
        public void WeatherEntry_WrongLength_Throws()
        {
            Assert.Throws<ArgumentException>(() => P3.Driver.Loxone.Miniserver.Driver.Data.Message.WeatherEntry.Parse(new byte[67]));
        }

        [Fact(Skip = "BUG: EventTableOfWeatherStates does not validate the entry count (the daytimer table does), a negative count jumps back to offset 0 and the parser re-reads the table until a random LoxoneUuid hash collision throws a duplicate key - EventTableOfWeatherStates.cs:106-118")]
        public void WeatherStates_NegativeEntryCountJumpingBackwards_IsRejectedImmediately()
        {
            // 16 weather blocks without entries (16 * 24 = 384 bytes) and one block with -6 entries:
            // 384 + 24 - 6 * 68 = 0, the parser starts again at offset 0
            var blocks = Enumerable.Range(0, 16).Select(_ => WeatherBytes(UuidA, 0, 0)).ToList();
            blocks.Add(WeatherBytes(UuidB, 0, -6));
            var payload = Concat(blocks.ToArray());
            var header = new Header(HeaderBytes(HeaderIdentifier.EventTableOfWeatherStates, (uint)payload.Length));

            var parse = Task.Run(() =>
            {
                try
                {
                    BinaryMessage.GenerateBinaryMessage(header, payload);
                    return (Exception)null;
                }
                catch (Exception e)
                {
                    return e;
                }
            });

            Assert.True(parse.Wait(TimeSpan.FromSeconds(2)), "parsing did not finish");
            var exception = Assert.IsAssignableFrom<ArgumentException>(parse.Result);
            Assert.Contains("count", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region generic

        [Fact]
        public void GenerateBinaryMessage_TextMessage_IsNotSupported()
        {
            Assert.Throws<NotSupportedException>(() => BinaryMessage.GenerateBinaryMessage(HeaderIdentifier.TextMessage, new byte[4], null));
        }

        [Theory]
        [InlineData(HeaderIdentifier.BinaryFile)]
        [InlineData(HeaderIdentifier.OutOfService)]
        [InlineData(HeaderIdentifier.KeepAlive)]
        [InlineData((HeaderIdentifier)99)]
        public void GenerateBinaryMessage_IdentifiersWithoutEventTable_ReturnNull(HeaderIdentifier identifier)
        {
            Assert.Null(BinaryMessage.GenerateBinaryMessage(identifier, new byte[24], null));
        }

        [Fact]
        public void Header_ParsesIdentifierInfoAndLittleEndianLength()
        {
            var header = new Header(new byte[] { 0x03, 0x04, 0x80, 0x00, 0x78, 0x56, 0x34, 0x12 });

            Assert.Equal(HeaderIdentifier.EventTableOfDaytimerStates, header.Identifier);
            Assert.Equal(0x80, header.Info);
            Assert.Equal(0x12345678u, header.DataLength);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(7)]
        [InlineData(9)]
        public void Header_WrongLength_Throws(int length)
        {
            Assert.Throws<ArgumentException>(() => new Header(new byte[length]));
        }

        [Theory]
        [InlineData(15)]
        [InlineData(17)]
        public void LoxoneUuid_WrongLength_Throws(int length)
        {
            Assert.Throws<ArgumentException>(() => new LoxoneUuid(new byte[length]));
        }

        [Fact]
        public void LoxoneUuid_FormatsLikeTheMiniserver()
        {
            Assert.Equal(UuidAString, new LoxoneUuid(UuidA.ToArray()).Uuid);
            Assert.Equal(UuidBString, new LoxoneUuid(UuidB.ToArray()).Uuid);
            Assert.Equal(UuidCString, new LoxoneUuid(UuidC.ToArray()).Uuid);
        }

        [Fact]
        public void LoxoneUuid_SameUuid_IsEqual()
        {
            Assert.Equal(new LoxoneUuid(UuidA.ToArray()), new LoxoneUuid(UuidAString));
            Assert.NotEqual(new LoxoneUuid(UuidA.ToArray()), new LoxoneUuid(UuidBString));
        }

        [Fact(Skip = "BUG: LoxoneUuid overrides Equals but GetHashCode returns the reference hash, so dictionary lookups by uuid (text/daytimer/weather tables) never hit and Values[uuid] never overwrites - LoxoneUuid.cs:46")]
        public void LoxoneUuid_EqualUuids_HaveEqualHashCodes()
        {
            var msg = Parse<EventTableOfTextStates>(HeaderIdentifier.EventTableOfTextStates, TextState(UuidA, UuidB, "x"));

            Assert.Equal(new LoxoneUuid(UuidAString).GetHashCode(), new LoxoneUuid(UuidAString).GetHashCode());
            Assert.True(msg.Values.ContainsKey(new LoxoneUuid(UuidAString)));
        }

        #endregion
    }
}
