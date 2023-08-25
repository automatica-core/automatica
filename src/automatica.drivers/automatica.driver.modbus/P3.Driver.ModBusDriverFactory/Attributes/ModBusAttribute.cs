using Automatica.Core.Driver;
using P3.Driver.ModBusDriver;

namespace P3.Driver.ModBusDriverFactory.Attributes
{
    public abstract class ModBusAttribute
    {
        public ModBusTable Table { get; }
        public ushort Register { get; }

        public double Factor { get; set; }
        public double Offset { get; set; }

        protected ModBusAttribute(IDriverContext driverContext)
        {
            Table = (ModBusTable)driverContext.NodeInstance.GetProperty("modbus-table").ValueInt.Value;
            Register = (ushort)driverContext.NodeInstance.GetProperty("modbus-register").ValueInt.Value;

            var fa = driverContext.NodeInstance.GetPropertyValue("factor", 1d);
            Factor = (double) fa;
            
            var off = driverContext.NodeInstance.GetPropertyValue("offset", 0d);
            Offset = (double) off;
            DriverContext = driverContext;
        }

        public abstract int RegisterLength { get; }
        public IDriverContext DriverContext { get; }

        public abstract ushort[] ConvertValueToBus(object value, out object convertedValue);

        public abstract object ConvertValueFromBus(ushort[] value);
    }
}
