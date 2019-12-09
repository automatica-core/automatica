using System;
using System.Threading.Tasks;
using P3.Driver.EBus.Config;

namespace P3.Driver.EBus.Interfaces
{
    public abstract class EBusIp : EBus
    {
        public IEBusIpConfig Config { get; }
        private bool _isRunning;

        private readonly object _lock = new object();
        private Task<Task> _receiverThread;

        protected EBusIp(IEBusIpConfig config) : base(config)
        {
            Config = config;
        }

        public override Task<bool> Connect()
        {
            _isRunning = true;

            _receiverThread = Task.Factory.StartNew(Run);
            return Task.FromResult(true);
        }

        private async Task Run()
        {
            await StartReceive();
            while (_isRunning)
            {

                var frame = await ReceiveFrame();
                if (!_isRunning)
                {
                    break;
                }
                if (frame == null)
                {
                    //todo log error
                    
                }


            }
            await StopReceive();
        }

        internal async Task<object> ReceiveFrame()
        {
            var data = await Receive();
            lock (_lock)
            {
                if (data == null || data.Length == 0)
                {
                    return null;
                }


                return null;
            }
        }



        protected abstract Task<byte[]> Receive();
        protected abstract Task StartReceive();
        protected abstract Task StopReceive();

        public override Task<bool> Disconnect()
        {
            _isRunning = false;

            return Task.FromResult(true);
        }
    }
}
