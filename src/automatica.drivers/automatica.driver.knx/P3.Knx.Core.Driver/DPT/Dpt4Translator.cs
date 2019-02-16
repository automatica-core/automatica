using System;
using System.Collections.Generic;
using System.Text;
using P3.Knx.Core.DPT.Base;

namespace P3.Knx.Core.DPT
{
    public class Dpt4Translator : DptToByteBase
    {
        public override string[] Ids => new[] {"4.*"};

        public override object FromDataPoint(byte[] data)
        {
            if (data == null || data.Length != 1)
                throw new FromDataPointException("data is invalid");

            return Convert.ToChar(data[0]);
        }
    }
}
