namespace P3.Driver.HueBridge.Data
{
    public class HueGroupAction
    {
        public bool On { get; set; }
        public int Bri => 0;
        public int Hue => 0;
        public int Sat => 0;
        public string Effect => "none";
        public int Ct => 0;
        public string Alert => "none";
        public string ColorMode => "xy";
        public bool Reachable => true;
    }
}
