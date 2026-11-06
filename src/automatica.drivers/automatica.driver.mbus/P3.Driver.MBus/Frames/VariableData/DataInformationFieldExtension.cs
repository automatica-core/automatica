using Automatica.Core.Driver.Utility;

namespace P3.Driver.MBus.Frames.VariableData
{
    public class DataInformationFieldExtension
    {
        private DataInformationFieldExtension()
        {

        }
        public bool HasExtension { get; set; }

        public bool DeviceUnit { get; set; }
        public byte Tariff { get; set; }
        public byte StorageNumber { get; set; }


        public static DataInformationFieldExtension CreateEmpty()
        {
            return new DataInformationFieldExtension();
        }
        public static DataInformationFieldExtension Parse(in byte data)
        {
            DataInformationFieldExtension dif = new DataInformationFieldExtension();
            dif.ParseData(data);
            return dif;
        }

        private void ParseData(in byte data)
        {
            HasExtension = Utils.IsBitSet(data, 7);
            DeviceUnit = Utils.IsBitSet(data, 6);

            // EN 13757-3: DIFE = E U TT SSSS -> tariff is bits 4-5, shifted down to 0..3
            Tariff = (byte)(Utils.SetBitsTo0(data, 0xCF) >> 4);
            StorageNumber = Utils.SetBitsTo0(data, 0xF0);


        }
    }
}
