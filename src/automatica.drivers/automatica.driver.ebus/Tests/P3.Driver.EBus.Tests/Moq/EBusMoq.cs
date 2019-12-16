using System.Threading.Tasks;
using P3.Driver.EBus.Config;

namespace P3.Driver.EBus.Tests.Moq
{
    class EBusMoq : EBus
    {
        public EBusMoq(IEBusConfig config) : base(config)
        {

        }

        public byte[] Write(byte[] data)
        {
            foreach (var b in data)
            {
                var msg = Add(b);


                if (msg != null)
                {
                    return msg;
                }
            }

            return null;
        }

        public override Task<bool> Connect()
        {
            return Task.FromResult(true);
        }

        public override Task<bool> Disconnect()
        {
            return Task.FromResult(true);
        }
    }
}
