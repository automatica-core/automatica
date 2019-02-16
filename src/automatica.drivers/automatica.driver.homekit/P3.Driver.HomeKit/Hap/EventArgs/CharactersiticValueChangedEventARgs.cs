using P3.Driver.HomeKit.Hap.Model;

namespace P3.Driver.HomeKit.Hap.EventArgs
{
    public class CharactersiticValueChangedEventArgs : System.EventArgs
    {
        public Characteristic Characteristic { get; }
        public object Value { get; }

        public CharactersiticValueChangedEventArgs(Characteristic characteristic, object value)
        {
            Characteristic = characteristic;
            Value = value;
        }
    }
}
