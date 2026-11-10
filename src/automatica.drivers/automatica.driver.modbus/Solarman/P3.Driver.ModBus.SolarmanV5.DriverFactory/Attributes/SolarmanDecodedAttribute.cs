using System;
using System.Threading.Tasks;
using Automatica.Core.Driver;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory.Attributes
{
    /// <summary>
    /// A register that is an enum or a bit field, the register map supplies the conversion to the node value.
    /// </summary>
    internal class SolarmanDecodedAttribute : SolarmanAttrribute
    {
        private readonly Func<ushort[], object> _decoder;

        public SolarmanDecodedAttribute(IDriverContext driverContext, SolarmanGroupAttribute parent, Func<ushort[], object> decoder) : base(driverContext, parent)
        {
            _decoder = decoder;
        }

        public override Task<object> ConvertValue(ushort[] data)
        {
            return Task.FromResult(_decoder(data));
        }
    }
}
