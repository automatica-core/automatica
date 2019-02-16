using System.IO;

namespace P3.Driver.ZWaveAeon.Channel
{
    public interface ISerialPort
    {
        Stream InputStream { get; }
        Stream OutputStream { get; }

        void Close();
        void Open();
    }
}