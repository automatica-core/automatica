using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.MBus;
using P3.Driver.MBus.Frames;
using P3.Driver.OmsDriverFactory.Helper;
using Xunit;

namespace P3.Driver.Oms.MBus.Tests
{
    public class OmsHelperTests
    {
        [Fact]
        public void Test_MBusFrame_Get_Iv()
        {
            var excpectedIv = Automatica.Core.Driver.Utility.Utils.StringToByteArray("2D4C00000000010E0D0D0D0D0D0D0D0D");

            var frame = MBusFrame.FromByteArray(MBusFrameType.LongFrame, NullLogger.Instance,
                Automatica.Core.Driver.Utility.Utils.StringToByteArray(
                    "685F5F6853F05B000000002D4C010E0D0050053FD0FEB726760CC7AAF0B52B41F0C541BD6306DCD8B91B3DA2311EF13D2514D0960082161EFEC4B6CB1E0B3328BE6177DCA594C1280024A835F1D655BA7182B256E94BD33AC0A6B08DA46781EB4E91E01216"));

            var iv = OmsHelper.GetIvFromMBusFrame(frame);
            Assert.Equal(excpectedIv, iv);
        }

        [Fact]
        public void Test_MBusFrame_Get_Cipher()
        {
            var excpectedCipher = Automatica.Core.Driver.Utility.Utils.StringToByteArray(
                "3FD0FEB726760CC7AAF0B52B41F0C541BD6306DCD8B91B3DA2311EF13D2514D0960082161EFEC4B6CB1E0B3328BE6177DCA594C1280024A835F1D655BA7182B256E94BD33AC0A6B08DA46781EB4E91E0");

            var frame = MBusFrame.FromByteArray(MBusFrameType.LongFrame, NullLogger.Instance,
                Automatica.Core.Driver.Utility.Utils.StringToByteArray(
                    "685F5F6853F05B000000002D4C010E0D0050053FD0FEB726760CC7AAF0B52B41F0C541BD6306DCD8B91B3DA2311EF13D2514D0960082161EFEC4B6CB1E0B3328BE6177DCA594C1280024A835F1D655BA7182B256E94BD33AC0A6B08DA46781EB4E91E01216"));

            var cipher = OmsHelper.GetCipherFromMBusFrame(frame);

            Assert.Equal(excpectedCipher, cipher);
        }

        [Fact]
        public void Test_MBusFrame_Decrypt()
        {
            var expectedData = Automatica.Core.Driver.Utility.Utils.StringToByteArray(
                "2F2F066D42222C5024100403C9D58D0004833C2BCA4B008410FB827386291D008410FB82F33CE5555900042B0000000004AB3CCD05000004FB140000000004FB943CA30100000483FF04000000002F2F");

            var frame = MBusFrame.FromByteArray(MBusFrameType.LongFrame, NullLogger.Instance,
                Automatica.Core.Driver.Utility.Utils.StringToByteArray(
                    "685F5F6873F05B000000002D4C010E00005005F72A3BA99D9128AB6008BEEE5336CA296EA1FFD143471C72DEAE2F716AB169FF071CC08605BC4EE99FC9D36ADE999BC910605B159ECBA642961D47560EB9C1E18071A883A18E7D5D9C8D81B9989880D6B016"));

            var key = "57B7CBDF2154C01795C75CCCEAD572CF";

            var aesKey = Automatica.Core.Driver.Utility.Utils.StringToByteArray(key);

            var decryptedFrame = OmsHelper.AesDecryptFrame(aesKey, frame);

            Assert.Equal(expectedData, decryptedFrame);
        }
    }
}
