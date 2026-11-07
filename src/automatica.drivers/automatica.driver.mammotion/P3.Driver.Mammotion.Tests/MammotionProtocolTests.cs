using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using P3.Driver.Mammotion.Driver;
using P3.Driver.Mammotion.Driver.Protocol;
using Xunit;

namespace P3.Driver.Mammotion.Tests
{
    public class MammotionProtocolTests
    {
        private const long Ms = 1762520000123;

        private static List<KeyValuePair<string, string>> LoginParams(string user, string password) => new()
        {
            new("username", user),
            new("password", password),
            new("client_id", "GxebgSt8si6pKqR"),
            new("grant_type", "password"),
            new("authType", "0")
        };

        // expected values computed with Python: hmac(md5(secret).hexdigest(), client_id + ts + "/oauth2/token" + json.dumps(p, ensure_ascii=False, separators=(",", ":")))
        [Fact]
        public void Sign_MatchesPython()
        {
            Assert.Equal("cad89aaebeee0b7017648b1a33d8b63b13c747ddef08a2101ff560a59b2a63d7", MammotionCloud.Sign(Ms, LoginParams("anna@example.com", "cHc=")));
        }

        [Fact]
        public void Sign_NonAsciiQuotesAndSlashes_MatchesPython()
        {
            var p = LoginParams("jörg/\"x\"+1", "a+b/c=");
            Assert.Equal("{\"username\":\"jörg/\\\"x\\\"+1\",\"password\":\"a+b/c=\",\"client_id\":\"GxebgSt8si6pKqR\",\"grant_type\":\"password\",\"authType\":\"0\"}", MammotionCloud.PythonJson(p));
            Assert.Equal("7cb8f6f03020c6bc42b6144a7ef134b683b319be6aee7cf1a17f95728b8131fe", MammotionCloud.Sign(Ms, p));
        }

        [Theory]
        [InlineData("{\"iot\":\"https://api-iot-business-eu-dcdn.mammotion.com\"}", "https://api-iot-business-eu-dcdn.mammotion.com")]
        [InlineData("{\"iot\":\"api-iot-business-eu-dcdn.mammotion.com/\"}", "https://api-iot-business-eu-dcdn.mammotion.com")]
        [InlineData("{\"sub\":\"1\"}", null)]
        public void IotFromJwt(string payload, string expected)
        {
            Assert.Equal(expected, MammotionCloud.IotFromJwt(MammotionDriverTests.Jwt(payload)));
        }

        [Fact]
        public void IotFromJwt_Garbage_IsNull()
        {
            Assert.Null(MammotionCloud.IotFromJwt("not-a-jwt"));
            Assert.Null(MammotionCloud.IotFromJwt("a.%%%.c"));
        }

        [Fact]
        public void TaskControl_EncodesTheLubaMsgByteForByte()
        {
            var bytes = LubaMessages.TaskControl(MowerAction.Start, 5, 1234, 1000);

            // msgtype 240, sender 7, rcver 1, msgattr 1, seqs 5, version 1, subtype 1234, nav { todev_taskctrl (37) { type 1, action 1, result 0 } }, timestamp 1000
            Assert.Equal("08F00110071801200128053001" + "38D209" + "5A09" + "AA0206080110011800" + "78E807", Convert.ToHexString(bytes));
        }

        [Fact]
        public void TaskControl_SeqWrapsAtOneByte()
        {
            var msg = ProtoReader.Read(LubaMessages.TaskControl(MowerAction.ReturnToDock, 0x1FF, 1, 0));
            Assert.Equal(0xFFUL, ProtoReader.Find(msg, 5)!.Value.Varint);
            var ctrl = ProtoReader.Sub(ProtoReader.Sub(msg, 11), 37);
            Assert.Equal(5, ProtoReader.Find(ctrl, 2)!.Value.Int32);
        }

        [Fact]
        public void RequestReports_AsksForContinuousReports()
        {
            var msg = ProtoReader.Read(LubaMessages.RequestReports(1, 42, Ms));
            Assert.Equal(244, ProtoReader.Find(msg, 1)!.Value.Int32);
            Assert.Equal(42, ProtoReader.Find(msg, 7)!.Value.Int32);
            Assert.Equal(Ms, ProtoReader.Find(msg, 15)!.Value.Int64);
            var cfg = ProtoReader.Sub(ProtoReader.Sub(msg, 10), 38);
            Assert.Equal(0, ProtoReader.Find(cfg, 1)!.Value.Int32);
            Assert.Equal(3000, ProtoReader.Find(cfg, 3)!.Value.Int32);
            Assert.Equal(0, ProtoReader.Find(cfg, 5)!.Value.Int32);
            Assert.Equal(new[] { 1, 3, 4, 6, 10, 8 }, cfg.Where(f => f.Number == 6).Select(f => f.Int32));
        }

        internal static byte[] ReportData(int state, int charge, int battery, int progressPercent, int area, int remaining, int total, int knife)
        {
            var dev = new ProtoWriter().Varint(1, state).Varint(2, charge).Varint(3, battery);
            var work = new ProtoWriter().Varint(1, 7).Varint(3, ((long)remaining << 16) | (long)total).Varint(4, ((long)progressPercent << 16) | (long)area).Varint(20, knife);
            var report = new ProtoWriter().Message(2, dev).Message(5, work);
            var sys = new ProtoWriter().Message(39, report);
            return new ProtoWriter().Varint(1, 244).Varint(2, 1).Varint(3, 7).Message(10, sys).ToArray();
        }

        [Fact]
        public void Decode_ReportData_SplitsAreaAndProgress()
        {
            var status = LubaMessages.Decode(ReportData(13, 0, 76, 45, 820, 33, 60, 55));

            Assert.Equal(13, status.DeviceState);
            Assert.Equal(0, status.ChargeState);
            Assert.Equal(76, status.Battery);
            Assert.Equal(45, status.Progress);
            Assert.Equal(820, status.Area);
            Assert.Equal(33, status.RemainingMinutes);
            Assert.Equal(55, status.KnifeHeight);
        }

        [Fact]
        public void Decode_BatteryAndWorkState()
        {
            var sys = new ProtoWriter()
                .Message(1, new ProtoWriter().Varint(1, 64))
                .Message(2, new ProtoWriter().Varint(1, 15).Varint(2, 1));
            var status = LubaMessages.Decode(new ProtoWriter().Varint(1, 244).Message(10, sys).ToArray());

            Assert.Equal(64, status.Battery);
            Assert.Equal(15, status.DeviceState);
            Assert.Equal(1, status.ChargeState);
            Assert.Null(status.Progress);
        }

        [Fact]
        public void Decode_OtherMessage_IsEmpty()
        {
            var nav = new ProtoWriter().Message(37, new ProtoWriter().Varint(1, 1));
            Assert.True(LubaMessages.Decode(new ProtoWriter().Varint(1, 240).Message(11, nav).ToArray()).IsEmpty);
        }

        [Fact]
        public void Decode_Truncated_Throws()
        {
            var bytes = ReportData(13, 0, 76, 45, 820, 33, 60, 55);
            Assert.Throws<System.IO.InvalidDataException>(() => LubaMessages.Decode(bytes.AsMemory(0, bytes.Length - 3)));
        }

        [Fact]
        public void Reader_NegativeInt32_IsSignExtended()
        {
            var field = ProtoReader.Read(new ProtoWriter().Varint(1, -2).ToArray()).Single();
            Assert.Equal(-2, field.Int32);
            Assert.Equal(10, new ProtoWriter().Varint(1, -2).ToArray().Length - 1);
        }

        [Theory]
        [InlineData("mqtt-eu.mammotion.com", "mqtt-eu.mammotion.com", 8883, true)]
        [InlineData("mqtt-eu.mammotion.com:1883", "mqtt-eu.mammotion.com", 1883, true)]
        [InlineData("ssl://mqtt-eu.mammotion.com:8883", "mqtt-eu.mammotion.com", 8883, true)]
        [InlineData("tcp://127.0.0.1", "127.0.0.1", 1883, false)]
        public void ParseHost(string value, string host, int port, bool tls)
        {
            Assert.Equal((host, port, tls), MqttNetConnection.ParseHost(value));
        }

        [Theory]
        [InlineData("/sys/pk1/Luba-ABC/thing/event/device_protobuf_msg_event/post", "pk1", "Luba-ABC")]
        [InlineData("/sys/proto/pk1/Luba-ABC/thing/event/device_protobuf_msg_event/post", "pk1", "Luba-ABC")]
        [InlineData("/sys/pk1/Luba-ABC/app/down/thing/status", "pk1", "Luba-ABC")]
        [InlineData("/other/pk1/Luba-ABC", null, null)]
        public void ParseTopic(string topic, string pk, string dn)
        {
            Assert.Equal((pk, dn), MammotionDriver.ParseTopic(topic));
        }

        [Theory]
        [InlineData("{\"action\":\"online\",\"iotId\":\"x\"}", true)]
        [InlineData("{\"action\":\"offline\"}", false)]
        [InlineData("{\"method\":\"thing.status\",\"params\":{\"status\":{\"value\":\"1\"}}}", true)]
        [InlineData("{\"method\":\"thing.status\",\"params\":{\"status\":{\"value\":\"3\"}}}", false)]
        [InlineData("{\"foo\":1}", null)]
        public void ParseOnline(string json, bool? expected)
        {
            Assert.Equal(expected, MammotionDriver.ParseOnline(Encoding.UTF8.GetBytes(json)));
        }
    }
}
