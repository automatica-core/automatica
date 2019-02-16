using P3.Driver.MBus.Config;
using P3.Driver.MBus.Frames;

namespace P3.Driver.MBus
{
    public interface IMBusInterface
    {
        bool Open(MBusConfig config);
        bool Close();

        void WriteFrame(MBusFrame frame);
        MBusFrame ReadFrame();

    }
}
