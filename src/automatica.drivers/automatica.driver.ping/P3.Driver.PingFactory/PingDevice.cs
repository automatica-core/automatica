using System;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using Timer = System.Threading.Timer;

namespace P3.Driver.PingFactory
{
    public class PingDevice : DriverBase
    {
        private string _ip;
        private int _interval;
        private int _timeout;
        private int _minSuccess;

        private Timer _timer;

        private readonly object _lock = new object();
        private readonly ReadOnlyMemory<byte> _buffer;
        

        public bool Value { get; private set; }
        public int CurrentSuccess { get; private set; }

        public PingDevice(IDriverContext driverContext) : base(driverContext)
        {

            string data = "ping-pong";
            _buffer = Encoding.ASCII.GetBytes(data);
        }

        public override bool Init()
        {
            _ip = GetPropertyValueString("ip");
            _interval = GetPropertyValueInt("interval");
            _timeout = GetPropertyValueInt("timeout");
            _minSuccess = GetPropertyValueInt("min_success");

            DriverContext.Logger.LogInformation($"Ping device {_ip} every {_interval}s");

            if (_interval == 0)
            {
                DoPing();
            }

            return base.Init();
        }

        public override Task<bool> Start()
        {
            if (_interval >= 0)
            {
                var interval = Convert.ToInt32(TimeSpan.FromSeconds(_interval).TotalMilliseconds);

                _timer = new Timer(state =>
                {
                    lock (_lock)
                    {
                        DoPing();
                    }


                }, null, interval, interval);
            }

            return base.Start();
        }

        private void DoPing()
        {
            var pingSender = new Ping();
            var options = new PingOptions
            {
                DontFragment = true
            };

            try
            {
                PingReply reply = pingSender.Send(_ip, _timeout, _buffer.ToArray(), options);

                DriverContext.Logger.LogDebug($"Ping reply for {_ip} is {reply.Status}");

                if (reply?.Status == IPStatus.Success)
                {
                    Increment();
                }
                else
                {
                    Decrement();
                }
            }
            catch (Exception)
            {
                DriverContext.Logger.LogDebug($"Ping reply for {_ip} failed");
                Decrement();
            }
        }

        private void Decrement()
        {
            if (CurrentSuccess <= 0)
            {
                CurrentSuccess = 0;
                Value = false;
                DispatchValue(false);
            }
            else
            {
                CurrentSuccess--;
                DriverContext.Logger.LogDebug($"Decrement count to {CurrentSuccess}");
            }

        }

        private void Increment()
        {
            if (CurrentSuccess >= _minSuccess)
            {
                Value = true;
                DispatchValue(true);
            }
            else
            {
                CurrentSuccess++;
                DriverContext.Logger.LogDebug($"Increment count to {CurrentSuccess}");
            }
        }

        public override Task<bool> Stop()
        {
            _timer?.Dispose();
            return base.Stop();
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
