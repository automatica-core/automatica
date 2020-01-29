using System;
using System.Text;
using P3.Knx.Core.DPT;
using P3.Knx.Core.DPT.Base;

namespace P3.Knx.Core.Driver.DPT
{
    internal abstract class Dpt16Translator : Dpt
    {
        protected abstract Encoding GetEncoding();

        public override object FromDataPoint(byte[] data)
        {
            if (data == null || data.Length != 14)
            {
                throw new FromDataPointException("data is invalid");
            }

            var encoding = GetEncoding();

            return encoding.GetString(data).Replace("\0", "");
        }

        public override byte[] ToDataPoint(object value)
        {
            if (value is string str)
            {
                if (str.Length > 14)
                {
                    str = str.Substring(0, 14);
                }

                var ret = new byte[15];
                Array.Fill(ret, (byte)0);

                var encoding = GetEncoding();
                var encoded = encoding.GetBytes(str);

                Array.Copy(encoded,0, ret, 1, encoded.Length);

                return ret;
            }
            throw new ToDataPointException($"{nameof(value)} must be of type {nameof(DateTime)}");
        }
    }

    internal class Dpt16_000_Translator : Dpt16Translator
    {
        public override string[] Ids => new[] { "16.000"};
        

        protected override Encoding GetEncoding()
        {
            return Encoding.ASCII;
        }
    }

    internal class Dpt16_001_Translator : Dpt16Translator
    {
        public override string[] Ids => new[] { "16.001" };


        protected override Encoding GetEncoding()
        {
            return Encoding.GetEncoding("iso-8859-1");
        }
    }
}
