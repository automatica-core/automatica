using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;


namespace P3.Driver.HomeKit.Bonjour
{
    internal class BonjourService
    {
        private readonly ILogger _logger;
        private readonly int _port;
        private readonly string _name;
        private MdnsService _mdns;
        private CancellationTokenSource _cts;

        public BonjourService(ILogger logger, int port, string name)
        {
            _logger = logger;
            _port = port;
            _name = name;
        }

      
        public Task<bool> Start()
        {
            try
            {
               

                _cts = new CancellationTokenSource();
                _mdns = new MdnsService(_logger, _port, _name);
                Task.Run(() =>
                {
                    try
                    {
                        _mdns.Start();
                    }
                    catch (OperationCanceledException)
                    {
                        _mdns.Stop();
                    }
                }, _cts.Token);
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.Message);
            }

            return Task.FromResult(true);
        }

        public Task<bool> Stop()
        {
            _cts.Cancel();
            return Task.FromResult(true);
        }
    }
}
