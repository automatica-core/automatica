using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Automatica.Core.Driver.Utility;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Automatica.Core.UnitTests.Drivers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using P3.Driver.MBus;
using P3.Driver.MBus.Frames;
using P3.Driver.OmsDriverFactory;
using P3.Driver.OmsDriverFactory.Helper;
using Xunit;

namespace P3.Driver.Oms.MBus.Tests
{
    public class OmsDecryptAndDispatchTests : DriverFactoryTestBase<OmsDriverFactory.OmsDriverFactory>
    {
        // made-up test key, not used by any real meter
        private const string TestKeyHex = "000102030405060708090A0B0C0D0E0F";
        private static readonly byte[] TestKey = Utils.StringToByteArray(TestKeyHex);

        private const string RealFrame =
            "685F5F6873F05B000000002D4C010E00005005F72A3BA99D9128AB6008BEEE5336CA296EA1FFD143471C72DEAE2F716AB169FF071CC08605BC4EE99FC9D36ADE999BC910605B159ECBA642961D47560EB9C1E18071A883A18E7D5D9C8D81B9989880D6B016";

        private const string RealKey = "57B7CBDF2154C01795C75CCCEAD572CF";

        private readonly List<(Guid Id, object Value)> _dispatched = new();

        private static byte[] LongFrame(byte control, byte address, byte ci, byte[] userData)
        {
            var frame = new byte[9 + userData.Length];
            frame[0] = 0x68;
            frame[1] = (byte)(3 + userData.Length);
            frame[2] = (byte)(3 + userData.Length);
            frame[3] = 0x68;
            frame[4] = control;
            frame[5] = address;
            frame[6] = ci;
            Array.Copy(userData, 0, frame, 7, userData.Length);

            byte cs = 0;
            for (var i = 4; i < 7 + userData.Length; i++)
            {
                cs += frame[i];
            }

            frame[7 + userData.Length] = cs;
            frame[8 + userData.Length] = 0x16;
            return frame;
        }

        /// <summary>Builds a mode 5 (AES-CBC) OMS frame like a meter would send it.</summary>
        private static MBusFrame EncryptedFrame(byte[] key, string plainDataBlocksHex, byte accessNumber = 0x2A, byte mode = 0x05)
        {
            var header = Utils.StringToByteArray("78563412" + "2D4C" + "01" + "0E") ;
            var plain = new List<byte> { 0x2F, 0x2F };
            plain.AddRange(Utils.StringToByteArray(plainDataBlocksHex));
            while (plain.Count % 16 != 0 || plain[^1] != 0x2F || plain[^2] != 0x2F)
            {
                plain.Add(0x2F);
            }

            var iv = new byte[16];
            iv[0] = header[4];
            iv[1] = header[5];
            Array.Copy(header, 0, iv, 2, 4);
            iv[6] = header[6];
            iv[7] = header[7];
            for (var i = 8; i < 16; i++)
            {
                iv[i] = accessNumber;
            }

            byte[] cipher;
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                cipher = aes.EncryptCbc(plain.ToArray(), iv, PaddingMode.None);
            }

            var userData = header.Concat(new byte[] { accessNumber, 0x00, (byte)((plain.Count / 16) << 4), mode }).Concat(cipher).ToArray();
            return MBusFrame.FromByteArray(MBusFrameType.LongFrame, NullLogger.Instance, LongFrame(0x44, 0xF0, 0x5B, userData));
        }

        private IDriverContext AttributeContext(string name)
        {
            var dispatcher = new Mock<IDispatcher>();
            dispatcher.Setup(d => d.DispatchValue(It.IsAny<IDispatchable>(), It.IsAny<object>(), It.IsAny<DispatchValueSource>()))
                .Callback<IDispatchable, object, DispatchValueSource>((self, value, _) =>
                {
                    lock (_dispatched)
                    {
                        _dispatched.Add((self.Id, value));
                    }
                })
                .Returns(Task.CompletedTask);

            var node = new NodeInstance { ObjId = Guid.NewGuid(), Name = name };
            return new DriverContextMock(node, DriverFactory, Factory, dispatcher.Object, NullLoggerFactory.Instance, TimeProvider.System);
        }

        private static VariableDataFrame Blocks(string hex)
        {
            var frame = new VariableDataFrame();
            frame.ParseVariableDataFrame(Utils.StringToByteArray(hex), 0, NullLogger.Instance);
            return frame;
        }

        [Fact]
        public void AesDecrypt_RealMeterFrame_ParsesAllDataBlocks()
        {
            var frame = MBusFrame.FromByteArray(MBusFrameType.LongFrame, NullLogger.Instance, Utils.StringToByteArray(RealFrame));

            var decrypted = OmsHelper.AesDecrypt(Utils.StringToByteArray(RealKey), frame, NullLogger.Instance) as VariableDataFrame;

            Assert.NotNull(decrypted);
            Assert.True(decrypted.DataBlocks.Count >= 9, $"{decrypted.DataBlocks.Count} blocks");
            Assert.IsType<DateTime>(decrypted.DataBlocks[0].Value);
            Assert.Equal(9295305, decrypted.DataBlocks[1].Value);
        }

        [Fact]
        public void AesDecrypt_WrongKey_ReturnsNull()
        {
            var frame = MBusFrame.FromByteArray(MBusFrameType.LongFrame, NullLogger.Instance, Utils.StringToByteArray(RealFrame));

            Assert.Null(OmsHelper.AesDecrypt(TestKey, frame, NullLogger.Instance));
            Assert.Empty(OmsHelper.AesDecryptFrame(TestKey, frame));
        }

        [Fact]
        public void AesDecrypt_SelfEncryptedFrame_RoundTrips()
        {
            var frame = EncryptedFrame(TestKey, "0403E8030000" + "022B6400");

            var decrypted = OmsHelper.AesDecrypt(TestKey, frame, NullLogger.Instance) as VariableDataFrame;

            Assert.NotNull(decrypted);
            Assert.Equal(1000, decrypted.DataBlocks[0].Value);
            Assert.Equal((short)100, decrypted.DataBlocks[1].Value);
        }

        [Fact]
        public void GetIvFromMBusFrame_UsesManufacturerIdVersionTypeAndAccessNumber()
        {
            var frame = EncryptedFrame(TestKey, "0403E8030000", accessNumber: 0x77);

            var iv = OmsHelper.GetIvFromMBusFrame(frame);

            Assert.Equal(Utils.StringToByteArray("2D4C78563412010E7777777777777777"), iv);
        }

        [Fact]
        public void SetData_FirstValue_IsDispatched()
        {
            var attribute = new OmsDriverAttribute(AttributeContext("a+"), 1);

            attribute.SetData(Blocks("0103" + "07" + "040301000000"));

            var value = Assert.Single(_dispatched);
            Assert.Equal(attribute.Id, value.Id);
            Assert.Equal(1, value.Value);
        }

        [Fact]
        public void SetData_SameValueAgain_IsNotDispatchedTwice()
        {
            var attribute = new OmsDriverAttribute(AttributeContext("a+"), 0);

            // new frames, new boxes: the values are compared by value, not by reference
            attribute.SetData(Blocks("040305000000"));
            attribute.SetData(Blocks("040305000000"));
            attribute.SetData(Blocks("040305000000"));

            Assert.Single(_dispatched);
        }

        [Fact]
        public void SetData_ChangedValue_IsDispatched()
        {
            var attribute = new OmsDriverAttribute(AttributeContext("a+"), 0);

            attribute.SetData(Blocks("040305000000"));
            attribute.SetData(Blocks("040306000000"));
            attribute.SetData(Blocks("040306000000"));
            attribute.SetData(Blocks("040305000000"));

            Assert.Equal(new object[] { 5, 6, 5 }, _dispatched.Select(d => d.Value).ToArray());
        }

        [Fact]
        public void SetData_IndexEqualToBlockCount_IsIgnored()
        {
            var attribute = new OmsDriverAttribute(AttributeContext("power-"), 2);

            var ex = Record.Exception(() => attribute.SetData(Blocks("040305000000" + "040306000000")));

            Assert.Null(ex);
            Assert.Empty(_dispatched);
        }

        [Fact]
        public void SetData_EmptyFrame_IsIgnored()
        {
            var attribute = new OmsDriverAttribute(AttributeContext("datetime"), 0);

            attribute.SetData(new VariableDataFrame());

            Assert.Empty(_dispatched);
        }

        private async Task<OmsDriver> CreateOmsDriver(params string[] attributeKeys)
        {
            var root = CreateNodeInstance(OmsDriverFactory.OmsDriverFactory.DeviceDriverGuid);
            root.GetProperty("mbus-oms-key").Value = TestKeyHex;
            root.GetProperty("mbus-oms-port").Value = "COM-TEST-NOT-OPENED";

            var keyToGuid = new Dictionary<string, Guid>
            {
                ["mbus-oms-datetime"] = new("49250543-3dac-43dd-abef-42fa6982189e"),
                ["mbus-oms-a+"] = new("cc736fc9-48e9-43aa-bc6e-c4a0356b77cb"),
                ["mbus-oms-a-"] = new("bdfbaff0-c3c6-4447-b02a-7c42b7ff413f")
            };
            foreach (var key in attributeKeys)
            {
                root.InverseThis2ParentNodeInstanceNavigation.Add(CreateNodeInstance(keyToGuid[key]));
            }

            var driver = (OmsDriver)await CreateDriver(root);
            await driver.Init();
            return driver;
        }

        private static void Deliver(OmsDriver driver, MBusFrame frame)
        {
            // the serial port callback; private in the driver, so invoke it the way OmsDevice does
            var method = typeof(OmsDriver).GetMethod("DecryptFrame", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            method.Invoke(driver, new object[] { frame });
        }

        [Fact]
        public async Task OmsDriver_Mode5Frame_IsDecryptedAndReachesTheAttributes()
        {
            var driver = await CreateOmsDriver("mbus-oms-datetime", "mbus-oms-a+");
            Assert.Equal(2, driver.Children.Count);

            var dateNode = driver.Children[0];
            var energyNode = driver.Children[1];

            Deliver(driver, EncryptedFrame(TestKey, "046D248EFB16" + "0403E8030000"));

            // DispatchRead is fire and forget: poll the dispatcher with a deadline
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while ((Dispatcher.GetValue(DispatchableType.NodeInstance, dateNode.Id) == null || Dispatcher.GetValue(DispatchableType.NodeInstance, energyNode.Id) == null)
                   && DateTime.UtcNow < deadline)
            {
                await Task.Delay(10);
            }

            Assert.Equal(new DateTime(2015, 6, 27, 14, 36, 0), Dispatcher.GetValue(DispatchableType.NodeInstance, dateNode.Id)?.Value);
            Assert.Equal(1000, Dispatcher.GetValue(DispatchableType.NodeInstance, energyNode.Id)?.Value);
        }

        [Fact]
        public async Task OmsDriver_TooShortOrForeignFrames_AreIgnored()
        {
            var driver = await CreateOmsDriver("mbus-oms-a+");

            var shortFrame = MBusFrame.FromByteArray(MBusFrameType.LongFrame, NullLogger.Instance, LongFrame(0x44, 0xF0, 0x5B, new byte[] { 1, 2, 3 }));
            var otherCi = MBusFrame.FromByteArray(MBusFrameType.LongFrame, NullLogger.Instance, LongFrame(0x44, 0xF0, 0x78, new byte[20]));
            var unencryptedMode = EncryptedFrame(TestKey, "0403E8030000", mode: 0x00);

            Assert.Null(Record.Exception(() => Deliver(driver, shortFrame)));
            Assert.Null(Record.Exception(() => Deliver(driver, otherCi)));
            Assert.Null(Record.Exception(() => Deliver(driver, unencryptedMode)));
            Assert.Null(Record.Exception(() => Deliver(driver, null)));
        }

        [Fact]
        public async Task OmsDriver_WrongKeyFrame_IsIgnored()
        {
            var driver = await CreateOmsDriver("mbus-oms-a+");

            var foreign = EncryptedFrame(Utils.StringToByteArray("F0E0D0C0B0A090807060504030201000"), "0403E8030000");

            Assert.Null(Record.Exception(() => Deliver(driver, foreign)));
        }
    }
}
