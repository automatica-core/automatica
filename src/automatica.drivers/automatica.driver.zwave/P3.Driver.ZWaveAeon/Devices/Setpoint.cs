namespace P3.Driver.ZWaveAeon.Devices
{
    public class Setpoint
    {
        public readonly float Value;
        public readonly Unit Unit;
        public readonly string Symbol;

        public Setpoint(float value, Unit unit)
        {
            Value = value;
            Unit = unit;
            Symbol = unit.GetSymbol();
        }

        public override string ToString()
        {
            return $"{Value} {Symbol}";
        }
    }
}
