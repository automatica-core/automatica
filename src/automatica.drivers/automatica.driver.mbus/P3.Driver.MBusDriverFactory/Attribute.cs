using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using P3.Driver.MBus.Frames.VariableData;

namespace P3.Driver.MBusDriverFactory
{
    public class Attribute : DriverNotWriteableBase
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

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            return Parent.Read(token);
        }


        public void SetValue(object value)
        {
            Value = value;

            DispatchRead(value);
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
