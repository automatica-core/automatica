using System;

namespace P3.Driver.Loxone.Miniserver.Driver.Data.Message
{
    public class LoxoneUuid
    {
        public string Uuid { get; }

        public LoxoneUuid(Span<byte> rawData)
        {
            if (rawData.Length != 16)
            {
                throw new ArgumentException($"{nameof(LoxoneUuid)} must have 9 bytes");
            }

            var data1 = rawData.Slice(0, 4);
            var data2 = rawData.Slice(4, 2);
            var data3 = rawData.Slice(6, 2);
            var data4 = rawData.Slice(8, 8);

            data1.Reverse();
            data2.Reverse();
            data3.Reverse();
            var data1Str = Automatica.Core.Driver.Utility.Utils.ByteArrayToString(data1).Replace(" ", "");
            var data2Str = Automatica.Core.Driver.Utility.Utils.ByteArrayToString(data2).Replace(" ", "");
            var data3Str = Automatica.Core.Driver.Utility.Utils.ByteArrayToString(data3).Replace(" ", "");
            var data4Str = Automatica.Core.Driver.Utility.Utils.ByteArrayToString(data4).Replace(" ", "");

            Uuid = $"{data1Str}-{data2Str}-{data3Str}-{data4Str}".ToLower();
        }
        public LoxoneUuid(string uuid)
        {
            Uuid = uuid;
        }
        public override bool Equals(object obj)
        {
            if(obj is LoxoneUuid uuid)
            {
                return uuid.Uuid == Uuid;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
