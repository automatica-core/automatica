using System.Runtime.CompilerServices;
using Automatica.Core.Base.Cryptography;
using Automatica.Core.Driver.Utility;
using P3.Knx.Core.Driver.DPT.Base;

[assembly: InternalsVisibleTo("P3.Driver.Knx.Tests")]

namespace P3.Knx.Core.Driver.DPT
{
    public class Dpt2Value
    {
        public bool Control { get; }
        public bool Value { get; }

        public Dpt2Value(bool control, bool value)
        {
            Control = control;
            Value = value;
        }

        public byte[] ToByteData()
        {
            var data = new[] {(byte) 0};

            if (Control)
            {
                data[0] = Utils.SetBitsTo1(data[0], 1);
            }
            if (Value)
            {
                data[0] = Utils.SetBitsTo1(data[0], 0);
            }

            return data;
        }
    }

    internal sealed class Dpt2Translator : Dpt
    {
        public override string[] Ids => new[] { "2.*"};

        public override object FromDataPoint(byte[] data)
        {
            if (data != null && data.Length == 1)
            {
                var control = (data[0] & 0x02) > 0;
                var value = (data[0] & 0x01) > 0;

                return new Dpt2Value(control, value);
            }

            throw new FromDataPointException($"data array length longer than 1 ({data?.ToHex(true)})");
        }

        public override byte[] ToDataPoint(object value)
        {
            if (value is Dpt2Value dpt2Value)
            {
                return dpt2Value.ToByteData();
            }

            throw new ToDataPointException($"{nameof(value)} has invalid type"); 
        }
    }
}
