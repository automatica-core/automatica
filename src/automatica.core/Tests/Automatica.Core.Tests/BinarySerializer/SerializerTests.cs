using Automatica.Core.Driver.Utility;
using Xunit;

namespace Automatica.Core.Tests.BinarySerializer
{
    public class SerializerTests
    {
        public const string DoubleBinaryString =
            "7B 22 54 79 70 65 22 3A 22 53 79 73 74 65 6D 2E 44 6F 75 62 6C 65 2C 20 53 79 73 74 65 6D 2E 50 72 69 76 61 74 65 2E 43 6F 72 65 4C 69 62 2C 20 56 65 72 73 69 6F 6E 3D 37 2E 30 2E 30 2E 30 2C 20 43 75 6C 74 75 72 65 3D 6E 65 75 74 72 61 6C 2C 20 50 75 62 6C 69 63 4B 65 79 54 6F 6B 65 6E 3D 37 63 65 63 38 35 64 37 62 65 61 37 37 39 38 65 22 2C 22 56 61 6C 75 65 22 3A 30 2E 31 7D";

        public const string BooleanBinaryString =
            "7B 22 54 79 70 65 22 3A 22 53 79 73 74 65 6D 2E 42 6F 6F 6C 65 61 6E 2C 20 53 79 73 74 65 6D 2E 50 72 69 76 61 74 65 2E 43 6F 72 65 4C 69 62 2C 20 56 65 72 73 69 6F 6E 3D 37 2E 30 2E 30 2E 30 2C 20 43 75 6C 74 75 72 65 3D 6E 65 75 74 72 61 6C 2C 20 50 75 62 6C 69 63 4B 65 79 54 6F 6B 65 6E 3D 37 63 65 63 38 35 64 37 62 65 61 37 37 39 38 65 22 2C 22 56 61 6C 75 65 22 3A 66 61 6C 73 65 7D";

        [Fact]
        public void TestDoubleSerialize()
        {
            var value = 0.1d;

            var binary = Base.Serialization.BinarySerializer.Serialize(value);
            var binaryString = Utils.ByteArrayToString(in binary);

            Assert.Equal(DoubleBinaryString, binaryString);

        }

        [Fact]
        public void TestDoubleDeserialize()
        {
            var binary = Utils.StringToByteArray(DoubleBinaryString.Replace(" ", ""));
            var doubleValue = Base.Serialization.BinarySerializer.Deserialize(binary);

            Assert.Equal(0.1d, doubleValue);
        }


        [Fact]
        public void TestBoolSerialize()
        {
            var value = false;

            var binary = Base.Serialization.BinarySerializer.Serialize(value);
            var binaryString = Utils.ByteArrayToString(in binary);

            Assert.Equal(BooleanBinaryString, binaryString);

        }

        [Fact]
        public void TestBoolDeserialize()
        {
            var binary = Utils.StringToByteArray(BooleanBinaryString.Replace(" ", ""));
            var doubleValue = Base.Serialization.BinarySerializer.Deserialize(binary);

            Assert.Equal(false, doubleValue);
        }

    }
}
