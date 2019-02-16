using P3.Knx.Core.Driver.Frames;
using System;
using System.Timers;

namespace P3.Knx.Core.Driver
{
    internal class HeartbeatMonitor
    {
        private readonly KnxConnection connection;
        private readonly Timer _timer;

        private int _heartbeatCount = 0;

        public EventHandler<EventArgs> OnHeartbeatFailure { get; set; }

        public HeartbeatMonitor(KnxConnection connection)
        {
            this.connection = connection;

            _timer = new Timer(50*1000);  
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if(_heartbeatCount >= 3)
            {
                OnHeartbeatFailure?.Invoke(this, EventArgs.Empty);
                Stop();
                return;
            }
            connection.SendFrame(new ConnectionStateRequestFrame(connection));
            _heartbeatCount++;
        }

        public void Start()
        {
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }


        public void Reset()
        {
            _timer.Stop();
            _timer.Start();
            _heartbeatCount = 0;
        }

        public void Stop()
        {
            _timer.Stop();
            _timer.Elapsed -= _timer_Elapsed;
        }
    }
}
