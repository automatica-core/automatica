using System;
using Automatica.Core.Driver;
using P3.Driver.HomeKit.Hap.Model;

namespace P3.Driver.HomeKitFactory.NodeInstances.Nodes
{
    internal class SwitchNode : BaseNode
    {
        public SwitchNode(IDriverContext driverContext, Characteristic characteristic, HomeKitDriver driver) : base(driverContext, characteristic, driver)
        {
        }

        internal override void SetValue(object value)
        {
            base.SetValue(Convert.ToBoolean(value));
        }

        protected override object ConvertValue(object value)
        {
            return Convert.ToInt32(value);
        }
    }
}
