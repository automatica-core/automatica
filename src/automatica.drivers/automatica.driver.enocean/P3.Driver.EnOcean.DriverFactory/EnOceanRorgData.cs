using System;
using System.Collections.Generic;
using System.Text;

namespace P3.Driver.EnOcean.DriverFactory
{
    public class EnOceanRorgData
    {
        protected EnOceanRorgData()
        {
            
        }

        protected static Guid GenerateNewGuid(Guid guid, int c)
        {
            byte[] gu = guid.ToByteArray();

            gu[gu.Length - 1] = (byte)(Convert.ToInt32(gu[gu.Length - 1]) + c);

            return new Guid(gu);
        }
    }
}
