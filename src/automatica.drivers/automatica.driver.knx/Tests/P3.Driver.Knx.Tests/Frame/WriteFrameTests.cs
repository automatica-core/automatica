using Automatica.Core.Base.Templates;
using P3.Knx.Core.Driver;
using P3.Knx.Core.Driver.DPT;
using P3.Knx.Core.Driver.Frames;
using Xunit;

namespace P3.Driver.Knx.Tests.Frame
{
    public class WriteFrameTests
    {
        [Fact]
        public void TestWriteFrame_Dpt_16_000_1()
        {
            
            var busValue = DptTranslator.Instance.ToDataPoint(
                PropertyHelper.GetNameAttributeFromEnumValue(Dpt16Type.Dpt16_000).EnumValue, "test_16_1_");

            var ws = WriteStatusFrame.CreateFrame(new KnxConnectionMock(), "0/0/0", busValue);

            var frame = ws.ToFrame();

            Assert.Equal(35, frame.Length);
        }
    }
}
