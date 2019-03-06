using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace P3.Knx.Core.Baos.Driver
{
    public class BaosDriver
    {
        private readonly string _port;
        private readonly BaosSerial _serial;

        public BaosDriver(string port, ILogger logger)
        {
            _port = port;
            _serial = new BaosSerial(_port, logger);
        }


        public Task<bool> Start()
        {
            return Task.FromResult(_serial.Open());
        }
        public Task<bool> Stop()
        {
            return Task.FromResult(_serial.Close());
        }


    }
}
