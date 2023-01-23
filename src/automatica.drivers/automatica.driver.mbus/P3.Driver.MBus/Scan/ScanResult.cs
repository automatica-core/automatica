using P3.Driver.MBus.Frames;

namespace P3.Driver.MBus.Scan
{
    public class ScanResult
    {
        public int DeviceId { get; }
        public MBusFrame Frame { get; }
        public ScanResult(int deviceId, MBusFrame frame)
        {
            DeviceId = deviceId;
            Frame = frame;
        }
    }
}
