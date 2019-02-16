using P3.Driver.HueBridge.Data;

namespace P3.Driver.HueBridge.EventArgs
{
    public class HueSwitchLightEventArgs : System.EventArgs
    {
        public HueSwitchLightEventArgs(HueLight light, bool state, int brightness)
        {
            Light = light;
            State = state;
            Brightness = brightness;
        }

        public HueLight Light { get; }
        public bool State { get; }
        public int Brightness { get; }
    }
}
