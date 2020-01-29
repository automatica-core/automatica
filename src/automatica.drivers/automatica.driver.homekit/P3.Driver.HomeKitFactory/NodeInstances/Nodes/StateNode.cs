using System;
using Automatica.Core.Driver;
using P3.Driver.HomeKit.Hap.Model;

namespace P3.Driver.HomeKitFactory.NodeInstances.Nodes
{
    internal class StateNode : BaseNode
    {
        public StateNode(IDriverContext driverContext, Characteristic characteristic, HomeKitDriver driver) : base(driverContext, characteristic, driver)
        {
        }

        protected override object ConvertValue(object value)
        {
            return Convert.ToBoolean(value);
        }
    }
}
