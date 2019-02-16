using Automatica.Core.Driver.Utility;
using P3.Knx.Core.DPT.Base;

namespace P3.Knx.Core.DPT
{
    public class Dpt3Data
    {
        public bool Direction { get; }
        public byte Value { get; }

        public Dpt3Data(bool direction, byte value)
        {
            Direction = direction;
            Value = value;
        }

        public byte ToValue()
        {
            byte ret = Value;
            if (ret > 7)
            {
                ret = 7;
            }

            if (Direction)
            {
                ret =  Utils.SetBitsTo1(ret, 3);
            }
            return ret;
        }
    }
    internal sealed class Dpt3Translator : Dpt
    {
        public override string[] Ids => new[] {"3.*"};

        public override object FromDataPoint(byte[] data)
        {
            if (data == null || data.Length != 1)
                throw new FromDataPointException("data is invalid");

            int input = data[0] & 0x0F;

            bool direction = (input >> 3) == 1;
            int step = input & 0x07;

            return new Dpt3Data(direction, (byte)step);
        }

        public override byte[] ToDataPoint(object value)
        {
            var dataPoint = new byte[1];
            dataPoint[0] = 0x00;

            int? input;
            if (value is Dpt3Data dpt3Data)
            {
                input = dpt3Data.ToValue();
            }
            else
            {
                input = GetValueAsInt(value);
            }

            if (!input.HasValue)
            {
                throw new ToDataPointException("input value received is not a valid type");
            }
            else
            {
                dataPoint[0] = (byte)input;
            }
            return dataPoint;
        }
    }
}
