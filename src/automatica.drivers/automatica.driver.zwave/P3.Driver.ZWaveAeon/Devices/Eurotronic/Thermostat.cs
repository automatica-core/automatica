using System;
using System.Threading.Tasks;
using P3.Driver.ZWaveAeon.CommandClasses;

namespace P3.Driver.ZWaveAeon.Devices.Eurotronic
{
    public class Thermostat : Device
    {

        public event EventHandler<MeasureEventArgs> TemperatureMeasured;

        public Thermostat(Node node)
            : base(node)
        {
            node.GetCommandClass<SensorMultiLevel>().Changed += SensorMultiLevel_Changed;
        }

        private void SensorMultiLevel_Changed(object sender, ReportEventArgs<SensorMultiLevelReport> e)
        {
            if (e.Report.Type == SensorType.Temperature)
            {
                OnTemperatureMeasured(new MeasureEventArgs(new Measure(e.Report.Value, Unit.Celsius)));
            }
        }

        public Task SetTemperature(double temperature)
        {
            throw new NotImplementedException();
        }

        protected virtual void OnTemperatureMeasured(MeasureEventArgs e)
        {
            TemperatureMeasured?.Invoke(this, e);
        }

    }
}
