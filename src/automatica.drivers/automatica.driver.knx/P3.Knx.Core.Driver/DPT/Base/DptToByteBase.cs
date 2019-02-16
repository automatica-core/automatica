namespace P3.Knx.Core.DPT.Base
{
    public abstract class DptToByteBase : Dpt
    {
        public override object FromDataPoint(byte[] data)
        {
            if (data == null || data.Length != 1)
                throw new FromDataPointException("data is invalid");
            
            return ConvertFromBusValue(ValidateMinMax(data[0]));
        }

        public virtual int ValidateMinMax(int value)
        {
            return value;
        }

        public virtual object ConvertFromBusValue(int value)
        {
            return value;
        }

        public virtual byte ConvertToBusValue(int value)
        {
            return (byte) value;
        }

        public sealed override byte[] ToDataPoint(object value)
         {
            var dataPoint = new byte[1];
            dataPoint[0] = 0x00;

            var input = GetValueAsInt(value);

            if (!input.HasValue)
            {
                throw new ToDataPointException($"{nameof(value)} has invalid type");
            }
            input = ValidateMinMax(input.Value);

            dataPoint[0] = ConvertToBusValue(input.Value);

            return dataPoint;
        }
    }
}
