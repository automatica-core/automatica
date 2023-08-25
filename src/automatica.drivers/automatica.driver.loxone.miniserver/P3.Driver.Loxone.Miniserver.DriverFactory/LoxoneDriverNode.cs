using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.Loxone.Miniserver.DriverFactory
{
    public class LoxoneDriverNode : DriverNoneAttributeBase
    {
        private readonly LoxoneDriver driver;
        private double? _value;
        private string _uuid;

        public LoxoneDriverNode(IDriverContext driverContext, LoxoneDriver driver) : base(driverContext)
        {
            this.driver = driver;
        }

        public void ValueChanged(double value)
        {
            if (_value != value)
            {
                _value = value;
                DispatchRead(value);
            }
        }


        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            await driver.WriteValue(_uuid, value);
            await writeContext.DispatchValue(value, token);
        }

        public override Task<bool> Start(CancellationToken token = default)
        {
            _uuid = GetPropertyValueString("uuid");
            var state = GetPropertyValueString("state");

            if (driver.LoxData.Controls.ContainsKey(_uuid) && driver.LoxData.Controls[_uuid].States.ContainsKey(state))
            {
                var stateUuid = driver.LoxData.Controls[_uuid].States[state];

                if (!driver.Nodes.ContainsKey(stateUuid.ToString().ToLower()))
                {
                    driver.Nodes.Add(stateUuid.ToString().ToLower(), new List<LoxoneDriverNode>());
                }
                driver.Nodes[stateUuid.ToString().ToLower()].Add(this);
            }
            else
            {
                return Task.FromResult(false);
            }
            return base.Start(token);
        }
        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
