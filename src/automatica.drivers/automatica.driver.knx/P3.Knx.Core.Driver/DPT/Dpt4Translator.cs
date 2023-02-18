using System;
using Automatica.Core.Base.Cryptography;
using P3.Knx.Core.Driver.DPT.Base;

namespace P3.Knx.Core.Driver.DPT
{
    public class Dpt4Translator : DptToByteBase
    {
        public override string[] Ids => new[] {"4.*"};

        public override object FromDataPoint(byte[] data)
        {
            if (data == null || data.Length != 1)
                throw new FromDataPointException($"data is invalid ({data?.ToHex(true)})");

            return Convert.ToChar(data[0]);
        }
    }
}
