namespace P3.Driver.HomeKit.Hap.Model
{
    public class WindowCovering : Accessory
    {
        internal WindowCovering()
        {
            
        }
        
        public Characteristic CurrentPosition { get; internal set; }
        public Characteristic TargetPosition { get; internal set; }

        public Characteristic PositionType { get; internal set; }
    }
}
