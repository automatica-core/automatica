using System;
using Automatica.Core.Driver.Utility;

namespace P3.Driver.MBus.Frames.VariableData
{
    public enum Unit
    {
        Unknown,
        EnergyW,
        EnergyJ,
        Volume,
        Mass,
        OnTime,
        OperatingTime,
        PowerW,
        PowerJh,
        VolumeFlowh,
        VolumeFlowmin,
        VolumeFlows,
        MassFlow,
        FlowTemperature,
        ReturnTemperature,
        TemperatureDifference,
        ExternalTemperature,
        Pressure,
        TimePoint,
        UnitsForHca,
        Reserved1,
        AveragingDuration,
        ActualityDuration,
        EnhancedId,
        BusAddress,
        FabricationNumber,
        ManufacturerSpecific,
        Volt,
        Ampere
    }



    public class ValueInformationField
    {
        public const int VifWithoutExtension = 0x7f;

        public bool HasExtension { get; private set; }

        public int UnitAndMultiplier { get; private set; }

        public Unit Unit { get; private set; }
        public int Multiplier { get; private set; }

        public byte RawData { get; private set; }

        private ValueInformationField()
        {

        }

        public static ValueInformationField Parse(in byte data)
        {
            var vif = new ValueInformationField();
            vif.ParseData(in data);

            return vif;
        }

        public void ParseExtensionUnit(ValueInformationField data)
        {
            switch (data.UnitAndMultiplier)
            {
                case 0x4:
                case 0x40 + 1:
                case 0x40 + 2:
                case 0x40 + 3:
                case 0x40 + 4:
                case 0x40 + 5:
                case 0x40 + 6:
                case 0x40 + 7:
                case 0x40 + 8:
                case 0x40 + 9:
                case 0x40 + 10:
                case 0x40 + 11:
                case 0x40 + 12:
                case 0x40 + 13:
                case 0x40 + 14:
                case 0x40 + 15:
                    Multiplier = (int)Math.Pow((data.UnitAndMultiplier & 0x0F), -9);
                    Unit = Unit.Volt;
                    break;
                case 0x5:
                case 0x50 + 1:
                case 0x50 + 2:
                case 0x50 + 3:
                case 0x50 + 4:
                case 0x50 + 5:
                case 0x50 + 6:
                case 0x50 + 7:
                case 0x50 + 8:
                case 0x50 + 9:
                case 0x50 + 10:
                case 0x50 + 11:
                case 0x50 + 12:
                case 0x50 + 13:
                case 0x50 + 14:
                case 0x50 + 15:
                    Multiplier = (int)Math.Pow((data.UnitAndMultiplier & 0x0F), -12);
                    Unit = Unit.Ampere;
                    break;
            }
        }

        private void ParseData(in byte data)
        {
            RawData = data;
            HasExtension = Utils.IsBitSet(data, 7);
            UnitAndMultiplier = data & VifWithoutExtension;

            switch (UnitAndMultiplier)
            {
                // E000 0nnn Energy 10(nnn-3) W
                case 0x00:
                case 0x00 + 1:
                case 0x00 + 2:
                case 0x00 + 3:
                case 0x00 + 4:
                case 0x00 + 5:
                case 0x00 + 6:
                case 0x00 + 7:
                    Multiplier = (UnitAndMultiplier & 0x07) - 3;
                    Unit = Unit.EnergyW;
                    break;
                // 0000 1nnn          Energy       10(nnn)J     (0.001kJ to 10000kJ)
                case 0x08:
                case 0x08 + 1:
                case 0x08 + 2:
                case 0x08 + 3:
                case 0x08 + 4:
                case 0x08 + 5:
                case 0x08 + 6:
                case 0x08 + 7:
                    Unit = Unit.EnergyJ;
                    Multiplier = (UnitAndMultiplier & 0x07);
                    break;
                // E001 1nnn Mass 10(nnn-3) kg 0.001kg to 10000kg
                case 0x18:
                case 0x18 + 1:
                case 0x18 + 2:
                case 0x18 + 3:
                case 0x18 + 4:
                case 0x18 + 5:
                case 0x18 + 6:
                case 0x18 + 7:
                    Unit = Unit.Mass;
                    Multiplier = (UnitAndMultiplier & 0x07) - 3;
                    break;
                // E010 1nnn Power 10(nnn-3) W 0.001W to 10000W
                case 0x28:
                case 0x28 + 1:
                case 0x28 + 2:
                case 0x28 + 3:
                case 0x28 + 4:
                case 0x28 + 5:
                case 0x28 + 6:
                case 0x28 + 7:
                    Unit = Unit.PowerW;
                    Multiplier = (UnitAndMultiplier & 0x07) - 3;
                    break;
                // E011 0nnn Power 10(nnn) J/h 0.001kJ/h to 10000kJ/h
                case 0x30:
                case 0x30 + 1:
                case 0x30 + 2:
                case 0x30 + 3:
                case 0x30 + 4:
                case 0x30 + 5:
                case 0x30 + 6:
                case 0x30 + 7:
                    Unit = Unit.PowerJh;
                    Multiplier = (UnitAndMultiplier & 0x07);
                    break;
                // E001 0nnn Volume 10(nnn-6) m3 0.001l to 10000l
                case 0x10:
                case 0x10 + 1:
                case 0x10 + 2:
                case 0x10 + 3:
                case 0x10 + 4:
                case 0x10 + 5:
                case 0x10 + 6:
                case 0x10 + 7:
                    Unit = Unit.Volume;
                    Multiplier = (UnitAndMultiplier & 0x07) - 6;
                    break;
                // E011 1nnn Volume Flow 10(nnn-6) m3/h 0.001l/h to 10000l/
                case 0x38:
                case 0x38 + 1:
                case 0x38 + 2:
                case 0x38 + 3:
                case 0x38 + 4:
                case 0x38 + 5:
                case 0x38 + 6:
                case 0x38 + 7:
                    Unit = Unit.VolumeFlowh;
                    Multiplier = (UnitAndMultiplier & 0x07) - 6;
                    break;
                // E100 0nnn Volume Flow ext. 10(nnn-7) m3/min 0.0001l/min to 1000l/min
                case 0x40:
                case 0x40 + 1:
                case 0x40 + 2:
                case 0x40 + 3:
                case 0x40 + 4:
                case 0x40 + 5:
                case 0x40 + 6:
                case 0x40 + 7:
                    Unit = Unit.VolumeFlowmin;
                    Multiplier = (UnitAndMultiplier & 0x07) - 7;

                    break;
                // E100 1nnn Volume Flow ext. 10(nnn-9) m3/s 0.001ml/s to 10000ml/
                case 0x48:
                case 0x48 + 1:
                case 0x48 + 2:
                case 0x48 + 3:
                case 0x48 + 4:
                case 0x48 + 5:
                case 0x48 + 6:
                case 0x48 + 7:
                    Unit = Unit.VolumeFlows;
                    Multiplier = (UnitAndMultiplier & 0x07) - 9;
                    break;
                // E101 0nnn Mass flow 10(nnn-3) kg/h 0.001kg/h to 10000kg/
                case 0x50:
                case 0x50 + 1:
                case 0x50 + 2:
                case 0x50 + 3:
                case 0x50 + 4:
                case 0x50 + 5:
                case 0x50 + 6:
                case 0x50 + 7:
                    Unit = Unit.MassFlow;
                    Multiplier = (UnitAndMultiplier & 0x07) - 3;
                    break;
                // E101 10nn Flow Temperature 10(nn-3) °C 0.001°C to 1°C
                case 0x58:
                case 0x58 + 1:
                case 0x58 + 2:
                case 0x58 + 3:
                    Unit = Unit.FlowTemperature;
                    Multiplier = (UnitAndMultiplier & 0x03) - 3;
                    break;
                // E101 11nn Return Temperature 10(nn-3) °C 0.001°C to 1°C
                case 0x5C:
                case 0x5C + 1:
                case 0x5C + 2:
                case 0x5C + 3:
                    Unit = Unit.ReturnTemperature;
                    Multiplier = (UnitAndMultiplier & 0x03) - 3;
                    break;
                // E110 10nn Pressure 10(nn-3) bar 1mbar to 1000mbar
                case 0x68:
                case 0x68 + 1:
                case 0x68 + 2:
                case 0x68 + 3:
                    Unit = Unit.Pressure;
                    Multiplier = (UnitAndMultiplier & 0x03) - 3;
                    break;
                // E010 00nn On Time
                // nn = 00 seconds
                // nn = 01 minutes
                // nn = 10   hours
                // nn = 11    days
                // E010 01nn Operating Time coded like OnTime
                // E111 00nn Averaging Duration coded like OnTime
                // E111 01nn Actuality Duration coded like OnTime
                case 0x20:
                case 0x20 + 1:
                case 0x20 + 2:
                case 0x20 + 3:
                case 0x24:
                case 0x24 + 1:
                case 0x24 + 2:
                case 0x24 + 3:
                case 0x70:
                case 0x70 + 1:
                case 0x70 + 2:
                case 0x70 + 3:
                case 0x74:
                case 0x74 + 1:
                case 0x74 + 2:
                case 0x74 + 3:
                    Unit = Unit.OnTime;
                    //TODO
                    break;
                // E110 110n Time Point
                // n = 0        date
                // n = 1 time & date
                // data type G
                // data type F
                case 0x6C:
                case 0x6C + 1:
                    Unit = Unit.TimePoint;
                    //TODO
                    break;
                // E110 00nn    Temperature Difference   10(nn-3)K   (mK to  K)
                case 0x60:
                case 0x60 + 1:
                case 0x60 + 2:
                case 0x60 + 3:
                    Multiplier = (UnitAndMultiplier & 0x03) - 3;
                    Unit = Unit.TemperatureDifference;
                    break;
                // E110 01nn External Temperature 10(nn-3) °C 0.001°C to 1°C
                case 0x64:
                case 0x64 + 1:
                case 0x64 + 2:
                case 0x64 + 3:
                    Unit = Unit.ExternalTemperature;
                    Multiplier = (UnitAndMultiplier & 0x03) - 3;
                    break;
                // E110 1110 Units for H.C.A. dimensionless
                case 0x6E:
                    Unit = Unit.UnitsForHca;
                    //TODO
                    break;
                // E110 1111 Reserved
                case 0x6F:
                    Unit = Unit.Reserved1;
                    break;
                // Custom VIF in the following string: never reached...
                case 0x7C:
                    Unit = Unit.Reserved1;
                    break;
                // Fabrication No
                case 0x78:
                    Unit = Unit.FabricationNumber;
                    //TODO
                    break;
                // Bus Address
                case 0x7A:
                    Unit = Unit.BusAddress;
                    //TODO
                    break;
                // Manufacturer specific: 7Fh / FF
                case 0x7F:
                case 0xFF:
                    Unit = Unit.ManufacturerSpecific;
                    //TODO
                    break;

                default:
                    Unit = Unit.Unknown;
                    break;
            }
        }
    }
}
