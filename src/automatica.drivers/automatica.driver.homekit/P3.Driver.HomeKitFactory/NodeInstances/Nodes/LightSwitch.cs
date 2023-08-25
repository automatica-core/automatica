using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using P3.Driver.HomeKit.Hap.Model;

namespace P3.Driver.HomeKitFactory.NodeInstances.Nodes
{
    internal class LightSwitch : BaseNode
    {
        public LightSwitch(IDriverContext driverContext, HomeKitDriver driver, Characteristic characteristic) : base(driverContext, characteristic, driver)
        {

        }

        protected override object ConvertValue(object value)
        {
            return Convert.ToInt32(value);
        }
    }
}
