namespace P3.Driver.ZWaveAeon.CommandClasses
{
    public class WakeUpReport : NodeReport
    {
        public readonly bool Awake;

        internal WakeUpReport(Node node) : base(node)
        {
            Awake = true;
        }

        public override string ToString()
        {
            return $"Awake:{Awake}";
        }
    }
}
