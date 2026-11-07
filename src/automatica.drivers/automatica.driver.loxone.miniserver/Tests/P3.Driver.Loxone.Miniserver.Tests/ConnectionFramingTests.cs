using System;
using System.Linq;
using P3.Driver.Loxone.Miniserver.Driver.Data.Message;
using Xunit;
using static P3.Driver.Loxone.Miniserver.Tests.LoxoneFrames;

namespace P3.Driver.Loxone.Miniserver.Tests
{
    /// <summary>
    /// Framing of the binary websocket stream (LoxoneMiniserverConnection.ProcessData / TryReadData).
    /// </summary>
    public class ConnectionFramingTests
    {
        [Fact]
        public void ProcessData_ThreeMessagesOfDifferentTypesInOneFrame_AllParsedInOrder()
        {
            var (connection, messages) = CreateConnection();

            var frame = Concat(
                ValueMessage((UuidA, 1)),
                Message(HeaderIdentifier.EventTableOfTextStates, TextState(UuidB, UuidC, "abc")),
                ValueMessage((UuidB, 3)));

            connection.ProcessData(frame);

            Assert.Equal(3, messages.Count);
            Assert.Equal(1, SingleValue(messages[0]));
            Assert.Equal("abc", Assert.IsType<EventTableOfTextStates>(messages[1]).Values.Single().Value.Text);
            Assert.Equal(3, SingleValue(messages[2]));
        }

        [Fact]
        public void ProcessData_CompleteMessageFollowedByPartialHeader_PartialIsKeptForNextFrame()
        {
            var (connection, messages) = CreateConnection();
            var second = ValueMessage((UuidB, 2));

            connection.ProcessData(Concat(ValueMessage((UuidA, 1)), second.Take(5).ToArray()));
            Assert.Single(messages);

            connection.ProcessData(second.Skip(5).ToArray());

            Assert.Equal(new[] { 1d, 2d }, messages.Select(SingleValue));
        }

        [Fact]
        public void ProcessData_PayloadSplitAcrossFrames_ParsedWhenComplete()
        {
            var (connection, messages) = CreateConnection();
            var message = ValueMessage((UuidA, 4.25), (UuidB, -1));

            connection.ProcessData(message.Take(8 + 10).ToArray());
            Assert.Empty(messages);
            connection.ProcessData(message.Skip(8 + 10).Take(20).ToArray());
            Assert.Empty(messages);
            connection.ProcessData(message.Skip(8 + 30).ToArray());

            var values = Assert.IsType<EventTableOfValueStates>(Assert.Single(messages)).Values;
            Assert.Equal(4.25, values[UuidAString]);
            Assert.Equal(-1, values[UuidBString]);
        }

        [Fact]
        public void ProcessData_HeaderSplitAcrossFrames_ParsedWhenComplete()
        {
            var (connection, messages) = CreateConnection();
            var message = ValueMessage((UuidA, 9));

            connection.ProcessData(message.Take(3).ToArray());
            connection.ProcessData(message.Skip(3).Take(4).ToArray());
            Assert.Empty(messages);

            connection.ProcessData(message.Skip(7).ToArray());

            Assert.Equal(9, SingleValue(Assert.Single(messages)));
        }

        [Fact]
        public void ProcessData_MessageDeliveredByteByByte_ParsedExactlyOnce()
        {
            var (connection, messages) = CreateConnection();
            var message = Concat(ValueMessage((UuidA, 5)), ValueMessage((UuidB, 6)));

            foreach (var b in message)
            {
                connection.ProcessData(new[] { b });
            }

            Assert.Equal(new[] { 5d, 6d }, messages.Select(SingleValue));
        }

        [Fact]
        public void ProcessData_EstimatedHeaderInOwnFrame_IsSkippedAndExactHeaderParsed()
        {
            var (connection, messages) = CreateConnection();
            var payload = ValueState(UuidA, 21.5);

            connection.ProcessData(EstimatedHeader(HeaderIdentifier.EventTableOfValueStates, 0));
            Assert.Empty(messages);
            connection.ProcessData(HeaderBytes(HeaderIdentifier.EventTableOfValueStates, (uint)payload.Length));
            Assert.Empty(messages);
            connection.ProcessData(payload);

            Assert.Equal(21.5, SingleValue(Assert.Single(messages)));
        }

        [Fact]
        public void ProcessData_EstimatedHeaderWithLengthOverLimit_DoesNotDropTheStream()
        {
            // the estimated length is only a guess, it must be skipped before the size limit is applied
            var (connection, messages) = CreateConnection();

            var frame = Concat(
                EstimatedHeader(HeaderIdentifier.EventTableOfValueStates, uint.MaxValue),
                ValueMessage((UuidA, 7)));

            connection.ProcessData(frame);

            Assert.Equal(7, SingleValue(Assert.Single(messages)));
        }

        [Fact]
        public void ProcessData_SeveralEstimatedHeadersBeforeExactHeader_AllSkipped()
        {
            var (connection, messages) = CreateConnection();

            var frame = Concat(
                EstimatedHeader(HeaderIdentifier.EventTableOfValueStates, 24),
                EstimatedHeader(HeaderIdentifier.EventTableOfTextStates, 100),
                ValueMessage((UuidA, 8)));

            connection.ProcessData(frame);

            Assert.Equal(8, SingleValue(Assert.Single(messages)));
        }

        [Fact]
        public void ProcessData_EstimatedHeaderWithOtherInfoBitsSet_IsStillTreatedAsEstimated()
        {
            var (connection, messages) = CreateConnection();

            var frame = Concat(
                HeaderBytes(HeaderIdentifier.EventTableOfValueStates, 24, 0xFF),
                ValueMessage((UuidA, 2)));

            connection.ProcessData(frame);

            Assert.Equal(2, SingleValue(Assert.Single(messages)));
        }

        [Fact]
        public void ProcessData_TextHeaderWithLength_OnlyTheHeaderIsConsumed()
        {
            // the text itself arrives as websocket text message, only the 8 byte header is in the binary stream
            var (connection, messages) = CreateConnection();

            var frame = Concat(HeaderBytes(HeaderIdentifier.TextMessage, 1234), ValueMessage((UuidA, 3)));

            connection.ProcessData(frame);

            Assert.Equal(3, SingleValue(Assert.Single(messages)));
        }

        [Fact]
        public void ProcessData_TextHeaderOnly_RaisesNoMessage()
        {
            var (connection, messages) = CreateConnection();

            connection.ProcessData(HeaderBytes(HeaderIdentifier.TextMessage, 42));

            Assert.Empty(messages);

            connection.ProcessData(ValueMessage((UuidA, 1)));
            Assert.Single(messages);
        }

        [Theory]
        [InlineData(HeaderIdentifier.KeepAlive)]
        [InlineData(HeaderIdentifier.OutOfService)]
        public void ProcessData_HeaderOnlyMessages_RaiseNoMessageAndKeepTheStreamInSync(HeaderIdentifier identifier)
        {
            var (connection, messages) = CreateConnection();

            connection.ProcessData(Concat(HeaderBytes(identifier, 0), ValueMessage((UuidA, 11))));

            Assert.Equal(11, SingleValue(Assert.Single(messages)));
        }

        [Fact]
        public void ProcessData_BinaryFile_IsConsumedWithoutMessage()
        {
            var (connection, messages) = CreateConnection();
            var file = Enumerable.Range(0, 37).Select(a => (byte)a).ToArray();

            connection.ProcessData(Concat(Message(HeaderIdentifier.BinaryFile, file), ValueMessage((UuidA, 12))));

            Assert.Equal(12, SingleValue(Assert.Single(messages)));
        }

        [Fact]
        public void ProcessData_UnknownIdentifier_IsConsumedWithoutMessage()
        {
            var (connection, messages) = CreateConnection();

            connection.ProcessData(Concat(Message((HeaderIdentifier)42, new byte[16]), ValueMessage((UuidA, 13))));

            Assert.Equal(13, SingleValue(Assert.Single(messages)));
        }

        [Fact]
        public void ProcessData_DataLengthOverLimit_IsDroppedWithoutMessage()
        {
            var (connection, messages) = CreateConnection();

            // anything behind the oversized header in the same frame is discarded as well
            connection.ProcessData(Concat(HeaderBytes(HeaderIdentifier.EventTableOfValueStates, MaxBinaryMessageLength + 1), ValueMessage((UuidA, 1))));

            Assert.Empty(messages);
        }

        [Fact]
        public void ProcessData_DataLengthUintMax_IsDroppedWithoutThrowing()
        {
            var (connection, messages) = CreateConnection();

            var exception = Record.Exception(() => connection.ProcessData(HeaderBytes(HeaderIdentifier.EventTableOfValueStates, uint.MaxValue)));

            Assert.Null(exception);
            Assert.Empty(messages);
        }

        [Fact]
        public void ProcessData_AfterOversizedMessageWasDropped_NextFrameIsParsed()
        {
            var (connection, messages) = CreateConnection();

            connection.ProcessData(HeaderBytes(HeaderIdentifier.EventTableOfDaytimerStates, MaxBinaryMessageLength + 100));
            connection.ProcessData(ValueMessage((UuidA, 14)));

            Assert.Equal(14, SingleValue(Assert.Single(messages)));
        }

        [Fact]
        public void ProcessData_DataLengthExactlyAtLimit_IsAccepted()
        {
            var (connection, messages) = CreateConnection();

            // a message at the limit is kept, so the value message behind it in the same frame is parsed
            var frame = Concat(Message(HeaderIdentifier.BinaryFile, new byte[MaxBinaryMessageLength]), ValueMessage((UuidA, 15)));

            connection.ProcessData(frame);

            Assert.Equal(15, SingleValue(Assert.Single(messages)));
        }

        [Fact(Skip = "BUG: TryReadData returns forever when the buffer does not start with 0x03 (e.g. the payload frame of a dropped oversized message), the buffer then grows without bound and no later message is parsed - LoxoneMiniserverConnection.cs:272")]
        public void ProcessData_PayloadOfDroppedOversizedMessage_DoesNotBlockLaterMessages()
        {
            var (connection, messages) = CreateConnection();

            // the miniserver sends header and payload as separate websocket frames
            connection.ProcessData(HeaderBytes(HeaderIdentifier.EventTableOfValueStates, MaxBinaryMessageLength + 24));
            connection.ProcessData(new byte[] { 0x10, 0x20, 0x30, 0x40 });
            connection.ProcessData(ValueMessage((UuidA, 16)));

            Assert.Equal(16, SingleValue(Assert.Single(messages)));
        }

        [Fact]
        public void ProcessData_InvalidDaytimerMessage_IsDroppedWithoutThrowing_AndNextMessageParsed()
        {
            var (connection, messages) = CreateConnection();

            // entry count 5 but no entries follow
            var invalid = Message(HeaderIdentifier.EventTableOfDaytimerStates, DaytimerWithCount(UuidA, 0, 5));
            var frame = Concat(invalid, ValueMessage((UuidB, 17)));

            var exception = Record.Exception(() => connection.ProcessData(frame));

            Assert.Null(exception);
            Assert.Equal(17, SingleValue(Assert.Single(messages)));
        }

        [Fact]
        public void ProcessData_TruncatedTextMessage_IsDroppedWithoutThrowing_AndNextMessageParsed()
        {
            var (connection, messages) = CreateConnection();

            // text length says 100 bytes, only 4 follow
            var payload = UuidA.Concat(UuidB).Concat(BitConverter.GetBytes(100u)).Concat(new byte[4]).ToArray();
            var frame = Concat(Message(HeaderIdentifier.EventTableOfTextStates, payload), ValueMessage((UuidC, 18)));

            var exception = Record.Exception(() => connection.ProcessData(frame));

            Assert.Null(exception);
            Assert.Equal(18, SingleValue(Assert.Single(messages)));
        }

        [Fact]
        public void ProcessData_EmptyTextTable_IsDroppedWithoutThrowing()
        {
            var (connection, messages) = CreateConnection();

            var exception = Record.Exception(() => connection.ProcessData(Concat(HeaderBytes(HeaderIdentifier.EventTableOfTextStates, 0), ValueMessage((UuidA, 19)))));

            Assert.Null(exception);
            Assert.Equal(19, SingleValue(Assert.Single(messages)));
        }

        [Fact]
        public void ProcessData_WeatherTableWithTooManyEntries_IsDroppedWithoutThrowing()
        {
            var (connection, messages) = CreateConnection();

            var frame = Concat(Message(HeaderIdentifier.EventTableOfWeatherStates, WeatherBytes(UuidA, 1, 3)), ValueMessage((UuidB, 20)));

            var exception = Record.Exception(() => connection.ProcessData(frame));

            Assert.Null(exception);
            Assert.Equal(20, SingleValue(Assert.Single(messages)));
        }

        [Fact]
        public void ProcessData_DuplicateUuidInValueTable_IsDroppedWithoutThrowing()
        {
            var (connection, messages) = CreateConnection();

            var frame = Concat(ValueMessage((UuidA, 1), (UuidA, 2)), ValueMessage((UuidB, 21)));

            var exception = Record.Exception(() => connection.ProcessData(frame));

            Assert.Null(exception);
            Assert.Equal(21, SingleValue(Assert.Single(messages)));
        }

        [Fact]
        public void ProcessData_EmptyValueTable_RaisesMessageWithoutValues()
        {
            var (connection, messages) = CreateConnection();

            connection.ProcessData(HeaderBytes(HeaderIdentifier.EventTableOfValueStates, 0));

            Assert.Empty(Assert.IsType<EventTableOfValueStates>(Assert.Single(messages)).Values);
        }

        [Fact]
        public void ProcessData_Null_IsIgnored()
        {
            var (connection, messages) = CreateConnection();

            connection.ProcessData(null);
            connection.ProcessData(ValueMessage((UuidA, 22)));

            Assert.Equal(22, SingleValue(Assert.Single(messages)));
        }

        [Fact]
        public void ProcessData_EmptyArray_IsIgnored()
        {
            var (connection, messages) = CreateConnection();

            connection.ProcessData(Array.Empty<byte>());

            Assert.Empty(messages);
        }

        [Fact]
        public void ProcessData_MessageHeaderIsPassedToTheMessage()
        {
            var (connection, messages) = CreateConnection();

            connection.ProcessData(ValueMessage((UuidA, 1), (UuidB, 2)));

            var header = Assert.Single(messages).Header;
            Assert.Equal(HeaderIdentifier.EventTableOfValueStates, header.Identifier);
            Assert.Equal(48u, header.DataLength);
            Assert.Equal(0, header.Info);
        }

        [Fact]
        public void ProcessData_ManyMessagesInOneFrame_AllParsed()
        {
            var (connection, messages) = CreateConnection();

            var frame = Concat(Enumerable.Range(0, 200).Select(i => ValueMessage((UuidA, i))).ToArray());

            connection.ProcessData(frame);

            Assert.Equal(Enumerable.Range(0, 200).Select(i => (double)i), messages.Select(SingleValue));
        }
    }
}
