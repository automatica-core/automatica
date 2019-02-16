using System;
using System.Threading.Tasks;
using P3.Driver.ZWaveAeon.CommandClasses;

namespace P3.Driver.ZWaveAeon.Devices
{
    public class BatteryDevice : Device
    {
        private byte? _controllerID;
        public event EventHandler<WakeUpEventArgs> WakeUp;

        public BatteryDevice(Node node)
            : base(node)
        {
            node.GetCommandClass<WakeUp>().Changed += WakeUp_Changed;

        }

        private async Task<byte> GetControllerID()
        {
            return (_controllerID ?? (_controllerID = await Node.Controller.GetNodeId())).Value;
        }

        private void WakeUp_Changed(object sender, ReportEventArgs<WakeUpReport> e)
        {
            if (e.Report.Awake)
            {
                OnAwaked();
                return;
            }
        }

        private void OnAwaked()
        {
            var eventArgs = new WakeUpEventArgs();
            OnWakeUp(eventArgs);

            Task.Run(async () =>
            {
                var timeout = Task.Delay(TimeSpan.FromSeconds(5));
                await Task.WhenAny(Task.WhenAll(eventArgs.WaitAll()), timeout);
                if (!timeout.IsCompleted)
                {
                    try
                    {
                        await Sleep();
                    }
                    catch
                    {
                        // NOP, device already sleeping
                    }
                }
            });
        }

        protected virtual void OnWakeUp(WakeUpEventArgs e)
        {
            WakeUp?.Invoke(this, e);
        }

        public async Task<TimeSpan> GetWakeUpInterval()
        {
            return (await Node.GetCommandClass<WakeUp>().GetInterval()).Interval;
        }

        public async Task SetWakeUpInterval(TimeSpan value)
        {
            var controllerNodeID = await Node.Controller.GetNodeId();
            await Node.GetCommandClass<WakeUp>().SetInterval(value, controllerNodeID);
        }

        public async Task<Measure> GetBatteryLevel()
        {
            var value = (await Node.GetCommandClass<Battery>().Get()).Value;
            return new Measure(value, Unit.Percentage);
        }

        public async Task Sleep()
        {
            await Node.GetCommandClass<WakeUp>().NoMoreInformation();
        }
    }
}
