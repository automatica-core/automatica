using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using P3.Driver.Loxone.Miniserver.Driver.Data.Message;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace P3.Driver.Loxone.Miniserver.DriverFactory
{
    public class LoxoneDriverNode : DriverBase
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
                DispatchValue(value);
            }
        }

        public override async Task WriteValue(IDispatchable source, object value)
        {
            await driver.WriteValue(_uuid, value);
        }

        public override bool Init()
        {
         
            return base.Init();
        }

        public override Task<bool> Start()
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
            return base.Start();
        }
        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
