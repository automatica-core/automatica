using System;
using System.Linq;
using Automatica.Core.Base.Cryptography;
using P3.Knx.Core.Driver.DPT.Base;

namespace P3.Knx.Core.Driver.DPT
{
   
    internal class Dpt11Translator : Dpt
    {
        public override string[] Ids => new[] {"11.001"};
        public override object FromDataPoint(byte[] data)
        {
            if (data == null || data.Length != 3)
            {
                throw new FromDataPointException($"data is invalid ({data?.ToHex(true)})");
            }

            data = data.Reverse().ToArray();
            var year = data[0];
            var month = data[1];
            var day = data[2];

            int iYear = year;

            if (year >= 90)
            {
                iYear += 1900;
            }
            else
            {
                iYear += 2000;
            }
            
            var dt = new DateOnly(iYear, month, day);

            return dt;
        }

        public override byte[] ToDataPoint(object value)
        {
            if (value is DateOnly dateOnly)
            {
                var data = new byte[3];

                byte year = 0;


                if (dateOnly.Year < 2000 && dateOnly.Year >= 1990)
                {
                    year = (byte)(dateOnly.Year - 1900);
                }
                else if (dateOnly.Year > 2000 && dateOnly.Year <= 2089)
                {
                    year = (byte)(dateOnly.Year - 2000);
                }
                else
                {
                    throw new ToDataPointException($"{nameof(DateTime.Year)} must be in range of 1990-2089");
                }

                data[0] = year;
                data[1] = (byte)dateOnly.Month;
                data[2] = (byte)dateOnly.Day;
                return data.Reverse().ToArray();

            }
            throw new ToDataPointException($"{nameof(value)} must be of type {nameof(DateTime)}");
        }
    }
}
