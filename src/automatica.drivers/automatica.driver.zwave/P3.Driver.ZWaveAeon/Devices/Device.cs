namespace P3.Driver.ZWaveAeon.Devices
{
    public class Device
    {
        public readonly Node Node;
        public string Name { get; set; }
        public Device(Node node)
        {
            Node = node;
        }

        public override string ToString()
        {
            return Name ?? Node.ToString();
        }
    }
}
