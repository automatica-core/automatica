using Automatica.Core.Driver.Utility;

namespace P3.Driver.MBus.Frames.VariableData
{
    public enum DataInformationFunction
    {
        InstantaneousValue = 0,
        MinimumValue,
        MaximumValue,
        ValueDurringErrorState
    }

    public enum DataFieldType
    {
        NoData = 0,
        Integer8Bit,
        Integer16Bit,
        Integer24Bit,
        Integer32Bit,
        Real32Bit,
        Integer48Bit,
        Integer64Bit,
        SelectionForReadout,
        Digit2Bcd,
        Digit4Bcd,
        Digit6Bcd,
        Digit8Bcd,
        VariableLength,
        Digit12Bcd,
        SpecialFunction
    }



    public class DataInformationField
    {
        private DataInformationField()
        {
            
        }
        public bool HasExtension { get; set; }
        public bool LsbStorageNumber { get; set; }
        public DataInformationFunction Function { get; set; }
        public DataFieldType DataFieldType { get; set; }

        public int DataFieldLength { get; set; }

        public static DataInformationField Parse(in byte data)
        {
            DataInformationField dif = new DataInformationField();
            dif.ParseData(in data);
            return dif;
        }

        private void ParseData(in byte data)
        {
            HasExtension = Utils.IsBitSet(data, 7);
            LsbStorageNumber = Utils.IsBitSet(data, 6);
            
            Function = (DataInformationFunction) ((Utils.BitValue(data, 5) << 1) | Utils.BitValue(data, 4));

            int dft = data;
            dft =  dft & ~0xF0;

            DataFieldType = (DataFieldType) dft;

            DataFieldLength = GetDataFieldLength(DataFieldType);

        }

        private int GetDataFieldLength(DataFieldType type)
        {
            switch (type)
            {
                case DataFieldType.NoData:
                case DataFieldType.SelectionForReadout:
                    return 0;
                case DataFieldType.Integer8Bit:
                case DataFieldType.Digit2Bcd:
                    return 1;
                case DataFieldType.Integer16Bit:
                case DataFieldType.Digit4Bcd:
                    return 2;
                case DataFieldType.Integer24Bit:
                case DataFieldType.Digit6Bcd:
                    return 3;
                case DataFieldType.Integer32Bit:
                case DataFieldType.Digit8Bcd:
                case DataFieldType.Real32Bit:
                    return 4;
                case DataFieldType.Integer48Bit:
                    case DataFieldType.Digit12Bcd:
                    return 6;
                case DataFieldType.Integer64Bit:
                 
                    return 8;
                case DataFieldType.SpecialFunction:
                    return 1;

                case DataFieldType.VariableLength:
                    return -1;
                default:
                    return 0;
            }
        }
    }
}
