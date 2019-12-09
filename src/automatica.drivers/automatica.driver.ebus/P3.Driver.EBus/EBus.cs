using System;
using System.Threading.Tasks;
using P3.Driver.EBus.Config;

namespace P3.Driver.EBus
{
    public abstract class EBus : IEBus
    {
        protected EBus(IEBusConfig config)
        {
            
        }

        public abstract Task<bool> Connect();

        public abstract Task<bool> Disconnect();
    }
}
