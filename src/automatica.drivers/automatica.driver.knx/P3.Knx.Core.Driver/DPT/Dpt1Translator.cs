using System.Runtime.CompilerServices;
using Automatica.Core.Base.Cryptography;
using P3.Knx.Core.Driver.DPT.Base;

[assembly: InternalsVisibleTo("P3.Driver.Knx.Tests")]

namespace P3.Knx.Core.Driver.DPT
{
    internal sealed class Dpt1Translator : Dpt
    {
        public override string[] Ids => new[] { "1.*"};

        public override object FromDataPoint(byte[] data)
        {
            if (data != null && data.Length == 1)
            {
                return data[0] >= 1;
            }

            throw new FromDataPointException($"data array length longer than 1 ({data?.ToHex(true)})");
        }

        public override byte[] ToDataPoint(object value)
        {
            var inputValue = GetValueAsInt(value);


            if (!inputValue.HasValue)
            {
                throw new ToDataPointException($"{nameof(value)} has invalid type");
            }

            var boolValue = inputValue.Value >= 1;

            return new [] { boolValue ? (byte)1 : (byte)0 };
        }
    }
}
