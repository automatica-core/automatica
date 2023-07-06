using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using P3.Driver.MBus.Frames.VariableData;

namespace P3.Driver.MBusDriverFactory
{
    public class Attribute : DriverBase
    {
        public string Address => $"{(int)UnitType}-{StorageNumber}-{Tariff}";

        public object Value { get; set; }

        public Attribute(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            StorageNumber = GetProperty("mbus-storageNumber").ValueInt.Value;
            Tariff = GetProperty("mbus-tariff").ValueInt.Value;
            UnitType = (Unit)GetProperty("mbus-type").ValueInt.Value;

            return base.Init(token);
        }

        public override Task<bool> Read(CancellationToken token = default)
        {
            return Parent.Read(token);
        }

        public void SetValue(object value)
        {
            Value = value;

            DispatchValue(value);
        }

        public Unit UnitType { get; private set; }

        public int Tariff { get; private set; }

        public int StorageNumber { get; private set; }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
